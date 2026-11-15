// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Geometry;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.SimulationPackage.Internal;

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// Streams a compiled model and generated jobs into an immutable <c>.msl</c> SQLite package.
/// </summary>
/// <remarks>
/// The writer builds in <c>&lt;target&gt;.tmp-&lt;pid&gt;-&lt;random&gt;</c> beside the target, uses WAL and
/// batch commits during the build, and publishes through a checkpoint, <c>journal_mode=DELETE</c>, a file
/// hash, a file fsync, a containing-directory fsync on Unix, and an atomic replace. The public payload-encoder
/// methods are completed in Step 11 features 4 and 5; this feature exposes the schema and publication barrier
/// and its contract-typed internal row seam.
/// </remarks>
public sealed class SimulationPackageWriter : ISimulationPackageWriter
{
    private readonly PackageBuildRequest request;
    private readonly PackageBuildOptions options;
    private readonly IPackageFileSystem fileSystem;
    private readonly string targetPath;
    private readonly string temporaryPath;
    private readonly string directory;
    private readonly string appVersion;
    private readonly PayloadEncoder payloadEncoder;
    private readonly HashSet<string> latticeSignatures = new(StringComparer.Ordinal);
    private SqliteConnection? connection;
    private SqliteTransaction? jobTransaction;
    private int pendingJobs;
    private int jobCount;
    private int modelId = -1;
    private string? modelSignature;
    private string? compiledModelSignature;
    private ImmutableArray<Cartesian3> cellVectors;
    private int lastJobId;
    private bool completed;
    private bool disposed;

    private SimulationPackageWriter(
        PackageBuildRequest request,
        PackageBuildOptions options,
        IPackageFileSystem fileSystem,
        string targetPath,
        string temporaryPath,
        SqliteConnection connection,
        PayloadEncoder payloadEncoder)
    {
        this.request = request;
        this.options = options;
        this.fileSystem = fileSystem;
        this.targetPath = targetPath;
        this.temporaryPath = temporaryPath;
        directory = Path.GetDirectoryName(targetPath)!;
        this.connection = connection;
        appVersion = SimulationPackageWriterSupport.ResolveAppVersion();
        this.payloadEncoder = payloadEncoder;
    }

    /// <summary>
    /// Creates a writer for a new package.
    /// </summary>
    /// <param name="request">The build request.</param>
    /// <param name="options">The build options.</param>
    /// <returns>A writer positioned on a fresh temporary database.</returns>
    /// <exception cref="IOException">The target exists and <see cref="PackageBuildOptions.Overwrite"/> is false.</exception>
    public static ISimulationPackageWriter Create(PackageBuildRequest request, PackageBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        return Create(request, options, new PackageFileSystem());
    }

    internal static SimulationPackageWriter Create(
        PackageBuildRequest request,
        PackageBuildOptions options,
        IPackageFileSystem fileSystem)
    {
        SimulationPackageWriterSupport.ValidateRequest(request);
        SimulationPackageWriterSupport.ValidateOptions(options);
        ArgumentNullException.ThrowIfNull(fileSystem);

        var targetPath = Path.GetFullPath(request.TargetPath);
        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new IOException($"The package path '{targetPath}' has no parent directory.");
        }

        fileSystem.EnsureDirectory(directory);
        if (fileSystem.FileExists(targetPath) && !options.Overwrite)
        {
            throw new IOException(
                $"The package '{targetPath}' already exists. Set {nameof(PackageBuildOptions.Overwrite)} to replace it.");
        }

        var temporaryPath = $"{targetPath}.tmp-{Environment.ProcessId}-{Guid.NewGuid():N}";
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = temporaryPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Private,
                Pooling = false,
            }.ToString());

        try
        {
            connection.Open();
            ConfigureDatabase(connection);
            CreateSchema(connection);
        }
        catch
        {
            connection.Dispose();
            SimulationPackageWriterSupport.TryDelete(fileSystem, temporaryPath);
            throw;
        }

        return new SimulationPackageWriter(
            request,
            options,
            fileSystem,
            targetPath,
            temporaryPath,
            connection,
            new PayloadEncoder(options.CompressionLevel));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The map/encode path lands in Step 11 feature 4. Feature 3 covers the schema and publication barrier
    /// through <see cref="WriteModelRowAsync"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">The model payload encoder is not part of this feature yet.</exception>
    public async Task WriteModelAsync(ParameterizedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfUnavailable();

        var payload = ModelPayloadMapper.Map(model);
        var bytes = payloadEncoder.EncodeModel(payload);
        var decoded = PayloadEncoder.DecodeModel(bytes);
        if (!string.Equals(decoded.Signature, model.Signature, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The decoded model payload signature does not match the parameterized model.");
        }

        if (!string.Equals(decoded.ModelSignature, model.ModelSignature, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The decoded model payload does not carry the compiled-model signature.");
        }

        var values = ModelPayloadMapper.ToParameterizationValues(decoded);
        var reencoded = ParameterizationSignatureV1.Compute(decoded.ModelSignature, values);
        if (!string.Equals(reencoded, model.Signature, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The parameterization signature re-encoded from the decoded payload does not match the model.");
        }

        await WriteModelRowAsync(
            new PackageModelRow(
                model.Signature,
                (int)decoded.FormatVersion,
                bytes,
                decoded.SymmetryContractVersion,
                decoded.SpaceGroupDataVersion,
                decoded.SpaceGroupDataSha256),
            cancellationToken).ConfigureAwait(false);
        compiledModelSignature = decoded.ModelSignature;
        cellVectors =
        [
            .. decoded.Structure.CellVectorsAng.Select(static row =>
                new Cartesian3(row[0], row[1], row[2])),
        ];
    }

    /// <inheritdoc />
    /// <remarks>
    /// The map/encode path lands in Step 11 feature 5. Feature 3 covers the schema and publication barrier
    /// through <see cref="WriteJobRowAsync"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">The job payload encoder is not part of this feature yet.</exception>
    public async Task WriteJobAsync(JobPackage job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfUnavailable();
        if (modelId < 0 || modelSignature is null || compiledModelSignature is null || cellVectors.IsDefault)
        {
            throw new InvalidOperationException("Write the model before the first job.");
        }

        var latticePayload = LatticePayloadMapper.Map(job.Lattice);
        var latticeBytes = payloadEncoder.EncodeLattice(latticePayload);
        var latticeProduct = (long)job.Lattice.SizeA * job.Lattice.SizeB * job.Lattice.SizeC * job.Lattice.PositionCount;
        var maxLatticeBytes = Math.Min((long)int.MaxValue, 4L * latticeProduct);
        var decodedLattice = PayloadEncoder.DecodeLattice(latticeBytes, maxLatticeBytes);
        var latticeSignature = LatticeSignatureV1.Compute(compiledModelSignature, latticePayload);
        if (!string.Equals(
                LatticeSignatureV1.Compute(compiledModelSignature, decodedLattice),
                latticeSignature,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The decoded lattice payload does not reproduce its signature.");
        }

        var jobPayload = JobPayloadMapper.Map(job.Descriptor, cellVectors, job.Lattice.DopingInfo);
        var jobBytes = payloadEncoder.EncodeJob(jobPayload);
        var decodedJob = PayloadEncoder.DecodeJob(jobBytes);
        if (decodedJob.FormatVersion != jobPayload.FormatVersion ||
            decodedJob.SimulationIndex != jobPayload.SimulationIndex ||
            decodedJob.Flags != jobPayload.Flags ||
            !string.Equals(decodedJob.DopingInfo, jobPayload.DopingInfo, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The decoded job payload does not reproduce its settings.");
        }

        var row = new PackageJobRow(
            job.Descriptor.JobId,
            modelSignature,
            new PackageLatticeRow(latticeSignature, (int)decodedLattice.FormatVersion, latticeBytes),
            decodedJob.Kind,
            job.Descriptor.CollectionName,
            job.Descriptor.ConfigName,
            job.Descriptor.CollectionIndex,
            job.Descriptor.ConfigIndex,
            job.Descriptor.JobIndex,
            decodedJob.TemperatureK,
            decodedJob.ElectricFieldVm,
            checked((long)decodedJob.TargetMcsp),
            checked((int)decodedJob.WriteOutCount),
            checked((long)decodedJob.PreRunMcsp),
            decodedJob.TimeLimitS is { } timeLimit ? checked((long)timeLimit) : null,
            decodedJob.MinSuccessRate,
            job.Lattice.SizeA,
            job.Lattice.SizeB,
            job.Lattice.SizeC,
            decodedJob.DopingInfo,
            decodedJob.Flags,
            job.Descriptor.Seed.AsMemory(),
            (int)decodedJob.FormatVersion,
            jobBytes);
        await WriteJobRowAsync(row, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PackageWriteResult> CompleteAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfUnavailable();
        if (modelId < 0)
        {
            throw new InvalidOperationException("A package must contain exactly one model before it can be completed.");
        }

        if (jobCount == 0)
        {
            throw new InvalidOperationException("A package must contain at least one job before it can be completed.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            CommitJobBatch();
            ExecuteNonQuery("PRAGMA wal_checkpoint(TRUNCATE);");
            ExecuteScalar("PRAGMA journal_mode = DELETE;");
            ExecuteNonQuery("PRAGMA optimize;");
            if (options.Vacuum)
            {
                ExecuteNonQuery("VACUUM;");
            }

            DisposeConnection();
            cancellationToken.ThrowIfCancellationRequested();

            var sha256 = await ComputeSha256Async(temporaryPath, cancellationToken).ConfigureAwait(false);
            fileSystem.FlushFile(temporaryPath);
            fileSystem.FlushDirectory(directory);
            fileSystem.Move(temporaryPath, targetPath, overwrite: true);

            var sizeBytes = new FileInfo(targetPath).Length;
            var warnings = SimulationPackageWriterSupport.AdvisoryWarnings(sizeBytes, options.AdvisorySizeBytes);
            var result = new PackageWriteResult(
                targetPath,
                sha256,
                jobCount,
                ModelCount: 1,
                latticeSignatures.Count,
                sizeBytes,
                warnings);
            completed = true;
            disposed = true;
            return result;
        }
        catch
        {
            Abort();
            throw;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return ValueTask.CompletedTask;
        }

        disposed = true;
        try
        {
            RollbackJobBatch();
            DisposeConnection();
        }
        finally
        {
            SimulationPackageWriterSupport.TryDelete(fileSystem, temporaryPath);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Writes the model row and the <c>schema_info</c> rows. This is the feature-3 seam for the payload encoder.
    /// </summary>
    /// <param name="model">The contract-typed model row.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the row is committed.</returns>
    internal Task WriteModelRowAsync(PackageModelRow model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfUnavailable();
        ArgumentNullException.ThrowIfNull(model);
        if (modelId >= 0)
        {
            throw new InvalidOperationException("A v1 package contains exactly one model row.");
        }

        SimulationPackageWriterSupport.ValidateModelRow(model);
        using var transaction = GetConnection().BeginTransaction();
        try
        {
            InsertSchemaInfo(transaction, model);
            using (var insert = CreateCommand(transaction))
            {
                insert.CommandText =
                    "INSERT INTO models(signature, format_version, payload) " +
                    "VALUES ($signature, $formatVersion, $payload);";
                insert.Parameters.AddWithValue("$signature", model.Signature);
                insert.Parameters.AddWithValue("$formatVersion", model.FormatVersion);
                insert.Parameters.Add("$payload", SqliteType.Blob).Value = model.Payload.ToArray();
                insert.ExecuteNonQuery();
            }

            using (var lookup = CreateCommand(transaction))
            {
                lookup.CommandText = "SELECT model_id FROM models WHERE signature = $signature;";
                lookup.Parameters.AddWithValue("$signature", model.Signature);
                modelId = (int)(long)lookup.ExecuteScalar()!;
            }

            modelSignature = model.Signature;
            transaction.Commit();
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
                // The transaction was already completed; the writer is still aborted.
            }

            transaction.Dispose();
            Abort();
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes one job row and inserts or reuses its lattice row. This is the feature-3 seam for the payload
    /// encoder.
    /// </summary>
    /// <param name="job">The contract-typed job row.</param>
    /// <param name="cancellationToken">Cancels the operation before the insert.</param>
    /// <returns>A task that completes when the row is inserted or the batch is committed.</returns>
    internal Task WriteJobRowAsync(PackageJobRow job, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfUnavailable();
        ArgumentNullException.ThrowIfNull(job);
        if (modelId < 0 || modelSignature is null)
        {
            throw new InvalidOperationException("Write the model before the first job.");
        }

        if (!string.Equals(job.ModelSignature, modelSignature, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The job references a different model signature.");
        }

        SimulationPackageWriterSupport.ValidateJobRow(job);
        if (job.JobId <= lastJobId)
        {
            throw new InvalidOperationException("Job ids must be strictly ascending in template order.");
        }

        jobTransaction ??= GetConnection().BeginTransaction();
        try
        {
            var latticeId = InsertOrGetLattice(job.Lattice, jobTransaction);
            InsertJob(job, latticeId, jobTransaction);
            lastJobId = job.JobId;
            jobCount++;
            pendingJobs++;
            if (pendingJobs >= options.BatchSize)
            {
                CommitJobBatch();
            }
        }
        catch
        {
            Abort();
            throw;
        }

        return Task.CompletedTask;
    }

    internal static void ConfigureDatabase(SqliteConnection connection)
    {
        Execute(connection, $"PRAGMA page_size = {SimulationPackageSchema.PageSize};");
        ExecuteScalar(connection, "PRAGMA journal_mode = WAL;");
        Execute(connection, "PRAGMA synchronous = NORMAL;");
        Execute(connection, "PRAGMA foreign_keys = ON;");
        Execute(connection, "PRAGMA temp_store = MEMORY;");
        Execute(connection, $"PRAGMA application_id = {SimulationPackageSchema.ApplicationId};");
        Execute(connection, $"PRAGMA user_version = {SimulationPackageSchema.FormatVersion};");
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = SimulationPackageSchema.CreateSql;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private void InsertSchemaInfo(SqliteTransaction transaction, PackageModelRow model)
    {
        var rows = new (string Key, string Value)[]
        {
            ("format_version", SimulationPackageSchema.FormatVersion.ToString(CultureInfo.InvariantCulture)),
            ("app_version", appVersion),
            ("created_utc", SimulationPackageWriterSupport.FormatCreatedUtc(request.CreatedUtc)),
            ("project_id", request.ProjectId.ToString("D", CultureInfo.InvariantCulture)),
            ("generator", $"mocassin .NET {appVersion}"),
            ("job_selection", request.JobSelection),
            ("symmetry_contract_version", model.SymmetryContractVersion),
            ("space_group_data_version", model.SpaceGroupDataVersion),
            ("space_group_data_sha256", model.SpaceGroupDataSha256),
        };

        using var command = CreateCommand(transaction);
        command.CommandText = "INSERT INTO schema_info(key, value) VALUES ($key, $value);";
        var key = command.Parameters.Add("$key", SqliteType.Text);
        var value = command.Parameters.Add("$value", SqliteType.Text);
        foreach (var row in rows)
        {
            key.Value = row.Key;
            value.Value = row.Value;
            command.ExecuteNonQuery();
        }
    }

    private long InsertOrGetLattice(PackageLatticeRow lattice, SqliteTransaction transaction)
    {
        using (var insert = CreateCommand(transaction))
        {
            insert.CommandText =
                "INSERT OR IGNORE INTO lattices(signature, format_version, payload) " +
                "VALUES ($signature, $formatVersion, $payload);";
            insert.Parameters.AddWithValue("$signature", lattice.Signature);
            insert.Parameters.AddWithValue("$formatVersion", lattice.FormatVersion);
            insert.Parameters.Add("$payload", SqliteType.Blob).Value = lattice.Payload.ToArray();
            if (insert.ExecuteNonQuery() > 0)
            {
                latticeSignatures.Add(lattice.Signature);
            }
        }

        using var lookup = CreateCommand(transaction);
        lookup.CommandText = "SELECT lattice_id FROM lattices WHERE signature = $signature;";
        lookup.Parameters.AddWithValue("$signature", lattice.Signature);
        return (long)lookup.ExecuteScalar()!;
    }

    private void InsertJob(PackageJobRow job, long latticeId, SqliteTransaction transaction)
    {
        using var command = CreateCommand(transaction);
        command.CommandText = """
            INSERT INTO jobs (
                job_id, model_id, lattice_id, kind, collection_name, config_name,
                collection_index, config_index, job_index, temperature_k, electric_field_vm,
                target_mcsp, write_out_count, pre_run_mcsp, time_limit_s, min_success_rate,
                lattice_a, lattice_b, lattice_c, doping_info, flags, seed, payload, created_utc)
            VALUES (
                $jobId, $modelId, $latticeId, $kind, $collectionName, $configName,
                $collectionIndex, $configIndex, $jobIndex, $temperatureK, $electricFieldVm,
                $targetMcsp, $writeOutCount, $preRunMcsp, $timeLimitS, $minSuccessRate,
                $latticeA, $latticeB, $latticeC, $dopingInfo, $flags, $seed, $payload, $createdUtc);
            """;
        command.Parameters.AddWithValue("$jobId", job.JobId);
        command.Parameters.AddWithValue("$modelId", modelId);
        command.Parameters.AddWithValue("$latticeId", latticeId);
        command.Parameters.AddWithValue("$kind", job.Kind);
        command.Parameters.AddWithValue("$collectionName", job.CollectionName);
        command.Parameters.AddWithValue("$configName", job.ConfigName);
        command.Parameters.AddWithValue("$collectionIndex", job.CollectionIndex);
        command.Parameters.AddWithValue("$configIndex", job.ConfigIndex);
        command.Parameters.AddWithValue("$jobIndex", job.JobIndex);
        command.Parameters.AddWithValue("$temperatureK", job.TemperatureK);
        command.Parameters.AddWithValue("$electricFieldVm", job.ElectricFieldVm);
        command.Parameters.AddWithValue("$targetMcsp", job.TargetMcsp);
        command.Parameters.AddWithValue("$writeOutCount", job.WriteOutCount);
        command.Parameters.AddWithValue("$preRunMcsp", job.PreRunMcsp);
        command.Parameters.AddWithValue("$timeLimitS", (object?)job.TimeLimitSeconds ?? DBNull.Value);
        command.Parameters.AddWithValue("$minSuccessRate", (object?)job.MinSuccessRate ?? DBNull.Value);
        command.Parameters.AddWithValue("$latticeA", job.LatticeA);
        command.Parameters.AddWithValue("$latticeB", job.LatticeB);
        command.Parameters.AddWithValue("$latticeC", job.LatticeC);
        command.Parameters.AddWithValue("$dopingInfo", job.DopingInfo);
        command.Parameters.AddWithValue("$flags", (long)job.Flags);
        command.Parameters.Add("$seed", SqliteType.Blob).Value = job.Seed.ToArray();
        command.Parameters.Add("$payload", SqliteType.Blob).Value = job.Payload.ToArray();
        command.Parameters.AddWithValue("$createdUtc", SimulationPackageWriterSupport.FormatCreatedUtc(request.CreatedUtc));
        command.ExecuteNonQuery();
    }

    private SqliteCommand CreateCommand(SqliteTransaction? transaction)
    {
        var command = GetConnection().CreateCommand();
        command.Transaction = transaction;
        return command;
    }

    private SqliteConnection GetConnection() =>
        connection ?? throw new ObjectDisposedException(nameof(SimulationPackageWriter));

    private void CommitJobBatch()
    {
        if (jobTransaction is null)
        {
            return;
        }

        jobTransaction.Commit();
        jobTransaction.Dispose();
        jobTransaction = null;
        pendingJobs = 0;
    }

    private void RollbackJobBatch()
    {
        if (jobTransaction is null)
        {
            return;
        }

        try
        {
            jobTransaction.Rollback();
        }
        catch (InvalidOperationException)
        {
            // The transaction was already completed; cleanup continues.
        }
        finally
        {
            jobTransaction.Dispose();
            jobTransaction = null;
            pendingJobs = 0;
        }
    }

    private void DisposeConnection()
    {
        connection?.Dispose();
        connection = null;
    }

    private void ExecuteNonQuery(string sql)
    {
        using var command = CreateCommand(transaction: null);
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private string ExecuteScalar(string sql)
    {
        using var command = CreateCommand(transaction: null);
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = fileSystem.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string ExecuteScalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void Abort()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        RollbackJobBatch();
        DisposeConnection();
        SimulationPackageWriterSupport.TryDelete(fileSystem, temporaryPath);
    }

    private void ThrowIfUnavailable()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (completed)
        {
            throw new InvalidOperationException("The package has already been completed.");
        }
    }
}

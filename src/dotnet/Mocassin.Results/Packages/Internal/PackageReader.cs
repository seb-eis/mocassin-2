// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Mocassin.Contracts;
using Mocassin.Contracts.Packages;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Reads a published <c>.msl</c> package read-only and decodes its payloads within explicit bounds.
/// </summary>
internal static class PackageReader
{
    private static readonly string[] requiredTables = ["jobs", "lattices", "models", "schema_info"];
    private static readonly string[] requiredIndices = ["ix_jobs_collection", "ix_jobs_temperature"];
    private static readonly string[] schemaInfoKeys =
    [
        "format_version",
        "app_version",
        "created_utc",
        "project_id",
        "generator",
        "job_selection",
        "symmetry_contract_version",
        "space_group_data_version",
        "space_group_data_sha256",
    ];

    /// <summary>Reads and decodes a package.</summary>
    public static async Task<PackageReadResult> ReadAsync(
        string path,
        PackageVerificationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        if (!File.Exists(path))
        {
            return PackageReadResult.Fatal(PackageDiagnosticCodes.UnreadableDatabase, $"The package '{path}' does not exist.");
        }

        var sizeBytes = new FileInfo(path).Length;
        var hasWalSidecar = File.Exists(path + "-wal") || File.Exists(path + "-shm");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException exception)
        {
            return PackageReadResult.Fatal(
                PackageDiagnosticCodes.UnreadableDatabase,
                $"The package is not a readable SQLite database: {exception.Message}");
        }

        var schemaReadable = false;
        try
        {
            await ExecuteNonQueryAsync(connection, "PRAGMA query_only = ON;", cancellationToken).ConfigureAwait(false);
            await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken).ConfigureAwait(false);
            var integrity = await ExecuteScalarAsync(connection, "PRAGMA integrity_check;", cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(Convert.ToString(integrity, CultureInfo.InvariantCulture), "ok", StringComparison.OrdinalIgnoreCase))
            {
                return PackageReadResult.Fatal(
                    PackageDiagnosticCodes.UnreadableDatabase,
                    $"PRAGMA integrity_check did not return ok: {integrity}");
            }

            schemaReadable = true;

            var applicationId = Convert.ToInt64(
                await ExecuteScalarAsync(connection, "PRAGMA application_id;", cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            var userVersion = Convert.ToInt64(
                await ExecuteScalarAsync(connection, "PRAGMA user_version;", cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture);
            var journalMode = Convert.ToString(
                await ExecuteScalarAsync(connection, "PRAGMA journal_mode;", cancellationToken).ConfigureAwait(false),
                CultureInfo.InvariantCulture) ?? string.Empty;

            var tables = await QueryStringsAsync(
                connection,
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;",
                cancellationToken).ConfigureAwait(false);
            var missingTables = requiredTables.Except(tables, StringComparer.Ordinal).ToArray();
            if (missingTables.Length > 0)
            {
                return PackageReadResult.Fatal(
                    PackageDiagnosticCodes.MissingSchemaElement,
                    $"Missing required table(s): {string.Join(", ", missingTables)}.");
            }

            var hasResultsTable = tables.Contains("results", StringComparer.Ordinal);
            var indices = await QueryStringsAsync(
                connection,
                "SELECT name FROM sqlite_master WHERE type = 'index' AND name NOT LIKE 'sqlite_autoindex%' ORDER BY name;",
                cancellationToken).ConfigureAwait(false);
            var missingIndices = requiredIndices.Except(indices, StringComparer.Ordinal).ToArray();
            if (missingIndices.Length > 0)
            {
                return PackageReadResult.Fatal(
                    PackageDiagnosticCodes.MissingSchemaElement,
                    $"Missing required index(es): {string.Join(", ", missingIndices)}.");
            }

            var missingColumns = await FindMissingColumnsAsync(connection, cancellationToken).ConfigureAwait(false);
            if (missingColumns.Length > 0)
            {
                return PackageReadResult.Fatal(
                    PackageDiagnosticCodes.MissingSchemaElement,
                    $"Missing required column(s): {string.Join(", ", missingColumns)}.");
            }

            var schema = await ReadSchemaInfoAsync(connection, cancellationToken).ConfigureAwait(false);
            if (schema.ErrorCode is not null)
            {
                return PackageReadResult.Fatal(schema.ErrorCode, schema.ErrorMessage!);
            }

            if (schema.Info is null)
            {
                return PackageReadResult.Fatal(PackageDiagnosticCodes.MissingSchemaElement, "schema_info is empty.");
            }

            if (schema.Info.FormatVersion != ContractVersions.PackageFormatVersion ||
                applicationId != PackageFormatConstants.ApplicationId ||
                userVersion != ContractVersions.PackageFormatVersion)
            {
                return PackageReadResult.Fatal(
                    PackageDiagnosticCodes.UnsupportedSchema,
                    "application_id, user_version, or schema_info.format_version is missing or unsupported.");
            }

            var models = await ReadModelsAsync(connection, options, cancellationToken).ConfigureAwait(false);
            var positionCounts = ReadModelPositionCounts(models);
            var lattices = await ReadLatticesAsync(connection, options, positionCounts, cancellationToken)
                .ConfigureAwait(false);
            var jobs = await ReadJobsAsync(connection, options, cancellationToken).ConfigureAwait(false);
            return new PackageReadResult(
                schema.Info,
                models,
                lattices,
                jobs,
                sizeBytes,
                hasResultsTable,
                hasWalSidecar,
                !string.Equals(journalMode, "delete", StringComparison.OrdinalIgnoreCase),
                null,
                null);
        }
        catch (SqliteException exception)
        {
            return schemaReadable
                ? PackageReadResult.Fatal(
                    PackageDiagnosticCodes.MissingSchemaElement,
                    $"The package schema is malformed: {exception.Message}")
                : PackageReadResult.Fatal(
                    PackageDiagnosticCodes.UnreadableDatabase,
                    $"The package is not a readable SQLite database: {exception.Message}");
        }
    }

    private static async Task<(PackageSchemaInfo? Info, string? ErrorCode, string? ErrorMessage)> ReadSchemaInfoAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT key, value FROM schema_info;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                values[reader.GetString(0)] = reader.GetString(1);
            }
        }

        var missing = schemaInfoKeys.Where(key => !values.ContainsKey(key)).ToArray();
        if (missing.Length > 0)
        {
            return (null, PackageDiagnosticCodes.MissingSchemaElement, $"Missing schema_info key(s): {string.Join(", ", missing)}.");
        }

        if (!int.TryParse(values["format_version"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var formatVersion))
        {
            return (null, PackageDiagnosticCodes.UnsupportedSchema, "schema_info.format_version is not an integer.");
        }

        if (!Guid.TryParse(values["project_id"], out var projectId))
        {
            return (null, PackageDiagnosticCodes.MissingSchemaElement, "schema_info.project_id is not a GUID.");
        }

        if (!DateTimeOffset.TryParse(
                values["created_utc"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var createdUtc))
        {
            return (null, PackageDiagnosticCodes.MissingSchemaElement, "schema_info.created_utc is not a timestamp.");
        }

        if (string.IsNullOrWhiteSpace(values["app_version"]) ||
            string.IsNullOrWhiteSpace(values["generator"]) ||
            string.IsNullOrWhiteSpace(values["job_selection"]) ||
            string.IsNullOrWhiteSpace(values["symmetry_contract_version"]) ||
            string.IsNullOrWhiteSpace(values["space_group_data_version"]) ||
            string.IsNullOrWhiteSpace(values["space_group_data_sha256"]))
        {
            return (null, PackageDiagnosticCodes.MissingSchemaElement, "schema_info contains an empty required value.");
        }

        return (new PackageSchemaInfo(
            formatVersion,
            values["app_version"],
            createdUtc,
            projectId,
            values["generator"],
            values["job_selection"],
            values["symmetry_contract_version"],
            values["space_group_data_version"],
            values["space_group_data_sha256"]), null, null);
    }

    private static async Task<ImmutableArray<PackageModelRecord>> ReadModelsAsync(
        SqliteConnection connection,
        PackageVerificationOptions options,
        CancellationToken cancellationToken)
    {
        var models = ImmutableArray.CreateBuilder<PackageModelRecord>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT model_id, signature, format_version, payload FROM models ORDER BY model_id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var payload = (byte[])reader[3];
            var decoded = PackagePayloadDecoder.TryDecodeModel(
                payload,
                options.MaxModelPayloadBytes,
                options.MaxClusterExpansionBytes,
                out var error);
            models.Add(new PackageModelRecord(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                payload,
                decoded,
                error));
        }

        return models.ToImmutable();
    }

    private static Dictionary<long, long> ReadModelPositionCounts(ImmutableArray<PackageModelRecord> models)
    {
        var counts = new Dictionary<long, long>();
        foreach (var model in models)
        {
            if (model.Decoded is { } payload && payload.Structure.PositionCount > 0)
            {
                counts[model.ModelId] = payload.Structure.PositionCount;
            }
        }

        return counts;
    }

    /// <summary>
    /// Reads each lattice within a bound derived from its referencing job and model.
    /// </summary>
    /// <remarks>
    /// A referenced lattice is bounded by <c>4 × a × b × c × position_count</c> from the job's
    /// <c>lattice_a/b/c</c> columns and the referenced model payload's <c>position_count</c>, capped by
    /// <see cref="PackageVerificationOptions.MaxLatticePayloadBytes"/>. An orphan lattice row (no referencing
    /// job), or one whose model payload did not decode, uses that cap as its fallback, so an unreferenced or
    /// malformed row can never force an unbounded allocation.
    /// </remarks>
    private static async Task<ImmutableArray<PackageLatticeRecord>> ReadLatticesAsync(
        SqliteConnection connection,
        PackageVerificationOptions options,
        IReadOnlyDictionary<long, long> modelPositionCounts,
        CancellationToken cancellationToken)
    {
        var bounds = await ReadLatticeBoundsAsync(connection, options, modelPositionCounts, cancellationToken)
            .ConfigureAwait(false);
        var lattices = ImmutableArray.CreateBuilder<PackageLatticeRecord>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT lattice_id, signature, format_version, payload FROM lattices ORDER BY lattice_id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var latticeId = reader.GetInt64(0);
            var payload = (byte[])reader[3];
            var maxDecompressedBytes = bounds.TryGetValue(latticeId, out var bound)
                ? bound
                : options.MaxLatticePayloadBytes;
            var decoded = PackagePayloadDecoder.TryDecode<LatticePayload>(
                payload,
                maxDecompressedBytes,
                out var error);
            lattices.Add(new PackageLatticeRecord(
                latticeId,
                reader.GetString(1),
                reader.GetInt32(2),
                payload,
                decoded,
                error));
        }

        return lattices.ToImmutable();
    }

    /// <summary>
    /// Computes the per-lattice bound from every referencing job; the tightest job wins.
    /// </summary>
    private static async Task<Dictionary<long, long>> ReadLatticeBoundsAsync(
        SqliteConnection connection,
        PackageVerificationOptions options,
        IReadOnlyDictionary<long, long> modelPositionCounts,
        CancellationToken cancellationToken)
    {
        var bounds = new Dictionary<long, long>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT lattice_id, model_id, lattice_a, lattice_b, lattice_c FROM jobs;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var latticeId = reader.GetInt64(0);
            var modelId = reader.GetInt64(1);
            if (!modelPositionCounts.TryGetValue(modelId, out var positionCount))
            {
                continue;
            }

            var bound = LatticePayloadBound.Compute(
                reader.GetInt64(2),
                reader.GetInt64(3),
                reader.GetInt64(4),
                positionCount,
                options.MaxLatticePayloadBytes);
            if (!bounds.TryGetValue(latticeId, out var existing) || bound < existing)
            {
                bounds[latticeId] = bound;
            }
        }

        return bounds;
    }

    private static async Task<ImmutableArray<PackageJobRecord>> ReadJobsAsync(
        SqliteConnection connection,
        PackageVerificationOptions options,
        CancellationToken cancellationToken)
    {
        var jobs = ImmutableArray.CreateBuilder<PackageJobRecord>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT job_id, model_id, lattice_id, kind, collection_name, config_name,
                   collection_index, config_index, job_index, temperature_k, electric_field_vm,
                   target_mcsp, write_out_count, pre_run_mcsp, time_limit_s, min_success_rate,
                   lattice_a, lattice_b, lattice_c, doping_info, flags, seed, payload, created_utc
            FROM jobs ORDER BY job_id;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var payload = (byte[])reader[22];
            var decoded = PackagePayloadDecoder.TryDecode<JobPayload>(
                payload,
                options.MaxJobPayloadBytes,
                out var error);
            jobs.Add(new PackageJobRecord(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetInt64(6),
                reader.GetInt64(7),
                reader.GetInt64(8),
                reader.GetDouble(9),
                reader.GetDouble(10),
                reader.GetInt64(11),
                reader.GetInt32(12),
                reader.GetInt64(13),
                reader.IsDBNull(14) ? null : reader.GetInt64(14),
                reader.IsDBNull(15) ? null : reader.GetDouble(15),
                reader.GetInt32(16),
                reader.GetInt32(17),
                reader.GetInt32(18),
                reader.GetString(19),
                reader.GetInt64(20),
                (byte[])reader[21],
                payload,
                reader.GetString(23),
                decoded,
                error));
        }

        return jobs.ToImmutable();
    }

    private static async Task<ImmutableArray<string>> FindMissingColumnsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var missing = ImmutableArray.CreateBuilder<string>();
        await CheckColumnsAsync("schema_info", ["key", "value"], missing, connection, cancellationToken).ConfigureAwait(false);
        await CheckColumnsAsync("models", ["model_id", "signature", "format_version", "payload"], missing, connection, cancellationToken).ConfigureAwait(false);
        await CheckColumnsAsync("lattices", ["lattice_id", "signature", "format_version", "payload"], missing, connection, cancellationToken).ConfigureAwait(false);
        await CheckColumnsAsync(
            "jobs",
            [
                "job_id", "model_id", "lattice_id", "kind", "collection_name", "config_name",
                "collection_index", "config_index", "job_index", "temperature_k", "electric_field_vm",
                "target_mcsp", "write_out_count", "pre_run_mcsp", "time_limit_s", "min_success_rate",
                "lattice_a", "lattice_b", "lattice_c", "doping_info", "flags", "seed", "payload", "created_utc",
            ],
            missing,
            connection,
            cancellationToken).ConfigureAwait(false);
        return missing.ToImmutable();
    }

    private static async Task CheckColumnsAsync(
        string table,
        IReadOnlyList<string> required,
        ImmutableArray<string>.Builder missing,
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var columns = await QueryStringsAsync(
            connection,
            $"SELECT name FROM pragma_table_info('{table}');",
            cancellationToken).ConfigureAwait(false);
        foreach (var column in required)
        {
            if (!columns.Contains(column, StringComparer.Ordinal))
            {
                missing.Add($"{table}.{column}");
            }
        }
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<string>> QueryStringsAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var values = new List<string>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}

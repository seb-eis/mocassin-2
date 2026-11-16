// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Packages.SimulationPackage.Internal;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;
using Mocassin.Packages.Tests.SimulationPackage.TestSupport;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Exercises the real generated-lattice and expanded-job payload encoding, deduplication, payload-derived
/// columns, canonical extension JSON, and streaming cancellation.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class JobAndLatticePayloadEncodingTests
{
    private const string sample = "samples/jobs/ceria-temperature-sweep.mocprj";

    [Fact]
    public void LatticePayloadRoundTripsAndSignsTheCompiledModel()
    {
        var scenario = Load();
        var lattice = scenario.Jobs[0].Lattice;
        var payload = LatticePayloadMapper.Map(lattice);
        var encoder = new PayloadEncoder(3);
        var bytes = encoder.EncodeLattice(payload);
        var expectedBytes = checked((long)lattice.SizeA * lattice.SizeB * lattice.SizeC * lattice.PositionCount);
        var decoded = PayloadEncoder.DecodeLattice(bytes, 4L * expectedBytes);

        Assert.Equal(payload.LatticeSize.ToArray(), decoded.LatticeSize.ToArray());
        Assert.Equal(payload.PositionCount, decoded.PositionCount);
        Assert.Equal(payload.Lattice.ToArray(), decoded.Lattice.ToArray());
        Assert.Equal(expectedBytes, decoded.Lattice.Length);
        Assert.Equal(
            LatticeSignatureV1.Compute(scenario.Applied.ModelSignature, payload),
            LatticeSignatureV1.Compute(scenario.Applied.ModelSignature, decoded));
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeLattice(bytes, maxDecompressedBytes: 8));
    }

    [Fact]
    public void JobPayloadRoundTripsAgainstTheResolvedSettings()
    {
        var scenario = Load();
        var job = scenario.Jobs[0];
        var payload = JobPayloadMapper.Map(
            job.Descriptor,
            scenario.Applied.CompiledModel.Structure.CellVectors,
            job.Lattice.DopingInfo);
        var bytes = new PayloadEncoder(3).EncodeJob(payload);
        var decoded = PayloadEncoder.DecodeJob(bytes);
        var settings = job.Descriptor.Settings;

        Assert.Equal(payload.FormatVersion, decoded.FormatVersion);
        Assert.Equal(payload.SimulationIndex, decoded.SimulationIndex);
        Assert.Equal(payload.Kind, decoded.Kind);
        Assert.Equal(settings.TemperatureK, decoded.TemperatureK);
        Assert.Equal((ulong)settings.TargetMcsp, decoded.TargetMcsp);
        Assert.Equal((uint)settings.WriteOutCount, decoded.WriteOutCount);
        Assert.Equal((ulong)settings.PreRunMcsp, decoded.PreRunMcsp);
        Assert.Equal(settings.TimeLimitSeconds is { } timeLimit ? (ulong)timeLimit : null, decoded.TimeLimitS);
        Assert.Equal(settings.MinSuccessRate, decoded.MinSuccessRate);
        Assert.Equal(settings.ElectricFieldModulusVm, decoded.ElectricFieldVm);
        Assert.Equal(settings.NormalizationEnergyEv, decoded.NormalizationEnergyEv);
        Assert.Equal(settings.MaxAttemptFrequencyHz, decoded.MaxAttemptFrequencyHz);
        Assert.Equal(settings.BreakTolerance, decoded.BreakTolerance);
        Assert.Equal((ulong)settings.BreakSampleLengthMcsp, decoded.BreakSampleLengthMcsp);
        Assert.Equal((ulong)settings.BreakSampleIntervalMcsp, decoded.BreakSampleIntervalMcsp);
        Assert.Equal((ulong)settings.ResultSampleMcsp, decoded.ResultSampleMcsp);
        Assert.Equal(settings.Instruction, decoded.Instruction);
        Assert.Equal(job.Lattice.DopingInfo, decoded.DopingInfo);
        Assert.Equal(
            (uint)SolverJobFlagsMapper.Compose(
                settings.Flags,
                job.Descriptor.Kind,
                settings.PreRunMcsp,
                job.Descriptor.UseDualDofCorrection),
            decoded.Flags);
        Assert.Equal(3, decoded.ElectricFieldDirection.Length);
        Assert.Equal(1.0, decoded.ElectricFieldDirection.Select(static value => value * value).Sum(), 12);
        Assert.Equal(payload.SelectionOptimizers.Length, decoded.SelectionOptimizers.Length);
        Assert.Equal(
            payload.SelectionOptimizers.Select(static item => (item.PositionId, item.ParticleId)).ToArray(),
            decoded.SelectionOptimizers.Select(static item => (item.PositionId, item.ParticleId)).ToArray());
        Assert.Equal(
            payload.Extensions.Select(static item => (item.Id, item.ConfigJson)).ToArray(),
            decoded.Extensions.Select(static item => (item.Id, item.ConfigJson)).ToArray());
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeJob(bytes, maxDecompressedBytes: 8));
    }

    [Fact]
    public void JobPayloadUsesCanonicalExtensionJsonWithPreservedNumberTokens()
    {
        var scenario = Load();
        var job = scenario.Jobs[0];
        using var document = JsonDocument.Parse(
            """{"z":1,"n":123456789012345678901234567890,"a":1.0}""");
        var descriptor = job.Descriptor with
        {
            Settings = job.Descriptor.Settings with
            {
                Extensions = [new JobExtension("ext", document.RootElement.Clone())],
            },
        };

        var payload = JobPayloadMapper.Map(
            descriptor,
            scenario.Applied.CompiledModel.Structure.CellVectors,
            dopingInfo: "v1:doping:1:2:2:0:1");

        var extension = Assert.Single(payload.Extensions);
        Assert.Equal("ext", extension.Id);
        Assert.Equal("""{"a":1.0,"n":123456789012345678901234567890,"z":1}""", extension.ConfigJson);
        Assert.Equal("v1:doping:1:2:2:0:1", payload.DopingInfo);
    }

    [Fact]
    public async Task WriterStoresPayloadDerivedColumnsAndDeduplicatesIdenticalLattices()
    {
        var scenario = Load();
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelAsync(scenario.Applied);
            foreach (var job in scenario.Jobs)
            {
                await writer.WriteJobAsync(job);
            }

            var result = await writer.CompleteAsync();
            Assert.Equal(scenario.Jobs.Length, result.JobCount);
            Assert.Equal(scenario.Jobs.Length, result.LatticeCount);
            Assert.Equal(1.0, result.LatticeReuseFactor, 12);
        }

        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        var rows = await ReadJobsAsync(connection);
        Assert.Equal(scenario.Jobs.Length, rows.Count);
        Assert.Equal(
            Enumerable.Range(1, scenario.Jobs.Length),
            rows.Select(static row => row.JobId));

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var job = scenario.Jobs[index];
            var payload = PayloadEncoder.DecodeJob(row.Payload);
            var settings = job.Descriptor.Settings;
            Assert.Equal(payload.Kind, row.Kind);
            Assert.Equal(payload.TemperatureK, row.TemperatureK);
            Assert.Equal(payload.ElectricFieldVm, row.ElectricFieldVm);
            Assert.Equal((long)payload.TargetMcsp, row.TargetMcsp);
            Assert.Equal((int)payload.WriteOutCount, row.WriteOutCount);
            Assert.Equal((long)payload.PreRunMcsp, row.PreRunMcsp);
            Assert.Equal((long?)payload.TimeLimitS, row.TimeLimitS);
            Assert.Equal(payload.MinSuccessRate, row.MinSuccessRate);
            Assert.Equal(job.Lattice.SizeA, row.LatticeA);
            Assert.Equal(job.Lattice.SizeB, row.LatticeB);
            Assert.Equal(job.Lattice.SizeC, row.LatticeC);
            Assert.Equal(payload.DopingInfo, row.DopingInfo);
            Assert.Equal(payload.Flags, (uint)row.Flags);
            Assert.Equal(job.Descriptor.CollectionName, row.CollectionName);
            Assert.Equal(job.Descriptor.ConfigName, row.ConfigName);
            Assert.Equal((long)job.Descriptor.CollectionIndex, row.CollectionIndex);
            Assert.Equal((long)job.Descriptor.ConfigIndex, row.ConfigIndex);
            Assert.Equal((long)job.Descriptor.JobIndex, row.JobIndex);
            Assert.Equal(32, row.Seed.Length);
            Assert.Equal(
                payload.Flags,
                (uint)SolverJobFlagsMapper.Compose(
                    settings.Flags,
                    job.Descriptor.Kind,
                    settings.PreRunMcsp,
                    job.Descriptor.UseDualDofCorrection));
        }

        Assert.Equal(
            scenario.Applied.Signature,
            await QueryStringAsync(connection, "SELECT m.signature FROM models m JOIN jobs j ON j.model_id = m.model_id LIMIT 1;"));
        Assert.Equal(
            (long)scenario.Jobs.Length,
            await QueryInt64Async(connection, "SELECT COUNT(DISTINCT lattice_id) FROM jobs;"));
    }

    [Fact]
    public async Task IdenticalLatticesReuseOneRowAndDifferentParticlesInsertAnother()
    {
        var scenario = Load();
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var first = scenario.Jobs[0];
        var shared = scenario.Jobs[1] with
        {
            Descriptor = scenario.Jobs[1].Descriptor with { JobId = 2 },
            Lattice = first.Lattice,
        };
        var distinct = scenario.Jobs[2];

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelAsync(scenario.Applied);
            await writer.WriteJobAsync(first);
            await writer.WriteJobAsync(shared);
            await writer.WriteJobAsync(distinct);
            var result = await writer.CompleteAsync();

            Assert.Equal(3, result.JobCount);
            Assert.Equal(2, result.LatticeCount);
            Assert.Equal(1.5, result.LatticeReuseFactor, 12);
        }
    }

    [Fact]
    public async Task JobIdsMustBeStrictlyAscending()
    {
        var scenario = Load();
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelAsync(scenario.Applied);
            await writer.WriteJobAsync(scenario.Jobs[0]);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                writer.WriteJobAsync(scenario.Jobs[0]));
        }

        Assert.Empty(temp.TemporaryFiles());
    }

    [Fact]
    public async Task JobIdGapsFromAPartialBuildArePreserved()
    {
        var scenario = Load();
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var gapped = scenario.Jobs[1] with
        {
            Descriptor = scenario.Jobs[1].Descriptor with { JobId = 3 },
        };
        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions(),
            new PackageFileSystem());
        await using (writer)
        {
            await writer.WriteModelAsync(scenario.Applied);
            await writer.WriteJobAsync(scenario.Jobs[0]);
            await writer.WriteJobAsync(gapped);
            var result = await writer.CompleteAsync();
            Assert.Equal(2, result.JobCount);
        }

        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT job_id FROM jobs ORDER BY job_id;";
        await using var reader = await command.ExecuteReaderAsync();
        var ids = new List<long>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt64(0));
        }

        Assert.Equal([1L, 3L], ids);
    }

    [Fact]
    public async Task CancellationMidStreamLeavesNoPublishedPackage()
    {
        var scenario = Load();
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var original = new byte[] { 0xAA, 0xBB };
        File.WriteAllBytes(path, original);
        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions { Overwrite = true, BatchSize = 1 },
            new PackageFileSystem());
        await writer.WriteModelAsync(scenario.Applied);
        await writer.WriteJobAsync(scenario.Jobs[0]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            writer.WriteJobAsync(scenario.Jobs[1], cancellation.Token));
        await writer.DisposeAsync();

        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Empty(temp.TemporaryFiles());
    }

    [Theory]
    [InlineData(0.0, null)]
    [InlineData(0.25, 0.25)]
    public void AuthoredNormalizationEnergyIsCanonicalizedInTheJobPayload(
        double authored,
        double? expected)
    {
        var project = LoadWithCollectionNormalizationEnergy(authored);
        var scenario = Load(project);

        var job = scenario.Jobs[0];
        Assert.Equal(authored, job.Descriptor.Settings.NormalizationEnergyEv);
        var payload = JobPayloadMapper.Map(
            job.Descriptor,
            scenario.Applied.CompiledModel.Structure.CellVectors,
            job.Lattice.DopingInfo);
        var decoded = PayloadEncoder.DecodeJob(new PayloadEncoder(3).EncodeJob(payload));

        Assert.Equal(expected, payload.NormalizationEnergyEv);
        Assert.Equal(expected, decoded.NormalizationEnergyEv);
    }

    private static Project LoadWithCollectionNormalizationEnergy(double value)
    {
        var node = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Combine(sample)))!;
        var collection = node["jobTemplates"]![0]!["collections"]![0]!;
        var settings = collection["baseSettings"] as JsonObject ?? new JsonObject();
        collection["baseSettings"] = settings;
        settings["normalizationEnergyEv"] = JsonValue.Create(value);
        return ProjectJson.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString()));
    }

    private static Scenario Load() => Load(ParameterizationTestSupport.LoadProject(sample));

    private static Scenario Load(Project project)
    {
        var compiled = ParameterizationTestSupport.Compile(project);
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model!;
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        Assert.True(
            expansion.IsSuccess,
            string.Join(Environment.NewLine, expansion.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        var jobs = new List<JobPackage>(expansion.Jobs.Length);
        foreach (var descriptor in expansion.Jobs)
        {
            var generated = SupercellGenerator.Generate(project, compiled, descriptor);
            Assert.True(
                generated.IsSuccess,
                string.Join(Environment.NewLine, generated.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
            jobs.Add(new JobPackage(descriptor, generated.Lattice!));
        }

        Assert.Equal(Enumerable.Range(1, jobs.Count), jobs.Select(static job => job.Descriptor.JobId));
        return new Scenario(project, compiled, applied, [.. jobs]);
    }

    private static async Task<List<JobRow>> ReadJobsAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT job_id, kind, collection_name, config_name, collection_index, config_index, job_index,
                   temperature_k, electric_field_vm, target_mcsp, write_out_count, pre_run_mcsp,
                   time_limit_s, min_success_rate, lattice_a, lattice_b, lattice_c, doping_info, flags, seed, payload
            FROM jobs ORDER BY job_id;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<JobRow>();
        while (await reader.ReadAsync())
        {
            rows.Add(new JobRow(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt64(4),
                reader.GetInt64(5),
                reader.GetInt64(6),
                reader.GetDouble(7),
                reader.GetDouble(8),
                reader.GetInt64(9),
                reader.GetInt32(10),
                reader.GetInt64(11),
                reader.IsDBNull(12) ? null : reader.GetInt64(12),
                reader.IsDBNull(13) ? null : reader.GetDouble(13),
                reader.GetInt32(14),
                reader.GetInt32(15),
                reader.GetInt32(16),
                reader.GetString(17),
                reader.GetInt64(18),
                (byte[])reader[19],
                (byte[])reader[20]));
        }

        return rows;
    }

    private static async Task<string> QueryStringAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static async Task<long> QueryInt64Async(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record Scenario(
        Project Project,
        CompiledModel Compiled,
        ParameterizedModel Applied,
        ImmutableArray<JobPackage> Jobs);

    private sealed record JobRow(
        int JobId,
        string Kind,
        string CollectionName,
        string ConfigName,
        long CollectionIndex,
        long ConfigIndex,
        long JobIndex,
        double TemperatureK,
        double ElectricFieldVm,
        long TargetMcsp,
        int WriteOutCount,
        long PreRunMcsp,
        long? TimeLimitS,
        double? MinSuccessRate,
        int LatticeA,
        int LatticeB,
        int LatticeC,
        string DopingInfo,
        long Flags,
        byte[] Seed,
        byte[] Payload);
}

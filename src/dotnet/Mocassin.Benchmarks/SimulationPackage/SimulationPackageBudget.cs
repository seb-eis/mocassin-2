// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Benchmarks.SimulationPackage;

/// <summary>
/// Runs the Step 11 Part B 10⁴-job simulation-package build once and records the budget metrics.
/// </summary>
/// <remarks>
/// The workload is the real compiler, parameterizer, job expander, supercell generator, and package writer on
/// the real ceria sample. Ten collections with one 10×10×10 configuration and 1,000 duplicate jobs each produce
/// 10,000 jobs. The runner is used both on the amended tree and, copied into a temporary pre-amendment worktree,
/// on the `net10.0` reference tree (amendment 1 §9.2).
/// </remarks>
public static class SimulationPackageBudget
{
    public const int CollectionCount = 10;
    public const int JobsPerConfig = 1_000;
    public const int LatticeSize = 10;
    public const int CompressionLevel = 3;
    private static readonly DateTimeOffset createdUtc = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Runs the budget workload and writes the metrics JSON.</summary>
    /// <param name="outputPath">The metrics output path.</param>
    /// <returns>0 on success.</returns>
    public static async Task<int> RunAsync(string outputPath)
    {
        var repository = FindRepositoryRoot();
        var sample = Path.Combine(repository, "samples/jobs/ceria-temperature-sweep.mocprj");
        var projectBytes = CreateWorkloadProjectJson(sample);
        var directory = Path.Combine(Path.GetTempPath(), "mocassin-partb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var packagePath = Path.Combine(directory, "ceria-10000.msl");

        var total = Stopwatch.StartNew();
        var project = ProjectJson.Deserialize(projectBytes);
        var compiled = new ModelCompiler().Compile(project).Model
            ?? throw new InvalidOperationException("The benchmark project did not compile.");
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model
            ?? throw new InvalidOperationException("The benchmark project did not parameterize.");
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        if (!expansion.IsSuccess)
        {
            throw new InvalidOperationException(string.Join(
                Environment.NewLine,
                expansion.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        var preparation = total.Elapsed;
        var generation = TimeSpan.Zero;
        var insertion = TimeSpan.Zero;
        var writer = SimulationPackageWriter.Create(
            new PackageBuildRequest(packagePath, project.Metadata.ProjectId, "all", createdUtc),
            new PackageBuildOptions { CompressionLevel = CompressionLevel });
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteModelAsync(applied).ConfigureAwait(false);
            foreach (var descriptor in expansion.Jobs)
            {
                var generationStopwatch = Stopwatch.StartNew();
                var generated = SupercellGenerator.Generate(project, compiled, descriptor);
                generation += generationStopwatch.Elapsed;
                if (!generated.IsSuccess)
                {
                    throw new InvalidOperationException(string.Join(
                        Environment.NewLine,
                        generated.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
                }

                var insertionStopwatch = Stopwatch.StartNew();
                await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice!)).ConfigureAwait(false);
                insertion += insertionStopwatch.Elapsed;
            }

            await writer.CompleteAsync().ConfigureAwait(false);
        }

        total.Stop();
        var counts = ReadCounts(packagePath);
        var metrics = new BudgetMetrics(
            Step: "step-11",
            Part: "Part B — 10\u2074-job simulation-package build",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Runtime: Environment.Version.ToString(),
            FrameworkDescription: System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            OperatingSystem: System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            SourceCommit: ReadSourceCommit(repository),
            Sample: "samples/jobs/ceria-temperature-sweep.mocprj",
            CollectionCount: CollectionCount,
            JobsPerConfig: JobsPerConfig,
            LatticeSize: LatticeSize,
            CompressionLevel: CompressionLevel,
            JobCount: counts.JobCount,
            LatticeCount: counts.LatticeCount,
            ModelCount: counts.ModelCount,
            TotalSeconds: total.Elapsed.TotalSeconds,
            PreparationSeconds: preparation.TotalSeconds,
            GenerationSeconds: generation.TotalSeconds,
            InsertionSeconds: insertion.TotalSeconds,
            JobInsertionRate: counts.JobCount / Math.Max(insertion.TotalSeconds, 1e-9),
            PackageBytes: new FileInfo(packagePath).Length,
            ModelPayloadBytes: counts.ModelPayloadBytes,
            PeakRssBytes: ReadPeakRssBytes(),
            PackageSha256: Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(packagePath))),
            IntegrityCheck: counts.IntegrityCheck);

        var text = JsonSerializer.Serialize(metrics, jsonOptions);
        await File.WriteAllTextAsync(outputPath, text + "\n").ConfigureAwait(false);
        Console.WriteLine(text);
        Directory.Delete(directory, recursive: true);
        return 0;
    }

    /// <summary>Builds the 10⁴-job project JSON from the sample template.</summary>
    /// <param name="samplePath">The sample project path.</param>
    /// <returns>The modified project JSON.</returns>
    public static byte[] CreateWorkloadProjectJson(
        string samplePath,
        int collectionCount = CollectionCount,
        int jobsPerConfig = JobsPerConfig,
        int latticeSize = LatticeSize)
    {
        var root = JsonNode.Parse(File.ReadAllText(samplePath))?.AsObject()
            ?? throw new InvalidDataException($"The project '{samplePath}' is not a JSON object.");
        var template = root["jobTemplates"]?.AsArray()[0]?.AsObject()
            ?? throw new InvalidDataException("The sample has no job template.");
        var sourceCollection = template["collections"]?.AsArray()[0]?.AsObject()
            ?? throw new InvalidDataException("The sample has no job collection.");
        var sourceConfig = sourceCollection["configs"]?.AsArray()[0]?.AsObject()
            ?? throw new InvalidDataException("The sample has no job configuration.");
        var collections = new JsonArray();
        for (var index = 0; index < collectionCount; index++)
        {
            var collection = sourceCollection.DeepClone().AsObject();
            collection["id"] = DeterministicGuid(9000 + index);
            collection["name"] = $"budget-{index.ToString("D2", CultureInfo.InvariantCulture)}";
            collection["jobCountPerConfig"] = jobsPerConfig;
            var config = sourceConfig.DeepClone().AsObject();
            config["id"] = DeterministicGuid(8000 + index);
            config["name"] = "budget";
            config["lattice"] = new JsonObject
            {
                ["a"] = latticeSize,
                ["b"] = latticeSize,
                ["c"] = latticeSize,
            };
            collection["configs"] = new JsonArray(config);
            collections.Add(collection);
        }

        template["collections"] = collections;
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    /// <summary>Finds the repository root from the benchmark output directory.</summary>
    /// <returns>The repository root.</returns>
    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mocassin.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the benchmark directory.");
    }

    private static string DeterministicGuid(int value) =>
        "018f4c9a-7f6b-7c3a-9d2e-" + value.ToString("D12", CultureInfo.InvariantCulture);

    private static (long JobCount, long LatticeCount, long ModelCount, long ModelPayloadBytes, string IntegrityCheck)
        ReadCounts(string path)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT COUNT(*) FROM jobs),
                   (SELECT COUNT(*) FROM lattices),
                   (SELECT COUNT(*) FROM models),
                   (SELECT length(payload) FROM models LIMIT 1),
                   (SELECT integrity_check FROM pragma_integrity_check);
            """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidDataException("The benchmark package has no rows.");
        }

        return (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetString(4));
    }

    private static long ReadPeakRssBytes()
    {
        const string statusPath = "/proc/self/status";
        if (File.Exists(statusPath))
        {
            foreach (var line in File.ReadLines(statusPath))
            {
                if (!line.StartsWith("VmHWM:", StringComparison.Ordinal))
                {
                    continue;
                }

                var value = line.Split(':', 2)[1].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (value.Length > 0 &&
                    long.TryParse(value[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kilobytes))
                {
                    return kilobytes * 1024;
                }
            }
        }

        return Process.GetCurrentProcess().PeakWorkingSet64;
    }

    private static string ReadSourceCommit(string repository)
    {
        var gitDirectory = Path.Combine(repository, ".git");
        if (File.Exists(gitDirectory))
        {
            // A linked worktree stores a "gitdir: <path>" file instead of a .git directory.
            var pointer = File.ReadAllText(gitDirectory).Trim();
            if (!pointer.StartsWith("gitdir: ", StringComparison.Ordinal))
            {
                return "unknown";
            }

            var target = pointer[8..];
            gitDirectory = Path.IsPathRooted(target)
                ? target
                : Path.GetFullPath(Path.Combine(repository, target));
        }

        var head = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(head))
        {
            return "unknown";
        }

        var content = File.ReadAllText(head).Trim();
        if (!content.StartsWith("ref: ", StringComparison.Ordinal))
        {
            return content;
        }

        var refPath = Path.Combine(gitDirectory, content[5..].Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : "unknown";
    }

    /// <summary>The budget metrics written by the runner.</summary>
    public sealed record BudgetMetrics(
        string Step,
        string Part,
        DateTimeOffset GeneratedAtUtc,
        string Runtime,
        string FrameworkDescription,
        string OperatingSystem,
        string SourceCommit,
        string Sample,
        int CollectionCount,
        int JobsPerConfig,
        int LatticeSize,
        int CompressionLevel,
        long JobCount,
        long LatticeCount,
        long ModelCount,
        double TotalSeconds,
        double PreparationSeconds,
        double GenerationSeconds,
        double InsertionSeconds,
        double JobInsertionRate,
        long PackageBytes,
        long ModelPayloadBytes,
        long PeakRssBytes,
        string PackageSha256,
        string IntegrityCheck);
}

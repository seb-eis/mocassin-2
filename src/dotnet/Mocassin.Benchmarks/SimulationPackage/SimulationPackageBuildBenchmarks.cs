// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Benchmarks.SimulationPackage;

/// <summary>
/// Benchmarks a small simulation-package build with the real writer.
/// </summary>
/// <remarks>
/// The 10⁴-job closeout budget is measured by <see cref="SimulationPackageBudget"/>; this BenchmarkDotNet case
/// keeps a repeatable micro-benchmark in the regular benchmark suite without building the full campaign per
/// iteration.
/// </remarks>
[MemoryDiagnoser]
public class SimulationPackageBuildBenchmarks
{
    private const int jobCount = 100;
    private const int latticeSize = 10;
    private static readonly DateTimeOffset createdUtc = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private byte[] projectBytes = null!;
    private string directory = null!;

    /// <summary>Builds the project fixture once.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var repository = SimulationPackageBudget.FindRepositoryRoot();
        projectBytes = SimulationPackageBudget.CreateWorkloadProjectJson(
            Path.Combine(repository, "samples/jobs/ceria-temperature-sweep.mocprj"),
            collectionCount: 1,
            jobsPerConfig: jobCount,
            latticeSize: latticeSize);
        directory = Path.Combine(Path.GetTempPath(), "mocassin-bench-msl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }

    /// <summary>Writes one package with 100 jobs.</summary>
    /// <returns>The job count, to prevent elimination.</returns>
    [Benchmark]
    public async Task<long> WritePackage()
    {
        var project = ProjectJson.Deserialize(projectBytes);
        var compiled = new ModelCompiler().Compile(project).Model
            ?? throw new InvalidOperationException("The benchmark project did not compile.");
        var applied = new ParameterizationService().Apply(project, compiled, template: null).Model
            ?? throw new InvalidOperationException("The benchmark project did not parameterize.");
        var expansion = JobTemplateExpander.Expand(project, compiled, project.JobTemplates.Single());
        if (!expansion.IsSuccess)
        {
            throw new InvalidOperationException("The benchmark project did not expand.");
        }

        var path = Path.Combine(directory, "bench-" + Guid.NewGuid().ToString("N") + ".msl");
        var writer = SimulationPackageWriter.Create(
            new PackageBuildRequest(path, project.Metadata.ProjectId, "all", createdUtc),
            new PackageBuildOptions());
        await using (writer.ConfigureAwait(false))
        {
            await writer.WriteModelAsync(applied).ConfigureAwait(false);
            foreach (var descriptor in expansion.Jobs)
            {
                var generated = SupercellGenerator.Generate(project, compiled, descriptor);
                if (!generated.IsSuccess)
                {
                    throw new InvalidOperationException("The benchmark lattice did not generate.");
                }

                await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice!)).ConfigureAwait(false);
            }

            await writer.CompleteAsync().ConfigureAwait(false);
        }

        var result = new FileInfo(path).Length;
        File.Delete(path);
        return result;
    }

    /// <summary>Removes any package left by a failed iteration.</summary>
    [IterationCleanup]
    public void Cleanup()
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.msl"))
        {
            File.Delete(file);
        }
    }

    /// <summary>Removes the benchmark directory.</summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Signatures;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Exercises the complete compiler through its public API on a real project.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class CompilationEndToEndTests
{
    [Fact]
    public async Task CeriaKmcCompilesDeterministicallyThroughThePublicApi()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var compiler = new ModelCompiler();

        var first = compiler.Compile(project);
        var second = compiler.Compile(project);

        Assert.True(first.IsSuccess, string.Join(Environment.NewLine, first.Diagnostics.Select(static item => item.ToString())));
        Assert.True(second.IsSuccess);
        Assert.NotNull(first.Model);
        Assert.NotNull(second.Model);
        Assert.False(string.IsNullOrWhiteSpace(first.Model!.ModelSignature));
        Assert.Equal(first.Model.ModelSignature, second.Model!.ModelSignature);
        Assert.Equal(CompiledModelJson.Write(first.Model), CompiledModelJson.Write(second.Model));
        Assert.Contains("compiled-inspection-v1", CompiledModelJson.Write(first.Model), StringComparison.Ordinal);
        Assert.NotEmpty(first.Model.Transitions.Rules);
        Assert.NotEmpty(first.Model.Simulations);
        Assert.Equal(10, first.Report.Stages.Length);
    }

    [Fact]
    public async Task RuntimeSettingsDoNotChangeTheModelSignature()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var compiler = new ModelCompiler();
        var baseline = compiler.Compile(project);
        var simulation = (Core.Model.Simulations.KineticSimulation)project.Model.Simulations[0];
        var changed = project with
        {
            Model = project.Model with
            {
                Simulations = [simulation with { TemperatureK = 1500.0, TargetMcsp = 5000 }],
            },
        };

        var updated = compiler.Compile(changed);

        Assert.True(baseline.IsSuccess);
        Assert.True(updated.IsSuccess);
        Assert.Equal(baseline.Model!.ModelSignature, updated.Model!.ModelSignature);
    }

    [Fact]
    public async Task CallerCacheReusesStructureRulesAndSigningKeys()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var cache = new RecordingCache(256);
        var compiler = new ModelCompiler();
        var options = new CompilationOptions { Cache = cache };

        Assert.True(compiler.Compile(project, options).IsSuccess);
        Assert.True(compiler.Compile(project, options).IsSuccess);

        Assert.Contains(cache.Hits, static key => key.StartsWith("structure|", StringComparison.Ordinal));
        Assert.Contains(cache.Hits, static key => key.StartsWith("rules|", StringComparison.Ordinal));
        Assert.Contains(cache.Hits, static key => key.StartsWith("signing|", StringComparison.Ordinal));
        Assert.Same(cache, options.Cache);
    }

    private sealed class RecordingCache(int capacity) : ICompilationCache
    {
        private readonly StageCache inner = new(capacity);

        public HashSet<string> Hits { get; } = new(StringComparer.Ordinal);

        public CompilationCacheStatistics Statistics => inner.Statistics;

        public bool TryGet<T>(string key, out T? value)
            where T : class
        {
            var found = inner.TryGet(key, out value);
            if (found)
            {
                Hits.Add(key);
            }

            return found;
        }

        public T GetOrAdd<T>(string key, Func<T> factory)
            where T : class => inner.GetOrAdd(key, factory);
    }
}

/// <summary>
/// Covers the simulation-selection cache key.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SimulationSelectionHashTests
{
    [Fact]
    public async Task RuntimeSettingsDoNotChangeTheSelectionHashButReferencesDo()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var baseline = ProjectHasher.ComputeSimulationSelection(project);
        var simulation = (Core.Model.Simulations.KineticSimulation)project.Model.Simulations[0];
        var changed = project with
        {
            Model = project.Model with
            {
                Simulations = [simulation with { TemperatureK = 1600.0 }],
            },
        };

        Assert.Equal(baseline, ProjectHasher.ComputeSimulationSelection(changed));

        var withoutTransition = changed with
        {
            Model = changed.Model with
            {
                Simulations = [(Core.Model.Simulations.KineticSimulation)changed.Model.Simulations[0] with { TransitionIds = [] }],
            },
        };
        Assert.NotEqual(baseline, ProjectHasher.ComputeSimulationSelection(withoutTransition));
    }
}

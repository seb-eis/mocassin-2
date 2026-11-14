// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Structure;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Emits the compilation diagnostics that the contract defines.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CompilationDiagnosticEmissionTests
{
    [Fact]
    public async Task UnknownSpaceGroupEmitsCmp001()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");
        var invalid = project with
        {
            Model = project.Model with
            {
                Structure = project.Structure with
                {
                    SpaceGroup = new SpaceGroupRef(999, AxisSetting.None, OriginChoice.None),
                },
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP001");
    }

    [Fact]
    public async Task InvalidCellEmitsCmp002()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");
        var invalid = project with
        {
            Model = project.Model with
            {
                Structure = project.Structure with
                {
                    Cell = project.Structure.Cell with { A = new Core.Units.LengthAngstrom(0.01) },
                },
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP002");
    }

    [Fact]
    public async Task MassViolationEmitsCmp005()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/vehicle5.mocprj"),
            kinetic: true);
        var stateChange = project.StateChanges.First(static item => item.Name == "A -> vacancy");
        var ab = project.Particles.First(static particle => particle.Symbol == "AB");
        var invalid = project with
        {
            Model = project.Model with
            {
                StateChanges = [
                    .. project.StateChanges.Select(item => item.Id == stateChange.Id
                        ? item with { AcceptorParticleId = ab.Id }
                        : item),
                ],
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP005");
    }

    [Fact]
    public async Task InvalidPathEmitsCmp007()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var transition = project.Model.KineticTransitions[0];
        var invalidPath = transition.ReferencePath.SetItem(1, new Fractional3(0.1234567, 0.2345678, 0.3456789));
        var invalid = project with
        {
            Model = project.Model with
            {
                KineticTransitions = [transition with { ReferencePath = invalidPath }],
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP007");
    }

    [Fact]
    public async Task MissingClusterCentreEmitsCmp003()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");
        var cluster = project.Model.Clusters[0];
        var invalid = project with
        {
            Model = project.Model with
            {
                Clusters = [cluster with { CenterSiteId = Core.Ids.CellSiteId.New() }],
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP003");
    }

    [Fact]
    public async Task MissingChainGroupEmitsCmp008()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var chain = project.Model.Chains[0];
        var invalidGroup = Core.Ids.StateChangeGroupId.New();
        var invalid = project with
        {
            Model = project.Model with
            {
                Chains = [chain with { GroupIds = chain.GroupIds.SetItem(1, invalidGroup) }],
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP008");
    }

    [Fact]
    public async Task ConflictingRuleOutcomesEmitCmp004()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/vehicle5.mocprj"),
            kinetic: true);
        var vacancy = project.Particles.First(static particle => particle.Symbol == "V");
        var a = project.Particles.First(static particle => particle.Symbol == "A");
        var secondVacancy = vacancy with
        {
            Id = Core.Ids.ParticleId.New(),
            Name = "Vacancy 2",
        };
        var secondChange = new Core.Model.Transitions.StateChange(
            Core.Ids.StateChangeId.New(),
            "A -> vacancy 2",
            a.Id,
            secondVacancy.Id);
        var group = project.Model.StateChangeGroups.First(static item => item.Name == "A vacancy");
        var invalid = project with
        {
            Model = project.Model with
            {
                Particles = [.. project.Particles, secondVacancy],
                StateChanges = [.. project.StateChanges, secondChange],
                StateChangeGroups = [
                    .. project.Model.StateChangeGroups.Select(item => item.Id == group.Id
                        ? item with { StateChangeIds = [.. item.StateChangeIds, secondChange.Id] }
                        : item),
                ],
            },
        };

        var result = new ModelCompiler().Compile(invalid);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP004");
    }

    [Fact]
    public async Task HardRuleLimitEmitsCmp006()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project, new CompilationOptions { MaxRuleCount = 0 });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP006");
    }

    [Fact]
    public async Task HardEnergyTableLimitEmitsCmp006()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var result = new ModelCompiler().Compile(project, new CompilationOptions { MaxEnergyTableBytes = 0 });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP006");
    }

    [Fact]
    public async Task CorruptStructureCacheEmitsCmp012()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");
        var cache = new CorruptStructureCache();
        var result = new ModelCompiler().Compile(project, new CompilationOptions { Cache = cache });

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static item => item.Code == "CMP012");
    }

    private sealed class CorruptStructureCache : ICompilationCache
    {
        private readonly Dictionary<string, object> values = new(StringComparer.Ordinal);

        public CompilationCacheStatistics Statistics => new(0, 0, 16);

        public bool TryGet<T>(string key, out T? value)
            where T : class
        {
            if (key.StartsWith("structure|", StringComparison.Ordinal))
            {
                value = (T)(object)CompiledStructureModel.Empty;
                return true;
            }

            value = null;
            return false;
        }

        public T GetOrAdd<T>(string key, Func<T> factory)
            where T : class
        {
            if (values.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            value = factory();
            values[key] = value;
            return (T)value;
        }
    }
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Guards the local compilation performance targets.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class CompilationPerformanceTests
{
    [Fact]
    public async Task CeriaCompilesWithinTheLocalBudget()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var compiler = new ModelCompiler();
        _ = compiler.Compile(project);
        var started = Stopwatch.GetTimestamp();

        var result = compiler.Compile(project);

        var elapsed = Stopwatch.GetElapsedTime(started);
        Assert.True(result.IsSuccess);
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Ceria compilation took {elapsed}.");
    }

    [Fact]
    public void TenThousandPositionModelCompilesWithinTheBudget()
    {
        var project = CreateLargeProject();
        var compiler = new ModelCompiler();
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var started = Stopwatch.GetTimestamp();

        var result = compiler.Compile(project);

        var elapsed = Stopwatch.GetElapsedTime(started);
        var after = GC.GetTotalMemory(forceFullCollection: true);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Equal(10_000, result.Model!.Structure.PositionCount);
        // Shared-runner smoke bound: the benchmark-class < 30 s target (step-08 section 13.4) is measured
        // locally via Mocassin.Benchmarks. CI observes 31-36 s for this test from runner load alone, so 90 s is
        // about 2.5x the worst observed duration and still catches order-of-magnitude regressions.
        Assert.True(elapsed < TimeSpan.FromSeconds(90), $"10k compilation took {elapsed}.");
        Assert.True(after - before < 2L * 1024 * 1024 * 1024, $"10k compilation used {after - before} bytes.");
    }

    private static Project CreateLargeProject()
    {
        var particle = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-00000000c001")),
            "O2-",
            "O",
            new ChargeElementary(-2.0),
            IsVacancy: false);
        var set = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-00000000c002")),
            "Oxygen",
            [particle.Id]);
        var sites = ImmutableArray.CreateBuilder<CellSite>(10_000);
        for (var a = 0; a < 100; a++)
        {
            for (var b = 0; b < 100; b++)
            {
                sites.Add(new CellSite(
                    CellSiteId.New(),
                    string.Create(System.Globalization.CultureInfo.InvariantCulture, $"O{a}_{b}"),
                    new Fractional3(a / 100.0, b / 100.0, 0.0),
                    set.Id,
                    SiteStability.Stable));
            }
        }

        var model = ModelDefinition.Empty with
        {
            Particles = [particle],
            ParticleSets = [set],
            StableEnvironment = new StableEnvironment(new LengthAngstrom(0.001), []),
            Structure = StructureModel.Default with
            {
                Name = "10k P1",
                SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                Cell = new CellParameters(
                    new LengthAngstrom(100.0),
                    new LengthAngstrom(100.0),
                    new LengthAngstrom(100.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                Sites = sites.MoveToImmutable(),
            },
        };
        return new Project(
            schema: null,
            schemaVersion: ProjectSchema.CurrentVersion,
            metadata: new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-00000000c000"),
                new string('0', 64),
                "10k test",
                Description: null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            model: model,
            parameterizations: [],
            jobTemplates: [],
            buildTemplates: [],
            viewSettings: ViewSettings.Default);
    }
}

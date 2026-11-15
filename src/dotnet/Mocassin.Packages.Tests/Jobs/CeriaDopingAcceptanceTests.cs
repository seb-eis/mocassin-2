// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.Jobs;

/// <summary>
/// Pins the Step 10 ceria acceptance scenario and the setting-invariance property with the real sample.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class CeriaDopingAcceptanceTests
{
    private const string ceriaSweep = "samples/jobs/ceria-temperature-sweep.mocprj";

    [Fact]
    public void TenByTenByTenCeriaAtOnePercentAppliesTenPrimaryAndFiveDependentExchanges()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var doping = project.Model.Dopings.Single();
        var request = new LatticeRequest(10, 10, 10);
        ImmutableArray<DopingValue> dopings = [new DopingValue(doping.Id, 0.01)];
        var service = new LatticePreviewService();

        var preview = service.TryCreatePreview(project, model, request, dopings, out var diagnostics);

        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        Assert.Equal(4_000, preview.Report.TotalPositions);
        var application = Assert.Single(preview.Report.Applications);
        Assert.Equal(1_000, application.AvailablePrimary);
        Assert.Equal(10, application.RequestedPrimary);
        Assert.Equal(10, application.AppliedPrimary);
        Assert.Equal(5, application.AppliedDependent);
        Assert.Equal(0.01, application.EffectiveFraction);
        Assert.True(preview.Report.IsChargeNeutral);

        var generated = SupercellGenerator.Generate(project, model, CreateJob(request, dopings));
        Assert.True(generated.IsSuccess);
        Assert.Equal(preview.Report.ToDopingInfo(), generated.Lattice!.Report.ToDopingInfo());
    }

    [Fact]
    public void ChangingASimulationSettingKeepsTheLatticeSeedRequestHashAndParticles()
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ceriaSweep);
        var template = project.JobTemplates.Single();
        var expansion = JobTemplateExpander.Expand(project, model, template);
        Assert.True(expansion.IsSuccess);
        var job = expansion.Jobs[0];

        var requestHash = LatticeRequestHasher.Compute(project, model, job.Lattice, job.Dopings);
        var first = SupercellGenerator.Generate(project, model, job);
        Assert.True(first.IsSuccess);

        var simulation = project.Model.Simulations.Single();
        var changedProject = project with
        {
            Model = project.Model with
            {
                Simulations =
                [
                    simulation with
                    {
                        TemperatureK = 1_273.0,
                        TargetMcsp = 9_999,
                        WriteOutCount = 3,
                    },
                ],
            },
        };
        var changedModel = new ModelCompiler().Compile(changedProject).Model!;

        var changedHash = LatticeRequestHasher.Compute(changedProject, changedModel, job.Lattice, job.Dopings);
        var second = SupercellGenerator.Generate(changedProject, changedModel, job);

        Assert.True(second.IsSuccess);
        Assert.Equal(requestHash, changedHash);
        Assert.Equal(
            Convert.ToHexStringLower(first.LatticeSeed.AsSpan()),
            Convert.ToHexStringLower(second.LatticeSeed.AsSpan()));
        Assert.Equal<byte>(first.Lattice!.Particles, second.Lattice!.Particles);
        Assert.Equal(first.Lattice.Report.ToDopingInfo(), second.Lattice.Report.ToDopingInfo());
    }

    private static JobDescriptor CreateJob(LatticeRequest request, ImmutableArray<DopingValue> dopings) => new(
        1,
        0,
        0,
        0,
        "collection",
        "config",
        simulationIdFromSample,
        0,
        SimulationKind.Kmc,
        UseDualDofCorrection: false,
        new ResolvedJobSettings(
            900.0,
            500,
            7,
            null,
            null,
            ExecutionFlags.Deterministic,
            null,
            0,
            0.0,
            Fractional3.Zero,
            null,
            null,
            0.01,
            1000,
            100,
            200,
            []),
        request,
        dopings,
        [],
        [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)]);

    private static readonly SimulationId simulationIdFromSample =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f70"));
}

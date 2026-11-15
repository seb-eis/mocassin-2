// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class LatticeRequestHasherTests
{
    private static readonly DopingId syntheticDoping =
        DopingId.Create(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff", System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public void SyntheticVectorMatchesTheRecordedProjection()
    {
        var hash = LatticeRequestHasher.Compute(
            Sequence(0x00),
            Sequence(0x20),
            Sequence(0x40),
            new LatticeRequest(10, 10, 10),
            [new LatticeDoping(syntheticDoping, 0, 0.01)],
            "2026.1",
            Sequence(0x60),
            "symmetry-1");

        Assert.Equal(
            "5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295",
            hash);
    }

    [Fact]
    public void PublicProjectionMatchesTheLowLevelProjectionForTheSameInputs()
    {
        var project = CreateProject();
        var model = CreateModel();
        var request = new LatticeRequest(10, 10, 10);

        var hash = LatticeRequestHasher.Compute(project, model, request, []);

        Assert.Equal(
            LatticeRequestHasher.Compute(
                Convert.FromHexString(ProjectHasher.ComputeStructure(project.Model.Structure)),
                Convert.FromHexString(ProjectHasher.ComputeParticles(project)),
                Convert.FromHexString(ProjectHasher.ComputeLattice(project)),
                request,
                [],
                model.SpaceGroupDataVersion,
                Convert.FromHexString(model.SpaceGroupDataSha256),
                model.SymmetryContractVersion),
            hash);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void DopingOrderIsIndependentOfTheAuthoredRequestOrder()
    {
        var project = CreateProject(CreateDoping(1), CreateDoping(0));
        var model = CreateModel();
        var first = new DopingValue(CreateDoping(1).Id, 0.1);
        var second = new DopingValue(CreateDoping(0).Id, 0.2);

        var forward = LatticeRequestHasher.Compute(
            project,
            model,
            new LatticeRequest(2, 2, 2),
            [first, second]);
        var reversed = LatticeRequestHasher.Compute(
            project,
            model,
            new LatticeRequest(2, 2, 2),
            [second, first]);

        Assert.Equal(forward, reversed);
    }

    [Theory]
    [InlineData(3, 2, 2)]
    [InlineData(2, 3, 2)]
    [InlineData(2, 2, 3)]
    public void CellSizeChangesTheHash(int a, int b, int c)
    {
        var project = CreateProject();
        var model = CreateModel();

        Assert.NotEqual(
            LatticeRequestHasher.Compute(project, model, new LatticeRequest(2, 2, 2), []),
            LatticeRequestHasher.Compute(project, model, new LatticeRequest(a, b, c), []));
    }

    [Fact]
    public void FractionAndProvenanceChangeTheHash()
    {
        var project = CreateProject(CreateDoping(0));
        var model = CreateModel();
        var dopingId = project.Model.Dopings[0].Id;

        var baseline = LatticeRequestHasher.Compute(
            project,
            model,
            new LatticeRequest(2, 2, 2),
            [new DopingValue(dopingId, 0.1)]);

        Assert.NotEqual(
            baseline,
            LatticeRequestHasher.Compute(
                project,
                model,
                new LatticeRequest(2, 2, 2),
                [new DopingValue(dopingId, 0.2)]));
        Assert.NotEqual(
            baseline,
            LatticeRequestHasher.Compute(
                project,
                model with { SymmetryContractVersion = "symmetry-2" },
                new LatticeRequest(2, 2, 2),
                [new DopingValue(dopingId, 0.1)]));
    }

    [Fact]
    public void UnknownDopingIsRejected()
    {
        var project = CreateProject();
        var model = CreateModel();

        Assert.Throws<ArgumentException>(() => LatticeRequestHasher.Compute(
            project,
            model,
            new LatticeRequest(2, 2, 2),
            [new DopingValue(DopingId.New(), 0.1)]));
    }

    private static byte[] Sequence(int start) => [.. Enumerable.Range(start, 32).Select(static value => (byte)value)];

    private static Doping CreateDoping(int priority) => new(
        DopingId.Create(Guid.Parse(
            priority == 0 ? "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7a50" : "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7a51",
            System.Globalization.CultureInfo.InvariantCulture)),
        $"doping-{priority}",
        priority,
        OccupationExchangeId.New(),
        DependentExchangeId: null,
        ChargeCompensation: false);

    private static Project CreateProject(params Doping[] dopings) => new(
        null,
        ProjectSchema.CurrentVersion,
        new ProjectMetadata(
            Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7a00", System.Globalization.CultureInfo.InvariantCulture),
            new string('a', 64),
            "Lattice request test",
            null,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch),
        new ModelDefinition { Dopings = [.. dopings] },
        [],
        [],
        [],
        ViewSettings.Default);

    private static CompiledModel CreateModel() => new(
        CompiledModel.CurrentFormatVersion,
        "test-model",
        "symmetry-1",
        "2026.1",
        new string('b', 64));
}

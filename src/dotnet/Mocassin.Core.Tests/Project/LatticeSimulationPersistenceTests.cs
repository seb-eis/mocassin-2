// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Project;

[Trait("Tier", "PR-fast")]
public sealed class LatticeSimulationPersistenceTests
{
    private const string preStep06ModelHash =
        "ad23d428401235cd2fc91fc325db3f061bb0cf7867eab60100053017b9674631";

    private const string preStep06DocumentHash =
        "b4fc7449a3cff18995d1167177de916fe4578ef3d7bb6bb9c03dfb3158db21e3";

    [Fact]
    public void PreStep06ProjectKeepsItsCanonicalModelAndDocumentHashes()
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", "project-transitions-minimal.mocprj")));

        Assert.Empty(project.Model.OccupationExchanges);
        Assert.Empty(project.Model.Dopings);
        Assert.Empty(project.Model.Simulations);
        Assert.Equal(preStep06ModelHash, ProjectHasher.Compute(project));
        Assert.Equal(preStep06DocumentHash, ProjectHasher.ComputeDocument(project));
    }

    [Fact]
    public void CombinedLatticeAndSimulationModelRoundTripsCanonically()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var oxygen = model.Particles.Single(static particle => particle.Name == "O2-");
        var vacancy = model.Particles.Single(static particle => particle.Name == "Vacancy");
        var oxygenSite = model.Structure.Sites.Single(static site => site.Name == "O");
        var exchange = new OccupationExchange(
            OccupationExchangeId.New(),
            "O2- -> VO",
            oxygenSite.Id,
            oxygen.Id,
            vacancy.Id);
        var doping = new Doping(
            DopingId.New(),
            "O vacancy doping",
            0,
            exchange.Id,
            DependentExchangeId: null,
            ChargeCompensation: false);
        var project = TransitionTestModels.CreateProject(model with
        {
            OccupationExchanges = [exchange],
            Dopings = [doping],
            Simulations = [SimulationTestModels.CreateKinetic()],
        });

        Assert.Empty(ProjectValidator.Validate(project));

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Single(loaded.Model.OccupationExchanges);
        Assert.Single(loaded.Model.Dopings);
        Assert.Single(loaded.Model.Simulations);
    }

    [Fact]
    public void LatticeAndSimulationEditsChangeModelAndDocumentHashes()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var project = TransitionTestModels.CreateProject(model with
        {
            Simulations = [SimulationTestModels.CreateKinetic()],
        });
        var edited = project with
        {
            Model = project.Model with
            {
                Simulations = [project.Model.Simulations[0] with { TemperatureK = 800.0 }],
            },
        };

        Assert.NotEqual(ProjectHasher.Compute(project), ProjectHasher.Compute(edited));
        Assert.NotEqual(ProjectHasher.ComputeDocument(project), ProjectHasher.ComputeDocument(edited));
    }
}

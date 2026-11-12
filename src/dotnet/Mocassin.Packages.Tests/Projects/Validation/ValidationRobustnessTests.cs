// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Projects.Validation;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed class ValidationRobustnessTests
{
    [Fact]
    public void ValidationIsDeterministicAcrossRuns()
    {
        var project = Load("project-ceria.mocprj");
        var options = new ValidationOptions { IncludeStatistics = true };

        var first = new ProjectValidationPipeline().Validate(project, options: options);
        var second = new ProjectValidationPipeline().Validate(project, options: options);
        var third = new ProjectValidationPipeline().Validate(project, options: options);

        Assert.Equal(first.Diagnostics.ToArray(), second.Diagnostics.ToArray());
        Assert.Equal(first.Diagnostics.ToArray(), third.Diagnostics.ToArray());
        Assert.Equal(first.Statistics, second.Statistics);
        Assert.Equal(first.Statistics, third.Statistics);
        Assert.Equal(first.Summary, second.Summary);
        Assert.Equal(first.IsValid, second.IsValid);
    }

    [Fact]
    public void CancelledValidationThrowsWithoutAPartialResult()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new ProjectValidationPipeline().Validate(
                Load("project-ceria.mocprj"),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public void MixedEmptyAndMobileSimulationsStillEmitSim039()
    {
        var project = Load("project-simulations-populated.mocprj");
        var (model, emptyTransition) = AddEmptyDonorTransition(project.Model);
        var empty = new KineticSimulation
        {
            Id = SimulationId.New(),
            Name = "No transitions",
            TransitionIds = [],
        };
        var noMobile = new KineticSimulation
        {
            Id = SimulationId.New(),
            Name = "No mobile start",
            TransitionIds = [TransitionRef.From(emptyTransition.Id)],
        };
        var mixed = project with { Model = model with { Simulations = [empty, noMobile] } };

        var outcome = CreateValidator().Validate(mixed, new ValidationOptions(), CancellationToken.None);

        Assert.Contains(outcome.Diagnostics, static item => item.Code == Step07DiagnosticCodes.NoMobileStartParticle);
        Assert.DoesNotContain(
            outcome.Diagnostics,
            static item => item.Code == Step07DiagnosticCodes.KmcSimulationWithoutMobileStart);
    }

    [Fact]
    public void Enm018AllowsEqualityAtTheCutoff()
    {
        var project = Load("project-simulations-populated.mocprj");
        var atCutoff = project with
        {
            Model = project.Model with
            {
                StableEnvironment = new StableEnvironment(new LengthAngstrom(1.25), []),
            },
        };

        var result = new ProjectValidationPipeline().Validate(atCutoff);

        Assert.DoesNotContain(
            result.Diagnostics,
            static item => item.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
    }

    [Fact]
    public void FailedValidationDoesNotMutateTheProject()
    {
        var project = Load("project-ceria-validation-bad.mocprj");
        var modelHash = ProjectHasher.Compute(project);
        var documentHash = ProjectHasher.ComputeDocument(project);

        var result = new ProjectValidationPipeline().Validate(
            project,
            options: new ValidationOptions { IncludeStatistics = true });

        Assert.False(result.IsValid);
        Assert.Equal(modelHash, ProjectHasher.Compute(project));
        Assert.Equal(documentHash, ProjectHasher.ComputeDocument(project));
    }

    private static FeasibilityValidator CreateValidator() =>
        new(new StructurePreviewService(), new EnergyPreviewService(), new TransitionPreviewService());

    private static (ModelDefinition Model, KineticTransition Transition) AddEmptyDonorTransition(
        ModelDefinition model)
    {
        var group = new StateChangeGroup(StateChangeGroupId.New(), "Empty start group", []);
        var chain = new TransitionChain(
            TransitionChainId.New(),
            "Empty start chain",
            TransitionPattern.Custom,
            [group.Id],
            [],
            GeneratesReverse: false);
        var transition = new KineticTransition(
            KineticTransitionId.New(),
            "No donors",
            chain.Id,
            [new Mocassin.Core.Geometry.Fractional3(0.25, 0.25, 0.25), new Mocassin.Core.Geometry.Fractional3(0.5, 0.25, 0.25)],
            ReverseTransitionId: null);

        return (
            model with
            {
                StateChangeGroups = [.. model.StateChangeGroups, group],
                Chains = [.. model.Chains, chain],
                KineticTransitions = [.. model.KineticTransitions, transition],
            },
            transition);
    }

    private static ProjectModel Load(string name) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", name)));
}

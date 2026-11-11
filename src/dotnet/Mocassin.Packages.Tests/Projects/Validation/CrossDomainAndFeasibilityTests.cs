// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
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
public sealed class CrossDomainAndFeasibilityTests
{
    [Fact]
    public void Enm018FiresAtTheFirstHopBeyondTheEnvironmentCutoff()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");

        var result = new ProjectValidationPipeline().Validate(project);

        var diagnostic = Assert.Single(
            result.Diagnostics,
            static item => item.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("kineticTransition", diagnostic.Path?.Domain);
        Assert.Equal(project.Model.KineticTransitions[0].Id.Value, diagnostic.Path?.ObjectId);
        Assert.Equal("referencePath[0]", diagnostic.Path?.Property);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Hint));
    }

    [Fact]
    public void Enm018IsSilentWhenTheCutoffCoversThePath()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var widened = project with
        {
            Model = project.Model with
            {
                StableEnvironment = new StableEnvironment(new LengthAngstrom(10.0), []),
            },
        };

        var result = new ProjectValidationPipeline().Validate(widened);

        Assert.DoesNotContain(
            result.Diagnostics,
            static item => item.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
    }

    [Fact]
    public void Enm018IsSuppressedAfterAPathResolutionError()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var transition = project.Model.KineticTransitions[0];
        var repeated = transition with
        {
            ReferencePath = [transition.ReferencePath[0], transition.ReferencePath[0], transition.ReferencePath[1]],
        };
        var broken = project with
        {
            Model = project.Model with { KineticTransitions = [repeated] },
        };

        var result = new ProjectValidationPipeline().Validate(broken);

        Assert.Equal(ValidationStageStatus.Failed, result.Stages[1].Status);
        Assert.DoesNotContain(
            result.Diagnostics,
            static item => item.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
    }

    [Fact]
    public void MobileKmcSimulationDoesNotEmitTheBuildabilityCodes()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");

        var result = new ProjectValidationPipeline().Validate(project);

        Assert.DoesNotContain(result.Diagnostics, static item => item.Code == Step07DiagnosticCodes.KmcSimulationWithoutMobileStart);
        Assert.DoesNotContain(result.Diagnostics, static item => item.Code == Step07DiagnosticCodes.NoMobileStartParticle);
    }

    [Fact]
    public void MmcSimulationsAreExcludedFromTheBuildabilityCodes()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var mmcOnly = project with
        {
            Model = project.Model with
            {
                Simulations = [.. project.Model.Simulations.Where(static simulation => simulation.Kind == SimulationKind.Mmc)],
            },
        };

        var result = new ProjectValidationPipeline().Validate(mmcOnly);

        Assert.DoesNotContain(result.Diagnostics, static item => item.Code == Step07DiagnosticCodes.KmcSimulationWithoutMobileStart);
        Assert.DoesNotContain(result.Diagnostics, static item => item.Code == Step07DiagnosticCodes.NoMobileStartParticle);
    }

    [Fact]
    public void Sim038IsEmittedPerSimulationWhenAnotherSimulationHasMobileStarts()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var (model, emptyTransition) = AddEmptyDonorTransition(project.Model);
        var second = new KineticSimulation
        {
            Id = SimulationId.New(),
            Name = "No mobile start",
            TransitionIds = [TransitionRef.From(emptyTransition.Id)],
        };
        var withSecond = project with
        {
            Model = model with { Simulations = [.. model.Simulations, second] },
        };

        var outcome = CreateValidator().Validate(withSecond, new ValidationOptions(), CancellationToken.None);

        var diagnostic = Assert.Single(
            outcome.Diagnostics,
            static item => item.Code == Step07DiagnosticCodes.KmcSimulationWithoutMobileStart);
        Assert.Equal(second.Id.Value, diagnostic.Path?.ObjectId);
        Assert.DoesNotContain(outcome.Diagnostics, static item => item.Code == Step07DiagnosticCodes.NoMobileStartParticle);
    }

    [Fact]
    public void Sim039SuppressesSim038WhenNoSimulationHasMobileStarts()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var (model, emptyTransition) = AddEmptyDonorTransition(project.Model);
        var onlyKmc = new KineticSimulation
        {
            Id = SimulationId.New(),
            Name = "No mobile start",
            TransitionIds = [TransitionRef.From(emptyTransition.Id)],
        };
        var withOnly = project with
        {
            Model = model with { Simulations = [onlyKmc] },
        };

        var outcome = CreateValidator().Validate(withOnly, new ValidationOptions(), CancellationToken.None);

        Assert.Contains(outcome.Diagnostics, static item => item.Code == Step07DiagnosticCodes.NoMobileStartParticle);
        Assert.DoesNotContain(
            outcome.Diagnostics,
            static item => item.Code == Step07DiagnosticCodes.KmcSimulationWithoutMobileStart);
    }

    [Fact]
    public void RequireBuildablePromotesTheBuildabilityWarnings()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");

        var relaxed = new ProjectValidationPipeline().Validate(project);
        var strict = new ProjectValidationPipeline().Validate(
            project,
            options: new ValidationOptions { RequireBuildable = true });

        Assert.Contains(relaxed.Diagnostics, static item => item.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
        Assert.True(relaxed.IsValid);
        Assert.False(strict.IsValid);
        Assert.Equal(relaxed.Summary, strict.Summary);
        Assert.Equal(relaxed.Diagnostics.ToArray(), strict.Diagnostics.ToArray());
    }

    [Fact]
    public void BudgetErrorsKeepStatisticsAndDoNotDependOnTheStatisticsOption()
    {
        var project = LoadFixture("project-simulations-populated.mocprj");
        var tight = new ValidationOptions
        {
            MaxEstimatedRuleCount = 1,
            MaxEstimatedMemoryBytes = -1,
        };

        var without = new ProjectValidationPipeline().Validate(project, options: tight);
        var with = new ProjectValidationPipeline().Validate(
            project,
            options: tight with { IncludeStatistics = true });

        Assert.Contains(without.Diagnostics, static item => item.Code == Step07DiagnosticCodes.EstimatedRuleCountExceeded);
        Assert.Contains(without.Diagnostics, static item => item.Code == Step07DiagnosticCodes.EstimatedMemoryExceeded);
        Assert.Equal(without.Diagnostics.ToArray(), with.Diagnostics.ToArray());
        Assert.Equal(without.IsValid, with.IsValid);
        Assert.Null(without.Statistics);
        Assert.NotNull(with.Statistics);
        Assert.False(with.IsValid);
        Assert.Equal(ValidationStageStatus.Failed, with.Stages[3].Status);
    }

    private static FeasibilityValidator CreateValidator() =>
        new(new StructurePreviewService(), new EnergyPreviewService(), new TransitionPreviewService());

    private static (ModelDefinition Model, KineticTransition Transition) AddEmptyDonorTransition(ModelDefinition model)
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
            [new Fractional3(0.25, 0.25, 0.25), new Fractional3(0.5, 0.25, 0.25)],
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

    private static ProjectModel LoadFixture(string name) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", name)));
}

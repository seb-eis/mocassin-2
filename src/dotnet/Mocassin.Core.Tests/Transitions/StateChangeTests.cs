// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class StateChangeTests
{
    [Fact]
    public void ValidStateChangesAndGroupsProduceNoDiagnostics()
    {
        Assert.Empty(TransitionModelValidator.Validate(TransitionTestModels.CreateCombinedModel()));
    }

    [Fact]
    public void UnknownParticleReferenceIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var invalid = model.StateChanges[0] with { DonorParticleId = ParticleId.New() };

        var diagnostics = TransitionModelValidator.Validate(WithStateChanges(model, [invalid, .. model.StateChanges.Skip(1)]));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnknownParticle);
    }

    [Fact]
    public void DonorEqualsAcceptorIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var stateChange = model.StateChanges[0];
        var invalid = stateChange with { AcceptorParticleId = stateChange.DonorParticleId };

        var diagnostics = TransitionModelValidator.Validate(WithStateChanges(model, [invalid, .. model.StateChanges.Skip(1)]));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.DonorEqualsAcceptor);
    }

    [Fact]
    public void BothVoidParticlesAreReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var invalid = model.StateChanges[0] with
        {
            DonorParticleId = ParticleId.Void,
            AcceptorParticleId = ParticleId.Void,
        };

        var diagnostics = TransitionModelValidator.Validate(WithStateChanges(model, [invalid, .. model.StateChanges.Skip(1)]));

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.BothParticlesVoid);
    }

    [Fact]
    public void ChargedAcceptorFromVoidIsAWarning()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var oxygenId = model.Particles[0].Id;
        var invalid = model.StateChanges[0] with
        {
            DonorParticleId = ParticleId.Void,
            AcceptorParticleId = oxygenId,
        };

        var diagnostics = TransitionModelValidator.Validate(WithStateChanges(model, [invalid, .. model.StateChanges.Skip(1)]));

        var diagnostic = Assert.Single(
            diagnostics,
            static item => item.Code == TransitionDiagnosticCodes.ChargedAcceptorFromVoid);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void EmptyGroupAndUnknownGroupMemberAreReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var empty = model.StateChangeGroups[0] with { StateChangeIds = [] };
        var unknown = model.StateChangeGroups[1] with { StateChangeIds = [StateChangeId.New()] };

        var diagnostics = TransitionModelValidator.Validate(
            WithGroups(model, [empty, unknown, .. model.StateChangeGroups.Skip(2)]));

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidGroup));
    }

    [Fact]
    public void MixedPhysicalAndChargeStateGroupIsReported()
    {
        var oxygen = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f10", "O2-", "O", -2.0);
        var vacancy = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f11", "Vacancy", "Vc", 0.0, isVacancy: true);
        var iron2 = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f18", "Fe2+", "Fe", 2.0);
        var iron3 = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f19", "Fe3+", "Fe", 3.0);
        var physical = new StateChange(StateChangeId.New(), "O to vacancy", oxygen.Id, vacancy.Id);
        var chargeState = new StateChange(StateChangeId.New(), "Fe2 to Fe3", iron2.Id, iron3.Id);
        var group = new StateChangeGroup(StateChangeGroupId.New(), "Mixed", [physical.Id, chargeState.Id]);
        var model = new ModelDefinition
        {
            Particles = [oxygen, vacancy, iron2, iron3],
            StateChanges = [physical, chargeState],
            StateChangeGroups = [group],
        };

        var diagnostics = TransitionModelValidator.Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.MixedGroupExchange);
    }

    [Fact]
    public void DuplicateStateChangeAndGroupNamesAreReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var duplicateStateChange = model.StateChanges[1] with { Name = model.StateChanges[0].Name };
        var duplicateGroup = model.StateChangeGroups[1] with { Name = model.StateChangeGroups[0].Name };
        var altered = WithGroups(
            WithStateChanges(model, [model.StateChanges[0], duplicateStateChange, .. model.StateChanges.Skip(2)]),
            [model.StateChangeGroups[0], duplicateGroup, .. model.StateChangeGroups.Skip(2)]);

        var diagnostics = TransitionModelValidator.Validate(altered);

        Assert.Equal(
            2,
            diagnostics.Count(static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.DuplicateName));
    }

    [Fact]
    public void MixedStableRoleAndUnstableRoleGroupIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var stable = model.StateChanges[0];
        var unstable = model.StateChanges[1] with
        {
            DonorParticleId = model.Particles[0].Id,
            AcceptorParticleId = ParticleId.Void,
        };
        var group = model.StateChangeGroups[0] with { StateChangeIds = [stable.Id, unstable.Id] };
        var altered = WithGroups(
            WithStateChanges(model, [stable, unstable, .. model.StateChanges.Skip(2)]),
            [group, .. model.StateChangeGroups.Skip(1)]);

        var diagnostics = TransitionModelValidator.Validate(altered);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.MixedGroupStability);
    }

    [Fact]
    public void ProjectValidatorSurfacesTransitionDiagnostics()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var invalid = model.StateChanges[0] with { DonorParticleId = ParticleId.New() };
        var project = TransitionTestModels.CreateProject(
            WithStateChanges(model, [invalid, .. model.StateChanges.Skip(1)]));

        var diagnostics = ProjectValidator.Validate(project);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnknownParticle);
    }

    private static ModelDefinition WithStateChanges(
        ModelDefinition model,
        IEnumerable<StateChange> stateChanges) =>
        model with { StateChanges = [.. stateChanges] };

    private static ModelDefinition WithGroups(
        ModelDefinition model,
        IEnumerable<StateChangeGroup> groups) =>
        model with { StateChangeGroups = [.. groups] };

    private static Particle CreateParticle(
        string id,
        string name,
        string symbol,
        double charge,
        bool isVacancy = false) =>
        new(
            ParticleId.Create(Guid.Parse(id)),
            name,
            symbol,
            new ChargeElementary(charge),
            isVacancy);
}

// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for the dense site-energy background, overrides, and reset-to-model.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationSiteEnergyTests
{
    [Fact]
    public async Task AuthoredBackgroundIsCopiedToEveryOwnedPosition()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var result = new ParameterizationService().Apply(project, model);

        Assert.True(result.IsSuccess);
        var applied = result.Model!;
        AssertAuthoredBackground(applied, project, model);
    }

    [Fact]
    public async Task OverrideReplacesEveryOwnedPositionAndResetRestoresTheAuthoredValue()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var authoredSite = model.Structure.Sites.Single(
            site => site.Id == CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f06")));
        var oxygen = model.Particles.IndexById[
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f01"))];
        var overrideEntry = Assert.Single(template.SiteEnergyOverrides);
        Assert.Equal(0.1, overrideEntry.Energy.Value);

        var applied = new ParameterizationService().Apply(project, model, template).Model!;
        foreach (var positionIndex in authoredSite.PositionIndices)
        {
            Assert.Equal(0.1, SiteEnergy(applied, model, positionIndex, oxygen));
        }

        var reset = ParameterizationTemplateEditor.RemoveSiteEnergyOverride(
            template,
            overrideEntry.SiteId,
            overrideEntry.ParticleId);
        Assert.Empty(reset.Diagnostics);
        var restored = new ParameterizationService().Apply(project, model, reset.Template).Model!;
        foreach (var positionIndex in authoredSite.PositionIndices)
        {
            Assert.Equal(-0.5, SiteEnergy(restored, model, positionIndex, oxygen));
        }
    }

    [Fact]
    public async Task InvalidOverrideTargetsAreWarningsAndLeaveTheBackgroundUnchanged()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var baseline = new ParameterizationService().Apply(project, model, template).Model!;
        var unknownSite = CellSiteId.Create(Guid.Parse("11111111-1111-7111-8111-111111111111"));
        var unknownParticle = ParticleId.Create(Guid.Parse("22222222-2222-7222-8222-222222222222"));
        var edited = template with
        {
            SiteEnergyOverrides =
            [
                .. template.SiteEnergyOverrides,
                new SiteEnergyOverride(unknownSite, unknownParticle, new EnergyEv(1.0)),
                new SiteEnergyOverride(template.SiteEnergyOverrides[0].SiteId, unknownParticle, new EnergyEv(2.0)),
            ],
        };

        var result = new ParameterizationService().Apply(project, model, edited);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Model);
        Assert.Equal(2, result.Diagnostics.Count(
            item => item.Code == ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget));
        Assert.All(result.Diagnostics, item => Assert.Equal(DiagnosticSeverity.Warning, item.Severity));
        Assert.Equal(baseline.Values.SiteEnergies.AsEnumerable(), result.Model!.Values.SiteEnergies.AsEnumerable());
    }

    [Fact]
    public async Task StaleAuthoredBackgroundEntryIsIgnoredWithPrm008()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var baseline = new ParameterizationService().Apply(project, model).Model!;
        var unknownSite = CellSiteId.Create(Guid.Parse("33333333-3333-7333-8333-333333333333"));
        var unknownParticle = ParticleId.Create(Guid.Parse("44444444-4444-7444-8444-444444444444"));
        var stale = project with
        {
            Model = project.Model with
            {
                SiteEnergies =
                [
                    .. project.Model.SiteEnergies,
                    new SiteEnergySet(
                        unknownSite,
                        [new SiteEnergyEntry(unknownParticle, new EnergyEv(3.0))]),
                ],
            },
        };

        var result = new ParameterizationService().Apply(stale, model);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(baseline.Values.SiteEnergies.AsEnumerable(), result.Model!.Values.SiteEnergies.AsEnumerable());
    }

    [Fact]
    public async Task ChangingOnlyTheAuthoredBackgroundChangesTheParameterizationSignature()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var service = new ParameterizationService();
        var baseline = service.Apply(project, model).Model!;
        var set = Assert.Single(project.Model.SiteEnergies);
        var entry = set.Entries[0];
        var changed = project with
        {
            Model = project.Model with
            {
                SiteEnergies =
                [
                    new SiteEnergySet(
                        set.SiteId,
                        [new SiteEnergyEntry(entry.ParticleId, new EnergyEv(-0.25))]),
                ],
            },
        };

        var result = service.Apply(changed, model);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(baseline.Signature, result.Model!.Signature);
        Assert.Equal(baseline.ModelSignature, result.Model.ModelSignature);
    }

    private static void AssertAuthoredBackground(
        ParameterizedModel applied,
        Mocassin.Core.Project.Project project,
        Mocassin.Compilation.Compiler.CompiledModel model)
    {
        foreach (var position in model.Structure.Positions)
        {
            Assert.Equal(0.0, applied.Values.SiteEnergies[position.P * model.Particles.Count]);
        }

        foreach (var set in project.Model.SiteEnergies)
        {
            var site = model.Structure.Sites.Single(candidate => candidate.Id == set.SiteId);
            var owned = site.PositionIndices.ToHashSet();
            foreach (var entry in set.Entries)
            {
                var particle = model.Particles.IndexById[entry.ParticleId];
                foreach (var position in model.Structure.Positions)
                {
                    var expected = owned.Contains(position.P) ? entry.Energy.Value : 0.0;
                    Assert.Equal(expected, SiteEnergy(applied, model, position.P, particle));
                }
            }
        }
    }

    private static double SiteEnergy(
        ParameterizedModel applied,
        Mocassin.Compilation.Compiler.CompiledModel model,
        int positionIndex,
        int particleIndex) =>
        applied.Values.SiteEnergies[(positionIndex * model.Particles.Count) + particleIndex];
}

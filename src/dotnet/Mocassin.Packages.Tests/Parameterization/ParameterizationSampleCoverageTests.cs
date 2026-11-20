// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Exercises creation and description over every checked-in sample project, including the reciprocal chiral
/// vehicle and interstitialcy models.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationSampleCoverageTests
{
    [Theory]
    [InlineData("samples/blank/blank.mocprj")]
    [InlineData("samples/ceria/ceria.mocprj")]
    [InlineData("samples/barium-zirconate/barium-zirconate.mocprj")]
    [InlineData("samples/lattice/bzo-doping.mocprj")]
    [InlineData("samples/lattice/ceria-hydration.mocprj")]
    [InlineData("samples/lattice/ceria-reduction.mocprj")]
    [InlineData("samples/parameterizations/ceria-parameters.mocprj")]
    [InlineData("samples/parameterizations/barium-zirconate-parameters.mocprj")]
    [InlineData("samples/simulations/centered-exchange.mocprj")]
    [InlineData("samples/simulations/ceria-kmc.mocprj")]
    [InlineData("samples/simulations/ceria-mmc.mocprj")]
    [InlineData("samples/transitions/ceria-kmc.mocprj")]
    [InlineData("samples/transitions/ceria-mmc.mocprj")]
    [InlineData("samples/transitions/interstitialcy5.mocprj")]
    [InlineData("samples/transitions/polaron-hop.mocprj")]
    [InlineData("samples/transitions/vehicle5.mocprj")]
    public void CreateAndDescribeEverySample(string path)
    {
        var project = ParameterizationTestSupport.LoadProject(path);
        var compilation = new ModelCompiler().Compile(project);
        Assert.True(
            compilation.IsSuccess,
            string.Join(Environment.NewLine, compilation.Diagnostics.Select(static item => item.ToString())));
        var model = compilation.Model!;

        var result = new ParameterizationFactory().Create(
            project,
            model,
            ParameterizationTemplateOptions.Default);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(model.ModelSignature, result.Template.ModelSignature);
        Assert.Equal(model.Energy.PairTableCount, result.Template.StablePairSets.Length + result.Template.UnstablePairSets.Length);
        Assert.Equal(model.Energy.ClusterTableCount, result.Template.GroupSets.Length);
        Assert.Equal(
            model.Transitions.Collections.Count(
                static collection => collection.Transition.Kind == Mocassin.Core.Model.Transitions.TransitionKind.Kinetic),
            result.Template.TransitionRuleSets.Length);
        Assert.False(result.Summary.HasExplicitValues);
        Assert.Equal(
            result.Summary.RequiredSlotCount,
            result.Summary.PairSlotCount + result.Summary.GroupSlotCount + result.Summary.KineticRuleSlotCount);
        Assert.Equal(result.Summary, ParameterizationFactory.Describe(project, model));

        var sets = result.Template.StablePairSets.AddRange(result.Template.UnstablePairSets);
        foreach (var set in sets.Where(static item => item.IsChiral))
        {
            var table = model.Energy.PairTables.Single(candidate => candidate.EnergyId == set.EnergyId);
            var representative = Assert.Single(
                model.Energy.PairGeometries,
                geometry => geometry.PairTableIndex == table.Index &&
                            string.Equals(geometry.GeometryId, table.EnergyId, StringComparison.Ordinal));
            Assert.Equal(representative.CenterSiteId, set.CenterSiteId);
            Assert.Equal(representative.PartnerSiteId, set.PartnerSiteId);
            Assert.Equal(representative.DistanceBucketFm, set.DistanceBucketFm);
        }
    }
}

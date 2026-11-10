// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class EnergyDiagnosticCodeTests
{
    private static readonly string[] codes =
    [
        EnergyDiagnosticCodes.InvalidCutoff,
        EnergyDiagnosticCodes.PeriodicAmbiguity,
        EnergyDiagnosticCodes.InvalidFilterRange,
        EnergyDiagnosticCodes.InvalidFilterSites,
        EnergyDiagnosticCodes.DuplicateFilter,
        EnergyDiagnosticCodes.InvalidUnstableEnvironment,
        EnergyDiagnosticCodes.InvalidClusterCenter,
        EnergyDiagnosticCodes.InvalidClusterSize,
        EnergyDiagnosticCodes.DuplicateClusterOffset,
        EnergyDiagnosticCodes.InvalidClusterPosition,
        EnergyDiagnosticCodes.DuplicateClusterGeometry,
        EnergyDiagnosticCodes.ClusterPermutationWarning,
        EnergyDiagnosticCodes.ClusterPermutationLimit,
        EnergyDiagnosticCodes.InvalidSiteEnergy,
        EnergyDiagnosticCodes.SiteEnergyOutOfRange,
        EnergyDiagnosticCodes.ForbiddenSiteEnergy,
        EnergyDiagnosticCodes.EnvironmentSamplingWarning,
        EnergyDiagnosticCodes.KmcPathOutsideEnvironment,
        EnergyDiagnosticCodes.EnvironmentSamplingLimit,
        EnergyDiagnosticCodes.ClustersPerCenterWarning,
        EnergyDiagnosticCodes.ClustersPerCenterLimit,
    ];

    [Fact]
    public void AllEnergyCodesAreUniqueAndHaveHelpText()
    {
        Assert.Equal(21, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(21, codes.Length);

        for (var index = 0; index < codes.Length; index++)
        {
            Assert.Equal($"ENM{index + 1:000}", codes[index]);
            Assert.False(string.IsNullOrWhiteSpace(EnergyDiagnosticCodes.HelpFor(codes[index])));
        }
    }

    [Fact]
    public void UnknownCodeHasNoHelpText()
    {
        Assert.Null(EnergyDiagnosticCodes.HelpFor("ENM999"));
        Assert.Null(EnergyDiagnosticCodes.HelpFor("PAR001"));
    }

    [Fact]
    public void Enm018IsDefinedButNotEmittedByStep05Validation()
    {
        var diagnostics = ProjectValidator.Validate(
            EnergyTestModels.CreateProject(EnergyTestModels.CreateCombinedModel()));

        Assert.Equal("ENM018", EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.KmcPathOutsideEnvironment);
    }
}

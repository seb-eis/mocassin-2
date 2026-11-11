// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Lattice;

[Trait("Tier", "PR-fast")]
public sealed class LatticeDiagnosticCodeTests
{
    private static readonly string[] codes =
    [
        LatticeDiagnosticCodes.UnknownReference,
        LatticeDiagnosticCodes.UnstableSite,
        LatticeDiagnosticCodes.SourceParticleNotInSet,
        LatticeDiagnosticCodes.TargetParticleNotInSet,
        LatticeDiagnosticCodes.SourceEqualsTarget,
        LatticeDiagnosticCodes.DuplicatePriority,
        LatticeDiagnosticCodes.UnknownExchange,
        LatticeDiagnosticCodes.CompensationShape,
        LatticeDiagnosticCodes.CompensationCharge,
        LatticeDiagnosticCodes.PriorityOutOfRange,
        LatticeDiagnosticCodes.InvalidName,
    ];

    [Fact]
    public void AllLatticeCodesAreUniqueAndHaveHelpText()
    {
        Assert.Equal(11, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(11, codes.Length);

        for (var index = 0; index < codes.Length; index++)
        {
            Assert.Equal($"LAT{index + 1:000}", codes[index]);
            Assert.False(string.IsNullOrWhiteSpace(LatticeDiagnosticCodes.HelpFor(codes[index])));
        }
    }

    [Fact]
    public void UnknownCodeHasNoHelpText()
    {
        Assert.Null(LatticeDiagnosticCodes.HelpFor("LAT999"));
        Assert.Null(LatticeDiagnosticCodes.HelpFor("SIM001"));
    }

    [Fact]
    public void ValidLatticeModelDoesNotEmitAnyLatticeCode()
    {
        var diagnostics = LatticeModelValidator.Validate(LatticeTestModels.CreateLatticeModel());

        Assert.DoesNotContain(
            diagnostics,
            diagnostic => diagnostic.Code.StartsWith("LAT", StringComparison.Ordinal));
    }
}

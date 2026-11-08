// SPDX-License-Identifier: MIT

namespace Mocassin.Symmetry.Tests;

/// <summary>
/// Smoke tests proving that the Mocassin.Symmetry.Tests test project is wired up.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SmokeTests
{
    /// <summary>
    /// Verifies that the test runner executes a trivial assertion.
    /// </summary>
    [Fact]
    public void SmokeTest()
    {
        Assert.True(true);
    }
}

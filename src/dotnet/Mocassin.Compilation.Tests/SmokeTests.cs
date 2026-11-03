// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Tests;

/// <summary>
/// Smoke tests proving that the Mocassin.Compilation.Tests test project is wired up.
/// </summary>
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

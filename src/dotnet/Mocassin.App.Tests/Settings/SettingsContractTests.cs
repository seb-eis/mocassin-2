// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies that the application settings stay outside the shared format manifest.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SettingsContractTests
{
    [Fact]
    public void TheSettingsFormatIsNotInTheContractManifest()
    {
        var manifest = File.ReadAllText(RepositoryPaths.Combine("contracts", "manifest.json"));

        Assert.DoesNotContain("mocassin.settings", manifest, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mocassin.Settings.json", manifest, StringComparison.OrdinalIgnoreCase);
    }
}

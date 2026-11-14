// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Pins the package-level catalog composition and the documentation drift for the <c>PRM</c> family.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationDiagnosticCatalogTests
{
    [Fact]
    public void DefaultCatalogContainsEveryParameterizationCodeExactlyOnce()
    {
        var expected = ParameterizationDiagnosticCatalog.Descriptors
            .Select(static descriptor => descriptor.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var actual = DiagnosticCatalog.Default.All
            .Where(static descriptor => descriptor.Code.StartsWith("PRM", StringComparison.Ordinal))
            .Select(static descriptor => descriptor.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(actual.Length, actual.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain("PRM007", actual);
        var rebase = Assert.Single(
            DiagnosticCatalog.Default.All,
            static descriptor => descriptor.Code == "PRM009");
        Assert.Equal(DiagnosticSeverity.Info, rebase.DefaultSeverity);
    }

    [Fact]
    public void EveryParameterizationDescriptorHasAReferenceRowAndHeading()
    {
        var text = File.ReadAllText(RepositoryPaths.Combine("docs", "reference", "diagnostics.md"));

        Assert.All(
            ParameterizationDiagnosticCatalog.Descriptors,
            descriptor =>
            {
                Assert.Equal("Step 09 — parameterization templates", descriptor.Owner);
                Assert.StartsWith("#prm", descriptor.DocumentationAnchor, StringComparison.Ordinal);
                Assert.Contains($"### {descriptor.Code}", text, StringComparison.Ordinal);
                Assert.Contains(
                    $"[`{descriptor.Code}`](#{descriptor.DocumentationAnchor.TrimStart('#')})",
                    text,
                    StringComparison.Ordinal);
            });
    }
}

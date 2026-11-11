// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed partial class DiagnosticDocumentationTests
{
    [Fact]
    public void ReferencePageMatchesTheCatalogExactly()
    {
        var path = RepositoryPaths.Combine("docs", "reference", "diagnostics.md");
        var text = File.ReadAllText(path);
        var documented = CodePattern().Matches(text)
            .Select(static match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
        var catalog = DiagnosticCatalog.Default.All
            .Select(static descriptor => descriptor.Code)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(documented.Except(catalog));
        Assert.Empty(catalog.Except(documented));
    }

    [Fact]
    public void EveryCatalogCodeHasAHeading()
    {
        var text = File.ReadAllText(RepositoryPaths.Combine("docs", "reference", "diagnostics.md"));

        foreach (var descriptor in DiagnosticCatalog.Default.All)
        {
            Assert.Contains($"### {descriptor.Code}", text, StringComparison.Ordinal);
        }
    }

    [GeneratedRegex(@"\b[A-Z]{3,4}\d{3}\b")]
    private static partial Regex CodePattern();
}

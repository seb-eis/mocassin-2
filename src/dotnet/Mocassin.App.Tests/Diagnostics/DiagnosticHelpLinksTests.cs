// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Diagnostics;

/// <summary>
/// Verifies the diagnostic help map against the catalog and the published reference anchors.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class DiagnosticHelpLinksTests
{
    [Fact]
    public void KnownCodesResolveAndUnknownCodesStayUnlinked()
    {
        Assert.Equal(
            DiagnosticHelpLinks.ReferenceBase + "#proj001",
            DiagnosticHelpLinks.Resolve("PROJ001"));
        Assert.Null(DiagnosticHelpLinks.Resolve("PROJ999"));
        Assert.Null(DiagnosticHelpLinks.Resolve(null));
        Assert.Null(DiagnosticHelpLinks.Resolve(" "));
    }

    [Fact]
    public void EveryCatalogAnchorExistsInTheReferencePage()
    {
        var docs = File.ReadAllText(RepositoryPaths.Combine("docs", "reference", "diagnostics.md"));
        var headings = Regex.Matches(docs, @"^### ([A-Z]{3,4}[0-9]{3})\s*$", RegexOptions.Multiline)
            .Select(match => "#" + match.Groups[1].Value.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var descriptor in DiagnosticCatalog.Default.All)
        {
            if (string.IsNullOrWhiteSpace(descriptor.DocumentationAnchor))
            {
                Assert.Null(DiagnosticHelpLinks.Resolve(descriptor.Code));
                continue;
            }

            Assert.Equal(
                DiagnosticHelpLinks.ReferenceBase + descriptor.DocumentationAnchor,
                DiagnosticHelpLinks.Resolve(descriptor.Code));
            if (!headings.Contains(descriptor.DocumentationAnchor))
            {
                missing.Add($"{descriptor.Code} -> {descriptor.DocumentationAnchor}");
            }
        }

        Assert.True(
            missing.Count == 0,
            "Diagnostic anchors are missing from docs/reference/diagnostics.md: " + string.Join(", ", missing));
    }
}

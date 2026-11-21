// SPDX-License-Identifier: MIT

using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// Resolves a diagnostic code to the published reference anchor, using the catalog's documented anchor.
/// </summary>
internal static class DiagnosticHelpLinks
{
    /// <summary>
    /// The published diagnostics reference root (the <c>site_url</c> from <c>zensical.toml</c>).
    /// </summary>
    public const string ReferenceBase = "https://seb-eis.github.io/mocassin-2/reference/diagnostics/";

    /// <summary>
    /// Resolves a known code to its documentation URI.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The URI, or <see langword="null"/> for an unknown or undocumented code.</returns>
    public static string? Resolve(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !DiagnosticCatalog.Default.TryGet(code, out var descriptor))
        {
            return null;
        }

        var anchor = descriptor.DocumentationAnchor;
        if (string.IsNullOrWhiteSpace(anchor))
        {
            return null;
        }

        return anchor.StartsWith('#') ? ReferenceBase + anchor : ReferenceBase + "#" + anchor;
    }
}

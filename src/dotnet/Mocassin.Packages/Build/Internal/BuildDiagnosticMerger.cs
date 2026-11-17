// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Build.Internal;

/// <summary>
/// Merges stage diagnostics into the authoritative, sorted, deduplicated build result.
/// </summary>
/// <remarks>
/// Exact <c>(code, path, message)</c> duplicates are removed, matching the CLI report contract. Ordering is the
/// existing severity-first diagnostic order.
/// </remarks>
internal static class BuildDiagnosticMerger
{
    /// <summary>
    /// Merges diagnostics from several sources.
    /// </summary>
    /// <param name="sources">The diagnostic sources in stage order.</param>
    /// <returns>The merged diagnostics.</returns>
    public static ImmutableArray<Diagnostic> Merge(params IEnumerable<Diagnostic>[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var bag = new DiagnosticBag();
        foreach (var source in sources)
        {
            if (source is not null)
            {
                bag.AddRange(source);
            }
        }

        var seen = new HashSet<(string Code, ModelPath? Path, string Message)>();
        var builder = ImmutableArray.CreateBuilder<Diagnostic>();
        foreach (var diagnostic in bag.Sorted)
        {
            if (seen.Add((diagnostic.Code, diagnostic.Path, diagnostic.Message)))
            {
                builder.Add(diagnostic);
            }
        }

        return builder.ToImmutable();
    }
}

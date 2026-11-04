// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Accumulates diagnostics and exposes stable, severity-first views for presentation and tests.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> diagnostics = [];

    /// <summary>
    /// Initializes an empty diagnostic bag.
    /// </summary>
    public DiagnosticBag()
    {
    }

    /// <summary>
    /// Initializes a diagnostic bag from an existing sequence.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to copy.</param>
    public DiagnosticBag(IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        this.diagnostics.AddRange(diagnostics);
    }

    /// <summary>
    /// Gets the number of diagnostics in the bag.
    /// </summary>
    public int Count => diagnostics.Count;

    /// <summary>
    /// Gets a value indicating whether the bag contains at least one error.
    /// </summary>
    public bool HasErrors => diagnostics.Exists(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets all diagnostics in stable order.
    /// </summary>
    public ImmutableArray<Diagnostic> Sorted => Sort(diagnostics);

    /// <summary>
    /// Gets the errors in stable order.
    /// </summary>
    public ImmutableArray<Diagnostic> Errors => Sort(diagnostics.FindAll(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

    /// <summary>
    /// Gets the warnings in stable order.
    /// </summary>
    public ImmutableArray<Diagnostic> Warnings => Sort(diagnostics.FindAll(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning));

    /// <summary>
    /// Gets the informational messages in stable order.
    /// </summary>
    public ImmutableArray<Diagnostic> Infos => Sort(diagnostics.FindAll(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Info));

    /// <summary>
    /// Adds a diagnostic to the bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic"/> is <see langword="null"/>.</exception>
    public void Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds a sequence of diagnostics to the bag.
    /// </summary>
    /// <param name="items">The diagnostics to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    public void AddRange(IEnumerable<Diagnostic> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        diagnostics.AddRange(items);
    }

    /// <summary>
    /// Removes all diagnostics from the bag.
    /// </summary>
    public void Clear() => diagnostics.Clear();

    private static ImmutableArray<Diagnostic> Sort(List<Diagnostic> items)
    {
        return
        [
            .. items
                .OrderBy(static diagnostic => SeverityRank(diagnostic.Severity))
                .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(static diagnostic => diagnostic.Path?.Domain ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(static diagnostic => diagnostic.Path?.ObjectId ?? Guid.Empty)
                .ThenBy(static diagnostic => diagnostic.Path?.Property ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal)
        ];
    }

    private static int SeverityRank(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 0,
        DiagnosticSeverity.Warning => 1,
        DiagnosticSeverity.Info => 2,
        _ => 3,
    };
}

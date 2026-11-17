// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Mocassin.Packages.Build;

/// <summary>
/// Parses and normalizes the 1-based full-expansion job selection.
/// </summary>
/// <remarks>
/// The grammar is <c>all</c> or a comma-separated list of <c>N</c>/<c>A-B</c> items. Duplicates are removed,
/// overlapping and adjacent ranges are merged, and the normal form is <c>all</c> when every expanded job is
/// selected, otherwise the sorted merged form without spaces (<c>1-3,5,7-9</c>). Malformed, empty, zero,
/// descending, and out-of-range selections return an error; the library never renumbers the unselected jobs.
/// </remarks>
public static class JobSelection
{
    /// <summary>
    /// Parses one selection expression against the complete template expansion.
    /// </summary>
    /// <param name="expression">The selection expression.</param>
    /// <param name="expandedCount">The complete template expansion count.</param>
    /// <returns>The normalized selection or a parse error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
    public static JobSelectionResult Parse(string expression, int expandedCount)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var text = expression.Trim();
        if (expandedCount < 1)
        {
            return Failed(expression, expandedCount, "The job template expands to no jobs.");
        }

        if (text.Length == 0)
        {
            return Failed(expression, expandedCount, "The job selection is empty.");
        }

        if (string.Equals(text, "all", StringComparison.Ordinal))
        {
            return Create(expression, Enumerable.Range(1, expandedCount), expandedCount);
        }

        var selected = new SortedSet<int>();
        foreach (var rawItem in text.Split(','))
        {
            var item = rawItem.Trim();
            if (item.Length == 0)
            {
                return Failed(expression, expandedCount, "The job selection contains an empty item.");
            }

            var dash = item.IndexOf('-');
            if (dash < 0)
            {
                if (!TryParseId(item, out var id))
                {
                    return Failed(expression, expandedCount, $"'{item}' is not a valid 1-based job id.");
                }

                if (id > expandedCount)
                {
                    return Failed(
                        expression,
                        expandedCount,
                        $"Job id {id.ToString(CultureInfo.InvariantCulture)} is out of range; the template expands to " +
                        $"{expandedCount.ToString(CultureInfo.InvariantCulture)} job(s).");
                }

                selected.Add(id);
                continue;
            }

            if (item.IndexOf('-', dash + 1) >= 0)
            {
                return Failed(expression, expandedCount, $"'{item}' is not a valid job range.");
            }

            var startText = item[..dash].Trim();
            var endText = item[(dash + 1)..].Trim();
            if (!TryParseId(startText, out var start) || !TryParseId(endText, out var end))
            {
                return Failed(expression, expandedCount, $"'{item}' is not a valid job range.");
            }

            if (start > end)
            {
                return Failed(expression, expandedCount, $"The job range '{item}' is descending.");
            }

            if (end > expandedCount)
            {
                return Failed(
                    expression,
                    expandedCount,
                    $"Job id {end.ToString(CultureInfo.InvariantCulture)} is out of range; the template expands to " +
                    $"{expandedCount.ToString(CultureInfo.InvariantCulture)} job(s).");
            }

            for (var id = start; id <= end; id++)
            {
                selected.Add(id);
            }
        }

        return Create(expression, selected, expandedCount);
    }

    private static JobSelectionResult Create(string expression, IEnumerable<int> selected, int expandedCount)
    {
        var ids = selected.ToImmutableArray();
        if (ids.Length == 0)
        {
            return Failed(expression, expandedCount, "The job selection selects no jobs.");
        }

        var normal = ids.Length == expandedCount ? "all" : Normalize(ids);
        return new JobSelectionResult(normal, ids, expandedCount, Error: null);
    }

    /// <summary>
    /// Builds the canonical expression of a sorted, unique id sequence.
    /// </summary>
    /// <param name="ids">The sorted ids.</param>
    /// <returns>The merged, comma-separated expression without spaces.</returns>
    public static string Normalize(IReadOnlyList<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var rangeStart = ids[0];
        var rangeEnd = ids[0];
        for (var index = 1; index <= ids.Count; index++)
        {
            if (index < ids.Count && ids[index] == rangeEnd + 1)
            {
                rangeEnd = ids[index];
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(',');
            }

            builder.Append(rangeStart.ToString(CultureInfo.InvariantCulture));
            if (rangeEnd != rangeStart)
            {
                builder.Append('-');
                builder.Append(rangeEnd.ToString(CultureInfo.InvariantCulture));
            }

            if (index < ids.Count)
            {
                rangeStart = ids[index];
                rangeEnd = ids[index];
            }
        }

        return builder.ToString();
    }

    private static bool TryParseId(string text, out int id) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;

    private static JobSelectionResult Failed(string expression, int expandedCount, string error) =>
        new(expression, ImmutableArray<int>.Empty, expandedCount, error);
}

/// <summary>
/// The normalized 1-based full-expansion job selection.
/// </summary>
/// <param name="Expression">The canonical expression, or the raw expression on failure.</param>
/// <param name="JobIds">The sorted selected job ids; empty on failure.</param>
/// <param name="ExpandedCount">The complete expansion count.</param>
/// <param name="Error">The parse error, or <see langword="null"/> on success.</param>
public sealed record JobSelectionResult(
    string Expression,
    ImmutableArray<int> JobIds,
    int ExpandedCount,
    string? Error)
{
    /// <summary>Gets a value indicating whether the selection is valid.</summary>
    public bool IsValid => Error is null;

    /// <summary>Gets the selected job count.</summary>
    public int SelectedCount => JobIds.Length;
}

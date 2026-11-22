// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One value of a domain's filter combo box.
/// </summary>
/// <param name="Label">The visible label.</param>
/// <param name="RequiredTag">The row tag a row must carry, or <see langword="null"/> for "all rows".</param>
internal sealed record SpikeFilterOption(string Label, string? RequiredTag)
{
    /// <summary>
    /// Determines whether a row passes the filter.
    /// </summary>
    /// <param name="row">The row to test.</param>
    /// <returns><see langword="true"/> when the row is visible.</returns>
    public bool Matches(SpikeRow row) =>
        RequiredTag is null || row.Tags.Contains(RequiredTag, StringComparer.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Label;
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One domain table: its shared column policy plus the rows built from the bound projects.
/// </summary>
/// <param name="Domain">The table domain.</param>
/// <param name="Columns">The shared column policy.</param>
/// <param name="Rows">The rows in model order.</param>
internal sealed record SpikeTable(
    SpikeDomain Domain,
    ImmutableArray<SpikeColumn> Columns,
    ImmutableArray<SpikeRow> Rows)
{
    /// <summary>
    /// Appends display-only rows from another sample.
    /// </summary>
    /// <param name="extra">The rows to append.</param>
    /// <returns>The combined table.</returns>
    public SpikeTable WithRows(ImmutableArray<SpikeRow> extra) => extra.IsDefaultOrEmpty
        ? this
        : this with { Rows = Rows.AddRange(extra) };
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Model.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.LayoutSpike.Visuals;

/// <summary>
/// A matrix-shaped value for the option-6 data-shape evaluation: the chain's per-position group occupation.
/// </summary>
/// <remarks>
/// Option 6 is not a sixth layout. The owner decision of 2026-11-22 defines it as a data-shape control for
/// matrix-shaped model data, so the spike exercises it inside the detail pane of every layout instead.
/// </remarks>
internal sealed record SpikeMatrix(
    string Title,
    ImmutableArray<string> ColumnLabels,
    ImmutableArray<SpikeMatrix.Row> Rows)
{
    /// <summary>
    /// Builds the occupation matrix for a chain.
    /// </summary>
    /// <param name="project">The owning project.</param>
    /// <param name="chain">The chain.</param>
    /// <returns>The matrix.</returns>
    public static SpikeMatrix FromChain(ProjectModel project, TransitionChain chain)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(chain);

        var groups = chain.GroupIds
            .Select(id => project.StateChangeGroups.FirstOrDefault(group => group.Id == id))
            .Where(group => group is not null)
            .Select(group => group!)
            .DistinctBy(group => group.Id)
            .ToImmutableArray();
        var columns = groups.Select(group => group.Name).ToImmutableArray();
        var rows = ImmutableArray.CreateBuilder<Row>(chain.GroupIds.Length);
        for (var position = 0; position < chain.GroupIds.Length; position++)
        {
            var active = project.StateChangeGroups
                .FirstOrDefault(group => group.Id == chain.GroupIds[position]);
            var cells = columns
                .Select(name => string.Equals(name, active?.Name, StringComparison.Ordinal) ? name : "·")
                .ToImmutableArray();
            rows.Add(new Row($"S{position}", cells));
        }

        return new SpikeMatrix($"Occupation matrix · {chain.Name}", columns, rows.ToImmutable());
    }

    /// <summary>
    /// One matrix row: a position label plus one cell per group.
    /// </summary>
    /// <param name="Label">The position label.</param>
    /// <param name="Cells">The cell texts.</param>
    internal sealed record Row(string Label, ImmutableArray<string> Cells);
}

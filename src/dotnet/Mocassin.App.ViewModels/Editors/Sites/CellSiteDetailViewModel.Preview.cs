// SPDX-License-Identifier: MIT
using System.Collections.Immutable;

using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// P1 preview construction for <see cref="CellSiteDetailViewModel"/>: orbit rows, whole-structure rows, and the
/// deterministic 2D projection. The rows carry positions and canonical indices only; no operation label (M-1).
/// </summary>
internal sealed partial class CellSiteDetailViewModel
{
    private StructurePreview? TryCreatePreview(StructureModel structure) =>
        previewService.TryCreatePreview(structure, 1, 1, 1, out _);

    private int SiteIndex()
    {
        var sites = project.Structure.Sites;
        if (IsNew)
        {
            return sites.IsDefault ? 0 : sites.Length;
        }

        for (var index = 0; index < sites.Length; index++)
        {
            if (sites[index].Id.Value == ObjectId)
            {
                return index;
            }
        }

        return -1;
    }

    private void RebuildOrbitRows()
    {
        OrbitRows.Clear();
        var preview = candidatePreview ?? (IsNew ? null : currentPreview);
        var index = SiteIndex();
        if (preview is null || index < 0 || index >= preview.Orbits.Length)
        {
            OrbitSize = 0;
            Multiplicity = 0;
            CartesianText = "—";
            return;
        }

        var orbit = preview.Orbits[index];
        OrbitSize = orbit.Positions.Length;
        Multiplicity = orbit.Multiplicity;
        for (var member = 0; member < orbit.Positions.Length; member++)
        {
            var p = orbit.PositionIndices[member];
            OrbitRows.Add(new SiteOrbitRowViewModel(
                p,
                preview.PositionList[p].OrbitMemberIndex,
                FormatFractional(orbit.Positions[member]),
                FormatCartesian(preview.CartesianPositions[p])));
        }

        var representative = orbit.PositionIndices.Length > 0 ? orbit.PositionIndices[0] : -1;
        CartesianText = representative >= 0
            ? FormatCartesian(preview.CartesianPositions[representative])
            : "—";
    }

    private void RebuildWholeStructureRows()
    {
        WholeStructureRows.Clear();
        var preview = candidatePreview ?? currentPreview;
        if (preview is null)
        {
            return;
        }

        var sites = previewSites.IsDefault ? ImmutableArray<CellSite>.Empty : previewSites;
        foreach (var position in preview.PositionList)
        {
            var siteName = position.ReferenceSiteIndex >= 0 && position.ReferenceSiteIndex < sites.Length
                ? sites[position.ReferenceSiteIndex].Name
                : string.Empty;
            var stability = position.ReferenceSiteIndex >= 0 && position.ReferenceSiteIndex < sites.Length
                ? sites[position.ReferenceSiteIndex].Stability
                : SiteStability.Stable;
            WholeStructureRows.Add(new SiteWholeStructureRowViewModel(
                position.P,
                FormatFractional(position.Fractional),
                FormatCartesian(preview.CartesianPositions[position.P]),
                siteName,
                stability));
        }
    }

    private void RebuildProjection()
    {
        ProjectionPoints.Clear();
        var preview = candidatePreview ?? currentPreview;
        if (preview is null)
        {
            return;
        }

        var points = preview.CartesianPositions
            .Select((position, index) => (P: index, U: Project(position.X, position.Y, position.Z).U, V: Project(position.X, position.Y, position.Z).V))
            .ToArray();
        if (points.Length == 0)
        {
            return;
        }

        var minU = points.Min(static point => point.U);
        var maxU = points.Max(static point => point.U);
        var minV = points.Min(static point => point.V);
        var maxV = points.Max(static point => point.V);
        var span = projectionCanvas - (2 * projectionMargin);
        foreach (var point in points)
        {
            var x = Normalize(point.U, minU, maxU, span);
            var y = projectionCanvas - Normalize(point.V, minV, maxV, span);
            ProjectionPoints.Add(new SiteProjectionPointViewModel(point.P, x, y));
        }
    }

    private (double U, double V) Project(double x, double y, double z) => SelectedPlane switch
    {
        "a–c" => (x, z),
        "b–c" => (y, z),
        _ => (x, y),
    };

    private static double Normalize(double value, double min, double max, double span) =>
        max - min <= 1e-12
            ? projectionMargin + (span / 2d)
            : projectionMargin + (((value - min) / (max - min)) * span);
}

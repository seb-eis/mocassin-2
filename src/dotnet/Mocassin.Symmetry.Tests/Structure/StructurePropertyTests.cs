// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class StructurePropertyTests
{
    [Property(MaxTest = 50)]
    public bool OverlapDetectionMatchesBruteForce(int[] values)
    {
        if (values is null)
        {
            return true;
        }

        var count = Math.Clamp(values.Length % 6, 0, 5);
        if (count < 2)
        {
            return true;
        }

        var setting = SpaceGroupCatalog.Default.GetSetting(225, AxisSetting.None, OriginChoice.None);
        if (setting is null)
        {
            return false;
        }

        var positions = new Fractional3[count];
        var references = new StructureSiteReference[count];
        for (var i = 0; i < count; i++)
        {
            var bits = (uint)values[i];
            positions[i] = new Fractional3(
                (bits % 1000) / 1000.0,
                ((bits >> 10) % 1000) / 1000.0,
                ((bits >> 20) % 1000) / 1000.0);
            references[i] = new StructureSiteReference(CellSiteId.New(), positions[i]);
        }

        var expected = HasBruteForceOverlap(setting, positions);
        var created = UnitCellVectorEncoder.TryCreate(
            setting,
            references,
            out var encoder,
            out var overlaps,
            out var diagnostics);
        var actual = overlaps.Length > 0;
        var hasOverlapDiagnostic = diagnostics.Any(static diagnostic => diagnostic.Code == "STR007");

        return actual == expected
            && hasOverlapDiagnostic == expected
            && (expected || (created && encoder is not null));
    }

    private static bool HasBruteForceOverlap(SpaceGroupSetting setting, Fractional3[] positions)
    {
        var grids = new GridFractional3[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            if (!GridFractional3.TryFromFractional(positions[i].TrimToUnitCell(), out grids[i], out _))
            {
                return false;
            }
        }

        for (var i = 0; i < grids.Length; i++)
        {
            var first = SpaceGroupOperations.ExpandGridOrbit(setting, grids[i]);
            for (var j = i + 1; j < grids.Length; j++)
            {
                var second = SpaceGroupOperations.ExpandGridOrbit(setting, grids[j]);
                if (first.Intersect(second).Any())
                {
                    return true;
                }
            }
        }

        return false;
    }
}

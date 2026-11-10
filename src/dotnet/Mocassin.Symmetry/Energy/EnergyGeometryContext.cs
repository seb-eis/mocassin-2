// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Resolved cell, encoder, and reference-site lookup shared by energy sampling and validation.
/// </summary>
internal sealed class EnergyGeometryContext
{
    private readonly Dictionary<CellSiteId, CellSite> siteById;
    private readonly Dictionary<CellSiteId, int> referenceIndexById;

    private EnergyGeometryContext(
        ModelDefinition model,
        SpaceGroupSetting setting,
        CellGeometry cell,
        UnitCellVectorEncoder encoder,
        ImmutableArray<CellSite> sites)
    {
        Model = model;
        Setting = setting;
        Cell = cell;
        Encoder = encoder;
        Geometry = new PeriodicGeometry(cell, encoder);
        Sites = sites;
        siteById = [];
        referenceIndexById = [];
        for (var index = 0; index < sites.Length; index++)
        {
            siteById.TryAdd(sites[index].Id, sites[index]);
            referenceIndexById.TryAdd(sites[index].Id, index);
        }
    }

    public ModelDefinition Model { get; }

    public SpaceGroupSetting Setting { get; }

    public CellGeometry Cell { get; }

    public UnitCellVectorEncoder Encoder { get; }

    public PeriodicGeometry Geometry { get; }

    public ImmutableArray<CellSite> Sites { get; }

    public static bool TryCreate(
        ModelDefinition model,
        SpaceGroupCatalog? catalog,
        out EnergyGeometryContext? context,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        context = null;
        var bag = new DiagnosticBag();
        var setting = SpaceGroupReferenceResolver.Resolve(
            catalog ?? SpaceGroupCatalog.Default,
            model.Structure.SpaceGroup,
            bag);
        if (setting is null)
        {
            diagnostics = bag.Sorted;
            return false;
        }

        if (!CellGeometry.TryCreate(model.Structure.Cell, setting, out var cell, out var cellDiagnostics) ||
            cell is null)
        {
            bag.AddRange(cellDiagnostics);
            diagnostics = bag.Sorted;
            return false;
        }

        var sites = model.Structure.Sites.IsDefault ? [] : model.Structure.Sites;
        var references = sites
            .Select(static site => new StructureSiteReference(site.Id, site.Fractional))
            .ToArray();
        if (!UnitCellVectorEncoder.TryCreate(
                setting,
                references,
                out var encoder,
                out _,
                out var encoderDiagnostics) ||
            encoder is null)
        {
            bag.AddRange(encoderDiagnostics);
            diagnostics = bag.Sorted;
            return false;
        }

        context = new EnergyGeometryContext(model, setting, cell, encoder, sites);
        diagnostics = bag.Sorted;
        return true;
    }

    public bool TryGetSite(CellSiteId siteId, out CellSite site) => siteById.TryGetValue(siteId, out site!);

    public bool TryGetReferenceIndex(CellSiteId siteId, out int referenceIndex) =>
        referenceIndexById.TryGetValue(siteId, out referenceIndex);

    public CellSite GetSiteAt(int p) => Sites[Encoder.ExpandedSites[p].ReferenceSiteIndex];

    public bool IsStableAt(int p) => GetSiteAt(p).Stability == SiteStability.Stable;

    public bool TryGetCenterP(CellSite site, out int p) => Encoder.TryIndexOf(site.Fractional, out p);

    public ImmutableArray<int> GetSitePIndices(CellSiteId siteId) =>
        referenceIndexById.TryGetValue(siteId, out var referenceIndex) &&
        referenceIndex >= 0 &&
        referenceIndex < Encoder.Orbits.Length
            ? Encoder.Orbits[referenceIndex].PositionIndices
            : [];

    public ImmutableArray<UnstableEnvironment> GetEffectiveUnstableEnvironments()
    {
        var authored = Model.UnstableEnvironments.IsDefault ? [] : Model.UnstableEnvironments;
        var stableCutoff = (Model.StableEnvironment ?? StableEnvironment.Default).Cutoff;
        var builder = ImmutableArray.CreateBuilder<UnstableEnvironment>();
        foreach (var site in Sites)
        {
            if (site.Stability != SiteStability.Unstable)
            {
                continue;
            }

            var environment = authored.FirstOrDefault(candidate => candidate.SiteId == site.Id);
            builder.Add(environment ?? new UnstableEnvironment(site.Id, stableCutoff, []));
        }

        return [.. builder.OrderBy(static environment => environment.SiteId.Value)];
    }

    public bool TryResolveOffset(
        CellSite center,
        Fractional3 offset,
        out Vector4I encoding,
        out int targetP)
    {
        encoding = default;
        targetP = -1;
        var absolute = center.Fractional + offset;
        if (!absolute.IsFinite)
        {
            return false;
        }

        var matches = new List<(int P, Vector4I Encoding)>();
        for (var p = 0; p < Encoder.PositionCount; p++)
        {
            var canonical = Encoder.Positions[p];
            if (TryGetComponentShift(absolute.A, canonical.A, out var a) &&
                TryGetComponentShift(absolute.B, canonical.B, out var b) &&
                TryGetComponentShift(absolute.C, canonical.C, out var c))
            {
                matches.Add((p, new Vector4I(a, b, c, p)));
            }
        }

        if (matches.Count != 1)
        {
            return false;
        }

        targetP = matches[0].P;
        encoding = matches[0].Encoding;
        return true;
    }

    public MinimumImageResult GetMinimumImage(int centerP, Vector4I target) =>
        Geometry.GetMinimumImage(new Vector4I(0, 0, 0, centerP), target);

    public static bool IsFiltered(
        ImmutableArray<InteractionFilter> filters,
        CellSite center,
        CellSite target,
        double distance,
        bool symmetric)
    {
        if (filters.IsDefaultOrEmpty)
        {
            return false;
        }

        foreach (var filter in filters)
        {
            if (distance < filter.Start.Value - GeometryTolerances.DistanceEqualityTolerance ||
                distance > filter.End.Value + GeometryTolerances.DistanceEqualityTolerance)
            {
                continue;
            }

            var forward = filter.SiteAId == center.Id && filter.SiteBId == target.Id;
            var reverse = symmetric && filter.SiteAId == target.Id && filter.SiteBId == center.Id;
            if (forward || reverse)
            {
                return true;
            }
        }

        return false;
    }

    public long CountPairImages(
        IEnumerable<int> centerPs,
        LengthAngstrom cutoff,
        ImmutableArray<InteractionFilter> filters,
        bool symmetric)
    {
        long count = 0;
        foreach (var centerP in centerPs)
        {
            var center = GetSiteAt(centerP);
            foreach (var image in Geometry.RadialSearch(new Vector4I(0, 0, 0, centerP), cutoff.Value))
            {
                if (!IsStableAt(image.Offset.P))
                {
                    continue;
                }

                var target = GetSiteAt(image.Offset.P);
                if (!IsFiltered(filters, center, target, image.DistanceAngstrom, symmetric))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool TryGetComponentShift(double coordinate, double canonical, out int shift)
    {
        var delta = coordinate - canonical;
        var rounded = Math.Round(delta, MidpointRounding.AwayFromZero);
        if (Math.Abs(Math.Abs(delta - rounded) - 0.5) <= 1e-12)
        {
            rounded = Math.Truncate(delta);
        }

        if (rounded < int.MinValue || rounded > int.MaxValue ||
            Math.Abs(delta - rounded) > GeometryTolerances.PathMatchTolerance)
        {
            shift = 0;
            return false;
        }

        shift = (int)rounded;
        return true;
    }
}

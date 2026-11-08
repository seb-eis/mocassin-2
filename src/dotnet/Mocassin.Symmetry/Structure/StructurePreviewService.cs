// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Caching;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.Diagnostics;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// Resolves catalog settings and builds cached, deterministic structure previews.
/// </summary>
/// <remarks>
/// The preview cache is scoped to the service, bounded, and thread-safe. Its key includes the canonical
/// structure hash, the requested supercell counts, and the catalog/contract versions.
/// </remarks>
public sealed class StructurePreviewService : IStructurePreviewService
{
    private readonly SpaceGroupCatalog catalog;
    private readonly SymmetryVersionInfo versions;
    private readonly SymmetryCache cache;

    /// <summary>
    /// Initializes a structure-preview service.
    /// </summary>
    /// <param name="catalog">The optional catalog override. The default is <see cref="SpaceGroupCatalog.Default"/>.</param>
    /// <param name="versions">The optional version override used by tests and version-injection scenarios.</param>
    /// <param name="cacheCapacity">The maximum number of cached previews.</param>
    public StructurePreviewService(
        SpaceGroupCatalog? catalog = null,
        SymmetryVersionInfo? versions = null,
        int cacheCapacity = 256)
    {
        var resolvedCatalog = catalog ?? SpaceGroupCatalog.Default;
        this.catalog = resolvedCatalog;
        this.versions = versions ?? SymmetryVersionInfo.FromCatalog(resolvedCatalog);
        cache = new SymmetryCache(
            this.versions.SpaceGroupDataVersion,
            this.versions.SymmetryContractVersion,
            cacheCapacity);
    }

    internal StructurePreviewService(
        SpaceGroupCatalog catalog,
        SymmetryVersionInfo versions,
        SymmetryCache cache)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.versions = versions ?? throw new ArgumentNullException(nameof(versions));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Gets the preview cache. Intended for diagnostics and tests.
    /// </summary>
    internal SymmetryCache Cache => cache;

    /// <inheritdoc />
    public SpaceGroupSetting? TryResolve(
        SpaceGroupRef reference,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var bag = new DiagnosticBag();
        var setting = SpaceGroupReferenceResolver.Resolve(catalog, reference, bag);
        diagnostics = bag.Sorted;
        return setting;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive.</exception>
    public StructurePreview? TryCreatePreview(
        StructureModel structureModel,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(structureModel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountA);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountB);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountC);

        var sites = structureModel.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structureModel.Sites;
        string? cacheKey = null;
        if (sites.All(static site => site.Fractional.IsFinite))
        {
            var structureHash = ProjectHasher.ComputeStructure(structureModel);
            cacheKey = CreateCacheKey(structureHash, cellCountA, cellCountB, cellCountC);
            if (cache.TryGet<PreviewCacheEntry>(cacheKey, out var cached) && cached is not null)
            {
                diagnostics = cached.Diagnostics;
                return cached.Preview;
            }
        }

        var bag = new DiagnosticBag();
        var setting = SpaceGroupReferenceResolver.Resolve(catalog, structureModel.SpaceGroup, bag);
        if (setting is null)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        CellGeometry.TryCreate(structureModel.Cell, setting, out var geometry, out var cellDiagnostics);
        bag.AddRange(cellDiagnostics);
        if (geometry is null)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        var references = sites
            .Select(static site => new StructureSiteReference(site.Id, site.Fractional))
            .ToArray();
        if (!UnitCellVectorEncoder.TryCreate(
                setting,
                references,
                out var encoder,
                out _,
                out var encoderDiagnostics)
            || encoder is null)
        {
            bag.AddRange(encoderDiagnostics);
            diagnostics = bag.Sorted;
            return null;
        }

        bag.AddRange(encoderDiagnostics);
        if (!UnitCellVectorEncoder.IsLatticeSizeSupported(
                cellCountA,
                cellCountB,
                cellCountC,
                encoder.PositionCount))
        {
            var count = UnitCellVectorEncoder.GetLatticePositionCount(
                cellCountA,
                cellCountB,
                cellCountC,
                encoder.PositionCount);
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.LinearIndexLimitExceeded,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The requested supercell contains {count} positions, above the signed 32-bit linear-index limit."),
                new ModelPath("structure", Guid.Empty, "cellCounts")));
            diagnostics = bag.Sorted;
            return null;
        }

        var preview = new StructurePreview(
            encoder.ExpandedSites,
            encoder,
            encoder.Positions.Select(geometry.ToCartesian).ToImmutableArray(),
            geometry.VolumeAngstrom3,
            encoder.Orbits,
            cellCountA,
            cellCountB,
            cellCountC);
        if (cacheKey is null)
        {
            diagnostics = bag.Sorted;
            return preview;
        }

        var entry = new PreviewCacheEntry(preview, bag.Sorted);
        var stored = cache.GetOrAdd(cacheKey, () => entry);
        diagnostics = stored.Diagnostics;
        return stored.Preview;
    }

    private string CreateCacheKey(
        string structureHash,
        int cellCountA,
        int cellCountB,
        int cellCountC) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"preview|{versions.SpaceGroupDataVersion}|{versions.SpaceGroupDataSha256}|{versions.SymmetryContractVersion}|{structureHash}|{cellCountA}|{cellCountB}|{cellCountC}");

    private sealed record PreviewCacheEntry(
        StructurePreview Preview,
        ImmutableArray<Diagnostic> Diagnostics);
}

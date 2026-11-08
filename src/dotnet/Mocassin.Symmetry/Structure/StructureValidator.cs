// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// Validates the catalog-dependent rules of a structure.
/// </summary>
/// <remarks>
/// This validator covers <c>STR001</c>, <c>STR002</c>, <c>STR003</c>, <c>STR007</c>, and the
/// <c>STR009</c> budget warning. Core-owned rules are emitted by <c>Mocassin.Core.Project.ProjectValidator</c>.
/// </remarks>
public sealed class StructureValidator : IStructureValidator
{
    private readonly SpaceGroupCatalog catalog;

    /// <summary>
    /// Initializes a structure validator.
    /// </summary>
    /// <param name="catalog">The optional catalog override. The default is <see cref="SpaceGroupCatalog.Default"/>.</param>
    public StructureValidator(SpaceGroupCatalog? catalog = null)
    {
        this.catalog = catalog ?? SpaceGroupCatalog.Default;
    }

    /// <inheritdoc />
    public ImmutableArray<Diagnostic> Validate(StructureModel structureModel)
    {
        ArgumentNullException.ThrowIfNull(structureModel);

        var bag = new DiagnosticBag();
        var setting = SpaceGroupReferenceResolver.Resolve(catalog, structureModel.SpaceGroup, bag);
        if (setting is null)
        {
            return bag.Sorted;
        }

        CellGeometry.TryCreate(structureModel.Cell, setting, out _, out var cellDiagnostics);
        bag.AddRange(cellDiagnostics);

        var sites = structureModel.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structureModel.Sites;
        var references = sites
            .Select(static site => new StructureSiteReference(site.Id, site.Fractional))
            .ToArray();
        UnitCellVectorEncoder.TryCreate(setting, references, out _, out _, out var encoderDiagnostics);
        bag.AddRange(encoderDiagnostics);

        return bag.Sorted;
    }
}

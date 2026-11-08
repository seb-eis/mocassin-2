// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// Validates the Core-owned structure rules without throwing for user data.
/// </summary>
/// <remarks>
/// Catalog-dependent cell, space-group, orbit, budget, and index-cap rules are validated by
/// <c>Mocassin.Symmetry</c>. This validator depends only on the project graph and primitive types.
/// </remarks>
internal static class StructureModelValidator
{
    /// <summary>
    /// Validates structure names, site limits, site names, particle-set references, and positions.
    /// </summary>
    /// <param name="structure">The structure to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(StructureModel structure)
    {
        ArgumentNullException.ThrowIfNull(structure);

        var bag = new DiagnosticBag();
        var sites = structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structure.Sites;
        var dummyPositions = structure.DummyPositions.IsDefault
            ? ImmutableArray<CellDummyPosition>.Empty
            : structure.DummyPositions;

        ValidateStructureName(structure.Name, bag);

        if (sites.Length > StructureModel.MaxReferenceSites)
        {
            bag.Add(Error(
                CoreStructureDiagnosticCodes.SiteLimitExceeded,
                $"The structure defines {sites.Length} reference sites; " +
                $"the limit is {StructureModel.MaxReferenceSites}.",
                "structure",
                Guid.Empty,
                "sites"));
        }

        ValidateSites(sites, bag);
        ValidateDummyPositions(dummyPositions, bag);
        return bag.Sorted;
    }

    private static void ValidateStructureName(string name, DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bag.Add(Error(
                CoreStructureDiagnosticCodes.InvalidStructureName,
                "Structure name must not be empty.",
                "structure",
                Guid.Empty,
                "name"));
        }
    }

    private static void ValidateSites(ImmutableArray<CellSite> sites, DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var site in sites)
        {
            ValidateName(
                site.Name,
                names,
                CoreStructureDiagnosticCodes.InvalidSiteName,
                "Site name must not be empty.",
                $"Site name '{site.Name}' is used more than once.",
                "cellSite",
                site.Id.Value,
                bag);

            if (site.ParticleSetId == ParticleSetId.Empty || site.ParticleSetId == default)
            {
                bag.Add(Error(
                    CoreStructureDiagnosticCodes.MissingParticleSet,
                    "Cell site must reference a particle set; the default and reserved empty identifiers are not allowed.",
                    "cellSite",
                    site.Id.Value,
                    "particleSetId"));
            }

            if (!IsInsideUnitCell(site.Fractional))
            {
                bag.Add(Error(
                    CoreStructureDiagnosticCodes.InvalidSitePosition,
                    "Reference site must be finite and inside [0,1) within the fractional trim tolerance.",
                    "cellSite",
                    site.Id.Value,
                    "fractional"));
            }
        }
    }

    private static void ValidateDummyPositions(
        ImmutableArray<CellDummyPosition> dummyPositions,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dummyPosition in dummyPositions)
        {
            ValidateName(
                dummyPosition.Name,
                names,
                CoreStructureDiagnosticCodes.InvalidDummyName,
                "Dummy-position name must not be empty.",
                $"Dummy-position name '{dummyPosition.Name}' is used more than once.",
                "cellDummyPosition",
                dummyPosition.Id.Value,
                bag);

            if (!IsInsideUnitCell(dummyPosition.Fractional))
            {
                bag.Add(Error(
                    CoreStructureDiagnosticCodes.InvalidDummyPosition,
                    "Dummy position must be finite and inside [0,1) within the fractional trim tolerance.",
                    "cellDummyPosition",
                    dummyPosition.Id.Value,
                    "fractional"));
            }
        }
    }

    private static void ValidateName(
        string name,
        HashSet<string> names,
        string code,
        string emptyMessage,
        string duplicateMessage,
        string domain,
        Guid objectId,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bag.Add(Error(code, emptyMessage, domain, objectId, "name"));
            return;
        }

        if (!names.Add(name))
        {
            bag.Add(Error(code, duplicateMessage, domain, objectId, "name"));
        }
    }

    private static bool IsInsideUnitCell(Fractional3 fractional) =>
        IsUnitCellComponent(fractional.A) &&
        IsUnitCellComponent(fractional.B) &&
        IsUnitCellComponent(fractional.C);

    private static bool IsUnitCellComponent(double value) =>
        double.IsFinite(value) &&
        value >= -GeometryTolerances.FractionalTrimTolerance &&
        value <= 1.0 + GeometryTolerances.FractionalTrimTolerance;

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property));
}

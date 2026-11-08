// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Diagnostics;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Structure;

/// <summary>
/// Resolves authored space-group references against the embedded catalog.
/// </summary>
internal static class SpaceGroupReferenceResolver
{
    /// <summary>
    /// Resolves a structured reference and appends <c>STR001</c> when no setting matches.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="reference">The authored reference.</param>
    /// <param name="bag">The diagnostic bag.</param>
    /// <returns>The matching setting, or <see langword="null"/> when the reference is invalid.</returns>
    public static SpaceGroupSetting? Resolve(
        SpaceGroupCatalog catalog,
        SpaceGroupRef reference,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(bag);

        var path = new ModelPath("structure", Guid.Empty, "spaceGroup");
        if (reference.Number is < 1 or > 230)
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.SpaceGroupReference,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Space-group number {reference.Number} is outside the supported range 1..230."),
                path));
            return null;
        }

        var setting = catalog.GetSetting(reference.Number, reference.Axis, reference.Origin);
        if (setting is not null)
        {
            return setting;
        }

        var allowed = catalog.Settings
            .Where(candidate => candidate.Number == reference.Number)
            .Select(static candidate => string.Create(
                CultureInfo.InvariantCulture,
                $"({JsonNamingPolicy.CamelCase.ConvertName(candidate.Axis.ToString())}, " +
                $"{JsonNamingPolicy.CamelCase.ConvertName(candidate.Origin.ToString())})"))
            .ToImmutableArray();
        var hint = allowed.IsEmpty
            ? null
            : string.Create(
                CultureInfo.InvariantCulture,
                $"Allowed settings for space group {reference.Number}: {string.Join(", ", allowed)}.");
        var axis = JsonNamingPolicy.CamelCase.ConvertName(reference.Axis.ToString());
        var origin = JsonNamingPolicy.CamelCase.ConvertName(reference.Origin.ToString());

        bag.Add(new Diagnostic(
            StructureDiagnosticCodes.SpaceGroupReference,
            DiagnosticSeverity.Error,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Space-group setting ({reference.Number}, {axis}, {origin}) is not in the embedded catalog."),
            path,
            hint));
        return null;
    }
}

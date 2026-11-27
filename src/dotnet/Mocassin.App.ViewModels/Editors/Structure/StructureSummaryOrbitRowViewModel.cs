// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// One row of the structure summary's orbit table.
/// </summary>
/// <param name="SiteName">The owning reference-site name.</param>
/// <param name="RepresentativeText">The orbit representative fractional position.</param>
/// <param name="Multiplicity">The orbit multiplicity.</param>
/// <param name="PositionCount">The number of canonical P1 positions in the orbit.</param>
internal sealed record StructureSummaryOrbitRowViewModel(
    string SiteName,
    string RepresentativeText,
    int Multiplicity,
    int PositionCount);

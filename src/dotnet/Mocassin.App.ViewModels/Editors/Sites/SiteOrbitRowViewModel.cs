// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// One row of the selected site's P1 orbit. It carries positions and the canonical indices only; no operation label
/// (M-1/D-11).
/// </summary>
/// <param name="P">The canonical per-cell P1 index.</param>
/// <param name="OrbitMemberIndex">The zero-based index within the reference site's sorted orbit.</param>
/// <param name="FractionalText">The fractional position.</param>
/// <param name="CartesianText">The Cartesian position in ångström.</param>
internal sealed record SiteOrbitRowViewModel(
    int P,
    int OrbitMemberIndex,
    string FractionalText,
    string CartesianText);

// SPDX-License-Identifier: MIT

using Mocassin.Core.Model.Structure;

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// One row of the whole-structure P1 position list, in canonical P order.
/// </summary>
/// <param name="P">The canonical per-cell P1 index.</param>
/// <param name="FractionalText">The fractional position.</param>
/// <param name="CartesianText">The Cartesian position in ångström.</param>
/// <param name="SiteName">The owning reference-site name.</param>
/// <param name="Stability">The owning reference site's stability.</param>
internal sealed record SiteWholeStructureRowViewModel(
    int P,
    string FractionalText,
    string CartesianText,
    string SiteName,
    SiteStability Stability);

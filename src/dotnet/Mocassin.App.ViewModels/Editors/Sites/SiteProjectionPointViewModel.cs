// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// One point of the deterministic 2D P1 projection.
/// </summary>
/// <param name="P">The canonical per-cell P1 index.</param>
/// <param name="X">The canvas x coordinate.</param>
/// <param name="Y">The canvas y coordinate.</param>
internal sealed record SiteProjectionPointViewModel(int P, double X, double Y);

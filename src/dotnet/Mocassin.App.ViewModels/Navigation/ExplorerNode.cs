// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// One immutable explorer node.
/// </summary>
/// <param name="Key">The stable node key.</param>
/// <param name="Label">The visible label.</param>
/// <param name="IconKey">The optional icon key.</param>
/// <param name="Path">The optional navigation path.</param>
/// <param name="Children">The optional child nodes.</param>
internal sealed record ExplorerNode(
    string Key,
    string Label,
    string? IconKey = null,
    ModelPath? Path = null,
    IReadOnlyList<ExplorerNode>? Children = null)
{
    /// <summary>
    /// Gets the child nodes, never <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<ExplorerNode> ChildNodes => Children ?? [];
}

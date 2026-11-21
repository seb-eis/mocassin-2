// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.ViewModels.Settings.Blocks;

/// <summary>
/// The additive <c>session</c> v1 settings block.
/// </summary>
internal sealed record SessionSettings
{
    /// <summary>
    /// Gets the project paths open at the last clean or unclean exit.
    /// </summary>
    public ImmutableArray<string> OpenProjects { get; init; } = [];

    /// <summary>
    /// Gets the active project path, or <see langword="null"/>.
    /// </summary>
    public string? ActiveProject { get; init; }

    /// <summary>
    /// Gets a value indicating whether the previous run ended cleanly.
    /// </summary>
    public bool CleanExit { get; init; } = true;

    /// <summary>
    /// Gets the UTC time the session block was written.
    /// </summary>
    public DateTimeOffset? SavedUtc { get; init; }
}

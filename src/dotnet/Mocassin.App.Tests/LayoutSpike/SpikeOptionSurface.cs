// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One layout option's selection surface. Every implementation hosts the same editor and binds the same workspace;
/// only the composition of list, hierarchy, tabs, splitter, or custom control differs.
/// </summary>
internal abstract class SpikeOptionSurface
{
    /// <summary>
    /// Initializes the surface.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    protected SpikeOptionSurface(SpikeWorkspaceViewModel workspace) =>
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

    /// <summary>Gets the shared workspace.</summary>
    protected SpikeWorkspaceViewModel Workspace { get; }

    /// <summary>
    /// Builds the option control.
    /// </summary>
    /// <returns>The option root.</returns>
    public abstract Control Build();
}

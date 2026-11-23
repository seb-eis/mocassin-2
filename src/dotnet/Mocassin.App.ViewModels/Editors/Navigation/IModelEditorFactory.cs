// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Editors.Navigation;

/// <summary>
/// Creates the editor view model for one model domain. Later features add the concrete factories; the framework
/// owns the registration seam.
/// </summary>
internal interface IModelEditorFactory
{
    /// <summary>
    /// Gets the domain descriptor.
    /// </summary>
    public ModelEditorDescriptor Descriptor { get; }

    /// <summary>
    /// Creates the editor for a project session.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    /// <returns>The editor.</returns>
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout);
}

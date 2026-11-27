// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Editors.Particles;

/// <summary>
/// Creates the particles editor for a project session.
/// </summary>
internal sealed class ParticlesEditorViewModelFactory : IModelEditorFactory
{
    /// <inheritdoc />
    public ModelEditorDescriptor Descriptor => ModelEditorDomains.Particles;

    /// <inheritdoc />
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout) =>
        new ParticlesEditorViewModel(session, layout);
}

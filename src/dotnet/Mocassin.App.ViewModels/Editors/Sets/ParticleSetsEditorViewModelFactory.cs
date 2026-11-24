// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Editors.Sets;

/// <summary>
/// Creates the particle-sets editor for a project session.
/// </summary>
internal sealed class ParticleSetsEditorViewModelFactory : IModelEditorFactory
{
    /// <inheritdoc />
    public ModelEditorDescriptor Descriptor => ModelEditorDomains.ParticleSets;

    /// <inheritdoc />
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout) =>
        new ParticleSetsEditorViewModel(session, layout);
}

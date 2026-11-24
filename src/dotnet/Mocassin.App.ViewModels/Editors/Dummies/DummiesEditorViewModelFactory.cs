// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Editors.Dummies;

/// <summary>
/// Creates the dummy-positions editor for a project session.
/// </summary>
internal sealed class DummiesEditorViewModelFactory : IModelEditorFactory
{
    /// <inheritdoc />
    public ModelEditorDescriptor Descriptor => ModelEditorDomains.DummyPositions;

    /// <inheritdoc />
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout) =>
        new DummiesEditorViewModel(session, layout);
}

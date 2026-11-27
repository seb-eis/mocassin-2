// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// A test factory for the particle editor.
/// </summary>
internal sealed class TestParticleEditorFactory : IModelEditorFactory
{
    /// <inheritdoc />
    public ModelEditorDescriptor Descriptor => ModelEditorDomains.Particles;

    /// <inheritdoc />
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout) =>
        new TestParticleEditor(session, layout);
}

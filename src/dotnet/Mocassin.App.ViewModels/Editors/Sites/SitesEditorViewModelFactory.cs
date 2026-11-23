// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// Creates the cell-site editor for a project session.
/// </summary>
internal sealed class SitesEditorViewModelFactory : IModelEditorFactory
{
    private readonly IStructurePreviewService previewService;
    private readonly IStructureValidator validator;

    /// <summary>
    /// Initializes the factory.
    /// </summary>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public SitesEditorViewModelFactory(
        IStructurePreviewService previewService,
        IStructureValidator validator)
    {
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <inheritdoc />
    public ModelEditorDescriptor Descriptor => ModelEditorDomains.Sites;

    /// <inheritdoc />
    public ModelEditorViewModel Create(ProjectSession session, IEditorPaneLayout layout) =>
        new SitesEditorViewModel(session, layout, previewService, validator);
}

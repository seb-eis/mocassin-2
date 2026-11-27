// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Editors.Navigation;

/// <summary>
/// Opens and reuses the model-editor documents for the five 24A domains and applies the navigation intent.
/// </summary>
internal sealed class ModelEditorNavigationTargetProvider : INavigationTargetProvider
{
    private readonly IProjectWorkspace workspace;
    private readonly DocumentCollectionViewModel documents;
    private readonly IReadOnlyList<IModelEditorFactory> factories;
    private readonly IEditorPaneLayout layout;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="documents">The open documents.</param>
    /// <param name="factories">The registered domain factories.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    public ModelEditorNavigationTargetProvider(
        IProjectWorkspace workspace,
        DocumentCollectionViewModel documents,
        IEnumerable<IModelEditorFactory> factories,
        IEditorPaneLayout layout)
    {
        this.workspace = workspace;
        this.documents = documents;
        this.factories = factories.ToList();
        this.layout = layout;
    }

    /// <inheritdoc />
    public Guid? ProjectScopeId => null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Domains =>
        ModelEditorDomains.All.Select(static descriptor => descriptor.Domain).ToArray();

    /// <inheritdoc />
    public Task<DocumentViewModel?> CreateAsync(
        ModelPath path,
        NavigationIntent intent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(path);
        var project = workspace.ActiveProject;
        if (project is null)
        {
            return Task.FromResult<DocumentViewModel?>(null);
        }

        var factory = factories.FirstOrDefault(candidate =>
            string.Equals(candidate.Descriptor.Domain, path.Domain, StringComparison.Ordinal));
        if (factory is null)
        {
            return Task.FromResult<DocumentViewModel?>(null);
        }

        var key = factory.Descriptor.KeyFor(project.Id);
        var document = documents.Find(key) as ModelEditorDocumentViewModel;
        if (document is null)
        {
            var editor = factory.Create(project.Session, layout);
            document = string.Equals(factory.Descriptor.Domain, ModelEditorDomains.Structure, StringComparison.Ordinal)
                ? new StructureEditorDocumentViewModel(factory.Descriptor, editor)
                : new ModelEditorDocumentViewModel(factory.Descriptor, editor);
        }

        return Task.FromResult<DocumentViewModel?>(
            document.TryNavigate(path, intent) ? document : null);
    }
}

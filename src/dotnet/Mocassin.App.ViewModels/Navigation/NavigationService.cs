// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Default navigation service over the document collection and the registered target providers.
/// </summary>
internal sealed class NavigationService : INavigationService
{
    private readonly DocumentCollectionViewModel documents;
    private readonly IProjectWorkspace workspace;
    private readonly IConsoleService console;
    private readonly IReadOnlyList<INavigationTargetProvider> providers;
    private readonly NavigationHistory history = new();

    /// <summary>
    /// Initializes a navigation service and validates provider uniqueness.
    /// </summary>
    /// <param name="documents">The document collection.</param>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="console">The application console.</param>
    /// <param name="providers">The registered target providers.</param>
    public NavigationService(
        DocumentCollectionViewModel documents,
        IProjectWorkspace workspace,
        IConsoleService console,
        IEnumerable<INavigationTargetProvider> providers)
    {
        this.documents = documents;
        this.workspace = workspace;
        this.console = console;
        this.providers = providers.ToList();
        ValidateProviders(this.providers);
    }

    /// <inheritdoc />
    public bool CanGoBack => history.CanGoBack;

    /// <inheritdoc />
    public bool CanGoForward => history.CanGoForward;

    /// <inheritdoc />
    public event EventHandler? HistoryChanged;

    /// <inheritdoc />
    public Task OpenDocumentAsync(DocumentViewModel document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        documents.OpenOrActivate(document);
        history.Push(new NavigationEntry(document.Key.ScopeId, document.Key, null, NavigationIntent.Activate));
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ActivateAsync(DocumentKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = documents.Find(key);
        if (document is null)
        {
            return Task.FromResult(false);
        }

        if (!ReferenceEquals(documents.ActiveDocument, document))
        {
            documents.ActiveDocument = document;
            history.Push(new NavigationEntry(key.ScopeId, key, null, NavigationIntent.Activate));
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<bool> NavigateToAsync(
        ModelPath path,
        NavigationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        var provider = ResolveProvider(path.Domain);
        if (provider is null)
        {
            console.Write(
                ConsoleSeverity.Information,
                "navigation",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Editing '{path.Domain}' is not available in this version."));
            return false;
        }

        var document = await provider.CreateAsync(path, intent, cancellationToken);
        if (document is null)
        {
            console.Write(
                ConsoleSeverity.Warning,
                "navigation",
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"No navigation target exists for '{path}'."));
            return false;
        }

        documents.OpenOrActivate(document);
        history.Push(new NavigationEntry(workspace.ActiveProject?.Id, document.Key, path, intent));
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc />
    public async Task GoBackAsync(CancellationToken cancellationToken = default)
    {
        var entry = history.Back();
        if (entry is null)
        {
            return;
        }

        await RestoreAsync(entry.Value, cancellationToken);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task GoForwardAsync(CancellationToken cancellationToken = default)
    {
        var entry = history.Forward();
        if (entry is null)
        {
            return;
        }

        await RestoreAsync(entry.Value, cancellationToken);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RestoreAsync(NavigationEntry entry, CancellationToken cancellationToken)
    {
        var document = documents.Find(entry.Key);
        if (document is not null)
        {
            documents.ActiveDocument = document;
            return;
        }

        if (entry.Path is null)
        {
            return;
        }

        var provider = ResolveProvider(entry.Path.Domain);
        if (provider is null)
        {
            return;
        }

        var recreated = await provider.CreateAsync(entry.Path, entry.Intent, cancellationToken);
        if (recreated is not null)
        {
            documents.OpenOrActivate(recreated);
        }
    }

    private INavigationTargetProvider? ResolveProvider(string domain)
    {
        var activeProjectId = workspace.ActiveProject?.Id;
        return providers.FirstOrDefault(
                   provider => provider.Domains.Contains(domain, StringComparer.Ordinal)
                       && provider.ProjectScopeId == activeProjectId)
            ?? providers.FirstOrDefault(
                provider => provider.Domains.Contains(domain, StringComparer.Ordinal)
                    && provider.ProjectScopeId is null);
    }

    private static void ValidateProviders(IReadOnlyList<INavigationTargetProvider> registered)
    {
        var claims = registered
            .SelectMany(provider => provider.Domains.Select(domain => (provider.ProjectScopeId, Domain: domain)))
            .GroupBy(claim => claim);
        foreach (var group in claims)
        {
            if (group.Count() > 1)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Navigation domain '{group.Key.Domain}' is registered more than once for the same project scope."));
            }
        }
    }
}

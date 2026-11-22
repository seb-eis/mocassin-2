// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Icons;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Project;

namespace Mocassin.App.ViewModels.Documents.Project;

/// <summary>
/// Read-only canonical project JSON document.
/// </summary>
internal sealed partial class ProjectJsonViewModel : DocumentViewModel, IInspectorContributor
{
    private readonly ProjectWorkspaceEntry entry;
    private readonly IClipboardService clipboard;

    [ObservableProperty]
    private string jsonText;

    /// <summary>
    /// Initializes a project JSON document.
    /// </summary>
    /// <param name="entry">The project workspace entry.</param>
    /// <param name="clipboard">The clipboard boundary.</param>
    public ProjectJsonViewModel(ProjectWorkspaceEntry entry, IClipboardService clipboard)
        : base(new DocumentKey(DocumentKinds.ProjectJson, entry.Id))
    {
        this.entry = entry;
        this.clipboard = clipboard;
        jsonText = ProjectJson.Serialize(entry.Project, indented: true);
        entry.Session.EditCoordinator.ProjectChanged += OnProjectChanged;
    }

    /// <inheritdoc />
    public override string Title => $"Project JSON — {entry.Name}";

    /// <inheritdoc />
    public override object? Icon => new AppIcon(IconKeys.Json);

    /// <inheritdoc />
    public override string? ToolTip => entry.Path;

    /// <inheritdoc />
    public override bool IsDirty => entry.Session.EditCoordinator.IsDirty;

    /// <inheritdoc />
    public override Task OnClosingAsync(CancellationToken cancellationToken = default)
    {
        entry.Session.EditCoordinator.ProjectChanged -= OnProjectChanged;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public object? CreateInspector(InspectorSelection selection) =>
        new InspectorSummaryViewModel(
            "Project JSON",
            [entry.Path, "Read-only canonical document"]);

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        if (args.Kind == ProjectChangeKind.Saved)
        {
            return;
        }

        JsonText = ProjectJson.Serialize(entry.Project, indented: true);
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsDirty));
    }

    /// <summary>
    /// Copies the JSON text.
    /// </summary>
    [RelayCommand]
    private Task CopyJsonAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(JsonText, cancellationToken);
}

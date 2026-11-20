// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
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
        JsonText = ProjectJson.Serialize(entry.Project, indented: true);
    }

    /// <inheritdoc />
    public override string Title => $"Project JSON — {entry.Name}";

    /// <inheritdoc />
    public override string? ToolTip => entry.Path;

    /// <summary>
    /// Gets the indented canonical project JSON.
    /// </summary>
    public string JsonText { get; }

    /// <inheritdoc />
    public object? CreateInspector(InspectorSelection selection) =>
        new InspectorSummaryViewModel(
            "Project JSON",
            [entry.Path, "Read-only canonical document"]);

    /// <summary>
    /// Copies the JSON text.
    /// </summary>
    [RelayCommand]
    private Task CopyJsonAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(JsonText, cancellationToken);
}

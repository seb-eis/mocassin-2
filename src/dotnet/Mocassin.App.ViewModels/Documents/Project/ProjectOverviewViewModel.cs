// SPDX-License-Identifier: MIT

using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Documents.Project;

/// <summary>
/// Read-only project overview document. Editing, validation, and statistics are Step 23B.
/// </summary>
internal sealed partial class ProjectOverviewViewModel : DocumentViewModel, IInspectorContributor
{
    private readonly ProjectWorkspaceEntry entry;
    private readonly IClipboardService clipboard;

    /// <summary>
    /// Initializes a project overview document.
    /// </summary>
    /// <param name="entry">The project workspace entry.</param>
    /// <param name="clipboard">The clipboard boundary.</param>
    public ProjectOverviewViewModel(ProjectWorkspaceEntry entry, IClipboardService clipboard)
        : base(new DocumentKey(DocumentKinds.ProjectOverview, entry.Id))
    {
        this.entry = entry;
        this.clipboard = clipboard;
    }

    /// <inheritdoc />
    public override string Title => $"Overview — {entry.Name}";

    /// <inheritdoc />
    public override string? ToolTip => entry.Path;

    /// <summary>
    /// Gets the project id text.
    /// </summary>
    public string ProjectIdText => entry.Id.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the project name.
    /// </summary>
    public string Name => entry.Name;

    /// <summary>
    /// Gets the project description.
    /// </summary>
    public string Description => entry.Project.Metadata.Description ?? "—";

    /// <summary>
    /// Gets the project author.
    /// </summary>
    public string Author => entry.Project.Metadata.Author ?? "—";

    /// <summary>
    /// Gets the project notes.
    /// </summary>
    public string Notes => entry.Project.Metadata.Notes ?? "—";

    /// <summary>
    /// Gets the creation timestamp text.
    /// </summary>
    public string CreatedUtcText => FormatUtc(entry.Project.Metadata.CreatedUtc);

    /// <summary>
    /// Gets the last-saved timestamp text.
    /// </summary>
    public string LastSavedText => FormatUtc(entry.LastWrittenUtc);

    /// <summary>
    /// Gets the model-modified timestamp text.
    /// </summary>
    public string ModifiedUtcText => FormatUtc(entry.Project.Metadata.ModifiedUtc);

    /// <summary>
    /// Gets the master seed.
    /// </summary>
    public string MasterSeed => entry.Project.Metadata.MasterSeed;

    /// <summary>
    /// Gets the schema version text.
    /// </summary>
    public string SchemaVersionText => entry.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the symmetry metadata summary.
    /// </summary>
    public string SymmetryText
    {
        get
        {
            var symmetry = entry.Project.Symmetry;
            return symmetry is null
                ? "Not recorded"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"Catalog {symmetry.SpaceGroupDataVersion ?? "unknown"} · contract {symmetry.SymmetryContractVersion ?? "unknown"}");
        }
    }

    /// <summary>
    /// Gets the project file path.
    /// </summary>
    public string FilePath => entry.Path;

    /// <summary>
    /// Gets a value indicating whether the project is open read-only.
    /// </summary>
    public bool IsReadOnly => entry.IsReadOnly;

    /// <summary>
    /// Gets the lock and read-only state text.
    /// </summary>
    public string LockStateText => entry.IsReadOnly
        ? entry.LockPath is null ? "Read-only" : $"Read-only (locked at {entry.LockPath})"
        : "Writable";

    /// <inheritdoc />
    public object? CreateInspector(InspectorSelection selection) =>
        new InspectorSummaryViewModel(
            Name,
            [FilePath, LockStateText, $"Project {ProjectIdText}"]);

    /// <summary>
    /// Copies the master seed.
    /// </summary>
    [RelayCommand]
    private Task CopyMasterSeedAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(MasterSeed, cancellationToken);

    /// <summary>
    /// Copies the project path.
    /// </summary>
    [RelayCommand]
    private Task CopyPathAsync(CancellationToken cancellationToken) =>
        clipboard.SetTextAsync(FilePath, cancellationToken);

    private static string FormatUtc(DateTimeOffset? value) =>
        value is null
            ? "Not yet saved"
            : value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);
}

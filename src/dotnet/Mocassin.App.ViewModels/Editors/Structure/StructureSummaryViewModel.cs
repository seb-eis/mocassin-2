// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// The structure summary. Every value is derived from <see cref="StructureModel"/> or the editor's own
/// <see cref="StructurePreview"/>; the summary deliberately never calls <c>ModelStatistics</c> (23B keeps
/// statistics an explicit on-demand action).
/// </summary>
internal sealed class StructureSummaryViewModel : ObservableObject, IDisposable
{
    private readonly ProjectSession session;
    private readonly IStructurePreviewService previewService;
    private readonly IStructureValidator validator;
    private int referenceSiteCount;
    private int p1PositionCount;
    private int stablePositionCount;
    private int unstablePositionCount;
    private double cellVolumeAngstrom3;
    private double referenceSiteDensity;
    private bool hasPreview;
    private string previewStatus = string.Empty;
    private string? largeP1WarningText;
    private bool disposed;

    /// <summary>
    /// Initializes the structure summary.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public StructureSummaryViewModel(
        ProjectSession session,
        IStructurePreviewService previewService,
        IStructureValidator validator)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        session.EditCoordinator.ProjectChanged += OnProjectChanged;
        Refresh();
    }

    /// <summary>
    /// Gets the reference-site count from <c>StructureModel.Sites</c>.
    /// </summary>
    public int ReferenceSiteCount
    {
        get => referenceSiteCount;
        private set => SetProperty(ref referenceSiteCount, value);
    }

    /// <summary>
    /// Gets the P1 position count from <c>StructurePreview.PositionList</c>.
    /// </summary>
    public int P1PositionCount
    {
        get => p1PositionCount;
        private set => SetProperty(ref p1PositionCount, value);
    }

    /// <summary>
    /// Gets the stable P1 **position** count. The label is deliberately "positions", never "sites".
    /// </summary>
    public int StablePositionCount
    {
        get => stablePositionCount;
        private set => SetProperty(ref stablePositionCount, value);
    }

    /// <summary>
    /// Gets the unstable P1 **position** count.
    /// </summary>
    public int UnstablePositionCount
    {
        get => unstablePositionCount;
        private set => SetProperty(ref unstablePositionCount, value);
    }

    /// <summary>
    /// Gets the unit-cell volume from <c>StructurePreview.CellVolumeAngstrom3</c>.
    /// </summary>
    public double CellVolumeAngstrom3
    {
        get => cellVolumeAngstrom3;
        private set
        {
            if (SetProperty(ref cellVolumeAngstrom3, value))
            {
                OnPropertyChanged(nameof(CellVolumeText));
            }
        }
    }

    /// <summary>
    /// Gets the reference-site density in Å⁻³ (<c>ReferenceSiteCount / CellVolumeAngstrom3</c>).
    /// </summary>
    public double ReferenceSiteDensity
    {
        get => referenceSiteDensity;
        private set
        {
            if (SetProperty(ref referenceSiteDensity, value))
            {
                OnPropertyChanged(nameof(ReferenceSiteDensityText));
                OnPropertyChanged(nameof(DensityLabel));
            }
        }
    }

    /// <summary>
    /// Gets the volume text.
    /// </summary>
    public string CellVolumeText => string.Create(
        CultureInfo.InvariantCulture,
        $"{CellVolumeAngstrom3:0.####} Å³");

    /// <summary>
    /// Gets the density text.
    /// </summary>
    public string ReferenceSiteDensityText => string.Create(
        CultureInfo.InvariantCulture,
        $"{ReferenceSiteDensity:0.######} Å⁻³");

    /// <summary>
    /// Gets the explicit density label.
    /// </summary>
    public string DensityLabel { get; } = "Reference-site density (reference sites / cell volume)";

    /// <summary>
    /// Gets a value indicating whether the preview is available.
    /// </summary>
    public bool HasPreview
    {
        get => hasPreview;
        private set => SetProperty(ref hasPreview, value);
    }

    /// <summary>
    /// Gets the preview status.
    /// </summary>
    public string PreviewStatus
    {
        get => previewStatus;
        private set => SetProperty(ref previewStatus, value);
    }

    /// <summary>
    /// Gets the orbit table.
    /// </summary>
    public ObservableCollection<StructureSummaryOrbitRowViewModel> Orbits { get; } = [];

    /// <summary>
    /// Gets the large-P1 warning text from the structure validator's <c>STR009</c>.
    /// </summary>
    public string? LargeP1WarningText
    {
        get => largeP1WarningText;
        private set
        {
            if (SetProperty(ref largeP1WarningText, value))
            {
                OnPropertyChanged(nameof(HasLargeP1Warning));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a large-P1 warning is present.
    /// </summary>
    public bool HasLargeP1Warning => !string.IsNullOrWhiteSpace(LargeP1WarningText);

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        session.EditCoordinator.ProjectChanged -= OnProjectChanged;
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        if (args.Kind == ProjectChangeKind.Saved)
        {
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        var project = session.Project;
        var structure = project.Structure;
        var sites = structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structure.Sites;
        ReferenceSiteCount = sites.Length;

        var preview = previewService.TryCreatePreview(structure, 1, 1, 1, out var previewDiagnostics);
        var validation = validator.Validate(structure);
        LargeP1WarningText = validation
            .FirstOrDefault(static diagnostic => string.Equals(diagnostic.Code, "STR009", StringComparison.Ordinal))
            ?.Message;

        Orbits.Clear();
        if (preview is null)
        {
            P1PositionCount = 0;
            StablePositionCount = 0;
            UnstablePositionCount = 0;
            CellVolumeAngstrom3 = 0d;
            ReferenceSiteDensity = 0d;
            HasPreview = false;
            PreviewStatus = previewDiagnostics.IsDefaultOrEmpty
                ? "No structure preview is available."
                : "No structure preview is available: "
                    + string.Join("; ", previewDiagnostics.Select(static diagnostic => diagnostic.Message));
            return;
        }

        HasPreview = true;
        PreviewStatus = "Structure preview ready.";
        P1PositionCount = preview.PositionList.Length;
        var stable = 0;
        var unstable = 0;
        foreach (var position in preview.PositionList)
        {
            if (position.ReferenceSiteIndex >= 0 && position.ReferenceSiteIndex < sites.Length
                && sites[position.ReferenceSiteIndex].Stability == SiteStability.Unstable)
            {
                unstable++;
            }
            else
            {
                stable++;
            }
        }

        StablePositionCount = stable;
        UnstablePositionCount = unstable;
        CellVolumeAngstrom3 = preview.CellVolumeAngstrom3;
        ReferenceSiteDensity = CellVolumeAngstrom3 > 0d
            ? ReferenceSiteCount / CellVolumeAngstrom3
            : 0d;

        foreach (var orbit in preview.Orbits)
        {
            var siteName = orbit.ReferenceSiteIndex >= 0 && orbit.ReferenceSiteIndex < sites.Length
                ? sites[orbit.ReferenceSiteIndex].Name
                : string.Empty;
            Orbits.Add(new StructureSummaryOrbitRowViewModel(
                siteName,
                FormatFractional(orbit.Representative),
                orbit.Multiplicity,
                orbit.Positions.Length));
        }
    }

    private static string FormatFractional(Mocassin.Core.Geometry.Fractional3 value) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({value.A:0.####}, {value.B:0.####}, {value.C:0.####})");
}

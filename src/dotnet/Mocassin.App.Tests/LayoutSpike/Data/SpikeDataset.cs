// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Editing;
using Mocassin.Core.Project;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// One bound project plus the spike tables built from it, and the real edit coordinator when the dataset is
/// editable.
/// </summary>
/// <remarks>
/// The dataset never fakes an undo stack: editable datasets are wrapped by the production
/// <see cref="ProjectEditCoordinator"/> over a production <see cref="ProjectDocument"/>, and a rebuild is driven by
/// the coordinator's <see cref="ProjectEditCoordinator.ProjectChanged"/> event.
/// </remarks>
internal sealed class SpikeDataset
{
    private readonly Func<Project, ImmutableArray<SpikeTable>> builder;

    /// <summary>
    /// Initializes a dataset.
    /// </summary>
    /// <param name="label">The sample label, matching the sample's file stem.</param>
    /// <param name="project">The loaded project value.</param>
    /// <param name="editable">Whether the dataset is the editable document for its domains.</param>
    /// <param name="builder">Builds the domain tables from the current project value.</param>
    public SpikeDataset(
        string label,
        Project project,
        bool editable,
        Func<Project, ImmutableArray<SpikeTable>> builder)
    {
        Label = label;
        this.builder = builder;
        Document = new ProjectDocument(project);
        Coordinator = editable ? new ProjectEditCoordinator(Document) : null;
        Tables = builder(project);
        if (Coordinator is not null)
        {
            Coordinator.ProjectChanged += (_, _) => Rebuild();
        }
    }

    /// <summary>
    /// Gets the sample label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the project document. It is the real editing document, not a copy of the display cells.
    /// </summary>
    public ProjectDocument Document { get; }

    /// <summary>
    /// Gets the real edit coordinator, or <see langword="null"/> for a display-only dataset.
    /// </summary>
    public ProjectEditCoordinator? Coordinator { get; }

    /// <summary>
    /// Gets a value indicating whether the dataset is the editable document for its domains.
    /// </summary>
    public bool IsEditable => Coordinator is not null;

    /// <summary>
    /// Gets the current tables.
    /// </summary>
    public ImmutableArray<SpikeTable> Tables { get; private set; }

    /// <summary>
    /// Raised after the tables were rebuilt from a changed project value.
    /// </summary>
    public event EventHandler? Rebuilt;

    /// <summary>
    /// Rebuilds the tables from the current project value.
    /// </summary>
    public void Rebuild()
    {
        Tables = builder(Document.Project);
        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets one table by domain.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <returns>The table, or <see langword="null"/> when the dataset owns no such table.</returns>
    public SpikeTable? Table(SpikeDomain domain) =>
        Tables.FirstOrDefault(table => table.Domain == domain);
}

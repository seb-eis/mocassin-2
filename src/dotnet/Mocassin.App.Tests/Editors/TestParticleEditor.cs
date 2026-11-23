// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// A concrete headless editor over the real particle domain, used to prove the shared framework without the
/// feature-2 particle UX.
/// </summary>
internal sealed class TestParticleEditor : ModelEditorViewModel<Particle>
{
    /// <summary>
    /// The test column policy.
    /// </summary>
    public static readonly IReadOnlyList<ModelColumn> TestColumns =
    [
        new("Name", ModelColumnWidth.Star(2d)),
        new("Charge", ModelColumnWidth.Fixed(90d), SortKind: ModelSortKind.Number),
        new("Vacancy", ModelColumnWidth.Fixed(90d)),
        new("Usage", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("Status", ModelColumnWidth.Fixed(80d), IsSortable: false),
    ];

    /// <summary>
    /// Initializes the test editor.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="layout">The layout.</param>
    public TestParticleEditor(ProjectSession session, IEditorPaneLayout layout)
        : base(session, ModelEditorDomains.Particles, layout)
    {
        Table.SetColumns(TestColumns);
        Table.IsMultiSelectEnabled = true;
        RebuildRows();
    }

    /// <summary>
    /// Adds one particle through the coordinator.
    /// </summary>
    /// <param name="name">The particle name.</param>
    /// <returns>The new id.</returns>
    public ParticleId AddParticle(string name)
    {
        var id = ParticleId.New();
        var result = Coordinator.Apply(
            "Add particle",
            (editor, project) => editor.AddParticle(
                project,
                new Particle(id, name, "X", new ChargeElementary(0d), IsVacancy: false)));
        if (result.Outcome == EditApplyOutcome.Applied)
        {
            RequestSelection(id.Value);
        }

        return id;
    }

    /// <inheritdoc />
    protected override ImmutableArray<Particle> GetModels(ProjectModel project) => project.Particles;

    /// <inheritdoc />
    protected override Guid GetId(Particle model) => model.Id.Value;

    /// <inheritdoc />
    protected override ModelTableRow ToRow(Particle model)
    {
        var usage = Usages.Count(ModelObjectRef.Particle(model.Id));
        return new ModelTableRow(
            model.Id.Value,
            [
                ModelTableCell.OfText(model.Name),
                ModelTableCell.OfNumber(model.Charge.Value),
                ModelTableCell.OfText(model.IsVacancy ? "yes" : "no"),
                ModelTableCell.OfNumber(usage),
                ModelTableCell.OfText(string.Empty),
            ]);
    }

    /// <inheritdoc />
    protected override ModelDetailViewModel CreateDetailFor(Particle model) => new TestParticleDetail(model);

    /// <inheritdoc />
    protected override void AddCore() => AddParticle($"Particle {Project.Particles.Length + 1}");

    /// <inheritdoc />
    protected override bool RemoveCore(Guid objectId)
    {
        var result = Coordinator.Apply(
            "Remove particle",
            (editor, project) => editor.RemoveParticle(project, ParticleId.Create(objectId)));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    /// <inheritdoc />
    protected override void DuplicateCore(Guid objectId)
    {
        var source = Project.Find(ParticleId.Create(objectId));
        if (source is null)
        {
            return;
        }

        var id = ParticleId.New();
        var result = Coordinator.Apply(
            "Duplicate particle",
            (editor, project) => editor.AddParticle(
                project,
                source with { Id = id, Name = $"{source.Name} copy" }));
        if (result.Outcome == EditApplyOutcome.Applied)
        {
            RequestSelection(id.Value);
        }
    }
}

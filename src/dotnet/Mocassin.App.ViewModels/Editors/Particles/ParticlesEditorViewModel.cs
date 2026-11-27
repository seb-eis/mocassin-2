// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Particles;

/// <summary>
/// The particles master-detail editor. The implicit void particle is pinned first and is read-only; authored
/// particles support add, per-field edit, duplicate, bulk charge, and blocked delete via the shared usage index.
/// </summary>
internal sealed class ParticlesEditorViewModel : ModelEditorViewModel<Particle>
{
    /// <summary>
    /// The particle table's column policy.
    /// </summary>
    public static readonly IReadOnlyList<ModelColumn> Columns =
    [
        new("Name", ModelColumnWidth.Star(2d)),
        new("Symbol", ModelColumnWidth.Star(1d)),
        new("Charge", ModelColumnWidth.Fixed(84d), SortKind: ModelSortKind.Number),
        new("Vacancy", ModelColumnWidth.Fixed(84d)),
        new("Index", ModelColumnWidth.Fixed(64d), SortKind: ModelSortKind.Number),
        new("Usage", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("Status", ModelColumnWidth.Fixed(90d), IsSortable: false),
    ];

    private string? bulkChargeError;

    /// <summary>
    /// Initializes the particles editor.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    public ParticlesEditorViewModel(ProjectSession session, IEditorPaneLayout layout)
        : base(session, ModelEditorDomains.Particles, layout)
    {
        Table.SetColumns(Columns);
        Table.IsMultiSelectEnabled = true;
        ActionBar = new ModelEditorActionBarViewModel(
            "Bulk set charge",
            "Charge (e)",
            ApplyBulkChargeText,
            () => CanEdit);
        RebuildRows();
    }

    /// <summary>
    /// Gets the bulk-charge validation message.
    /// </summary>
    public string? BulkChargeError
    {
        get => bulkChargeError;
        private set
        {
            if (SetProperty(ref bulkChargeError, value))
            {
                OnPropertyChanged(nameof(HasBulkChargeError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the bulk-charge input is invalid.
    /// </summary>
    public bool HasBulkChargeError => !string.IsNullOrWhiteSpace(BulkChargeError);

    /// <inheritdoc />
    protected override ImmutableArray<Particle> GetModels(ProjectModel project)
    {
        var builder = ImmutableArray.CreateBuilder<Particle>(project.Particles.Length + 1);
        builder.Add(Particle.Void);
        builder.AddRange(project.Particles);
        return builder.MoveToImmutable();
    }

    /// <inheritdoc />
    protected override Guid GetId(Particle model) => model.Id.Value;

    /// <inheritdoc />
    protected override ModelTableRow ToRow(Particle model)
    {
        var usage = model.IsVoid ? 0 : Usages.Count(ModelObjectRef.Particle(model.Id));
        var index = model.IsVoid ? 0 : IndexOfAuthored(model.Id) + 1;
        var severity = SeverityFor(model.Id.Value);
        return new ModelTableRow(
            model.Id.Value,
            [
                ModelTableCell.OfText(model.IsVoid ? "Void (implicit)" : model.Name),
                ModelTableCell.OfText(model.Symbol),
                ModelTableCell.OfNumber(model.Charge.Value),
                ModelTableCell.OfText(model.IsVacancy ? "yes" : "no"),
                ModelTableCell.OfNumber(index),
                ModelTableCell.OfNumber(usage),
                ModelTableCell.OfText(severity == ModelRowSeverity.None ? string.Empty : severity.ToString()),
            ],
            severity,
            severity == ModelRowSeverity.None ? string.Empty : severity.ToString());
    }

    /// <inheritdoc />
    protected override ModelDetailViewModel? CreateDetailFor(Particle model) =>
        model.IsVoid ? null : new ParticleDetailViewModel(model);

    /// <inheritdoc />
    protected override bool IsReadOnlyObject(Guid objectId) => objectId == ParticleId.Void.Value;

    /// <inheritdoc />
    protected override string? GetDetailMessage(Guid objectId) =>
        objectId == ParticleId.Void.Value
            ? "The implicit void particle is pinned at index 0. It is never persisted, cannot be edited or "
                + "removed, and is not counted as an authored particle."
            : null;

    /// <inheritdoc />
    protected override void AddCore() => AddParticle(NextParticleName());

    /// <inheritdoc />
    protected override void DuplicateCore(Guid objectId)
    {
        var source = Project.Find(ParticleId.Create(objectId));
        if (source is null || source.IsVoid)
        {
            return;
        }

        var id = ParticleId.New();
        var result = Coordinator.Apply(
            "Duplicate particle",
            (editor, project) => editor.AddParticle(
                project,
                source with { Id = id, Name = UniqueName(source.Name + " copy") }));
        if (result.Outcome == EditApplyOutcome.Applied && result.HasChange)
        {
            RequestSelection(id.Value);
        }
    }

    /// <inheritdoc />
    protected override bool RemoveManyCore(IReadOnlyList<Guid> objectIds)
    {
        var steps = objectIds
            .Where(id => id != ParticleId.Void.Value)
            .Select(id => (Func<ProjectEditor, ProjectModel, ProjectEditResult>)((editor, project) =>
                editor.RemoveParticle(project, ParticleId.Create(id))))
            .ToArray();
        if (steps.Length == 0)
        {
            return false;
        }

        var result = Coordinator.Apply("Remove particle(s)", BulkEditComposer.Compose(steps));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    /// <summary>
    /// Adds one authored particle through the coordinator.
    /// </summary>
    /// <param name="name">The unique particle name.</param>
    /// <returns>The new particle id.</returns>
    public ParticleId AddParticle(string name)
    {
        var id = ParticleId.New();
        var result = Coordinator.Apply(
            "Add particle",
            (editor, project) => editor.AddParticle(
                project,
                new Particle(id, name, "X", new ChargeElementary(0d), IsVacancy: false)));
        if (result.Outcome == EditApplyOutcome.Applied && result.HasChange)
        {
            RequestSelection(id.Value);
        }

        return id;
    }

    /// <summary>
    /// Applies one charge to every selected authored particle as one composed, all-or-nothing undo entry.
    /// </summary>
    /// <param name="charge">The charge in elementary units.</param>
    /// <returns>The app-level result, or <see langword="null"/> for an invalid input or no selection.</returns>
    public EditApplyResult? SetChargeForSelection(double charge)
    {
        if (!double.IsFinite(charge) || charge < ChargeElementary.Minimum || charge > ChargeElementary.Maximum)
        {
            SetBulkChargeError(
                $"The charge must be between {ChargeElementary.Minimum} and {ChargeElementary.Maximum}.");
            return null;
        }

        BulkChargeError = null;
        var ids = Table.SelectedRows
            .Select(static row => row.ObjectId)
            .Where(id => id != ParticleId.Void.Value)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            SetBulkChargeError("Select at least one authored particle.");
            return null;
        }

        var value = new ChargeElementary(charge);
        var steps = ids
            .Select(id => (Func<ProjectEditor, ProjectModel, ProjectEditResult>)((editor, project) =>
            {
                var particle = project.Find(ParticleId.Create(id));
                return particle is null
                    ? new ProjectEditResult(project, null, [])
                    : editor.UpdateParticle(project, particle with { Charge = value });
            }))
            .ToArray();

        var result = Coordinator.Apply("Set particle charge", BulkEditComposer.Compose(steps));
        if (result.Outcome == EditApplyOutcome.Rejected)
        {
            SetBulkChargeError(result.Message);
        }
        else
        {
            SetBulkChargeError(null);
        }

        return result;
    }

    /// <summary>
    /// Gets the usage count text for the inspector.
    /// </summary>
    /// <param name="id">The particle id.</param>
    /// <returns>The usage count.</returns>
    public int UsageCount(ParticleId id) => id == ParticleId.Void ? 0 : Usages.Count(ModelObjectRef.Particle(id));

    private void ApplyBulkChargeText(string text)
    {
        if (!double.TryParse(
                text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var charge))
        {
            SetBulkChargeError("Enter a number such as 0, -2, or 3.5.");
            return;
        }

        SetChargeForSelection(charge);
    }

    private void SetBulkChargeError(string? message)
    {
        BulkChargeError = message;
        ActionBar?.SetError(message);
    }

    private int IndexOfAuthored(ParticleId id)
    {
        var particles = Project.Particles;
        for (var index = 0; index < particles.Length; index++)
        {
            if (particles[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }

    private ModelRowSeverity SeverityFor(Guid objectId)
    {
        var domain = ModelEditorDomains.Particle;
        var diagnostics = Session.ValidationCoordinator.Diagnostics
            .Concat(DiagnosticSummary.Diagnostics)
            .Where(diagnostic => diagnostic.Path is { } path
                && path.ObjectId == objectId
                && string.Equals(path.Domain, domain, StringComparison.Ordinal))
            .ToArray();
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return ModelRowSeverity.Error;
        }

        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning))
        {
            return ModelRowSeverity.Warning;
        }

        return diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Info)
            ? ModelRowSeverity.Info
            : ModelRowSeverity.None;
    }

    private string NextParticleName()
    {
        var names = Project.Particles
            .Select(static particle => particle.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; ; index++)
        {
            var candidate = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"Particle {index}");
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private string UniqueName(string baseName)
    {
        var names = Project.Particles
            .Select(static particle => particle.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(baseName))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{baseName} {suffix}");
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}

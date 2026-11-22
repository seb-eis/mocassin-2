// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Diagnostics;

using Mocassin.App.Tests.LayoutSpike.Data;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// The type-specific editor shared by all five layouts. It owns the fields, the inline validation message, the
/// searchable reference pickers, and the undo/redo entry points, and applies every change through the real
/// <see cref="ProjectEditCoordinator"/>.
/// </summary>
internal sealed partial class SpikeEditorViewModel : ObservableObject
{
    private readonly SpikeDomainViewModel domain;
    private SpikeRow? row;
    private string statusText = "Select a row to edit.";
    private string? errorText;
    private string readOnlyMessage = string.Empty;
    private SpikeMatrix? matrix;

    /// <summary>
    /// Initializes the editor for one domain.
    /// </summary>
    /// <param name="domain">The owning domain surface.</param>
    public SpikeEditorViewModel(SpikeDomainViewModel domain)
    {
        this.domain = domain;
        ApplyCommand = new RelayCommand(Apply, CanApply);
        RevertCommand = new RelayCommand(Revert, () => HasEditableRow);
        UndoCommand = new RelayCommand(Undo, () => Coordinator?.CanUndo == true);
        RedoCommand = new RelayCommand(Redo, () => Coordinator?.CanRedo == true);
    }

    /// <summary>Raised when the field list was replaced and the editor view must rebuild.</summary>
    public event EventHandler? FieldsChanged;

    /// <summary>Gets the current fields.</summary>
    public ObservableCollection<SpikeEditorField> Fields { get; } = [];

    /// <summary>Gets the editor heading.</summary>
    public string HeaderText => row is null
        ? "No selection"
        : $"{domain.Title} · {row.Title}";

    /// <summary>Gets a value indicating whether a row is selected.</summary>
    public bool HasSelection => row is not null;

    /// <summary>Gets a value indicating whether the selected row is editable.</summary>
    public bool HasEditableRow => row is not null && !row.IsReadOnly && Coordinator is not null;

    /// <summary>Gets the read-only explanation for an implicit or sample row.</summary>
    public string ReadOnlyMessage
    {
        get => readOnlyMessage;
        private set => SetProperty(ref readOnlyMessage, value);
    }

    /// <summary>Gets the inline error text.</summary>
    public string ErrorText
    {
        get => errorText ?? string.Empty;
        private set
        {
            if (SetProperty(ref errorText, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>Gets a value indicating whether the editor shows an inline error.</summary>
    public bool HasError => !string.IsNullOrEmpty(errorText);

    /// <summary>Gets the matrix-shaped preview for chain and transition rows, or <see langword="null"/>.</summary>
    public SpikeMatrix? Matrix
    {
        get => matrix;
        private set
        {
            if (SetProperty(ref matrix, value))
            {
                OnPropertyChanged(nameof(HasMatrix));
            }
        }
    }

    /// <summary>Gets a value indicating whether a matrix preview is available.</summary>
    public bool HasMatrix => matrix is not null;

    /// <summary>Gets the editor status text.</summary>
    public string StatusText
    {
        get => statusText;
        private set => SetProperty(ref statusText, value);
    }

    /// <summary>Gets the glyph-plus-text undo state.</summary>
    public string UndoText => Coordinator?.CanUndo == true ? "↶ Undo available" : "↶ Nothing to undo";

    /// <summary>Gets the glyph-plus-text redo state.</summary>
    public string RedoText => Coordinator?.CanRedo == true ? "↷ Redo available" : "↷ Nothing to redo";

    /// <summary>Gets the validation message shown in the editor's status glyph.</summary>
    public string ValidationGlyph => !HasSelection ? "·" : HasError ? "✕" : "✓";

    /// <summary>Gets the apply command.</summary>
    public RelayCommand ApplyCommand { get; }

    /// <summary>Gets the revert command.</summary>
    public RelayCommand RevertCommand { get; }

    /// <summary>Gets the undo command.</summary>
    public RelayCommand UndoCommand { get; }

    /// <summary>Gets the redo command.</summary>
    public RelayCommand RedoCommand { get; }

    /// <summary>Gets the document that owns the selected row.</summary>
    public SpikeDataset? Dataset => domain.Editable;

    /// <summary>Gets the coordinator that owns the selected row.</summary>
    public ProjectEditCoordinator? Coordinator => Dataset?.Coordinator;

    /// <summary>
    /// Loads a selected row into the editor.
    /// </summary>
    /// <param name="selected">The selected row, or <see langword="null"/>.</param>
    public void Load(SpikeRow? selected)
    {
        row = selected;
        ErrorText = string.Empty;
        Fields.Clear();
        if (selected is null)
        {
            ReadOnlyMessage = string.Empty;
            StatusText = "Select a row to edit.";
        }
        else if (selected.IsReadOnly)
        {
            ReadOnlyMessage = selected.Explanation
                ?? $"Read-only sample row from {selected.SourceLabel}. Editing is demonstrated on the bound "
                + "ceria documents.";
            StatusText = "Read-only row: no editor fields.";
            Matrix = null;
        }
        else if (Dataset is { } dataset)
        {
            ReadOnlyMessage = string.Empty;
            foreach (var field in SpikeEditorFields.Build(dataset.Document.Project, domain.Domain, selected))
            {
                Fields.Add(field);
            }

            Matrix = BuildMatrix(dataset.Document.Project, selected);
            StatusText = $"Editing through ProjectEditCoordinator · {dataset.Label}";
        }
        else
        {
            ReadOnlyMessage = "This domain is display-only in the spike.";
            StatusText = "Display-only domain.";
            Matrix = null;
        }

        OnPropertyChanged(nameof(HeaderText));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasEditableRow));
        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(RedoText));
        OnPropertyChanged(nameof(ValidationGlyph));
        FieldsChanged?.Invoke(this, EventArgs.Empty);
        RefreshCommands();
    }

    /// <summary>
    /// Applies the current field values through the real edit coordinator.
    /// </summary>
    public void Apply()
    {
        if (row is null || Coordinator is not { } coordinator)
        {
            return;
        }

        var values = new SpikeEditorValues(Fields);
        var candidate = SpikeEditorMutation.Build(
            coordinator.Document.Project,
            domain.Domain,
            row.Id,
            values,
            out var description,
            out var localError);
        if (candidate is null)
        {
            ErrorText = localError ?? "The edit is not valid.";
            StatusText = "Rejected locally — the project is unchanged.";
            OnPropertyChanged(nameof(ValidationGlyph));
            RefreshCommands();
            return;
        }

        var result = coordinator.Apply(description, _ => candidate);
        switch (result.Outcome)
        {
            case EditApplyOutcome.Applied when result.HasChange:
                ErrorText = string.Empty;
                StatusText = $"{description} applied. Undo is available.";
                break;
            case EditApplyOutcome.Applied:
                ErrorText = string.Empty;
                StatusText = "No change: the fields already match the project.";
                break;
            case EditApplyOutcome.Rejected:
                ErrorText = Describe(result.Edit?.Diagnostics);
                StatusText = "Rejected by the project validator — the project is unchanged.";
                break;
            default:
                ErrorText = result.Message ?? "This project is read-only.";
                StatusText = "Read-only — the project is unchanged.";
                break;
        }

        OnPropertyChanged(nameof(ValidationGlyph));
        RefreshCommands();
    }

    /// <summary>
    /// Reverts the fields to the current project values.
    /// </summary>
    public void Revert()
    {
        Load(row);
        StatusText = "Changes reverted to the project values.";
    }

    /// <summary>
    /// Undoes the most recent edit through the real document stack.
    /// </summary>
    public void Undo()
    {
        if (Coordinator?.Undo() == true)
        {
            StatusText = "Undo applied. Rows and fields were rebuilt from the document.";
        }
        else
        {
            StatusText = "Nothing to undo.";
        }

        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(RedoText));
        RefreshCommands();
    }

    /// <summary>
    /// Redoes the most recently undone edit through the real document stack.
    /// </summary>
    public void Redo()
    {
        if (Coordinator?.Redo() == true)
        {
            StatusText = "Redo applied. Rows and fields were rebuilt from the document.";
        }
        else
        {
            StatusText = "Nothing to redo.";
        }

        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(RedoText));
        RefreshCommands();
    }

    private bool CanApply() => HasEditableRow && Fields.Count > 0;

    private SpikeMatrix? BuildMatrix(Mocassin.Core.Project.Project project, SpikeRow selected)
    {
        var chain = domain.Domain switch
        {
            SpikeDomain.Chains => SpikeModelLookup.FindChain(project, selected.Id),
            SpikeDomain.Transitions => SpikeModelLookup.FindTransitionChain(project, selected.Id),
            _ => null,
        };
        return chain is null ? null : SpikeMatrix.FromChain(project, chain);
    }

    private void RefreshCommands()
    {
        ApplyCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(RedoText));
    }

    private static string Describe(IReadOnlyList<Diagnostic>? diagnostics)
    {
        var first = diagnostics?.FirstOrDefault(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return first is null ? "Rejected by the project validator." : $"{first.Code}: {first.Message}";
    }
}

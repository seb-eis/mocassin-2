// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// The single write path over one <see cref="ProjectDocument"/>, with undo/redo, dirty tracking, and change
/// notifications.
/// </summary>
/// <remarks>
/// A coordinator is owned by a project session and lives on the UI thread. It never mutates a project itself: every
/// edit goes through <c>ProjectDocument.Apply</c> and therefore <see cref="ProjectEditor"/>, which validates
/// the candidate with the structural <see cref="ProjectValidator"/>. It is not the full validation pipeline; a
/// structurally valid but cross-domain-invalid edit is accepted and reported by the validation surface later.
/// </remarks>
internal sealed partial class ProjectEditCoordinator : ObservableObject
{
    /// <summary>
    /// The app-owned read-only refusal message. It is deliberately not a diagnostic code (M-1).
    /// </summary>
    internal const string ReadOnlyMessage = "This project is open read-only; changes cannot be saved.";

    private ProjectDocument document;
    private bool canEdit;
    private bool isDirty;
    private bool canUndo;
    private bool canRedo;

    /// <summary>
    /// Initializes the coordinator over a project document.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="canEdit">Whether the session may edit; a read-only session refuses every apply.</param>
    public ProjectEditCoordinator(ProjectDocument document, bool canEdit = true)
    {
        this.document = document ?? throw new ArgumentNullException(nameof(document));
        this.canEdit = canEdit;
        RefreshState();
    }

    /// <summary>
    /// Gets the current project document.
    /// </summary>
    public ProjectDocument Document => document;

    /// <summary>
    /// Gets a value indicating whether the session may edit. A read-only session returns
    /// <see cref="EditApplyOutcome.ReadOnly"/> from every apply and never changes.
    /// </summary>
    public bool CanEdit => canEdit;

    /// <summary>
    /// Gets the cached dirty state of the document.
    /// </summary>
    public bool IsDirty => isDirty;

    /// <summary>
    /// Gets a value indicating whether undo is available.
    /// </summary>
    public bool CanUndo => canUndo;

    /// <summary>
    /// Gets a value indicating whether redo is available.
    /// </summary>
    public bool CanRedo => canRedo;

    /// <summary>
    /// Raised after an accepted edit, undo, redo, save notification, or document replacement.
    /// </summary>
    public event EventHandler<ProjectChangedEventArgs>? ProjectChanged;

    /// <summary>
    /// Raised when the structural validator refuses an edit; no <see cref="ProjectChanged"/> is raised.
    /// </summary>
    public event EventHandler<EditRejectedEventArgs>? EditRejected;

    /// <summary>
    /// Applies one validated mutation, records one undoable change, and clears the redo stack on success.
    /// </summary>
    /// <param name="description">The user-visible change description.</param>
    /// <param name="mutation">The pure mutation that returns the candidate project.</param>
    /// <returns>
    /// A read-only refusal, a rejection with diagnostics, or an acceptance. A same-hash no-op is accepted with
    /// <see cref="EditApplyResult.HasChange"/> false and raises no event.
    /// </returns>
    public EditApplyResult Apply(string description, Func<ProjectModel, ProjectModel> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(mutation);

        if (!canEdit)
        {
            return new EditApplyResult(EditApplyOutcome.ReadOnly, null, ReadOnlyMessage);
        }

        var previous = document.Project;
        var edit = document.Apply(description, mutation);
        if (!edit.IsSuccess)
        {
            var message = edit.Diagnostics
                .FirstOrDefault(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                ?.Message;
            EditRejected?.Invoke(this, new EditRejectedEventArgs(description, edit.Diagnostics));
            return new EditApplyResult(EditApplyOutcome.Rejected, edit, message);
        }

        if (edit.Change is null)
        {
            return new EditApplyResult(EditApplyOutcome.Applied, edit, null);
        }

        RefreshState();
        OnProjectChanged(ProjectChangeKind.Edited, previous, document.Project, description);
        return new EditApplyResult(EditApplyOutcome.Applied, edit, null);
    }

    /// <summary>
    /// Applies one typed <see cref="ProjectEditor"/> operation through the single write path.
    /// </summary>
    /// <param name="description">The user-visible change description.</param>
    /// <param name="operation">The typed operation, invoked with the document's own editor.</param>
    /// <returns>
    /// A read-only refusal, a rejection with diagnostics, or an acceptance. A same-hash no-op is accepted with
    /// <see cref="EditApplyResult.HasChange"/> false and raises no event.
    /// </returns>
    public EditApplyResult Apply(
        string description,
        Func<ProjectEditor, ProjectModel, ProjectEditResult> operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(operation);

        if (!canEdit)
        {
            return new EditApplyResult(EditApplyOutcome.ReadOnly, null, ReadOnlyMessage);
        }

        var previous = document.Project;
        var edit = document.Apply(description, operation);
        if (!edit.IsSuccess)
        {
            var message = edit.Diagnostics
                .FirstOrDefault(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                ?.Message;
            EditRejected?.Invoke(this, new EditRejectedEventArgs(description, edit.Diagnostics));
            return new EditApplyResult(EditApplyOutcome.Rejected, edit, message);
        }

        if (edit.Change is null)
        {
            return new EditApplyResult(EditApplyOutcome.Applied, edit, null);
        }

        RefreshState();
        OnProjectChanged(ProjectChangeKind.Edited, previous, document.Project, description);
        return new EditApplyResult(EditApplyOutcome.Applied, edit, null);
    }

    /// <summary>
    /// Undoes the most recent change.
    /// </summary>
    /// <returns><see langword="true"/> when a change was undone and announced.</returns>
    public bool Undo()
    {
        if (!canEdit || !document.CanUndo)
        {
            return false;
        }

        var change = document.UndoStack[^1];
        var previous = document.Project;
        if (!document.Undo())
        {
            return false;
        }

        RefreshState();
        OnProjectChanged(ProjectChangeKind.Undo, previous, document.Project, change.Description);
        return true;
    }

    /// <summary>
    /// Redoes the most recently undone change.
    /// </summary>
    /// <returns><see langword="true"/> when a change was redone and announced.</returns>
    public bool Redo()
    {
        if (!canEdit || !document.CanRedo)
        {
            return false;
        }

        var change = document.RedoStack[^1];
        var previous = document.Project;
        if (!document.Redo())
        {
            return false;
        }

        RefreshState();
        OnProjectChanged(ProjectChangeKind.Redo, previous, document.Project, change.Description);
        return true;
    }

    /// <summary>
    /// Refreshes the cached dirty state and announces that the document's current value was saved.
    /// </summary>
    /// <remarks>
    /// The save path must first call <see cref="ProjectDocument.MarkSaved()"/> (the no-arg overload) so the clean
    /// point is the exact in-memory value; the store's returned timestamp is kept outside the document.
    /// </remarks>
    public void NotifySaved()
    {
        var current = document.Project;
        RefreshState();
        OnProjectChanged(ProjectChangeKind.Saved, current, current, "Save project");
    }

    /// <summary>
    /// Replaces the edited document and refreshes the read-only and undo/redo state.
    /// </summary>
    /// <param name="document">The new document; a reload or recovery document has empty stacks.</param>
    /// <param name="canEdit">Whether the session may edit the replacement.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public void ReplaceDocument(ProjectDocument document, bool canEdit)
    {
        ArgumentNullException.ThrowIfNull(document);

        var previous = this.document.Project;
        this.document = document;
        SetCanEdit(canEdit);
        RefreshState();
        OnProjectChanged(ProjectChangeKind.Replaced, previous, document.Project, "Replace project document");
    }

    /// <summary>
    /// Updates the cached read-only state when the session acquires or loses a lock.
    /// </summary>
    /// <param name="canEdit">Whether the session may now edit.</param>
    internal void SetCanEdit(bool canEdit)
    {
        if (this.canEdit == canEdit)
        {
            return;
        }

        this.canEdit = canEdit;
        OnPropertyChanged(nameof(CanEdit));
    }

    private void RefreshState()
    {
        var dirty = document.IsDirty;
        if (isDirty != dirty)
        {
            isDirty = dirty;
            OnPropertyChanged(nameof(IsDirty));
        }

        var undo = document.CanUndo;
        if (canUndo != undo)
        {
            canUndo = undo;
            OnPropertyChanged(nameof(CanUndo));
        }

        var redo = document.CanRedo;
        if (canRedo != redo)
        {
            canRedo = redo;
            OnPropertyChanged(nameof(CanRedo));
        }
    }

    private void OnProjectChanged(
        ProjectChangeKind kind,
        ProjectModel previous,
        ProjectModel current,
        string description) =>
        ProjectChanged?.Invoke(this, new ProjectChangedEventArgs(kind, previous, current, description));
}

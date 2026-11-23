// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Editing;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The detail-editor base. A detail owns the editable field values (which is why it is an
/// <see cref="ObservableValidator"/>), assembles one typed <see cref="ProjectEditor"/> operation, and commits it
/// through the single coordinator write path.
/// </summary>
/// <remarks>
/// A single-field detail commits on <c>Enter</c>/focus loss; a multi-field form assembles every field into one
/// operation behind Apply. Nothing is written before the operation is committed.
/// </remarks>
internal abstract partial class ModelDetailViewModel : ObservableValidator
{
    private ProjectEditCoordinator? coordinator;
    private string? errorMessage;
    private string? conflictMessage;
    private bool hasUncommittedChanges;
    private bool isRemoved;

    /// <summary>
    /// Initializes a detail for one object.
    /// </summary>
    /// <param name="objectId">The stable object id.</param>
    protected ModelDetailViewModel(Guid objectId)
    {
        ObjectId = objectId;
        ApplyCommand = new RelayCommand(Apply, () => CanApply);
        RevertCommand = new RelayCommand(Revert, () => CanRevert);
    }

    /// <summary>
    /// Gets the stable object id.
    /// </summary>
    public Guid ObjectId { get; }

    /// <summary>
    /// Gets the coordinator after the editor attaches the detail.
    /// </summary>
    public ProjectEditCoordinator? Coordinator => coordinator;

    /// <summary>
    /// Gets or sets a value indicating whether the form holds uncommitted changes.
    /// </summary>
    public bool HasUncommittedChanges
    {
        get => hasUncommittedChanges;
        protected set
        {
            if (SetProperty(ref hasUncommittedChanges, value))
            {
                ApplyCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the user-visible change description.
    /// </summary>
    protected virtual string Description => "Apply edit";

    /// <summary>
    /// Gets the inline error from the last rejected commit.
    /// </summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the object changed elsewhere while this form was open.
    /// </summary>
    public string? ConflictMessage
    {
        get => conflictMessage;
        private set
        {
            if (SetProperty(ref conflictMessage, value))
            {
                OnPropertyChanged(nameof(HasConflict));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the object no longer exists.
    /// </summary>
    public bool IsRemoved
    {
        get => isRemoved;
        private set
        {
            if (SetProperty(ref isRemoved, value))
            {
                ApplyCommand.NotifyCanExecuteChanged();
                RevertCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an inline error is present.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether a conflict is present.
    /// </summary>
    public bool HasConflict => !string.IsNullOrWhiteSpace(ConflictMessage);

    /// <summary>
    /// Gets a value indicating whether Apply is enabled.
    /// </summary>
    public bool CanApply => coordinator?.CanEdit == true && !IsRemoved;

    /// <summary>
    /// Gets a value indicating whether Revert is enabled.
    /// </summary>
    public bool CanRevert => !IsRemoved;

    /// <summary>
    /// Gets the command that commits the assembled operation.
    /// </summary>
    public IRelayCommand ApplyCommand { get; }

    /// <summary>
    /// Gets the command that restores the last committed values.
    /// </summary>
    public IRelayCommand RevertCommand { get; }

    /// <summary>
    /// Raised when a navigation request asks the view to focus a field.
    /// </summary>
    public event EventHandler<string>? FocusRequested;

    /// <summary>
    /// Attaches the coordinator that owns the write path.
    /// </summary>
    /// <param name="value">The session coordinator.</param>
    internal void Attach(ProjectEditCoordinator value)
    {
        coordinator = value;
        ApplyCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Commits the detail's typed operation through the coordinator.
    /// </summary>
    /// <returns>The app-level result.</returns>
    public EditApplyResult Commit()
    {
        if (coordinator is null)
        {
            throw new InvalidOperationException("The detail is not attached to an editor coordinator.");
        }

        var result = coordinator.Apply(Description, ApplyCore);
        switch (result.Outcome)
        {
            case EditApplyOutcome.Applied:
                ErrorMessage = null;
                ConflictMessage = null;
                HasUncommittedChanges = false;
                break;
            case EditApplyOutcome.Rejected:
                ErrorMessage = result.Message ?? "The edit was rejected.";
                break;
            case EditApplyOutcome.ReadOnly:
                ErrorMessage = result.Message;
                break;
        }

        return result;
    }

    /// <summary>
    /// Restores the committed values.
    /// </summary>
    public void Revert()
    {
        RevertCore();
        ErrorMessage = null;
        ConflictMessage = null;
        HasUncommittedChanges = false;
    }

    /// <summary>
    /// Requests focus for one field.
    /// </summary>
    /// <param name="property">The field property name.</param>
    public void FocusField(string property)
    {
        if (string.IsNullOrWhiteSpace(property))
        {
            return;
        }

        FocusRequested?.Invoke(this, property);
    }

    /// <summary>
    /// Marks the object as changed elsewhere without silently rebasing the open fields.
    /// </summary>
    internal void MarkConflict() =>
        ConflictMessage = "The project changed elsewhere. Apply keeps this form's values; Revert loads the "
            + "current committed value.";

    /// <summary>
    /// Marks the object as removed elsewhere.
    /// </summary>
    internal void MarkRemoved() => IsRemoved = true;

    /// <summary>
    /// Refreshes reference-picker options after a foreign edit.
    /// </summary>
    /// <param name="project">The current project snapshot.</param>
    internal void RefreshOptions(ProjectModel project) => RefreshOptionsCore(project);

    /// <summary>
    /// Assembles the detail's typed operation. The delegate receives the document's own editor and project.
    /// </summary>
    /// <param name="editor">The document's editor.</param>
    /// <param name="project">The current project value.</param>
    /// <returns>The typed edit result.</returns>
    protected abstract ProjectEditResult ApplyCore(ProjectEditor editor, ProjectModel project);

    /// <summary>
    /// Restores the committed field values.
    /// </summary>
    protected virtual void RevertCore()
    {
    }

    /// <summary>
    /// Refreshes the reference-picker options from the snapshot.
    /// </summary>
    /// <param name="project">The current project snapshot.</param>
    protected virtual void RefreshOptionsCore(ProjectModel project)
    {
    }

    private void Apply() => Commit();
}

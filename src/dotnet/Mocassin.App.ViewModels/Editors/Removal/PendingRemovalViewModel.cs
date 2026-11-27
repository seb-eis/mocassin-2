// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Editors.Removal;

/// <summary>
/// The document-level "remove after confirmation" state and commands. The <c>Delete</c> gesture requests removal,
/// the document view renders the prompt, and Confirm runs the editor's bulk removal.
/// </summary>
internal sealed partial class PendingRemovalViewModel : ObservableObject
{
    private readonly Action<IReadOnlyList<Guid>> confirm;
    private string? text;
    private IReadOnlyList<Guid> objectIds = [];
    private bool isPending;

    /// <summary>
    /// Initializes the pending-removal state.
    /// </summary>
    /// <param name="confirm">The callback that performs the removal for the pending object ids.</param>
    public PendingRemovalViewModel(Action<IReadOnlyList<Guid>> confirm)
    {
        this.confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        ConfirmCommand = new RelayCommand(Confirm, () => IsPending);
        CancelCommand = new RelayCommand(Clear, () => IsPending);
    }

    /// <summary>
    /// Gets a value indicating whether a removal awaits confirmation.
    /// </summary>
    public bool IsPending
    {
        get => isPending;
        private set => SetProperty(ref isPending, value);
    }

    /// <summary>
    /// Gets the confirmation prompt.
    /// </summary>
    public string? Text
    {
        get => text;
        private set => SetProperty(ref text, value);
    }

    /// <summary>
    /// Gets the command that confirms the pending removal.
    /// </summary>
    public IRelayCommand ConfirmCommand { get; }

    /// <summary>
    /// Gets the command that cancels the pending removal.
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// Arms the confirmation for one or more object ids.
    /// </summary>
    /// <param name="prompt">The user-visible prompt.</param>
    /// <param name="ids">The object ids to remove on confirmation.</param>
    public void Request(string prompt, IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        text = prompt;
        objectIds = ids;
        IsPending = true;
        OnPropertyChanged(nameof(Text));
        NotifyCommands();
    }

    /// <summary>
    /// Clears the pending removal without changing the project.
    /// </summary>
    public void Clear()
    {
        objectIds = [];
        IsPending = false;
        OnPropertyChanged(nameof(Text));
        NotifyCommands();
    }

    private void Confirm()
    {
        var ids = objectIds;
        Clear();
        if (ids.Count > 0)
        {
            confirm(ids);
        }
    }

    private void NotifyCommands()
    {
        ConfirmCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}

// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Confirms a single action.
/// </summary>
internal sealed partial class ConfirmDialogViewModel : DialogViewModel
{
    /// <summary>
    /// Initializes a new confirmation dialog.
    /// </summary>
    /// <param name="request">The confirmation request.</param>
    public ConfirmDialogViewModel(ConfirmDialogRequest request)
        : base(request.Title, request.Message)
    {
        ConfirmText = request.ConfirmText;
        CancelText = request.CancelText;
        IsDestructive = request.IsDestructive;
    }

    /// <summary>
    /// Gets the confirm button text.
    /// </summary>
    public string ConfirmText { get; }

    /// <summary>
    /// Gets the cancel button text.
    /// </summary>
    public string CancelText { get; }

    /// <summary>
    /// Gets a value indicating whether the confirmation is destructive.
    /// </summary>
    public bool IsDestructive { get; }

    /// <summary>
    /// Confirms the dialog.
    /// </summary>
    [RelayCommand]
    private void Confirm() => Close(DialogResult.Confirmed);
}

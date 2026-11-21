// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Base view model for a dialog shown in the in-shell overlay.
/// </summary>
internal abstract partial class DialogViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new dialog view model.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    protected DialogViewModel(string title, string? message)
    {
        Title = title;
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the dialog message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets a value indicating whether the dialog can be cancelled with Escape.
    /// </summary>
    public virtual bool CanCancel => true;

    /// <summary>
    /// Gets the result once the dialog has closed.
    /// </summary>
    public DialogResult? Result { get; private set; }

    /// <summary>
    /// Occurs when the dialog closes.
    /// </summary>
    public event EventHandler<DialogViewModel>? Closed;

    /// <summary>
    /// Closes the dialog with a result.
    /// </summary>
    /// <param name="result">The result to record.</param>
    public void Close(DialogResult result)
    {
        if (Result is not null)
        {
            return;
        }

        Result = result;
        Closed?.Invoke(this, this);
    }

    /// <summary>
    /// Cancels the dialog.
    /// </summary>
    [RelayCommand]
    protected void Cancel() => Close(DialogResult.Canceled);
}

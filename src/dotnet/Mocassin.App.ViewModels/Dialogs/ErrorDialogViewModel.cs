// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Reports an error with optional diagnostic details.
/// </summary>
internal sealed partial class ErrorDialogViewModel : DialogViewModel
{
    /// <summary>
    /// Initializes a new error dialog.
    /// </summary>
    /// <param name="request">The error request.</param>
    public ErrorDialogViewModel(ErrorDialogRequest request)
        : base(request.Title, request.Message)
    {
        Details = request.Details ?? [];
    }

    /// <summary>
    /// Gets the optional diagnostics shown in the details section.
    /// </summary>
    public IReadOnlyList<ErrorDialogDetail> Details { get; }

    /// <summary>
    /// Gets a value indicating whether the details section has content.
    /// </summary>
    public bool HasDetails => Details.Count > 0;

    /// <summary>
    /// Dismisses the dialog.
    /// </summary>
    [RelayCommand]
    private void Dismiss() => Close(DialogResult.Dismissed);
}

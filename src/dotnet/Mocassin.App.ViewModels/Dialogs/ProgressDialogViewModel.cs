// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Shows a long-running operation with a progress bar and a cancel action.
/// </summary>
internal sealed partial class ProgressDialogViewModel : DialogViewModel, IDisposable
{
    private readonly CancellationTokenSource cancellation = new();

    [ObservableProperty]
    private string? statusText;

    [ObservableProperty]
    private double? fraction;

    [ObservableProperty]
    private bool isIndeterminate;

    [ObservableProperty]
    private bool isCompleted;

    /// <summary>
    /// Initializes a new progress dialog.
    /// </summary>
    /// <param name="request">The progress request.</param>
    public ProgressDialogViewModel(ProgressDialogRequest request)
        : base(request.Title, request.StatusText ?? string.Empty)
    {
        StatusText = request.StatusText;
        IsIndeterminate = request.IsIndeterminate;
        IsCancelable = request.CanCancel;
    }

    /// <inheritdoc />
    public override bool CanCancel => false;

    /// <summary>
    /// Gets a value indicating whether the operation can be cancelled.
    /// </summary>
    public bool IsCancelable { get; }

    /// <summary>
    /// Gets the token that the operation observes.
    /// </summary>
    public CancellationToken CancellationToken => cancellation.Token;

    /// <summary>
    /// Reports progress.
    /// </summary>
    /// <param name="value">The completion fraction in the range 0..1, or <see langword="null"/>.</param>
    /// <param name="status">The optional status line.</param>
    public void Report(double? value, string? status = null)
    {
        Fraction = value;
        if (status is not null)
        {
            StatusText = status;
        }
    }

    /// <summary>
    /// Marks the operation as completed.
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        Fraction = 1;
    }

    /// <summary>
    /// Requests cancellation of the operation.
    /// </summary>
    [RelayCommand]
    private void RequestCancel()
    {
        if (!IsCancelable || cancellation.IsCancellationRequested)
        {
            return;
        }

        StatusText = "Cancelling…";
        cancellation.Cancel();
    }

    /// <inheritdoc />
    public void Dispose() => cancellation.Dispose();
}

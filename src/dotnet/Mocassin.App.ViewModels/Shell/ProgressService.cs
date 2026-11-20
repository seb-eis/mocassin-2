// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Holds the short-lived progress state mirrored by the status bar.
/// </summary>
internal sealed partial class ProgressService : ObservableObject
{
    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private bool isIndeterminate;

    [ObservableProperty]
    private double? fraction;

    [ObservableProperty]
    private string? statusText;

    /// <summary>
    /// Starts a new progress operation.
    /// </summary>
    /// <param name="title">The operation title.</param>
    /// <param name="indeterminate">Whether the operation has no known duration.</param>
    public void Begin(string title, bool indeterminate = false)
    {
        IsActive = true;
        IsIndeterminate = indeterminate;
        Fraction = null;
        StatusText = title;
    }

    /// <summary>
    /// Reports progress for the active operation.
    /// </summary>
    /// <param name="fraction">The completion fraction in the range 0..1, or <see langword="null"/>.</param>
    /// <param name="statusText">The optional status text.</param>
    public void Report(double? fraction, string? statusText = null)
    {
        Fraction = fraction;
        if (statusText is not null)
        {
            StatusText = statusText;
        }
    }

    /// <summary>
    /// Ends the active progress operation.
    /// </summary>
    public void End()
    {
        IsActive = false;
        IsIndeterminate = false;
        Fraction = null;
        StatusText = null;
    }
}

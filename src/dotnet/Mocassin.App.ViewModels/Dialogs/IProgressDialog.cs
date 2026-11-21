// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Reports progress and closes an open progress surface.
/// </summary>
internal interface IProgressDialog : IDisposable
{
    /// <summary>
    /// Gets the token that is cancelled by the progress surface.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Reports a progress value and an optional status line.
    /// </summary>
    /// <param name="fraction">The completion fraction in the range 0..1, or <see langword="null"/>.</param>
    /// <param name="statusText">The optional status line.</param>
    public void Report(double? fraction, string? statusText = null);

    /// <summary>
    /// Marks the operation as complete and closes the surface.
    /// </summary>
    public void Complete();
}

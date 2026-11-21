// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Dialogs;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Ignored progress handle for dialog-service tests.
/// </summary>
internal sealed class RecordingProgressDialog : IProgressDialog
{
    private readonly CancellationTokenSource cancellation = new();

    /// <inheritdoc />
    public CancellationToken CancellationToken => cancellation.Token;

    /// <inheritdoc />
    public void Report(double? fraction, string? statusText = null)
    {
    }

    /// <inheritdoc />
    public void Complete()
    {
    }

    /// <inheritdoc />
    public void Dispose() => cancellation.Dispose();
}

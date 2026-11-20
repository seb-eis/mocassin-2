// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Records clipboard writes for document tests.
/// </summary>
internal sealed class RecordingClipboardService : IClipboardService
{
    /// <summary>
    /// Gets the copied values.
    /// </summary>
    public List<string> Values { get; } = [];

    /// <inheritdoc />
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Values.Add(text);
        return Task.CompletedTask;
    }
}

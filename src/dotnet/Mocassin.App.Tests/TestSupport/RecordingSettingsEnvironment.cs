// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Records settings-environment operations.
/// </summary>
internal sealed class RecordingSettingsEnvironment : ISettingsEnvironment
{
    /// <inheritdoc />
    public string SettingsPath { get; } = "/tmp/Mocassin/Mocassin.Settings.json";

    /// <inheritdoc />
    public string DataDirectory { get; } = "/tmp/Mocassin";

    /// <summary>
    /// Gets the number of open-folder calls.
    /// </summary>
    public int OpenFolderCalls { get; private set; }

    /// <summary>
    /// Gets the number of clear calls.
    /// </summary>
    public int ClearCalls { get; private set; }

    /// <inheritdoc />
    public Task OpenConfigFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenFolderCalls++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> ClearLogsAndCacheAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClearCalls++;
        return Task.FromResult(1234L);
    }
}

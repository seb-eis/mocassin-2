// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Records the requested process exit code.
/// </summary>
internal sealed class RecordingShellLifetime : Mocassin.App.ViewModels.Shell.IShellLifetime
{
    /// <summary>
    /// Gets the requested exit code.
    /// </summary>
    public int? ExitCode { get; private set; }

    /// <inheritdoc />
    public void Shutdown(int exitCode = 0) => ExitCode = exitCode;
}

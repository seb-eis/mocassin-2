// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Requests the shell to end the application. The implementation lives in the application host.
/// </summary>
internal interface IShellLifetime
{
    /// <summary>
    /// Shuts the application down.
    /// </summary>
    /// <param name="exitCode">The process exit code.</param>
    public void Shutdown(int exitCode = 0);
}

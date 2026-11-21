// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Startup;

/// <summary>
/// Shuts down the Avalonia desktop lifetime.
/// </summary>
internal sealed class AppShellLifetime : IShellLifetime
{
    /// <inheritdoc />
    public void Shutdown(int exitCode = 0)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(exitCode);
            return;
        }

        Environment.Exit(exitCode);
    }
}

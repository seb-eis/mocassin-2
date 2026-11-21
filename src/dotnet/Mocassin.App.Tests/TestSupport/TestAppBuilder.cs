// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Headless;
using Mocassin.App.Composition;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Builds the real application for the Avalonia headless test session without starting the host.
/// </summary>
public static class TestAppBuilder
{
    /// <summary>
    /// Builds the headless Avalonia application.
    /// </summary>
    /// <returns>The application builder.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>(() => new App(AppStartupOptions.Default, startHost: false))
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia()
            .WithInterFont();
}

// SPDX-License-Identifier: MIT

using Avalonia;
using Mocassin.App.Composition;

namespace Mocassin.App;

/// <summary>
/// Application entry point. It parses the command line before Avalonia starts.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>0 on a normal exit, 1 on a startup failure, and 2 on a usage error.</returns>
    public static int Main(string[] args) => Run(args, Console.Error, StartApplication);

    internal static int Run(
        string[] args,
        TextWriter error,
        Func<AppStartupOptions, int> startApplication)
    {
        if (!AppStartupOptions.TryParse(args, out var options, out var errorMessage))
        {
            error.WriteLine(errorMessage);
            error.WriteLine(AppStartupOptions.UsageText);
            return 2;
        }

        try
        {
            return startApplication(options);
        }
#pragma warning disable CA1031 // The process boundary converts a startup failure into exit code 1.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            error.WriteLine($"MOCASSIN failed to start: {exception.Message}");
            return 1;
        }
    }

    private static int StartApplication(AppStartupOptions options) =>
        AppBuilder.Configure<App>(() => new App(options, startHost: true))
            .UsePlatformDetect()
            .WithInterFont()
            .StartWithClassicDesktopLifetime([]);
}

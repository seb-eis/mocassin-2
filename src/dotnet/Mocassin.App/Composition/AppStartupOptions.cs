// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;

namespace Mocassin.App.Composition;

/// <summary>
/// Parses the GUI startup arguments. Unknown arguments are usage errors.
/// </summary>
internal sealed record AppStartupOptions
{
    /// <summary>
    /// Gets the default startup options.
    /// </summary>
    public static AppStartupOptions Default { get; } = new();

    /// <summary>
    /// Gets the usage text written for an invalid command line.
    /// </summary>
    public static string UsageText =>
        "Usage: mocassin [--project <path>] [--log-level <trace|debug|information|warning|error|critical>] "
        + "[--data-dir <path>]";

    /// <summary>
    /// Gets the optional project path to open on start.
    /// </summary>
    public string? ProjectPath { get; init; }

    /// <summary>
    /// Gets the log level override for this run.
    /// </summary>
    public LogLevel LogLevel { get; init; } = LogLevel.Information;

    /// <summary>
    /// Gets the optional application-data override.
    /// </summary>
    public string? DataDirectory { get; init; }

    /// <summary>
    /// Parses the command line.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="options">The parsed options on success.</param>
    /// <param name="errorMessage">The error message on failure.</param>
    /// <returns><see langword="true"/> when the arguments are valid.</returns>
    public static bool TryParse(
        IReadOnlyList<string> args,
        out AppStartupOptions options,
        out string? errorMessage)
    {
        string? projectPath = null;
        string? dataDirectory = null;
        var logLevel = LogLevel.Information;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--project":
                    if (!TryReadValue(args, ref index, out projectPath, out errorMessage))
                    {
                        options = Default;
                        return false;
                    }

                    break;

                case "--data-dir":
                    if (!TryReadValue(args, ref index, out dataDirectory, out errorMessage))
                    {
                        options = Default;
                        return false;
                    }

                    break;

                case "--log-level":
                    if (!TryReadValue(args, ref index, out var levelText, out errorMessage)
                        || levelText is null
                        || !TryParseLogLevel(levelText, out logLevel))
                    {
                        options = Default;
                        errorMessage ??= $"Unknown log level '{levelText}'.";
                        return false;
                    }

                    break;

                default:
                    options = Default;
                    errorMessage = $"Unknown argument '{argument}'.";
                    return false;
            }
        }

        options = new AppStartupOptions
        {
            ProjectPath = projectPath,
            DataDirectory = dataDirectory,
            LogLevel = logLevel,
        };
        errorMessage = null;
        return true;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        out string? value,
        out string? errorMessage)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = null;
            errorMessage = $"Missing value for '{args[index]}'.";
            return false;
        }

        index++;
        value = args[index];
        errorMessage = null;
        return true;
    }

    private static bool TryParseLogLevel(string text, out LogLevel level)
    {
        switch (text.ToUpperInvariant())
        {
            case "TRACE":
                level = LogLevel.Trace;
                return true;
            case "DEBUG":
                level = LogLevel.Debug;
                return true;
            case "INFORMATION":
                level = LogLevel.Information;
                return true;
            case "WARNING":
                level = LogLevel.Warning;
                return true;
            case "ERROR":
                level = LogLevel.Error;
                return true;
            case "CRITICAL":
            case "FATAL":
                level = LogLevel.Critical;
                return true;
            default:
                level = LogLevel.Information;
                return false;
        }
    }
}

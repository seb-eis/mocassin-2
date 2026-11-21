// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;
using Mocassin.App.Composition;

namespace Mocassin.App.Tests.Composition;

/// <summary>
/// Verifies the startup command-line contract.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class StartupOptionsTests
{
    [Fact]
    public void TheDefaultOptionsUseInformationLoggingAndNoOverrides()
    {
        Assert.True(AppStartupOptions.TryParse([], out var options, out var error));

        Assert.Null(error);
        Assert.Null(options.ProjectPath);
        Assert.Null(options.DataDirectory);
        Assert.Equal(LogLevel.Information, options.LogLevel);
    }

    [Fact]
    public void TheDocumentedArgumentsAreParsed()
    {
        var args = new[] { "--project", "/tmp/ceria.mocprj", "--log-level", "debug", "--data-dir", "/tmp/data" };

        Assert.True(AppStartupOptions.TryParse(args, out var options, out var error));

        Assert.Null(error);
        Assert.Equal("/tmp/ceria.mocprj", options.ProjectPath);
        Assert.Equal("/tmp/data", options.DataDirectory);
        Assert.Equal(LogLevel.Debug, options.LogLevel);
    }

    [Fact]
    public void AnUnknownArgumentIsAUsageError()
    {
        Assert.False(AppStartupOptions.TryParse(["--bogus"], out _, out var error));
        Assert.Contains("Unknown argument '--bogus'", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ARepeatedValueWithoutAnArgumentIsAUsageError()
    {
        Assert.False(AppStartupOptions.TryParse(["--project"], out _, out var error));
        Assert.Contains("Missing value", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownLogLevelIsAUsageError()
    {
        Assert.False(AppStartupOptions.TryParse(["--log-level", "verbose"], out _, out var error));
        Assert.Contains("Unknown log level", error, StringComparison.Ordinal);
    }

    [Fact]
    public void FatalIsAcceptedAsCritical()
    {
        Assert.True(AppStartupOptions.TryParse(["--log-level", "fatal"], out var options, out _));
        Assert.Equal(LogLevel.Critical, options.LogLevel);
    }

    [Fact]
    public void TheProgramReturnsTwoForAUsageErrorAndDoesNotStart()
    {
        var started = false;
        using var error = new StringWriter();

        var exitCode = Program.Run(["--bogus"], error, _ =>
        {
            started = true;
            return 0;
        });

        Assert.Equal(2, exitCode);
        Assert.False(started);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheProgramReturnsOneWhenStartupThrows()
    {
        using var error = new StringWriter();

        var exitCode = Program.Run([], error, _ => throw new InvalidOperationException("no display"));

        Assert.Equal(1, exitCode);
        Assert.Contains("no display", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheProgramPassesTheParsedOptionsToTheLauncher()
    {
        AppStartupOptions? seen = null;
        using var error = new StringWriter();

        var exitCode = Program.Run(["--data-dir", "/tmp/data"], error, options =>
        {
            seen = options;
            return 0;
        });

        Assert.Equal(0, exitCode);
        Assert.NotNull(seen);
        Assert.Equal("/tmp/data", seen.DataDirectory);
    }
}

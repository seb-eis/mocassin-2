// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine;

namespace Mocassin.Cli.Tests.CommandLine;

/// <summary>
/// Exercises the guarded Ctrl+C/SIGTERM handler on every platform, including the Windows dispatch.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CliSignalHandlerTests
{
    [Fact]
    public void TheHandlerInstallsAndDisposesOnTheCurrentPlatform()
    {
        using var handler = new CliSignalHandler();

        Assert.False(handler.Token.IsCancellationRequested);
        Assert.True(handler.Token.CanBeCanceled);
    }

    [Fact]
    public void DisposingTwiceIsSafe()
    {
        var handler = new CliSignalHandler();
        handler.Dispose();
        handler.Dispose();
    }
}

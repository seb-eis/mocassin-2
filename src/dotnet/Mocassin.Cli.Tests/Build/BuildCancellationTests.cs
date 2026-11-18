// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Build;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Packages.Build;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// Regression tests for cancellation after the package has been published.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class BuildCancellationTests
{
    private static string FixturePath =>
        RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj");

    [Fact]
    public async Task CancellationDuringPostPublicationVerificationLeavesThePublishedPackage()
    {
        using var directory = new TempDirectory();
        var target = System.IO.Path.Combine(directory.Path, "cancel-after-publish.msl");
        var verifier = new BlockingVerifier();
        var command = new BuildCommand(new BuildPipeline(), verifier, TimeProvider.System);
        var application = new CliApplication(new CliCommandGraph(), command);
        using var cancellation = new CancellationTokenSource();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var run = application.RunAsync(
            ["build", FixturePath, "-o", target, "--json"],
            stdout,
            stderr,
            cancellation.Token);
        await verifier.Started.WaitAsync(TimeSpan.FromSeconds(60));
        Assert.True(File.Exists(target), "The package must be published before verification starts.");

        cancellation.Cancel();
        var exitCode = await run;

        Assert.Equal(6, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout.ToString());
        using var report = JsonDocument.Parse(stdout.ToString());
        var root = report.RootElement;
        Assert.Equal(6, root.GetProperty("exitCode").GetInt32());
        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.True(root.GetProperty("package").GetProperty("published").GetBoolean());
        Assert.Equal("skipped", root.GetProperty("verification").GetString());
        Assert.Contains(
            root.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
        Assert.True(File.Exists(target));
        Assert.Empty(System.IO.Directory.EnumerateFiles(
            directory.Path,
            System.IO.Path.GetFileName(target) + ".tmp-*",
            SearchOption.TopDirectoryOnly));
    }

    private sealed class BlockingVerifier : IPackageVerifier
    {
        private readonly TaskCompletionSource started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => started.Task;

        public async Task<PackageVerificationReport> VerifyAsync(
            string path,
            PackageVerificationOptions options,
            CancellationToken cancellationToken = default)
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The cancellation token was not observed.");
        }
    }
}

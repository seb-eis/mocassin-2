// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// Exercises the promoted compiled-bound parameterization warning end to end with a real project fixture.
/// </summary>
/// <remarks>
/// The fixture is a clean buildable ceria variant: it validates with no warnings, so
/// <c>--warnings-as-errors</c> reaches the parameterization stage, where the deliberately unset explicit
/// template produces <c>PRM003</c>/<c>PRM006</c>. The same fixture builds successfully without the flag. This
/// closes the earlier gap where the exit-4 rule was only covered through a scripted pipeline seam.
/// </remarks>
[Trait("Tier", "PR-fast")]
public sealed class BuildParameterizationWarningTests
{
    private static string FixturePath =>
        RepositoryPaths.Combine("tests", "contracts", "project-parameterization-warning.mocprj");

    [Fact]
    public async Task TheFixtureValidatesWithoutWarnings()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("validate", FixturePath, "--json");

        Assert.Equal(0, exitCode);
        JsonSchemaAssert.Matches("validation-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        var summary = report.RootElement.GetProperty("summary");
        Assert.Equal(0, summary.GetProperty("errors").GetInt32());
        Assert.Equal(0, summary.GetProperty("warnings").GetInt32());
        Assert.True(report.RootElement.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task PromotedCompiledParameterizationWarningExitsFourBeforeTheWriter()
    {
        using var directory = new TempDirectory();
        var target = System.IO.Path.Combine(directory.Path, "warning.msl");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "build",
            FixturePath,
            "-o",
            target,
            "--warnings-as-errors",
            "--json");

        Assert.Equal(4, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        var root = report.RootElement;
        Assert.False(root.GetProperty("ok").GetBoolean());
        Assert.Equal(4, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("package").ValueKind);
        var diagnostics = root.GetProperty("diagnostics").EnumerateArray().ToArray();
        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.GetProperty("code").GetString() is "PRM003" or "PRM006");
        Assert.All(
            diagnostics.Where(static diagnostic =>
                diagnostic.GetProperty("code").GetString() is "PRM003" or "PRM006"),
            static diagnostic => Assert.Equal("warning", diagnostic.GetProperty("severity").GetString()));
        var stages = root.GetProperty("timings").EnumerateArray()
            .Select(static stage => stage.GetProperty("stage").GetString())
            .ToArray();
        Assert.Contains("parameterize", stages);
        Assert.DoesNotContain("generate", stages);
        Assert.DoesNotContain("publish", stages);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task TheSameFixtureBuildsWhenTheFlagIsOmitted()
    {
        using var directory = new TempDirectory();
        var target = System.IO.Path.Combine(directory.Path, "warning.msl");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "build",
            FixturePath,
            "-o",
            target,
            "--json");

        Assert.Equal(0, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.True(report.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(report.RootElement.GetProperty("package").GetProperty("published").GetBoolean());
        Assert.True(File.Exists(target));
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PRM003");

        var (verifyExit, verifyStdout, _) = await CliTestRunner.RunAsync("verify", target, "--json");
        Assert.Equal(0, verifyExit);
        using var verify = JsonDocument.Parse(verifyStdout);
        Assert.True(verify.RootElement.GetProperty("valid").GetBoolean());
    }
}

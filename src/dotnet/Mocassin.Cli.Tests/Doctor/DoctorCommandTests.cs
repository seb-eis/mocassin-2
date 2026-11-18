// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Doctor;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;

namespace Mocassin.Cli.Tests.Doctor;

[Trait("Tier", "PR-fast")]
public sealed class DoctorCommandTests
{
    [Fact]
    public async Task HealthyChecksProduceASchemaValidReport()
    {
        var probe = new FixedSolverProbe(new SolverProbeResult(true, "/opt/mocassin-sim", "1.2.3", "solver ok"));
        var command = new DoctorCommand(
            [new FixedDoctorCheck("first", DoctorCheckStatus.Ok, "fine"), new FixedDoctorCheck("second", DoctorCheckStatus.Ok, "also fine")],
            probe);

        var (exitCode, stdout, stderr) = await RunAsync(command, "doctor", "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("doctor-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        var root = report.RootElement;
        Assert.Equal("doctor", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("healthy").GetBoolean());
        Assert.Equal(2, root.GetProperty("checks").GetArrayLength());
        Assert.Equal("ok", root.GetProperty("checks")[0].GetProperty("status").GetString());
        Assert.Equal("/opt/mocassin-sim", root.GetProperty("environment").GetProperty("solverPath").GetString());
        Assert.Equal("1.2.3", root.GetProperty("environment").GetProperty("solverVersion").GetString());
    }

    [Fact]
    public async Task WarningsDoNotMakeTheEnvironmentUnhealthy()
    {
        var command = new DoctorCommand(
            [new FixedDoctorCheck("warning", DoctorCheckStatus.Warn, "optional"), new FixedDoctorCheck("ok", DoctorCheckStatus.Ok, "fine")],
            new FixedSolverProbe(new SolverProbeResult(false, null, null, "missing")));

        var (exitCode, stdout, _) = await RunAsync(command, "doctor", "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.True(report.RootElement.GetProperty("healthy").GetBoolean());
    }

    [Fact]
    public async Task AFailingCheckExitsOneAndIsUnhealthy()
    {
        var command = new DoctorCommand(
            [new FixedDoctorCheck("failing", DoctorCheckStatus.Fail, "broken")],
            new FixedSolverProbe(new SolverProbeResult(false, null, null, "missing")));

        var (exitCode, stdout, _) = await RunAsync(command, "doctor", "--json");

        Assert.Equal(1, exitCode);
        JsonSchemaAssert.Matches("doctor-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.False(report.RootElement.GetProperty("healthy").GetBoolean());
        Assert.Equal("fail", report.RootElement.GetProperty("checks")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task AThrowingCheckIsAFailureNotACrash()
    {
        var command = new DoctorCommand(
            [new ThrowingDoctorCheck("throwing")],
            new FixedSolverProbe(new SolverProbeResult(false, null, null, "missing")));

        var (exitCode, stdout, _) = await RunAsync(command, "doctor", "--json");

        Assert.Equal(1, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal("fail", report.RootElement.GetProperty("checks")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task HumanOutputPrintsTheCheckTable()
    {
        var command = new DoctorCommand(
            [new FixedDoctorCheck("runtime", DoctorCheckStatus.Ok, "fine")],
            new FixedSolverProbe(new SolverProbeResult(false, null, null, "missing")));

        var (exitCode, stdout, _) = await RunAsync(command, "doctor");

        Assert.Equal(0, exitCode);
        Assert.Contains("CHECK", stdout, StringComparison.Ordinal);
        Assert.Contains("runtime", stdout, StringComparison.Ordinal);
        Assert.Contains("Environment:", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: healthy (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheRealChecksAreHealthyOnCi()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("doctor", "--json");

        Assert.Equal(0, exitCode);
        JsonSchemaAssert.Matches("doctor-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        var root = report.RootElement;
        Assert.True(root.GetProperty("healthy").GetBoolean());
        var checks = root.GetProperty("checks").EnumerateArray()
            .ToDictionary(
                static check => check.GetProperty("name").GetString()!,
                static check => check.GetProperty("status").GetString()!,
                StringComparer.Ordinal);
        Assert.Equal("ok", checks["runtime"]);
        Assert.Equal("ok", checks["sqlite"]);
        Assert.Equal("ok", checks["write-permission"]);
        Assert.Equal("ok", checks["symmetry-catalog"]);
        Assert.Equal("ok", checks["project-templates"]);
        Assert.NotEqual("fail", checks["solver"]);
        Assert.False(string.IsNullOrWhiteSpace(
            root.GetProperty("environment").GetProperty("sqliteVersion").GetString()));
    }

    [Fact]
    public async Task AParseErrorEmitsOneDoctorReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("doctor", "--bogus", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("doctor-v1", stdout);
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneDoctorReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("doctor", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("doctor-v1", stdout);
    }

    [Fact]
    public async Task PreCancelledDoctorIsExitSixWithCli006()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new CliApplication();

        var exitCode = await application.RunAsync(["doctor", "--json"], stdout, stderr, cancellation.Token);

        Assert.Equal(6, exitCode);
        using var report = JsonDocument.Parse(stdout.ToString());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        DoctorCommand command,
        params string[] arguments)
    {
        var application = new CliApplication(
            new CliCommandGraph(),
            new Mocassin.Cli.Build.BuildCommand(),
            doctorCommand: command);
        return CliTestRunner.RunAsync(application, arguments);
    }

    private sealed class FixedDoctorCheck : IDoctorCheck
    {
        private readonly DoctorCheckResult result;

        public FixedDoctorCheck(string name, DoctorCheckStatus status, string message)
        {
            Name = name;
            result = new DoctorCheckResult(name, status, message);
        }

        public string Name { get; }

        public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingDoctorCheck : IDoctorCheck
    {
        public ThrowingDoctorCheck(string name) => Name = name;

        public string Name { get; }

        public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("check defect");
    }

    private sealed class FixedSolverProbe : ISolverProbe
    {
        private readonly SolverProbeResult result;

        public FixedSolverProbe(SolverProbeResult result) => this.result = result;

        public Task<SolverProbeResult> ProbeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}

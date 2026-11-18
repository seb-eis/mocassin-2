// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Cli.Build;
using Mocassin.Cli.CommandLine;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;

namespace Mocassin.Cli.Tests.Build;

/// <summary>
/// Pins the single build failure/status to exit-code mapping, including defensive branches.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class BuildExitMapperTests
{
    [Theory]
    [InlineData(BuildFailureKind.Load, 1)]
    [InlineData(BuildFailureKind.Validation, 1)]
    [InlineData(BuildFailureKind.JobExpansion, 1)]
    [InlineData(BuildFailureKind.LatticeGeneration, 1)]
    [InlineData(BuildFailureKind.Preview, 1)]
    [InlineData(BuildFailureKind.Compilation, 4)]
    [InlineData(BuildFailureKind.Parameterization, 4)]
    [InlineData(BuildFailureKind.Io, 3)]
    [InlineData(BuildFailureKind.JobSelection, 2)]
    [InlineData(BuildFailureKind.TemplateSelection, 2)]
    [InlineData(BuildFailureKind.Internal, 10)]
    public void FailedRunsMapToTheDocumentedExitCode(BuildFailureKind failure, int expected)
    {
        var (exitCode, _) = BuildExitMapper.Map(Outcome(BuildStatus.Failed, failure));

        Assert.Equal(expected, exitCode);
    }

    [Fact]
    public void TemplateSelectionWithAProj010ReferenceIsExitOne()
    {
        var (exitCode, _) = BuildExitMapper.Map(
            Outcome(
                BuildStatus.Failed,
                BuildFailureKind.TemplateSelection,
                new Diagnostic("PROJ010", DiagnosticSeverity.Error, "The reference does not exist.")));

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void PackageWriteWithAPackageErrorIsExitOneAndWithoutOneIsInternal()
    {
        var withPackageError = BuildExitMapper.Map(
            Outcome(
                BuildStatus.Failed,
                BuildFailureKind.PackageWrite,
                new Diagnostic("PKG004", DiagnosticSeverity.Error, "The payload is corrupt.")));
        var withoutPackageError = BuildExitMapper.Map(Outcome(BuildStatus.Failed, BuildFailureKind.PackageWrite));

        Assert.Equal(1, withPackageError.ExitCode);
        Assert.DoesNotContain(
            withPackageError.CliDiagnostics,
            static diagnostic => diagnostic.Code == "CLI007");
        Assert.Equal(10, withoutPackageError.ExitCode);
        Assert.Contains(
            withoutPackageError.CliDiagnostics,
            static diagnostic => diagnostic.Code == "CLI007");
    }

    [Fact]
    public void NoneWithAFailedStatusIsInternal()
    {
        var (exitCode, diagnostics) = BuildExitMapper.Map(Outcome(BuildStatus.Failed, BuildFailureKind.None));

        Assert.Equal(10, exitCode);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "CLI007");
    }

    [Fact]
    public void CancelledRunsExitSix()
    {
        var (exitCode, diagnostics) = BuildExitMapper.Map(Outcome(BuildStatus.Cancelled, BuildFailureKind.None));

        Assert.Equal(6, exitCode);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "CLI006");
    }

    [Fact]
    public void NoFailureKindOrStatusMapsToReservedExitFive()
    {
        foreach (var failure in Enum.GetValues<BuildFailureKind>())
        {
            foreach (var status in Enum.GetValues<BuildStatus>())
            {
                var (exitCode, _) = BuildExitMapper.Map(Outcome(status, failure));
                Assert.NotEqual(CliExitCode.Partial, exitCode);
            }
        }
    }

    private static BuildOutcome Outcome(
        BuildStatus status,
        BuildFailureKind failure,
        params Diagnostic[] diagnostics)
    {
        var result = new BuildResult(
            status,
            failure,
            BuildIdentity.Empty,
            diagnostics.ToImmutableArray(),
            Selection: null,
            JobSelection: null,
            Preview: null,
            Package: null,
            Timings: ImmutableArray<BuildStageTiming>.Empty,
            Elapsed: TimeSpan.Zero);
        var request = new BuildCommandRequest(
            "project.mocprj",
            Json: true,
            Preview: false,
            OutputPath: null,
            BuildTemplateId: null,
            JobTemplateId: null,
            ParameterizationId: null,
            Jobs: null,
            Parallel: 0,
            Compact: false,
            Force: false,
            WarningsAsErrors: false,
            Quiet: false);
        return new BuildOutcome(
            result,
            request,
            Project: null,
            TargetPath: null,
            OutputExists: false,
            VerificationFailed: false,
            VerifyElapsed: TimeSpan.Zero,
            VerificationDiagnostics: ImmutableArray<Diagnostic>.Empty);
    }
}

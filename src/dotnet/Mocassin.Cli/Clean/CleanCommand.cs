// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Clean;

/// <summary>
/// Implements <c>mocassin clean --dry-run</c>: a read-only listing of <c>Job%05d</c> folders.
/// </summary>
/// <remarks>
/// Step 12 has no deletion path. Without <c>--dry-run</c>, and for the reserved <c>--results</c> option, the
/// command emits a <c>clean-v1</c> report with <c>CLI008</c> and exits 2. The package is opened read-only
/// through the inspector; no file, folder, <c>.msl</c>, <c>.msr</c>, or <c>.mcs</c> is modified.
/// </remarks>
internal sealed class CleanCommand
{
    private readonly IMslPackageInspector inspector;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public CleanCommand()
        : this(new MslPackageInspector(), TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes the command with explicit services for tests.
    /// </summary>
    /// <param name="inspector">The read-only package inspector.</param>
    /// <param name="timeProvider">The clock used for the age filter.</param>
    internal CleanCommand(IMslPackageInspector inspector, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.inspector = inspector;
        this.timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs the command.
    /// </summary>
    /// <param name="request">The typed request.</param>
    /// <param name="stdout">The standard-output writer.</param>
    /// <param name="stderr">The standard-error writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public async Task<int> RunAsync(
        CleanRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        int? olderThanDays = request.OlderThanDays > 0 ? request.OlderThanDays : null;
        if (request.Results is not null)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Usage,
                    request.DryRun,
                    TryFullPath(request.PackagePath),
                    TryFullPath(request.Directory),
                    olderThanDays,
                    [CliDiagnosticFactory.Reserved(
                        "The --results option is reserved for Step 20/28.")]));
        }

        var input = PackageInput.CheckReadable(request.PackagePath);
        if (input is not null)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(CliExitCode.Io, request.DryRun, TryFullPath(request.PackagePath), null, olderThanDays, [input]));
        }

        try
        {
            var inspection = await inspector.InspectAsync(request.PackagePath, cancellationToken)
                .ConfigureAwait(false);
            var packagePath = Path.GetFullPath(request.PackagePath);
            var directory = request.Directory is null
                ? Path.GetDirectoryName(packagePath)
                : Path.GetFullPath(request.Directory);
            if (directory is null || !Directory.Exists(directory))
            {
                return Finish(
                    request,
                    stdout,
                    stderr,
                    Failure(
                        CliExitCode.Io,
                        request.DryRun,
                        packagePath,
                        directory,
                        olderThanDays,
                        [CliDiagnosticFactory.Io($"The job-folder directory '{directory}' does not exist.")]));
            }

            var folders = ListFolders(directory, request.OlderThanDays);
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            var exitCode = CliExitCode.Success;
            if (!request.DryRun)
            {
                exitCode = CliExitCode.Usage;
                diagnostics.Add(CliDiagnosticFactory.Reserved(
                    "Deleting job folders and result rows arrives with Step 28; pass --dry-run to list."));
            }

            var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
            var report = new CleanReport(
                ContractVersions.CleanReportVersion,
                "clean",
                exitCode == 0,
                exitCode,
                ReportEnvelope.Count(reportDiagnostics),
                reportDiagnostics,
                request.DryRun,
                packagePath,
                directory,
                inspection.JobSelection,
                inspection.JobCount,
                folders,
                folders.Sum(static folder => folder.SizeBytes),
                olderThanDays,
                RemovedCount: 0);
            return Finish(request, stdout, stderr, report);
        }
        catch (InvalidDataException exception)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Invalid,
                    request.DryRun,
                    TryFullPath(request.PackagePath),
                    TryFullPath(request.Directory),
                    olderThanDays,
                    [CliDiagnosticFactory.NotAPackage(
                        $"The package '{request.PackagePath}' cannot be read: {exception.Message}",
                        $"Run 'mocassin verify {request.PackagePath}' for package detail.")]));
        }
        catch (OperationCanceledException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Cancelled,
                    request.DryRun,
                    TryFullPath(request.PackagePath),
                    TryFullPath(request.Directory),
                    olderThanDays,
                    [CliDiagnosticFactory.Cancelled("The command was cancelled.")]));
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Io,
                    request.DryRun,
                    TryFullPath(request.PackagePath),
                    TryFullPath(request.Directory),
                    olderThanDays,
                    [CliDiagnosticFactory.Io($"The job folders could not be listed: {exception.Message}")]));
        }
#pragma warning disable CA1031 // The command boundary converts an unexpected failure into CLI007/exit 10.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return Finish(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Internal,
                    request.DryRun,
                    TryFullPath(request.PackagePath),
                    TryFullPath(request.Directory),
                    olderThanDays,
                    [CliDiagnosticFactory.Internal(
                        $"Unexpected internal error: {exception.GetType().Name}: {exception.Message}")]));
        }
    }

    private CleanReportFolder[] ListFolders(string directory, int olderThanDays)
    {
        var now = timeProvider.GetUtcNow();
        var folders = new List<CleanReportFolder>();
        foreach (var path in Directory.EnumerateDirectories(directory))
        {
            if (!TryParseJobId(Path.GetFileName(path), out var jobId))
            {
                continue;
            }

            var lastWrite = Directory.GetLastWriteTimeUtc(path);
            if (olderThanDays > 0 && lastWrite >= now.AddDays(-olderThanDays).UtcDateTime)
            {
                continue;
            }

            folders.Add(new CleanReportFolder(
                path,
                jobId,
                new DateTimeOffset(lastWrite).ToString("O", CultureInfo.InvariantCulture),
                DirectorySize(path)));
        }

        return folders
            .OrderBy(static folder => folder.JobId)
            .ThenBy(static folder => folder.Path, StringComparer.Ordinal)
            .ToArray();
    }

    private static long DirectorySize(string path)
    {
        long size = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                size += new FileInfo(file).Length;
            }
            catch (IOException)
            {
                // A disappearing file contributes nothing to the advisory size.
            }
            catch (UnauthorizedAccessException)
            {
                // An unreadable file contributes nothing to the advisory size.
            }
        }

        return size;
    }

    private static bool TryParseJobId(string name, out int jobId)
    {
        jobId = 0;
        return name.Length == 8
            && name.StartsWith("Job", StringComparison.Ordinal)
            && int.TryParse(name[3..], NumberStyles.None, CultureInfo.InvariantCulture, out jobId);
    }

    private static int Finish(
        CleanRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CleanReport report)
    {
        if (request.Json)
        {
            CleanReportWriter.WriteJson(stdout, report);
        }
        else
        {
            foreach (var diagnostic in report.Diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            CleanReportWriter.WriteHuman(stdout, report);
        }

        return report.ExitCode;
    }

    private static CleanReport Failure(
        int exitCode,
        bool dryRun,
        string? packagePath,
        string? directory,
        int? olderThanDays,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new CleanReport(
            ContractVersions.CleanReportVersion,
            "clean",
            Ok: false,
            exitCode,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            dryRun,
            packagePath,
            directory,
            JobSelection: null,
            JobCount: 0,
            Folders: [],
            TotalBytes: 0,
            olderThanDays,
            RemovedCount: 0);
    }

    private static string? TryFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
        {
            return null;
        }
    }
}

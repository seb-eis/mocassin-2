// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.CommandLine.Reports;
using Mocassin.Cli.Diagnostics;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.NewProject;

/// <summary>
/// Implements <c>mocassin new</c> with the embedded blank and ceria starters.
/// </summary>
/// <remarks>
/// The command validates the name, creates the fresh project in memory, validates it, and writes it through a
/// temporary sibling so an existing project is never replaced. The project factory is a seam for the
/// template-defect internal-error test; production uses <see cref="ProjectTemplateFactory"/>.
/// </remarks>
internal sealed class NewProjectCommand
{
    private readonly Func<ProjectTemplateKind, string, DateTimeOffset, Project> factory;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public NewProjectCommand()
        : this(ProjectTemplateFactory.CreateProject, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes the command with explicit seams for tests.
    /// </summary>
    /// <param name="factory">The fresh-project factory.</param>
    /// <param name="timeProvider">The clock.</param>
    internal NewProjectCommand(
        Func<ProjectTemplateKind, string, DateTimeOffset, Project> factory,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.factory = factory;
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
    public Task<int> RunAsync(
        NewProjectRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (!TryParseTemplate(request.Template, out var templateKind))
        {
            return Task.FromResult(WriteUsageError(
                request,
                stdout,
                stderr,
                [CliDiagnosticFactory.Usage($"Unknown project template '{request.Template}'.")]));
        }

        var templateName = templateKind == ProjectTemplateKind.Ceria ? "ceria" : "blank";
        if (!TryNormalizeName(request.Name, out var name, out var nameError))
        {
            return Task.FromResult(WriteUsageError(
                request,
                stdout,
                stderr,
                [CliDiagnosticFactory.Usage(nameError!)]));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Cancelled,
                    null,
                    null,
                    templateName,
                    [CliDiagnosticFactory.Cancelled("The command was cancelled.")]),
                diagnosticsRendered: false));
        }

        string directory;
        string targetPath;
        try
        {
            directory = Path.GetFullPath(request.OutputDirectory ?? Environment.CurrentDirectory);
            targetPath = Path.Combine(directory, name + ".mocprj");
        }
        catch (Exception exception) when (exception is ArgumentException
                                              or NotSupportedException
                                              or PathTooLongException)
        {
            return Task.FromResult(WriteUsageError(
                request,
                stdout,
                stderr,
                [CliDiagnosticFactory.Usage($"The output directory '{request.OutputDirectory}' is not valid.")]));
        }

        if (File.Exists(targetPath))
        {
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Io,
                    targetPath,
                    null,
                    templateName,
                    [CliDiagnosticFactory.OutputExists(
                        $"The project '{targetPath}' already exists. Choose another name or directory.")]),
                diagnosticsRendered: false));
        }

        if (Directory.Exists(targetPath))
        {
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Io,
                    targetPath,
                    null,
                    templateName,
                    [CliDiagnosticFactory.Io(
                        $"The project path '{targetPath}' is an existing directory.")]),
                diagnosticsRendered: false));
        }

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Io,
                    null,
                    null,
                    templateName,
                    [CliDiagnosticFactory.Io(
                        $"The output directory '{directory}' could not be created: {exception.Message}")]),
                diagnosticsRendered: false));
        }

        Project project;
        try
        {
            project = factory(templateKind, name, timeProvider.GetUtcNow());
        }
#pragma warning disable CA1031 // A defective embedded template is a recorded internal error.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Internal,
                    null,
                    null,
                    templateName,
                    [CliDiagnosticFactory.Internal(
                        $"The project template is defective: {exception.GetType().Name}: {exception.Message}")]),
                diagnosticsRendered: false));
        }

        var validation = new ProjectValidationPipeline().Validate(
            project,
            options: new ValidationOptions { RunBuildStage = false },
            cancellationToken: cancellationToken);
        if (!validation.IsValid)
        {
            var diagnostics = validation.Diagnostics.Add(
                CliDiagnosticFactory.Internal("The embedded project template failed validation."));
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(
                    CliExitCode.Internal,
                    null,
                    project.Metadata.ProjectId,
                    templateName,
                    diagnostics),
                diagnosticsRendered: false));
        }

        var bytes = ProjectJson.SerializeToUtf8Bytes(project, indented: true);
        try
        {
            AtomicFileWriter.Write(targetPath, bytes, overwrite: false);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            var diagnostic = File.Exists(targetPath)
                ? CliDiagnosticFactory.OutputExists(
                    $"The project '{targetPath}' already exists. Choose another name or directory.")
                : CliDiagnosticFactory.Io(
                    $"The project '{targetPath}' could not be written: {exception.Message}");
            return Task.FromResult(WriteReport(
                request,
                stdout,
                stderr,
                Failure(CliExitCode.Io, null, null, templateName, [diagnostic]),
                diagnosticsRendered: false));
        }

        var report = new NewProjectReport(
            ContractVersions.NewProjectReportVersion,
            "newProject",
            Ok: true,
            CliExitCode.Success,
            new ReportSummary(0, 0, 0),
            [],
            targetPath,
            project.Metadata.ProjectId,
            templateName);
        return Task.FromResult(WriteReport(request, stdout, stderr, report, diagnosticsRendered: false));
    }

    private static int WriteUsageError(
        NewProjectRequest request,
        TextWriter stdout,
        TextWriter stderr,
        IReadOnlyList<Diagnostic> diagnostics)
    {
        if (request.Json)
        {
            NewProjectReportWriter.WriteJson(stdout, NewProjectReportWriter.UsageError(diagnostics));
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }
        }

        return CliExitCode.Usage;
    }

    private static int WriteReport(
        NewProjectRequest request,
        TextWriter stdout,
        TextWriter stderr,
        NewProjectReport report,
        bool diagnosticsRendered)
    {
        if (request.Json)
        {
            NewProjectReportWriter.WriteJson(stdout, report);
        }
        else
        {
            if (!diagnosticsRendered)
            {
                foreach (var diagnostic in report.Diagnostics)
                {
                    stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
                }
            }

            NewProjectReportWriter.WriteHuman(stdout, report);
        }

        return report.ExitCode;
    }

    private static NewProjectReport Failure(
        int exitCode,
        string? projectPath,
        Guid? projectId,
        string? template,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        return new NewProjectReport(
            ContractVersions.NewProjectReportVersion,
            "newProject",
            Ok: false,
            exitCode,
            ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            projectPath,
            projectId,
            template);
    }

    private static bool TryParseTemplate(string value, out ProjectTemplateKind kind)
    {
        switch (value)
        {
            case "blank":
                kind = ProjectTemplateKind.Blank;
                return true;
            case "ceria":
                kind = ProjectTemplateKind.Ceria;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static bool TryNormalizeName(string raw, out string name, out string? error)
    {
        ArgumentNullException.ThrowIfNull(raw);
        name = raw.Trim();
        if (name.EndsWith(".mocprj", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^".mocprj".Length].Trim();
        }

        if (name.Length == 0)
        {
            error = "A project name is required.";
            return false;
        }

        if (name.Length > 128)
        {
            error = "The project name must not exceed 128 characters.";
            return false;
        }

        if (name.Contains('/') || name.Contains('\\') || name.Contains("..", StringComparison.Ordinal))
        {
            error = "The project name must not contain path separators or '..'.";
            return false;
        }

        if (name.Any(char.IsControl))
        {
            error = "The project name must not contain control characters.";
            return false;
        }

        foreach (var invalid in ":*?\"<>|")
        {
            if (name.Contains(invalid, StringComparison.Ordinal))
            {
                error = string.Create(
                    CultureInfo.InvariantCulture,
                    $"The project name must not contain the character '{invalid}'.");
                return false;
            }
        }

        error = null;
        return true;
    }
}

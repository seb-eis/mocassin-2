// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Diagnostics;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Signatures;
using Mocassin.Contracts;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Compile;

/// <summary>
/// Implements <c>mocassin compile</c>: parse-only load, pre-compile validation, the pure compiler, an optional
/// canonical inspection document, and the <c>compile-v1</c> report.
/// </summary>
/// <remarks>
/// The command never applies a parameterization, expands jobs, or writes a package. The compiler stays pure and
/// I/O-free; the CLI owns the file write and the report.
/// </remarks>
internal sealed class CompileCommand
{
    private readonly IModelCompiler compiler;

    /// <summary>
    /// Initializes the production command.
    /// </summary>
    public CompileCommand()
        : this(new ModelCompiler())
    {
    }

    /// <summary>
    /// Initializes the command with an explicit compiler for tests.
    /// </summary>
    /// <param name="compiler">The model compiler.</param>
    internal CompileCommand(IModelCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        this.compiler = compiler;
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
        CompileRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        ValidationResult? validation = null;
        CompilationResult? compilation = null;
        string? outputPath = null;
        string? outputSha256 = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
                .LoadAsync(request.ProjectPath, cancellationToken)
                .ConfigureAwait(false);
            if (load.Project is null)
            {
                var exitCode = ClassifyLoadExit(load.FailureKind);
                var diagnostics = load.Diagnostics;
                if (!diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    diagnostics = diagnostics.Add(CliDiagnosticFactory.Io(
                        $"The project '{request.ProjectPath}' could not be loaded."));
                }

                return Finish(
                    request,
                    stdout,
                    stderr,
                    CreateReport(exitCode, null, null, null, null, diagnostics));
            }

            var project = load.Project;
            validation = new ProjectValidationPipeline().Validate(
                project,
                loadDiagnostics: load.Diagnostics,
                options: new ValidationOptions { RunBuildStage = false },
                cancellationToken: cancellationToken);
            if (!validation.IsValid)
            {
                return Finish(
                    request,
                    stdout,
                    stderr,
                    CreateReport(CliExitCode.Invalid, validation, null, null, null, validation.Diagnostics));
            }

            compilation = compiler.Compile(project, cancellationToken: cancellationToken);
            if (compilation.Status != CompilationStatus.Succeeded || compilation.Model is null)
            {
                return Finish(
                    request,
                    stdout,
                    stderr,
                    CreateReport(
                        CliExitCode.Compilation,
                        validation,
                        compilation,
                        null,
                        null,
                        compilation.Diagnostics));
            }

            var model = compilation.Model;
            if (request.OutputPath is not null)
            {
                try
                {
                    var fullPath = Path.GetFullPath(request.OutputPath);
                    if (Directory.Exists(fullPath))
                    {
                        return Finish(
                            request,
                            stdout,
                            stderr,
                            CreateReport(
                                CliExitCode.Io,
                                validation,
                                compilation,
                                null,
                                null,
                                [CliDiagnosticFactory.Io(
                                    $"The output path '{fullPath}' is an existing directory.")]));
                    }

                    var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                        .GetBytes(CompiledModelJson.Write(model));
                    AtomicFileWriter.Write(fullPath, bytes, overwrite: true);
                    outputPath = fullPath;
                    outputSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
                }
                catch (Exception exception) when (exception is IOException
                                                      or UnauthorizedAccessException
                                                      or NotSupportedException
                                                      or System.Security.SecurityException
                                                      or ArgumentException)
                {
                    return Finish(
                        request,
                        stdout,
                        stderr,
                        CreateReport(
                            CliExitCode.Io,
                            validation,
                            compilation,
                            null,
                            null,
                            [CliDiagnosticFactory.Io(
                                $"The inspection document could not be written: {exception.Message}")]));
                }
            }

            return Finish(
                request,
                stdout,
                stderr,
                CreateReport(
                    CliExitCode.Success,
                    validation,
                    compilation,
                    outputPath,
                    outputSha256,
                    []));
        }
        catch (OperationCanceledException)
        {
            return Finish(
                request,
                stdout,
                stderr,
                CreateReport(
                    CliExitCode.Cancelled,
                    validation,
                    compilation,
                    null,
                    null,
                    [CliDiagnosticFactory.Cancelled("The command was cancelled.")]));
        }
#pragma warning disable CA1031 // The command boundary converts an unexpected failure into CLI007/exit 10.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            return Finish(
                request,
                stdout,
                stderr,
                CreateReport(
                    CliExitCode.Internal,
                    validation,
                    compilation,
                    null,
                    null,
                    [CliDiagnosticFactory.Internal(
                        $"Unexpected internal error: {exception.GetType().Name}: {exception.Message}")]));
        }
    }

    private static int Finish(
        CompileRequest request,
        TextWriter stdout,
        TextWriter stderr,
        CompileReport report)
    {
        if (request.Json)
        {
            CompileReportWriter.WriteJson(stdout, report);
        }
        else
        {
            foreach (var diagnostic in report.Diagnostics)
            {
                stderr.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            }

            CompileReportWriter.WriteHuman(stdout, report);
        }

        return report.ExitCode;
    }

    private static CompileReport CreateReport(
        int exitCode,
        ValidationResult? validation,
        CompilationResult? compilation,
        string? outputPath,
        string? outputSha256,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var reportDiagnostics = CliDiagnosticFactory.ToReportDiagnostics(diagnostics);
        var model = compilation?.Model;
        var report = compilation?.Report;
        return new CompileReport(
            ContractVersions.CompileReportVersion,
            "compile",
            exitCode == 0,
            exitCode,
            CommandLine.Reports.ReportEnvelope.Count(reportDiagnostics),
            reportDiagnostics,
            validation?.ProjectId,
            validation?.ModelHash,
            validation?.DocumentHash,
            model?.ModelSignature,
            report?.CompilerVersion,
            outputPath,
            outputSha256,
            model is null || report is null ? null : MapCounts(report.Counts),
            report is null ? null : new CompileReportCache(report.Cache.Hits, report.Cache.Misses, report.Cache.Capacity),
            report is null
                ? []
                : report.Stages
                    .Select(static stage => new CompileReportStage(
                        stage.Stage.ToString().ToLowerInvariant(),
                        stage.Elapsed.TotalMilliseconds))
                    .ToArray());
    }

    private static CompileReportCounts MapCounts(CompilationCounts counts) =>
        new(
            counts.Particles,
            counts.Positions,
            counts.Sites,
            counts.Environments,
            counts.PairTables,
            counts.ClusterTables,
            counts.Directions,
            counts.Rules,
            counts.Simulations);

    private static int ClassifyLoadExit(ProjectLoadFailureKind failureKind) => failureKind switch
    {
        ProjectLoadFailureKind.NotFound or ProjectLoadFailureKind.TooLarge or ProjectLoadFailureKind.Io =>
            CliExitCode.Io,
        _ => CliExitCode.Invalid,
    };
}

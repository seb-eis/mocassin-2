// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// The typed request of the <c>validate</c> command.
/// </summary>
/// <param name="ProjectPath">The project path.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
/// <param name="WarningsAsErrors">Whether warnings are promoted to errors.</param>
/// <param name="Statistics">Whether model statistics were requested.</param>
internal sealed record ValidateRequest(
    string ProjectPath,
    bool Json,
    bool WarningsAsErrors,
    bool Statistics);

/// <summary>
/// The typed request of the <c>build</c> and <c>build --preview</c> commands.
/// </summary>
/// <param name="ProjectPath">The project path.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
/// <param name="Preview">Whether the no-write preview mode was requested.</param>
/// <param name="OutputPath">The explicit package output path, or <see langword="null"/>.</param>
/// <param name="BuildTemplateId">The build template ID, or <see langword="null"/>.</param>
/// <param name="JobTemplateId">The job template ID, or <see langword="null"/>.</param>
/// <param name="ParameterizationId">The parameterization template ID, or <see langword="null"/>.</param>
/// <param name="Jobs">The raw job selection expression, or <see langword="null"/>.</param>
/// <param name="Parallel">The requested concurrency; zero means the implementation default.</param>
/// <param name="Compact">Whether the compact size preset was requested.</param>
/// <param name="Force">Whether an existing package may be replaced.</param>
/// <param name="WarningsAsErrors">Whether validation warnings are promoted to errors.</param>
/// <param name="Quiet">Whether stage, progress, and log events are suppressed.</param>
internal sealed record BuildCommandRequest(
    string ProjectPath,
    bool Json,
    bool Preview,
    string? OutputPath,
    Guid? BuildTemplateId,
    Guid? JobTemplateId,
    Guid? ParameterizationId,
    string? Jobs,
    int Parallel,
    bool Compact,
    bool Force,
    bool WarningsAsErrors,
    bool Quiet);

/// <summary>
/// The typed request of the <c>new</c> command.
/// </summary>
/// <param name="Name">The requested project name and file stem.</param>
/// <param name="OutputDirectory">The output directory, or <see langword="null"/> for the current directory.</param>
/// <param name="Template">The template name: <c>blank</c> or <c>ceria</c>.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record NewProjectRequest(
    string Name,
    string? OutputDirectory,
    string Template,
    bool Json);

/// <summary>
/// The typed request of the <c>compile</c> command.
/// </summary>
/// <param name="ProjectPath">The project path.</param>
/// <param name="OutputPath">The inspection output path, or <see langword="null"/>.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record CompileRequest(
    string ProjectPath,
    string? OutputPath,
    bool Json);

/// <summary>
/// The typed request of the <c>inspect</c> command.
/// </summary>
/// <param name="PackagePath">The package path.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record InspectRequest(
    string PackagePath,
    bool Json);

/// <summary>
/// The typed request of the <c>verify</c> command.
/// </summary>
/// <param name="PackagePath">The package path.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record VerifyRequest(
    string PackagePath,
    bool Json);

/// <summary>
/// The typed request of the <c>doctor</c> command.
/// </summary>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record DoctorRequest(bool Json);

/// <summary>
/// The typed request of the <c>clean</c> command.
/// </summary>
/// <param name="PackagePath">The package path.</param>
/// <param name="Directory">The job-folder directory, or <see langword="null"/> for the package directory.</param>
/// <param name="OlderThanDays">The optional age filter in days; zero means no filter.</param>
/// <param name="DryRun">Whether the dry-run listing was requested.</param>
/// <param name="Results">The reserved results-store path, or <see langword="null"/>.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record CleanRequest(
    string PackagePath,
    string? Directory,
    int OlderThanDays,
    bool DryRun,
    string? Results,
    bool Json);

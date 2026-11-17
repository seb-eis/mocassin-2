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
/// The typed request of the <c>build</c> command, including its interim no-write preview mode.
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
internal sealed record BuildRequest(
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
/// The typed request of the interim <c>build --preview</c> command.
/// </summary>
/// <param name="ProjectPath">The project path.</param>
/// <param name="JobTemplateId">The selected job template ID, or <see langword="null"/>.</param>
/// <param name="Json">Whether the JSON report was requested.</param>
internal sealed record PreviewRequest(string ProjectPath, Guid? JobTemplateId, bool Json);

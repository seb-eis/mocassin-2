// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Checks that the embedded blank and ceria starters load and validate without errors.
/// </summary>
internal sealed class ProjectTemplateCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "project-templates";

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var templates = ProjectTemplateFactory.All;
            var issues = new List<string>();
            foreach (var template in templates)
            {
                var validation = new ProjectValidationPipeline().Validate(
                    template.Project,
                    options: new ValidationOptions { RunBuildStage = false },
                    cancellationToken: cancellationToken);
                if (!validation.IsValid)
                {
                    issues.Add($"{template.Kind}: " + string.Join(
                        ", ",
                        validation.Diagnostics
                            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                            .Select(static diagnostic => diagnostic.Code)));
                }
            }

            return Task.FromResult(issues.Count == 0
                ? new DoctorCheckResult(
                    Name,
                    DoctorCheckStatus.Ok,
                    $"{templates.Length} embedded project templates are valid.")
                : new DoctorCheckResult(
                    Name,
                    DoctorCheckStatus.Fail,
                    "An embedded project template is invalid: " + string.Join("; ", issues) + "."));
        }
        catch (Exception exception) when (exception is InvalidOperationException
                                              or InvalidDataException
                                              or IOException
                                              or NotSupportedException
                                              or JsonException)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Fail,
                $"The embedded project templates could not be loaded: {exception.Message}",
                exception.GetType().Name));
        }
    }
}

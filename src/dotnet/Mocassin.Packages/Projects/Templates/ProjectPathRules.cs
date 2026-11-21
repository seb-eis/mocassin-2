// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Shared pure project name and target-path rules used by the CLI and the GUI. The caller supplies the single
/// file-system fact (<see cref="ProjectTargetState"/>); every other check is pure.
/// </summary>
public static class ProjectPathRules
{
    /// <summary>
    /// The maximum project name length.
    /// </summary>
    public const int MaxNameLength = 128;

    /// <summary>
    /// Normalizes a project name. A single trailing <c>.mocprj</c> is removed.
    /// </summary>
    /// <param name="rawName">The raw name.</param>
    /// <returns>The normalized name or the first failure.</returns>
    public static ProjectNameResult NormalizeName(string? rawName)
    {
        var name = (rawName ?? string.Empty).Trim();
        if (name.EndsWith(".mocprj", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^".mocprj".Length].Trim();
        }

        if (name.Length == 0)
        {
            return Failure(ProjectPathFailure.EmptyName, name);
        }

        if (name.Length > MaxNameLength)
        {
            return Failure(ProjectPathFailure.NameTooLong, name);
        }

        if (name.Contains('/') || name.Contains('\\'))
        {
            return Failure(ProjectPathFailure.PathSeparator, name);
        }

        if (name.Contains("..", StringComparison.Ordinal))
        {
            return Failure(ProjectPathFailure.ParentDirectory, name);
        }

        if (name.Any(char.IsControl))
        {
            return Failure(ProjectPathFailure.ControlCharacter, name);
        }

        foreach (var invalid in ":*?\"<>|")
        {
            if (name.Contains(invalid, StringComparison.Ordinal))
            {
                return new ProjectNameResult(
                    false,
                    name,
                    ProjectPathFailure.ReservedCharacter,
                    GetMessage(ProjectPathFailure.ReservedCharacter, invalid.ToString()));
            }
        }

        return new ProjectNameResult(true, name, ProjectPathFailure.None, null);
    }

    /// <summary>
    /// Combines a valid name with a directory and maps the supplied target state to a decision.
    /// </summary>
    /// <param name="directory">The target directory.</param>
    /// <param name="name">The normalized name result.</param>
    /// <param name="targetState">The caller-supplied target state.</param>
    /// <returns>The target decision.</returns>
    public static ProjectTargetResult ValidateTarget(
        string? directory,
        ProjectNameResult name,
        ProjectTargetState targetState)
    {
        if (!name.IsSuccess)
        {
            return new ProjectTargetResult(false, string.Empty, name.Failure, name.Message);
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            return new ProjectTargetResult(
                false,
                string.Empty,
                ProjectPathFailure.InvalidTargetPath,
                GetMessage(ProjectPathFailure.InvalidTargetPath));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Combine(directory, name.Name + ".mocprj"));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new ProjectTargetResult(
                false,
                string.Empty,
                ProjectPathFailure.InvalidTargetPath,
                GetMessage(ProjectPathFailure.InvalidTargetPath));
        }

        return targetState switch
        {
            ProjectTargetState.File => new ProjectTargetResult(
                false,
                fullPath,
                ProjectPathFailure.ExistingTarget,
                GetMessage(ProjectPathFailure.ExistingTarget, fullPath)),
            ProjectTargetState.Directory => new ProjectTargetResult(
                false,
                fullPath,
                ProjectPathFailure.IoFailure,
                GetMessage(ProjectPathFailure.IoFailure, fullPath)),
            _ => new ProjectTargetResult(true, fullPath, ProjectPathFailure.None, null),
        };
    }

    /// <summary>
    /// Gets the user-facing message for a failure. The CLI and the GUI use the same text.
    /// </summary>
    /// <param name="failure">The failure.</param>
    /// <param name="detail">The optional target path or reserved character.</param>
    /// <returns>The message.</returns>
    public static string GetMessage(ProjectPathFailure failure, string? detail = null) =>
        failure switch
        {
            ProjectPathFailure.None => string.Empty,
            ProjectPathFailure.EmptyName => "A project name is required.",
            ProjectPathFailure.NameTooLong => "The project name must not exceed 128 characters.",
            ProjectPathFailure.PathSeparator or ProjectPathFailure.ParentDirectory =>
                "The project name must not contain path separators or '..'.",
            ProjectPathFailure.ControlCharacter => "The project name must not contain control characters.",
            ProjectPathFailure.ReservedCharacter => detail is { Length: 1 }
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"The project name must not contain the character '{detail}'.")
                : "The project name must not contain a reserved character.",
            ProjectPathFailure.InvalidTargetPath => "The selected target directory is not valid.",
            ProjectPathFailure.ExistingTarget => detail is null
                ? "The project already exists. Choose another name or directory."
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"The project '{detail}' already exists. Choose another name or directory."),
            ProjectPathFailure.IoFailure => detail is null
                ? "The target path is not accessible."
                : string.Create(CultureInfo.InvariantCulture, $"The project path '{detail}' is an existing directory."),
            _ => "The project path is not valid.",
        };

    private static ProjectNameResult Failure(ProjectPathFailure failure, string name) =>
        new(false, name, failure, GetMessage(failure));
}

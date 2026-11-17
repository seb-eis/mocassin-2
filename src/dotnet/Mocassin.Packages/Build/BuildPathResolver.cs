// SPDX-License-Identifier: MIT

using Mocassin.Core.Model;

namespace Mocassin.Packages.Build;

/// <summary>
/// Resolves the effective package output path of a build.
/// </summary>
/// <remarks>
/// An explicit target is used verbatim after <see cref="Path.GetFullPath(string)"/>. Without one, a selected
/// build template's output name is treated as a file-name stem: path separators and <c>..</c> are rejected and
/// <c>.msl</c> is appended when missing. Otherwise the default is the project file stem with <c>.msl</c> in the
/// project directory.
/// </remarks>
public static class BuildPathResolver
{
    /// <summary>
    /// Resolves the effective output path.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="targetPath">The explicit target path, or <see langword="null"/>.</param>
    /// <param name="buildTemplate">The selected build template, or <see langword="null"/>.</param>
    /// <param name="path">The resolved absolute path, or <see langword="null"/> on failure.</param>
    /// <param name="error">The resolution error, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> when the path was resolved.</returns>
    /// <exception cref="ArgumentException"><paramref name="projectPath"/> is empty.</exception>
    public static bool TryResolve(
        string projectPath,
        string? targetPath,
        BuildTemplate? buildTemplate,
        out string? path,
        out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        path = null;
        error = null;

        var projectFullPath = Path.GetFullPath(projectPath);
        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            try
            {
                path = Path.GetFullPath(targetPath);
            }
            catch (Exception exception) when (exception is ArgumentException
                                                  or NotSupportedException
                                                  or PathTooLongException)
            {
                error = $"The output path '{targetPath}' is not valid: {exception.Message}";
            }

            return path is not null;
        }

        var directory = Path.GetDirectoryName(projectFullPath);
        if (string.IsNullOrEmpty(directory))
        {
            error = $"The project path '{projectFullPath}' has no parent directory.";
            return false;
        }

        var outputName = buildTemplate?.OutputName;
        if (string.IsNullOrWhiteSpace(outputName))
        {
            path = Path.Combine(directory, Path.GetFileNameWithoutExtension(projectFullPath) + ".msl");
            return true;
        }

        if (outputName.Contains('/') || outputName.Contains('\\') || outputName.Contains("..", StringComparison.Ordinal))
        {
            error = $"The build template output name '{outputName}' is not a file-name stem.";
            return false;
        }

        var fileName = outputName.EndsWith(".msl", StringComparison.OrdinalIgnoreCase)
            ? outputName
            : outputName + ".msl";
        path = Path.Combine(directory, fileName);
        return true;
    }
}

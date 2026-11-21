// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.Files;

/// <summary>
/// App-layer project file probe and directory operations. View models never touch System.IO directly.
/// </summary>
internal sealed class ProjectFileProbe : IProjectFileProbe
{
    /// <inheritdoc />
    public StringComparison PathComparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <inheritdoc />
    public string GetFullPath(string path) => Path.GetFullPath(path);

    /// <inheritdoc />
    public string Combine(string directory, string fileName) => Path.Combine(directory, fileName);

    /// <inheritdoc />
    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    /// <inheritdoc />
    public ProjectTargetState GetTargetState(string path)
    {
        var exists = File.Exists(path);
        var isDirectory = Directory.Exists(path);
        return ProjectTargetStateMapper.From(exists, isDirectory);
    }

    /// <inheritdoc />
    public bool TryCreateDirectory(string directory, out string? error)
    {
        try
        {
            Directory.CreateDirectory(directory);
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            error = $"The directory '{directory}' could not be created: {exception.Message}";
            return false;
        }
    }

    /// <inheritdoc />
    public ProjectFileStamp GetStamp(string path)
    {
        if (!File.Exists(path))
        {
            return ProjectFileStamp.Missing;
        }

        try
        {
            var info = new FileInfo(path);
            return new ProjectFileStamp(info.Length, info.LastWriteTimeUtc);
        }
        catch (IOException)
        {
            return ProjectFileStamp.Missing;
        }
        catch (UnauthorizedAccessException)
        {
            return ProjectFileStamp.Missing;
        }
    }
}

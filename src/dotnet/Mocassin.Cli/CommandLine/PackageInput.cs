// SPDX-License-Identifier: MIT

using Mocassin.Cli.Diagnostics;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Pre-checks a package path before the read-only inspector or verifier opens it.
/// </summary>
internal static class PackageInput
{
    /// <summary>
    /// Returns a <c>CLI002</c> diagnostic when the path is missing, a directory, or unreadable.
    /// </summary>
    /// <param name="path">The package path.</param>
    /// <returns>The diagnostic, or <see langword="null"/> when the file is readable.</returns>
    public static Diagnostic? CheckReadable(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Directory.Exists(path))
        {
            return CliDiagnosticFactory.Io($"The package path '{path}' is a directory.");
        }

        if (!File.Exists(path))
        {
            return CliDiagnosticFactory.Io($"The package '{path}' was not found.");
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or NotSupportedException
                                              or System.Security.SecurityException)
        {
            return CliDiagnosticFactory.Io($"The package '{path}' could not be read: {exception.Message}");
        }

        return null;
    }
}

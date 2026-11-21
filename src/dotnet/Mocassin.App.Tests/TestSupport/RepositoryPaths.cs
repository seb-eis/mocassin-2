// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Locates repository files from the test output directory.
/// </summary>
internal static class RepositoryPaths
{
    /// <summary>
    /// Gets the repository root.
    /// </summary>
    public static string Root { get; } = FindRoot();

    /// <summary>
    /// Combines path parts under the repository root.
    /// </summary>
    /// <param name="parts">The path parts.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "Mocassin.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }
}

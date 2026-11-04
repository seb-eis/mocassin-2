// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Tests;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string Combine(params string[] parts) =>
        Path.Combine([Root, .. parts]);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mocassin.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }
}

// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Mocassin.Cli.Tests.CommandLine;

namespace Mocassin.Cli.Tests.TestSupport;

/// <summary>
/// Creates and mutates real <c>.msl</c> packages for the inspection-command tests.
/// </summary>
internal static class PackageTestSupport
{
    public static string SamplePath => RepositoryPaths.Combine(
        "samples",
        "jobs",
        "ceria-temperature-sweep.mocprj");

    /// <summary>
    /// Builds a real package through the CLI.
    /// </summary>
    /// <param name="directory">The target directory.</param>
    /// <returns>The package path.</returns>
    public static async Task<string> CreateAsync(string directory)
    {
        var path = System.IO.Path.Combine(directory, "package.msl");
        var (exitCode, _, _) = await CliTestRunner.RunAsync(
            "build",
            SamplePath,
            "-o",
            path,
            "--json");
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(path));
        return path;
    }

    /// <summary>
    /// Executes one SQL statement against a package.
    /// </summary>
    /// <param name="path">The package path.</param>
    /// <param name="sql">The statement.</param>
    public static void Execute(string path, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadWrite;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

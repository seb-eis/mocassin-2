// SPDX-License-Identifier: MIT

using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Checks SQLite availability and reports the bundled SQLite library version.
/// </summary>
internal sealed class SqliteCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "sqlite";

    /// <inheritdoc />
    public string Name => CheckName;

    /// <summary>
    /// Opens an in-memory connection and returns <c>sqlite_version()</c>.
    /// </summary>
    /// <returns>The SQLite version string.</returns>
    public static string ReadVersion()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? "unknown";
    }

    /// <inheritdoc />
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var version = ReadVersion();
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Ok,
                $"SQLite {version}",
                version));
        }
        catch (Exception exception) when (exception is SqliteException or InvalidOperationException)
        {
            return Task.FromResult(new DoctorCheckResult(
                Name,
                DoctorCheckStatus.Fail,
                $"SQLite is not available: {exception.Message}",
                exception.GetType().Name));
        }
    }
}

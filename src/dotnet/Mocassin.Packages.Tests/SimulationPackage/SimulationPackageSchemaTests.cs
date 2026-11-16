// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Packages.Tests.SimulationPackage.TestSupport;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Verifies the published <c>.msl</c> schema, pragmas, version metadata, and immutability guarantees with real
/// SQLite files.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SimulationPackageSchemaTests
{
    [Fact]
    public async Task PublishedPackageUsesTheContractSchemaAndPragmas()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");

        var result = await SimulationPackageTestSupport.BuildAsync(path);

        Assert.Equal(path, result.Path);
        Assert.Equal(1, result.ModelCount);
        Assert.Equal(1, result.JobCount);
        Assert.Equal(1, result.LatticeCount);
        Assert.Equal(new FileInfo(path).Length, result.SizeBytes);
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))),
            result.Sha256);
        Assert.Empty(result.Warnings);
        Assert.Empty(temp.TemporaryFiles());
        Assert.False(File.Exists(path + "-wal"));
        Assert.False(File.Exists(path + "-shm"));

        await using var connection = await OpenReadOnlyAsync(path);
        Assert.Equal("delete", await QueryStringAsync(connection, "PRAGMA journal_mode;"));
        Assert.Equal(8192L, await QueryInt64Async(connection, "PRAGMA page_size;"));
        Assert.Equal(0x4D4F4353L, await QueryInt64Async(connection, "PRAGMA application_id;"));
        Assert.Equal(1L, await QueryInt64Async(connection, "PRAGMA user_version;"));

        var tables = await QueryStringsAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;");
        Assert.Equal(["jobs", "lattices", "models", "schema_info"], tables);
        Assert.DoesNotContain("results", tables);
        Assert.Equal(
            ["ix_jobs_collection", "ix_jobs_temperature"],
            await QueryStringsAsync(
                connection,
                "SELECT name FROM sqlite_master WHERE type = 'index' " +
                "AND name NOT LIKE 'sqlite_autoindex%' ORDER BY name;"));
        Assert.Equal(
            ["lattices", "models"],
            await QueryStringsAsync(
                connection,
                "SELECT \"table\" FROM pragma_foreign_key_list('jobs') ORDER BY \"table\";"));
        Assert.Equal(
            ["model_id", "signature", "format_version", "payload"],
            await QueryStringsAsync(
                connection,
                "SELECT name FROM pragma_table_info('models') ORDER BY cid;"));
        Assert.Equal(
            ["lattice_id", "signature", "format_version", "payload"],
            await QueryStringsAsync(
                connection,
                "SELECT name FROM pragma_table_info('lattices') ORDER BY cid;"));
        Assert.Equal(
            ["key", "value"],
            await QueryStringsAsync(
                connection,
                "SELECT name FROM pragma_table_info('schema_info') ORDER BY cid;"));
        Assert.Equal(
            [
                "job_id",
                "model_id",
                "lattice_id",
                "kind",
                "collection_name",
                "config_name",
                "collection_index",
                "config_index",
                "job_index",
                "temperature_k",
                "electric_field_vm",
                "target_mcsp",
                "write_out_count",
                "pre_run_mcsp",
                "time_limit_s",
                "min_success_rate",
                "lattice_a",
                "lattice_b",
                "lattice_c",
                "doping_info",
                "flags",
                "seed",
                "payload",
                "created_utc",
            ],
            await QueryStringsAsync(
                connection,
                "SELECT name FROM pragma_table_info('jobs') ORDER BY cid;"));
    }

    [Fact]
    public async Task BuildSessionPragmasAreApplied()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("pragmas.msl");
        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        await connection.OpenAsync();

        SimulationPackageWriter.ConfigureDatabase(connection);

        Assert.Equal("wal", await QueryStringAsync(connection, "PRAGMA journal_mode;"));
        Assert.Equal(1L, await QueryInt64Async(connection, "PRAGMA foreign_keys;"));
        Assert.Equal(1L, await QueryInt64Async(connection, "PRAGMA synchronous;"));
        Assert.Equal(2L, await QueryInt64Async(connection, "PRAGMA temp_store;"));
        Assert.Equal(8192L, await QueryInt64Async(connection, "PRAGMA page_size;"));
        Assert.Equal(0x4D4F4353L, await QueryInt64Async(connection, "PRAGMA application_id;"));
        Assert.Equal(1L, await QueryInt64Async(connection, "PRAGMA user_version;"));
    }

    [Fact]
    public async Task SchemaInfoCarriesTheBuildAndSymmetryMetadata()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        await SimulationPackageTestSupport.BuildAsync(path);

        await using var connection = await OpenReadOnlyAsync(path);
        var info = await QueryPairsAsync(connection, "SELECT key, value FROM schema_info;");

        Assert.Equal(9, info.Count);
        Assert.Equal("1", info["format_version"]);
        Assert.Equal("all", info["job_selection"]);
        Assert.Equal(
            SimulationPackageTestSupport.ProjectId.ToString("D"),
            info["project_id"]);
        Assert.Equal("2026-11-15T12:30:00.0000000Z", info["created_utc"]);
        Assert.Equal("symmetry-1", info["symmetry_contract_version"]);
        Assert.Equal("spglib-2.8.0/catalog-1", info["space_group_data_version"]);
        Assert.Equal(new string('a', 64), info["space_group_data_sha256"]);
        Assert.False(string.IsNullOrWhiteSpace(info["app_version"]));
        Assert.Equal($"mocassin .NET {info["app_version"]}", info["generator"]);
    }

    [Fact]
    public async Task JobRowUsesThePayloadDerivedAndAuthoredColumns()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        await SimulationPackageTestSupport.BuildAsync(path);

        await using var connection = await OpenReadOnlyAsync(path);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT model_id, lattice_id, kind, collection_name, config_name,
                   temperature_k, write_out_count, pre_run_mcsp, lattice_a, lattice_b, lattice_c,
                   flags, length(seed), length(payload), created_utc
            FROM jobs;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(1L, reader.GetInt64(1));
        Assert.Equal("kmc", reader.GetString(2));
        Assert.Equal("collection", reader.GetString(3));
        Assert.Equal("config", reader.GetString(4));
        Assert.Equal(900.0, reader.GetDouble(5));
        Assert.Equal(7L, reader.GetInt64(6));
        Assert.Equal(0L, reader.GetInt64(7));
        Assert.Equal(2L, reader.GetInt64(8));
        Assert.Equal(2L, reader.GetInt64(9));
        Assert.Equal(2L, reader.GetInt64(10));
        Assert.Equal(0x40L, reader.GetInt64(11));
        Assert.Equal(32L, reader.GetInt64(12));
        Assert.Equal(4L, reader.GetInt64(13));
        Assert.Equal("2026-11-15T12:30:00.0000000Z", reader.GetString(14));
        Assert.False(await reader.ReadAsync());

        Assert.Equal(
            SimulationPackageTestSupport.ModelSignature,
            await QueryStringAsync(connection, "SELECT signature FROM models;"));
        Assert.Equal(
            SimulationPackageTestSupport.LatticeSignature,
            await QueryStringAsync(connection, "SELECT signature FROM lattices;"));
    }

    [Fact]
    public async Task AdvisorySizeProducesPkg009WithoutFailingTheBuild()
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("ceria.msl");
        var result = await SimulationPackageTestSupport.BuildAsync(
            path,
            new PackageBuildOptions { AdvisorySizeBytes = 1 });

        var warning = Assert.Single(result.Warnings);
        Assert.Equal("PKG009", warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.True(File.Exists(result.Path));
    }

    private static async Task<SqliteConnection> OpenReadOnlyAsync(string path)
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<string> QueryStringAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static async Task<long> QueryInt64Async(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<List<string>> QueryStringsAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<Dictionary<string, string>> QueryPairsAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            values[reader.GetString(0)] = reader.GetString(1);
        }

        return values;
    }
}

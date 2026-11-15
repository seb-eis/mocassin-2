// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Packages;

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// The <c>.msl</c> v1 SQLite schema, pragmas, and <c>schema_info</c> keys.
/// </summary>
/// <remarks>
/// The schema is the one in data contracts §3.2. It contains only <c>schema_info</c>, <c>models</c>,
/// <c>lattices</c>, and <c>jobs</c>; there is no results table and no combined view. Build-time pragmas are
/// applied by <see cref="SimulationPackageWriter"/>; the published database uses <c>journal_mode=DELETE</c>.
/// </remarks>
internal static class SimulationPackageSchema
{
    /// <summary>The package format version written to <c>PRAGMA user_version</c> and <c>schema_info</c>.</summary>
    public const int FormatVersion = 1;

    /// <summary>The SQLite <c>application_id</c>: <c>"MOCS"</c>.</summary>
    public const long ApplicationId = PackageFormatConstants.ApplicationId;

    /// <summary>The SQLite page size, set before the first table.</summary>
    public const int PageSize = 8192;

    /// <summary>The required <c>schema_info</c> keys.</summary>
    public static readonly string[] SchemaInfoKeys =
    [
        "format_version",
        "app_version",
        "created_utc",
        "project_id",
        "generator",
        "job_selection",
        "symmetry_contract_version",
        "space_group_data_version",
        "space_group_data_sha256",
    ];

    /// <summary>The table and index DDL. All statements are idempotent within a new database.</summary>
    public const string CreateSql = """
        CREATE TABLE schema_info (
            key   TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );

        CREATE TABLE models (
            model_id       INTEGER PRIMARY KEY,
            signature      TEXT    NOT NULL UNIQUE,
            format_version INTEGER NOT NULL,
            payload        BLOB    NOT NULL
        );

        CREATE TABLE lattices (
            lattice_id     INTEGER PRIMARY KEY,
            signature      TEXT    NOT NULL UNIQUE,
            format_version INTEGER NOT NULL,
            payload        BLOB    NOT NULL
        );

        CREATE TABLE jobs (
            job_id            INTEGER PRIMARY KEY,
            model_id          INTEGER NOT NULL REFERENCES models(model_id),
            lattice_id        INTEGER NOT NULL REFERENCES lattices(lattice_id),
            kind              TEXT    NOT NULL CHECK (kind IN ('kmc','mmc')),
            collection_name   TEXT    NOT NULL,
            config_name       TEXT    NOT NULL,
            collection_index  INTEGER NOT NULL,
            config_index      INTEGER NOT NULL,
            job_index         INTEGER NOT NULL,
            temperature_k     REAL    NOT NULL,
            electric_field_vm REAL    NOT NULL DEFAULT 0.0,
            target_mcsp       INTEGER NOT NULL,
            write_out_count   INTEGER NOT NULL DEFAULT 100,
            pre_run_mcsp      INTEGER NOT NULL DEFAULT 0,
            time_limit_s      INTEGER,
            min_success_rate  REAL,
            lattice_a         INTEGER NOT NULL,
            lattice_b         INTEGER NOT NULL,
            lattice_c         INTEGER NOT NULL,
            doping_info       TEXT    NOT NULL DEFAULT '',
            flags             INTEGER NOT NULL DEFAULT 0,
            seed              BLOB    NOT NULL,
            payload           BLOB    NOT NULL,
            created_utc       TEXT    NOT NULL,
            UNIQUE (collection_index, config_index, job_index)
        );

        CREATE INDEX ix_jobs_temperature ON jobs(temperature_k);
        CREATE INDEX ix_jobs_collection  ON jobs(collection_name, config_name);
        """;
}

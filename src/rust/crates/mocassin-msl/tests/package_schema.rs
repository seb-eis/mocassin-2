// SPDX-License-Identifier: MIT

//! Package identity, schema, and column/payload cross-check tests on mutated fixture copies.

mod support;

use std::fs;
use std::path::PathBuf;

use mocassin_msl::{Package, PackageError};
use mocassin_types::{ErrorClass, ExitCode, ReaderLimits};
use rusqlite::Connection;

use support::{TempDir, copy_fixture};

fn mutated(label: &str, sql: &str) -> (TempDir, PathBuf) {
    let temp = TempDir::new(label);
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);
    let connection = Connection::open(&path).expect("open the copy read-write");
    connection.execute_batch(sql).expect("apply the mutation");
    drop(connection);
    (temp, path)
}

fn open_error(path: &std::path::Path) -> PackageError {
    match Package::open_readonly(path, ReaderLimits::default()) {
        Ok(_) => panic!("the mutated package must not open"),
        Err(error) => error,
    }
}

#[test]
fn a_wrong_application_id_is_rejected() {
    let (_temp, path) = mutated("app-id", "PRAGMA application_id = 12345;");
    let error = open_error(&path);
    assert!(matches!(error, PackageError::InvalidSchemaInfo(_)));
    assert_eq!(error.kind(), ErrorClass::Package);
}

#[test]
fn a_future_user_version_is_rejected() {
    let (_temp, path) = mutated("user-version", "PRAGMA user_version = 2;");
    let error = open_error(&path);
    assert!(matches!(
        error,
        PackageError::SchemaVersion { found: 2, .. }
    ));
    assert_eq!(error.exit_code(), ExitCode::PackageError);
}

#[test]
fn a_future_schema_info_version_is_rejected() {
    let (_temp, path) = mutated(
        "schema-info-version",
        "UPDATE schema_info SET value = '2' WHERE key = 'format_version';",
    );
    let error = open_error(&path);
    assert!(matches!(
        error,
        PackageError::SchemaVersion { found: 2, .. }
    ));
}

#[test]
fn a_missing_schema_info_key_is_rejected() {
    let (_temp, path) = mutated(
        "missing-key",
        "DELETE FROM schema_info WHERE key = 'project_id';",
    );
    let error = open_error(&path);
    assert!(matches!(
        error,
        PackageError::MissingSchemaElement {
            what: "schema_info key",
            name: "project_id",
        }
    ));
}

#[test]
fn a_duplicate_schema_info_key_is_rejected() {
    let (_temp, path) = mutated(
        "duplicate-key",
        "
        ALTER TABLE schema_info RENAME TO schema_info_old;
        CREATE TABLE schema_info (key TEXT, value TEXT);
        INSERT INTO schema_info SELECT key, value FROM schema_info_old;
        INSERT INTO schema_info VALUES ('format_version', '1');
        DROP TABLE schema_info_old;
        ",
    );
    let error = open_error(&path);
    assert!(matches!(error, PackageError::InvalidSchemaInfo(_)));
    assert!(error.to_string().contains("twice"));
}

#[test]
fn a_missing_table_is_rejected() {
    let (_temp, path) = mutated(
        "missing-table",
        "ALTER TABLE models RENAME TO models_renamed;",
    );
    let error = open_error(&path);
    assert!(matches!(
        error,
        PackageError::MissingSchemaElement {
            what: "table",
            name: "models",
        }
    ));
}

#[test]
fn a_missing_column_is_rejected() {
    let (_temp, path) = mutated(
        "missing-column",
        "ALTER TABLE models RENAME COLUMN signature TO signature_renamed;",
    );
    let error = open_error(&path);
    assert!(matches!(
        error,
        PackageError::MissingSchemaElement {
            what: "column",
            name: "signature",
        }
    ));
}

#[test]
fn a_model_format_version_column_mismatch_is_a_model_error() {
    let (_temp, path) = mutated("model-version", "UPDATE models SET format_version = 2;");
    let package = Package::open_readonly(&path, ReaderLimits::default()).expect("open");
    let error = package.load_job(1).expect_err("the model column disagrees");
    assert_eq!(error.kind(), ErrorClass::Model);
    assert_eq!(error.exit_code(), ExitCode::ModelError);
    assert!(matches!(
        error.source,
        PackageError::ColumnMismatch {
            column: "models.format_version",
            ..
        }
    ));
}

#[test]
fn a_lattice_format_version_column_mismatch_is_a_model_error() {
    let (_temp, path) = mutated("lattice-version", "UPDATE lattices SET format_version = 2;");
    let package = Package::open_readonly(&path, ReaderLimits::default()).expect("open");
    let error = package
        .load_job(1)
        .expect_err("the lattice column disagrees");
    assert_eq!(error.kind(), ErrorClass::Model);
    assert!(matches!(
        error.source,
        PackageError::ColumnMismatch {
            column: "lattices.format_version",
            ..
        }
    ));
}

#[test]
fn a_corrupt_model_payload_is_a_package_error() {
    let (_temp, path) = mutated("corrupt-model", "UPDATE models SET payload = x'00';");
    let package = Package::open_readonly(&path, ReaderLimits::default()).expect("open");
    let error = package.load_job(1).expect_err("the model frame is corrupt");
    assert_eq!(error.kind(), ErrorClass::Package);
    assert!(matches!(error.source, PackageError::InvalidFrame { .. }));
}

#[test]
fn a_corrupt_job_payload_is_a_package_error() {
    let temp = TempDir::new("corrupt-job");
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);
    let frame = zstd::bulk::compress(&[0x01], 3).expect("compress a non-map document");
    let connection = Connection::open(&path).expect("open the copy read-write");
    connection
        .execute(
            "UPDATE jobs SET payload = ?1 WHERE job_id = 1",
            rusqlite::params![frame],
        )
        .expect("replace the job payload");
    drop(connection);

    let package = Package::open_readonly(&path, ReaderLimits::default()).expect("open");
    let error = package
        .load_job(1)
        .expect_err("the job payload is not a map");
    assert_eq!(error.kind(), ErrorClass::Package);
    assert!(error.source.to_string().contains("expected a map"));
}

#[test]
fn an_empty_or_non_database_file_is_a_package_error() {
    let temp = TempDir::new("bad-files");

    let empty = temp.join("empty.msl");
    fs::write(&empty, []).expect("write an empty file");
    let error = open_error(&empty);
    assert_eq!(error.kind(), ErrorClass::Package);

    let junk = temp.join("junk.msl");
    fs::write(&junk, b"this is not a SQLite database").expect("write junk");
    let error = open_error(&junk);
    assert_eq!(error.kind(), ErrorClass::Package);
}

// SPDX-License-Identifier: MIT

//! End-to-end process tests for `mocassin-sim`.

mod support;

use std::fs;
use std::path::{Path, PathBuf};
use std::process::Output;

use support::{TempDir, command, copy_package, fixture_package, run};

fn stdout(output: &Output) -> String {
    String::from_utf8_lossy(&output.stdout).into_owned()
}

fn stderr(output: &Output) -> String {
    String::from_utf8_lossy(&output.stderr).into_owned()
}

fn exit_code(output: &Output) -> i32 {
    output.status.code().expect("the process exited normally")
}

fn canonical_fixture() -> String {
    fs::canonicalize(fixture_package())
        .expect("canonicalize the fixture")
        .to_str()
        .expect("UTF-8 path")
        .to_owned()
}

fn golden(package: &str) -> String {
    let template = fs::read_to_string(
        Path::new(env!("CARGO_MANIFEST_DIR"))
            .join("../../../../tests/contracts/packages/msl-v1/dry-run-v1.txt"),
    )
    .expect("read the checked-in dry-run golden");
    template
        .replace("{package}", package)
        .replace("{version}", env!("CARGO_PKG_VERSION"))
}

fn mutate(label: &str, sql: &str) -> (TempDir, PathBuf) {
    let temp = TempDir::new(label);
    let path = copy_package(&temp);
    let connection = rusqlite::Connection::open(&path).expect("open the copy");
    connection.execute_batch(sql).expect("apply the mutation");
    drop(connection);
    (temp, path)
}

#[test]
fn help_and_version_exit_zero() {
    let help = run(&["--help"]);
    assert_eq!(exit_code(&help), 0);
    assert!(stdout(&help).contains("Usage"));

    let version = run(&["--version"]);
    assert_eq!(exit_code(&version), 0);
    let text = stdout(&version);
    assert!(text.starts_with("mocassin-sim "));
    assert!(text.contains(env!("CARGO_PKG_VERSION")));
}

#[test]
fn missing_and_unknown_arguments_are_usage_errors() {
    let missing = run(&[]);
    assert_eq!(exit_code(&missing), 10);
    assert!(stderr(&missing).contains("error:"));

    let unknown = run(&["--db", "x.msl", "--job", "1", "--bogus"]);
    assert_eq!(exit_code(&unknown), 10);
    assert!(stderr(&unknown).contains("error:"));
}

#[test]
fn the_dry_run_summary_matches_the_frozen_format() {
    let package = canonical_fixture();
    let output = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);

    assert_eq!(exit_code(&output), 0, "{}", stderr(&output));
    assert_eq!(stdout(&output), golden(&package));
    assert!(output.stderr.is_empty(), "{}", stderr(&output));
}

#[test]
fn the_dry_run_writes_nothing() {
    let temp = TempDir::new("no-write");
    let package = copy_package(&temp);
    let package_text = package.to_str().expect("UTF-8 path").to_owned();
    let io = temp.join("io");
    let io_text = io.to_str().expect("UTF-8 path").to_owned();

    let output = run(&[
        "--db",
        &package_text,
        "--job",
        "1",
        "--io",
        &io_text,
        "--dry-run",
        "--log-level",
        "error",
    ]);

    assert_eq!(exit_code(&output), 0, "{}", stderr(&output));
    assert!(!io.exists(), "the dry run must not create --io");
    let entries: Vec<String> = fs::read_dir(temp.path())
        .expect("read the temporary directory")
        .map(|entry| {
            entry
                .expect("directory entry")
                .file_name()
                .to_string_lossy()
                .into_owned()
        })
        .collect();
    assert_eq!(entries, vec!["copy.msl"]);
    for suffix in ["-wal", "-shm"] {
        let sidecar = format!("{}{suffix}", package.display());
        assert!(!Path::new(&sidecar).exists(), "no {suffix} sidecar");
    }
}

#[test]
fn without_dry_run_is_a_usage_error() {
    let package = canonical_fixture();
    let output = run(&["--db", &package, "--job", "1", "--log-level", "error"]);
    assert_eq!(exit_code(&output), 10);
    assert!(stderr(&output).contains("simulation engines are implemented"));
    assert!(stdout(&output).is_empty());
}

#[test]
fn an_unknown_job_is_an_argument_error() {
    let package = canonical_fixture();
    let output = run(&[
        "--db",
        &package,
        "--job",
        "99",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 10);
    assert!(stderr(&output).contains("job 99"));
}

#[test]
fn a_missing_or_directory_package_is_an_io_error() {
    let temp = TempDir::new("io-errors");
    let missing = temp.join("missing.msl");
    let output = run(&[
        "--db",
        missing.to_str().expect("UTF-8 path"),
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 16);

    let directory = temp.path();
    let output = run(&[
        "--db",
        directory.to_str().expect("UTF-8 path"),
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 16);
}

#[test]
fn a_corrupt_package_is_a_package_error() {
    let (_temp, path) = mutate("corrupt", "UPDATE models SET payload = x'00';");
    let output = run(&[
        "--db",
        path.to_str().expect("UTF-8 path"),
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 11, "{}", stderr(&output));
    assert!(stderr(&output).contains("payload"));
}

#[test]
fn a_signature_mismatch_is_a_model_error() {
    let (_temp, path) = mutate(
        "signature",
        "UPDATE models SET signature = 'dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd';",
    );
    let output = run(&[
        "--db",
        path.to_str().expect("UTF-8 path"),
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 12, "{}", stderr(&output));
    assert!(stderr(&output).contains("signature mismatch"));
}

#[test]
fn extension_requests_and_conflicts_are_usage_errors() {
    let package = canonical_fixture();

    let requested = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--extension",
        "x.so",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&requested), 10);
    assert!(stderr(&requested).contains("extension"));

    let conflict = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--extension",
        "x.so",
        "--no-extensions",
    ]);
    assert_eq!(exit_code(&conflict), 10);
}

#[test]
fn invalid_option_values_are_usage_errors() {
    let package = canonical_fixture();
    let package = package.as_str();
    for arguments in [
        vec!["--db", package, "--job", "1", "--seed", "zz"],
        vec!["--db", package, "--job", "1", "--threads", "0"],
        vec!["--db", package, "--job", "1", "--threads", "257"],
        vec!["--db", package, "--job", "1", "--jump-log-max-ev", "0"],
        vec!["--db", package, "--job", "1", "--jump-log-max-ev", "NaN"],
        vec!["--db", package, "--job", "1", "--log-level", "verbose"],
    ] {
        let with_dry_run: Vec<&str> = arguments.iter().copied().chain(["--dry-run"]).collect();
        let output = run(&with_dry_run);
        assert_eq!(exit_code(&output), 10, "{arguments:?}: {}", stderr(&output));
    }
}

#[test]
fn the_seed_override_is_reported_by_hash() {
    let package = canonical_fixture();
    let seed = "00".repeat(32);
    let output = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--seed",
        &seed,
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 0, "{}", stderr(&output));
    assert!(
        stdout(&output)
            .contains("seed_hash=66687aadf862bd776c8fc18b8e9f8e20089714856ee233b3902a591d0d5f2925"),
        "{}",
        stdout(&output)
    );
}

#[test]
fn log_level_and_format_control_stderr() {
    let package = canonical_fixture();

    let human = run(&["--db", &package, "--job", "1", "--dry-run"]);
    assert_eq!(exit_code(&human), 0);
    assert!(stderr(&human).contains("job loaded"), "{}", stderr(&human));
    assert!(stderr(&human).contains("INFO"), "{}", stderr(&human));

    let json = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--log-format",
        "json",
    ]);
    assert_eq!(exit_code(&json), 0);
    assert!(
        stderr(&json).contains("\"message\":\"job loaded\""),
        "{}",
        stderr(&json)
    );

    let quiet = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&quiet), 0);
    assert!(quiet.stderr.is_empty(), "{}", stderr(&quiet));

    // RUST_LOG is not consulted; --log-level is the only filter.
    let with_rust_log = command()
        .args([
            "--db",
            &package,
            "--job",
            "1",
            "--dry-run",
            "--log-level",
            "error",
        ])
        .env("RUST_LOG", "trace")
        .output()
        .expect("run mocassin-sim");
    assert_eq!(exit_code(&with_rust_log), 0);
    assert!(
        with_rust_log.stderr.is_empty(),
        "{}",
        stderr(&with_rust_log)
    );
}

#[test]
fn no_extensions_alone_is_a_no_op() {
    let package = canonical_fixture();
    let output = run(&[
        "--db",
        &package,
        "--job",
        "1",
        "--dry-run",
        "--no-extensions",
        "--log-level",
        "error",
    ]);
    assert_eq!(exit_code(&output), 0);
}

#[cfg(debug_assertions)]
#[test]
fn a_panic_is_an_internal_error() {
    let package = canonical_fixture();
    let output = command()
        .args([
            "--db",
            package.as_str(),
            "--job",
            "1",
            "--dry-run",
            "--log-level",
            "error",
        ])
        .env("MOCASSIN_SIM_TEST_PANIC", "1")
        .output()
        .expect("run mocassin-sim");

    assert_eq!(exit_code(&output), 13, "{}", stderr(&output));
    assert!(stderr(&output).contains("internal error"));
}

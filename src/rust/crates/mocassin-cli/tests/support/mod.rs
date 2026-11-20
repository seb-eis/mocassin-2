// SPDX-License-Identifier: MIT

//! Shared helpers for the `mocassin-sim` process tests.

#![allow(dead_code)]

use std::fs;
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static COUNTER: AtomicU64 = AtomicU64::new(0);

/// A unique temporary directory that removes itself on drop.
pub struct TempDir {
    path: PathBuf,
}

impl TempDir {
    /// Creates a unique directory under the system temporary directory.
    pub fn new(label: &str) -> Self {
        let unique = COUNTER.fetch_add(1, Ordering::Relaxed);
        let path = std::env::temp_dir().join(format!(
            "mocassin-cli-test-{label}-{}-{unique}",
            std::process::id()
        ));
        fs::create_dir_all(&path).expect("create the temporary directory");
        Self { path }
    }

    /// Returns the directory path.
    pub fn path(&self) -> &Path {
        &self.path
    }

    /// Joins a name onto the directory.
    pub fn join(&self, name: &str) -> PathBuf {
        self.path.join(name)
    }
}

impl Drop for TempDir {
    fn drop(&mut self) {
        let _ = fs::remove_dir_all(&self.path);
    }
}

/// Returns the workspace `tests/contracts/packages` directory.
pub fn packages_dir() -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR")).join("../../../../tests/contracts/packages")
}

/// Returns the `msl-v1` package path.
pub fn fixture_package() -> PathBuf {
    packages_dir().join("msl-v1/ceria-temperature-sweep.msl")
}

/// Returns the `mocassin-sim` binary path.
pub fn binary() -> &'static str {
    env!("CARGO_BIN_EXE_mocassin-sim")
}

/// Builds a clean command with the test-only environment removed.
pub fn command() -> Command {
    let mut command = Command::new(binary());
    command.env_remove("RUST_LOG");
    command.env_remove("MOCASSIN_SIM_TEST_PANIC");
    command
}

/// Runs the binary and returns its output.
pub fn run(arguments: &[&str]) -> Output {
    command()
        .args(arguments)
        .output()
        .expect("run mocassin-sim")
}

/// Copies the fixture package into a temporary directory.
pub fn copy_package(directory: &TempDir) -> PathBuf {
    let target = directory.join("copy.msl");
    fs::copy(fixture_package(), &target).expect("copy the package");
    target
}

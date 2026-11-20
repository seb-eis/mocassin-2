// SPDX-License-Identifier: MIT

//! Shared helpers for the RNG vector tests.

#![allow(dead_code)]

pub mod json;

use std::path::{Path, PathBuf};

/// Returns the checked-in cross-language RNG vector document.
pub fn vectors_path() -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR")).join("../../../../tests/contracts/rng-vectors.json")
}

// SPDX-License-Identifier: MIT

//! Fuzz the read-only package open and one-job load over a mutated SQLite file.

#![no_main]

use std::fs;
use std::sync::atomic::{AtomicU64, Ordering};

use libfuzzer_sys::fuzz_target;
use mocassin_types::ReaderLimits;

fuzz_target!(|data: &[u8]| {
    static NEXT: AtomicU64 = AtomicU64::new(0);
    let path = std::env::temp_dir().join(format!(
        "mocassin-msl-fuzz-{}-{}.msl",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));

    if fs::write(&path, data).is_ok()
        && let Ok(package) = mocassin_msl::Package::open_readonly(&path, ReaderLimits::default())
    {
        let _ = package.job_ids();
        let _ = package.load_job(1);
    }

    let _ = fs::remove_file(&path);
});

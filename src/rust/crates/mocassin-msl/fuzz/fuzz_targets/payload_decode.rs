// SPDX-License-Identifier: MIT

//! Fuzz the job and lattice typed decode paths.

#![no_main]

use libfuzzer_sys::fuzz_target;
use mocassin_types::ReaderLimits;

fuzz_target!(|data: &[u8]| {
    let limits = ReaderLimits::default();
    let _ = mocassin_msl::decode_job_payload(data, &limits);
    let _ = mocassin_msl::decode_lattice_payload(data, &limits);
});

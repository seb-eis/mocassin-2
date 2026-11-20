// SPDX-License-Identifier: MIT

//! Fuzz the model-payload scan (the decode entry point runs the bounded scan first).

#![no_main]

use libfuzzer_sys::fuzz_target;
use mocassin_types::ReaderLimits;

fuzz_target!(|data: &[u8]| {
    let limits = ReaderLimits::default();
    let _ = mocassin_msl::decode_model_payload(data, &limits);
});

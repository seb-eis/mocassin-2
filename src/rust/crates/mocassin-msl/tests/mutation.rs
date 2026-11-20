// SPDX-License-Identifier: MIT

//! Property tests: truncations and byte mutations of real payloads and SQLite pages never panic.

mod support;

use std::fs;
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

use mocassin_msl::{Package, decode_job_payload, decode_model_payload};
use mocassin_types::ReaderLimits;
use proptest::prelude::*;

static COUNTER: AtomicU64 = AtomicU64::new(0);

fn model_payload() -> Vec<u8> {
    fs::read(support::raw_payload("model-1.bin")).expect("read the model payload")
}

fn job_payload() -> Vec<u8> {
    fs::read(support::raw_payload("job-1.bin")).expect("read the job payload")
}

fn temp_package_copy() -> PathBuf {
    let unique = COUNTER.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "mocassin-msl-mutation-{}-{unique}.msl",
        std::process::id()
    ));
    fs::copy(
        support::fixture_file("msl-v1", "ceria-temperature-sweep.msl"),
        &path,
    )
    .expect("copy the fixture package");
    path
}

proptest! {
    #![proptest_config(ProptestConfig {
        cases: 24,
        ..ProptestConfig::default()
    })]

    #[test]
    fn truncated_model_payloads_fail_without_panicking(cut in 0_usize..2048) {
        let payload = model_payload();
        let cut = cut.min(payload.len());
        let result = decode_model_payload(&payload[..cut], &ReaderLimits::default());
        prop_assert!(result.is_err());
    }

    #[test]
    fn mutated_model_bytes_fail_without_panicking(index in 0_usize..2048, value in any::<u8>()) {
        let mut payload = model_payload();
        let index = index % payload.len();
        payload[index] = value;
        let _ = decode_model_payload(&payload, &ReaderLimits::default());
        prop_assert!(true);
    }

    #[test]
    fn mutated_job_bytes_fail_without_panicking(index in 0_usize..512, value in any::<u8>()) {
        let mut payload = job_payload();
        let index = index % payload.len();
        payload[index] = value;
        let _ = decode_job_payload(&payload, &ReaderLimits::default());
        prop_assert!(true);
    }

    #[test]
    fn arbitrary_messagepack_bytes_fail_without_panicking(
        bytes in prop::collection::vec(any::<u8>(), 0..2048)
    ) {
        let limits = ReaderLimits::default();
        let _ = decode_model_payload(&bytes, &limits);
        let _ = decode_job_payload(&bytes, &limits);
        let _ = mocassin_msl::decode_lattice_payload(&bytes, &limits);
        prop_assert!(true);
    }

    #[test]
    fn mutated_sqlite_pages_fail_without_panicking(offset in 0_usize..98_304, value in any::<u8>()) {
        let path = temp_package_copy();
        let mut bytes = fs::read(&path).expect("read the package copy");
        let offset = offset % bytes.len();
        bytes[offset] = value;
        fs::write(&path, &bytes).expect("write the mutated package");

        if let Ok(package) = Package::open_readonly(&path, ReaderLimits::default()) {
            let _ = package.job_ids();
            let _ = package.load_job(1);
        }

        let _ = fs::remove_file(&path);
        prop_assert!(true);
    }
}

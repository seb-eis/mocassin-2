// SPDX-License-Identifier: MIT

//! Criterion benchmark for the Step 13 package load path.
//!
//! The budgets are fixture budgets on the benchmark laptop class:
//! open + cold `load_job` below 100 ms and warm `load_job` well below that. They are not a promise for a 512 MiB
//! package; the reader's limits bound the large-package path separately.

use std::hint::black_box;
use std::path::{Path, PathBuf};

use criterion::{Criterion, criterion_group, criterion_main};
use mocassin_msl::Package;
use mocassin_types::ReaderLimits;

fn fixture() -> PathBuf {
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl")
}

fn load(c: &mut Criterion) {
    let path = fixture();
    let limits = ReaderLimits::default();
    let mut group = c.benchmark_group("msl_load");

    group.bench_function("open", |bench| {
        bench.iter(|| black_box(Package::open_readonly(&path, limits).expect("open the fixture")));
    });

    group.bench_function("cold_load_job_1", |bench| {
        bench.iter(|| {
            let package = Package::open_readonly(&path, limits).expect("open the fixture");
            black_box(package.load_job(1).expect("load job 1"))
        });
    });

    group.bench_function("warm_load_job_1", |bench| {
        let package = Package::open_readonly(&path, limits).expect("open the fixture");
        let _ = package.load_job(1).expect("warm the model cache");
        bench.iter(|| black_box(package.load_job(1).expect("load job 1")));
    });

    group.finish();
}

criterion_group!(benches, load);
criterion_main!(benches);

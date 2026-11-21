// SPDX-License-Identifier: MIT

//! Step 14 context-initialization benchmark.
//!
//! Two scenarios share one runner class: the real-writer table-free ceria 10×10×10 package and the populated
//! `step-13-populated` model on a hand-built 10×10×10 lattice (3,000 environments, real pair/cluster tables).
//! Setup (job cloning) is outside the measured region. See Step 14 §20 and `tests/reference/bench/`.

use std::path::Path;

use criterion::{BatchSize, Criterion, criterion_group, criterion_main};
use mocassin_msl::Package;
use mocassin_solver::context::{ContextOptions, InitOutcome, SimulationContext};
use mocassin_types::{InitialLattice, JobPackage, ParticleId, ReaderLimits};
use std::hint::black_box;

fn load(package: &str, file: &str, job_id: u32) -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(package)
        .join(file);
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the fixture opens")
        .load_job(job_id)
        .expect("the fixture job maps")
}

fn ceria_10x10x10() -> JobPackage {
    load("step-14-ceria-10x10x10", "step-14-ceria-10x10x10.msl", 1)
}

fn populated_10x10x10_clone() -> JobPackage {
    let job = load("step-13-populated", "step-13-populated.msl", 1);
    let positions = job.lattice.position_count as usize;
    let particles = vec![ParticleId::new(1); 10 * 10 * 10 * positions];
    JobPackage {
        lattice: InitialLattice {
            signature: "step-14-populated-10x10x10-clone".to_owned(),
            lattice_size: [10, 10, 10],
            position_count: job.lattice.position_count,
            particles,
        },
        ..job
    }
}

fn initialize(job: JobPackage) {
    let context = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect("the benchmark job initializes");
    let InitOutcome::Ready(context) = context else {
        panic!("a fresh job is not already completed");
    };
    black_box(context);
}

fn benchmark(criterion: &mut Criterion) {
    let mut group = criterion.benchmark_group("context");
    let ceria = ceria_10x10x10();
    group.bench_function("ceria_10x10x10", |bencher| {
        bencher.iter_batched(|| ceria.clone(), initialize, BatchSize::SmallInput);
    });

    let populated = populated_10x10x10_clone();
    group.bench_function("populated_clone_10x10x10", |bencher| {
        bencher.iter_batched(|| populated.clone(), initialize, BatchSize::SmallInput);
    });
    group.finish();
}

criterion_group!(benches, benchmark);
criterion_main!(benches);

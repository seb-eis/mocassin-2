// SPDX-License-Identifier: MIT

//! Criterion benchmarks for the Step 13 RNG performance budgets.
//!
//! Budgets (Step 13 design §4.5, single-threaded release):
//!
//! | Measurement | Budget |
//! | --- | ---: |
//! | `next_u32` | >= 250 M draws/s |
//! | `next_u64` | >= 150 M draws/s |
//! | `next_f64` | >= 120 M draws/s |
//! | `next_bounded(2/10/1024)` | >= 200 M calls/s, <= 1.01 draws/call |
//! | buffer refill (64 words) | <= 250 ns |
//! | `export_state` + `from_state` | <= 500 ns |
//! | `fork` (32-byte domain) | <= 1 us |
//!
//! Criterion reports throughput in draws/s (or per 64-word refill); the closeout evidence records the
//! environment and the measured values in `tests/reference/bench/step-13-rng.json`.

use std::hint::black_box;

use criterion::{Criterion, Throughput, criterion_group, criterion_main};

use mocassin_rng::Rng;

const SEED: [u8; 32] = [0x5a; 32];
const FORK_DOMAIN: [u8; 32] = [0x41; 32];

fn draws(c: &mut Criterion) {
    let mut group = c.benchmark_group("rng");

    group.throughput(Throughput::Elements(1));
    group.bench_function("next_u32", |bench| {
        let mut rng = Rng::from_seed(SEED);
        bench.iter(|| black_box(rng.next_u32()));
    });

    group.bench_function("next_u64", |bench| {
        let mut rng = Rng::from_seed(SEED);
        bench.iter(|| black_box(rng.next_u64()));
    });

    group.bench_function("next_f64", |bench| {
        let mut rng = Rng::from_seed(SEED);
        bench.iter(|| black_box(rng.next_f64()));
    });

    for upper in [2_u32, 10, 1024] {
        group.bench_function(format!("next_bounded_{upper}"), |bench| {
            let mut rng = Rng::from_seed(SEED);
            bench.iter(|| black_box(rng.next_bounded(upper)));
        });
    }

    group.throughput(Throughput::Elements(64));
    group.bench_function("refill_64_words", |bench| {
        let mut rng = Rng::from_seed(SEED);
        bench.iter(|| {
            for _ in 0..64 {
                black_box(rng.next_u32());
            }
        });
    });

    group.throughput(Throughput::Elements(1));
    group.bench_function("state_round_trip", |bench| {
        let mut rng = Rng::from_seed(SEED);
        let _ = rng.next_u64();
        bench.iter(|| {
            let state = rng.export_state();
            black_box(Rng::from_state(&state).expect("the exported state is valid"))
        });
    });

    group.bench_function("fork_32_bytes", |bench| {
        let mut rng = Rng::from_seed(SEED);
        bench.iter(|| black_box(rng.fork(black_box(&FORK_DOMAIN))));
    });

    group.finish();
}

criterion_group!(benches, draws);
criterion_main!(benches);

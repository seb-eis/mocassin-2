// SPDX-License-Identifier: MIT

//! Property tests for the Step 13 RNG contract.
//!
//! These tests are deterministic: every case is derived from a proptest seed, and the generator never reads the
//! clock or the operating system.

use mocassin_rng::{Rng, RngStateV1};
use proptest::prelude::*;

proptest! {
    #[test]
    fn bounded_draws_stay_half_open(seed: [u8; 32], upper in 1_u32..=u32::MAX) {
        let mut rng = Rng::from_seed(seed);
        for _ in 0..16 {
            prop_assert!(rng.next_bounded(upper) < upper, "upper={upper}");
        }
    }

    #[test]
    fn doubles_stay_in_the_unit_interval(seed: [u8; 32]) {
        let mut rng = Rng::from_seed(seed);
        for _ in 0..64 {
            let value = rng.next_f64();
            prop_assert!((0.0..1.0).contains(&value), "value={value}");
        }
    }

    #[test]
    fn export_and_restore_reproduce_the_continuation(
        seed: [u8; 32],
        skip_words in 0_usize..=4096,
    ) {
        let mut rng = Rng::from_seed(seed);
        for _ in 0..skip_words {
            let _ = rng.next_u32();
        }

        let state = rng.export_state();
        prop_assert_eq!(RngStateV1::from_bytes(state.as_bytes()), Ok(state.clone()));

        let mut resumed = Rng::from_state(&state).expect("the exported state is valid");
        for _ in 0..32 {
            prop_assert_eq!(resumed.next_u64(), rng.next_u64());
        }
    }

    #[test]
    fn fork_never_advances_the_parent(
        seed: [u8; 32],
        skip_words in 0_usize..=256,
        domain in proptest::collection::vec(any::<u8>(), 1..=255),
    ) {
        let mut parent = Rng::from_seed(seed);
        for _ in 0..skip_words {
            let _ = parent.next_u32();
        }
        let before = parent.export_state();

        let mut child = parent.try_fork(&domain).expect("1..=255 bytes is a valid domain");
        prop_assert_eq!(parent.export_state(), before);

        // The child is an independent stream: it can be resumed exactly like any other.
        let _ = child.next_u32();
        let child_state = child.export_state();
        let mut resumed = Rng::from_state(&child_state).expect("the child state is valid");
        prop_assert_eq!(resumed.next_u32(), child.next_u32());
    }
}

#[test]
fn bounded_draws_are_roughly_uniform_over_sixteen_buckets() {
    const DRAWS: u32 = 160_000;
    const BUCKETS: usize = 16;
    const EXPECTED: f64 = DRAWS as f64 / BUCKETS as f64;

    let mut rng = Rng::from_seed([0x42; 32]);
    let mut counts = [0_u32; BUCKETS];
    for _ in 0..DRAWS {
        counts[rng.next_bounded(BUCKETS as u32) as usize] += 1;
    }

    let chi_square: f64 = counts
        .iter()
        .map(|&count| {
            let deviation = f64::from(count) - EXPECTED;
            deviation * deviation / EXPECTED
        })
        .sum();

    // 15 degrees of freedom: 60 is far above the 1e-7 critical value, so a real bias still fails.
    assert!(
        chi_square < 60.0,
        "chi-square {chi_square}, counts {counts:?}"
    );
}

#[test]
fn bounded_draws_consume_close_to_one_word_per_call() {
    let mut rng = Rng::from_seed([0x43; 32]);
    let before = rng.export_state().word_pos();
    for _ in 0..100_000 {
        let _ = rng.next_bounded(10);
    }
    let consumed = rng.export_state().word_pos() - before;

    // The rejection probability of Lemire's method is below 1e-8 for upper = 10; the budget is 1.01 words/call.
    assert!(
        consumed <= 101_000,
        "consumed {consumed} words for 100000 draws"
    );
    assert!(
        consumed >= 100_000,
        "consumed {consumed} words for 100000 draws"
    );
}

#[test]
fn forks_with_different_domains_diverge() {
    let mut parent = Rng::from_seed([0x44; 32]);
    let mut second = parent.fork(b"extension:second");
    let mut first = parent.fork(b"extension:first");
    assert_ne!(first.next_u64(), second.next_u64());
}

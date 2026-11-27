// SPDX-License-Identifier: MIT

//! PR-fast acceptance tests for the Step 15 feature-1 acceptance-exponential kernel.

use mocassin_solver::energy::acceptance_exponential;
use mocassin_types::SolverJobFlags;

#[test]
fn the_libm_kernel_is_the_default_and_computes_exp_minus_barrier() {
    assert!(!SolverJobFlags::default().contains(SolverJobFlags::USE_FAST_EXP));
    for barrier in [-100.0, -2.0, 0.0, 2.0, 100.0] {
        assert_eq!(acceptance_exponential(barrier, false), (-barrier).exp());
    }
}

#[test]
fn the_fast_kernel_stays_within_the_frozen_relative_error_bound() {
    for barrier in (-2800..=2800).map(|step| f64::from(step) * 0.25) {
        let expected = (-barrier).exp();
        let actual = acceptance_exponential(barrier, true);
        let relative_error = (actual - expected).abs() / expected;
        assert!(
            relative_error <= 0.035,
            "barrier {barrier}: expected {expected}, got {actual}, relative error {relative_error}"
        );
    }
}

#[test]
fn both_kernels_preserve_the_standard_non_finite_semantics() {
    for use_fast_exp in [false, true] {
        assert_eq!(acceptance_exponential(f64::INFINITY, use_fast_exp), 0.0);
        assert_eq!(
            acceptance_exponential(f64::NEG_INFINITY, use_fast_exp),
            f64::INFINITY
        );
        assert!(acceptance_exponential(f64::NAN, use_fast_exp).is_nan());
    }
}

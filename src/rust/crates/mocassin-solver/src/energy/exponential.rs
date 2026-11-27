// SPDX-License-Identifier: MIT

//! The acceptance-exponential kernel.

const LN_2: f64 = core::f64::consts::LN_2;
const MIN_FAST_EXP: f64 = -700.0;
const MAX_FAST_EXP: f64 = 700.0;

/// Computes the raw acceptance probability `exp(-barrier)`.
///
/// The default (`use_fast_exp = false`) is the platform libm implementation through [`f64::exp`]. The
/// optional fast path splits `x = -barrier` into a binary exponent and a cubic residual, constructing the
/// exponent bits directly. It is used only for finite normal-range inputs; non-finite and extreme inputs use
/// libm so overflow, underflow, and NaN keep their standard meaning. The approximation is an opt-in job flag
/// (`SolverJobFlags::USE_FAST_EXP`), never the default.
#[must_use]
pub fn acceptance_exponential(barrier: f64, use_fast_exp: bool) -> f64 {
    let exponent = -barrier;
    if !use_fast_exp || !exponent.is_finite() || !(MIN_FAST_EXP..=MAX_FAST_EXP).contains(&exponent)
    {
        return exponent.exp();
    }

    let scaled = exponent * core::f64::consts::LOG2_E;
    let integral = scaled.floor();
    let fraction = (scaled - integral) * LN_2;
    let residual = 1.0 + fraction * (1.0 + fraction * (0.5 + fraction * (1.0 / 6.0)));
    let bits = ((integral as i32 + 1023) as u64) << 52;
    residual * f64::from_bits(bits)
}

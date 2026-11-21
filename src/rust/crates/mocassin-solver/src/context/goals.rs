// SPDX-License-Identifier: MIT

//! Counters, goals, normalization, and the time step of a context.
//!
//! Step 14 allocates and zeroes the per-particle counters and the two scalar accumulators once; Steps 16/17
//! increment them and Step 18 writes them to `.mcs`. The goal arithmetic, the block size, the normalization
//! state, and the time step are derived once here from the job and the resumed state (Step 14 §10/§11).

use mocassin_types::{JobPackage, SimulationKind};

use super::error::ContextError;

/// The six per-particle counters of one simulation.
///
/// The field order is the contract (Step 18 writes the document in this order): skipped attempts, successful
/// attempts, rejected attempts, blocked attempts, start-unstable advances, and end-unstable advances.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq)]
pub struct ParticleCounters {
    /// Attempts skipped because the selected particle could not move at all.
    pub skip: u64,
    /// Attempts accepted with both a state and a time advance.
    pub success: u64,
    /// Attempts rejected by the Metropolis/KMC acceptance rule.
    pub rejected: u64,
    /// Attempts blocked by the exact occupation check.
    pub blocked: u64,
    /// Accepted attempts whose start node was unstable (state advances, time does not).
    pub start_unstable: u64,
    /// Accepted attempts whose end node was unstable (time advances, state does not).
    pub end_unstable: u64,
}

/// The counters of one context, zeroed at construction.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct Counters {
    /// One entry per compiled particle, including void.
    pub per_particle: Vec<ParticleCounters>,
    /// Total attempts across every particle.
    pub cycles: u64,
    /// Accepted state advances across every particle.
    pub mcs: u64,
}

impl Counters {
    /// Allocates the zeroed counter block for `particle_count` compiled particles.
    #[must_use]
    pub fn zeroed(particle_count: u32) -> Self {
        Self {
            per_particle: vec![ParticleCounters::default(); particle_count as usize],
            cycles: 0,
            mcs: 0,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::{Counters, ParticleCounters};

    #[test]
    fn the_counter_block_starts_zeroed() {
        let counters = Counters::zeroed(5);
        assert_eq!(counters.per_particle.len(), 5);
        assert_eq!(counters.cycles, 0);
        assert_eq!(counters.mcs, 0);
        assert!(
            counters
                .per_particle
                .iter()
                .all(|entry| *entry == ParticleCounters::default())
        );
    }

    #[test]
    fn the_per_particle_counter_is_six_u64() {
        assert_eq!(
            core::mem::size_of::<ParticleCounters>(),
            6 * core::mem::size_of::<u64>()
        );
    }
}

/// The KMC goal arithmetic and block size.
///
/// `total` is the pre-run plus main goal rounded **up** to the next multiple of 100; `cycles_per_block` is
/// `total / 100` (always at least 1 for an accepted KMC job). MMC has no goals and stores zeros.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq)]
pub struct Goals {
    /// Pre-run target cycles (`pre_run_mcsp × mobile_count`).
    pub prerun: u64,
    /// Main-run target cycles (`target_mcsp × mobile_count`).
    pub main: u64,
    /// `prerun + main` rounded up to a multiple of 100.
    pub total: u64,
    /// `total / 100`.
    pub cycles_per_block: u64,
}

impl Goals {
    /// Returns the zeroed goals of an MMC context.
    #[must_use]
    pub const fn zeroed() -> Self {
        Self {
            prerun: 0,
            main: 0,
            total: 0,
            cycles_per_block: 0,
        }
    }

    /// Computes the KMC goals; an MMC job returns zeros.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::NoMobileParticles`] for a KMC job with no mobile environment,
    /// [`ContextError::InvalidInput`] (`"total goal"`) when both MCSP targets are zero, and
    /// [`ContextError::InvalidInput`] (`"goal overflow"`) when a product, sum, or the round-up overflows `u64`.
    pub fn for_job(job: &JobPackage, mobile_count: u32) -> Result<Self, ContextError> {
        if job.kind == SimulationKind::Mmc {
            return Ok(Self::zeroed());
        }
        if mobile_count == 0 {
            return Err(ContextError::NoMobileParticles);
        }

        let mobile = u64::from(mobile_count);
        let prerun = job
            .settings
            .pre_run_mcsp
            .checked_mul(mobile)
            .ok_or_else(goal_overflow)?;
        let main = job
            .settings
            .target_mcsp
            .checked_mul(mobile)
            .ok_or_else(goal_overflow)?;
        let sum = prerun.checked_add(main).ok_or_else(goal_overflow)?;
        if sum == 0 {
            return Err(ContextError::InvalidInput {
                what: "total goal",
                detail: "both pre-run and main MCSP targets are zero".to_owned(),
            });
        }
        let total = sum
            .checked_add(99)
            .and_then(|value| value.checked_div(100))
            .and_then(|value| value.checked_mul(100))
            .ok_or_else(goal_overflow)?;

        Ok(Self {
            prerun,
            main,
            total,
            cycles_per_block: total / 100,
        })
    }
}

fn goal_overflow() -> ContextError {
    ContextError::InvalidInput {
        what: "goal overflow",
        detail: "the goal product, sum, or round-up exceeds u64".to_owned(),
    }
}

/// The normalization state a resumed `.mcs` restores.
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct NormalizationState {
    /// The fixed normalization factor.
    pub k_fix: f64,
    /// The automatic normalization factor (`1 / RawMax` while it is positive).
    pub k_auto: f64,
    /// The observed raw maximum jump probability; zero fresh.
    pub raw_max: f64,
    /// The effective normalization factor (`K_fix × K_auto`).
    pub k_norm: f64,
}

impl NormalizationState {
    /// Returns the unit state of an MMC context.
    #[must_use]
    pub const fn unit() -> Self {
        Self {
            k_fix: 1.0,
            k_auto: 1.0,
            raw_max: 0.0,
            k_norm: 1.0,
        }
    }
}

/// The normalization and time-step state of a context.
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct Normalization {
    /// The fixed normalization factor.
    pub k_fix: f64,
    /// The automatic normalization factor.
    pub k_auto: f64,
    /// The observed raw maximum; zero fresh, preserved on resume.
    pub raw_max: f64,
    /// The effective normalization factor.
    pub k_norm: f64,
    /// The attempt-frequency denominator.
    pub nu_max: f64,
    /// The degree-of-freedom factor (1 or 2).
    pub dof: u32,
    /// The KMC time step per jump attempt in seconds; 0 for MMC.
    pub dt: f64,
}

impl Normalization {
    /// Computes the fresh KMC normalization from the job, `kT`, and the selectable count; MMC returns units.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::NonFiniteFactor`] (exit 13) for a non-finite/non-positive `K_fix`, `K_norm`,
    /// `nu_max`, or `dt`.
    pub fn fresh(
        job: &JobPackage,
        kt_ev: f64,
        selectable_count: u64,
    ) -> Result<Self, ContextError> {
        if job.kind == SimulationKind::Mmc {
            return Ok(Self::mmc());
        }

        let k_fix = match job.settings.normalization_energy_ev {
            None => 1.0,
            Some(energy) => {
                let value = (energy / kt_ev).exp();
                if !value.is_finite() || value <= 0.0 {
                    return Err(ContextError::NonFiniteFactor { what: "K_fix" });
                }
                value
            }
        };
        let raw_max = 0.0;
        let k_auto = 1.0;
        let k_norm = k_fix * k_auto;
        let k_norm = if k_norm.is_finite() && k_norm > 0.0 {
            k_norm
        } else {
            k_fix
        };
        if !k_norm.is_finite() || k_norm <= 0.0 {
            return Err(ContextError::NonFiniteFactor { what: "K_norm" });
        }

        Self::build(
            job,
            selectable_count,
            NormalizationState {
                k_fix,
                k_auto,
                raw_max,
                k_norm,
            },
        )
    }

    /// Builds the resumed KMC normalization from the stored state; MMC returns units.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::NonFiniteFactor`] (exit 13) for an invalid stored factor or a non-finite
    /// `nu_max`/`dt`.
    pub fn resumed(
        job: &JobPackage,
        selectable_count: u64,
        state: &NormalizationState,
    ) -> Result<Self, ContextError> {
        if job.kind == SimulationKind::Mmc {
            return Ok(Self::mmc());
        }
        for (what, value) in [
            ("K_fix", state.k_fix),
            ("K_auto", state.k_auto),
            ("K_norm", state.k_norm),
        ] {
            if !value.is_finite() || value <= 0.0 {
                return Err(ContextError::NonFiniteFactor { what });
            }
        }
        if !state.raw_max.is_finite() || state.raw_max < 0.0 {
            return Err(ContextError::NonFiniteFactor { what: "RawMax" });
        }
        Self::build(job, selectable_count, *state)
    }

    fn mmc() -> Self {
        Self {
            k_fix: 1.0,
            k_auto: 1.0,
            raw_max: 0.0,
            k_norm: 1.0,
            nu_max: 1.0,
            dof: 1,
            dt: 0.0,
        }
    }

    fn build(
        job: &JobPackage,
        selectable_count: u64,
        state: NormalizationState,
    ) -> Result<Self, ContextError> {
        let nu_max = active_nu_max(job)?;
        let dof = if job
            .settings
            .flags
            .contains(mocassin_types::SolverJobFlags::USE_DUAL_DOF_CORRECTION)
        {
            2
        } else {
            1
        };

        if selectable_count == 0 {
            return Err(ContextError::NoSelectableJumps);
        }
        let dt = f64::from(dof) * state.k_norm / (nu_max * selectable_count as f64);
        if !dt.is_finite() || dt <= 0.0 {
            return Err(ContextError::NonFiniteFactor { what: "dt" });
        }

        Ok(Self {
            k_fix: state.k_fix,
            k_auto: state.k_auto,
            raw_max: state.raw_max,
            k_norm: state.k_norm,
            nu_max,
            dof,
            dt,
        })
    }
}

fn active_nu_max(job: &JobPackage) -> Result<f64, ContextError> {
    let value = match job.settings.max_attempt_frequency_hz {
        Some(value) => value,
        None => {
            let simulation = job
                .model
                .simulations
                .get(job.settings.simulation_index as usize)
                .ok_or(ContextError::InvalidInput {
                    what: "simulation index",
                    detail: "the selected simulation does not exist".to_owned(),
                })?;
            let mut maximum = 0.0_f64;
            for collection_id in &simulation.jump_collection_ids {
                let collection = job
                    .model
                    .transitions
                    .collections
                    .get(*collection_id as usize)
                    .ok_or(ContextError::InvalidInput {
                        what: "simulation collection",
                        detail: format!("collection {collection_id} does not exist"),
                    })?;
                for rule_id in &collection.rule_ids {
                    let rule = job
                        .model
                        .transitions
                        .rules
                        .get(rule_id.get() as usize)
                        .ok_or(ContextError::InvalidInput {
                            what: "collection rule",
                            detail: format!("rule {rule_id} does not exist"),
                        })?;
                    maximum = maximum.max(rule.frequency_hz);
                }
            }
            maximum
        }
    };

    if !value.is_finite() || value <= 0.0 {
        return Err(ContextError::NonFiniteFactor { what: "nu_max" });
    }
    Ok(value)
}

// SPDX-License-Identifier: MIT

//! The counter block of a context.
//!
//! Step 14 allocates and zeroes the per-particle counters and the two scalar accumulators once; Steps 16/17
//! increment them and Step 18 writes them to `.mcs`. The goal arithmetic, the block size, the normalization
//! state, and the time step are feature 5 of this step and extend this module; they are not part of the
//! feature-1 skeleton.

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

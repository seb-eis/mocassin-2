// SPDX-License-Identifier: MIT

//! The lattice and environment state arenas.
//!
//! One entry per absolute supercell environment (`A·B·C·P`), stored as separate arrays so the hot per-cycle
//! code touches one array at a time:
//!
//! - `particles`: the current particle, copied verbatim from the validated initial lattice;
//! - `flags`: the derived `STABLE`/`MOBILE`/`HAS_MOBILE_TRACKER` bits;
//! - `mobile_tracker`: the sequential mobile tracker id, or [`NO_MOBILE_TRACKER`].
//!
//! The initial state is derived exactly once here. Later state changes recompute the three fields for the
//! changed environment through the Step 15 helper; nothing rescans the arenas.
//!
//! Rules (Step 14 design §3):
//!
//! - `STABLE` = the compiled site is stable **and** the particle is not void. The original used
//!   `particle != void` only; the compiled stability flag is also honoured (deviation D-7).
//! - `MOBILE` = the particle is not void and the P1 `(position, particle)` has a non-negative jump count.
//!   `jump_count` is `u32` in the validated model, so `-1` and `0` collapse to zero and every non-void
//!   environment is mobile, including a passive species outside every KMC mobile mask (review F-26). The
//!   mobile mask only bounds the tracker id space, matching the original's passive-inclusive behaviour.
//! - Mobile tracker ids are assigned sequentially in ascending absolute-environment order; passive species get
//!   one (the preserved original quirk).

use mocassin_types::{JobPackage, ParticleId};

use super::CancelProbe;
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;

/// The compiled P1 position is stable.
pub const STABLE: u8 = 1;
/// The environment holds a non-void particle and can participate in a transition.
pub const MOBILE: u8 = 2;
/// The environment owns a mobile tracker id.
pub const HAS_MOBILE_TRACKER: u8 = 4;

/// The `mobile_tracker` value of an environment without a tracker id.
pub const NO_MOBILE_TRACKER: u32 = u32::MAX;

/// Particle index zero is void by contract (the reader validates the particle table).
const VOID_PARTICLE: u8 = 0;

/// The initial lattice and environment states of one context.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct LatticeState {
    particles: Vec<ParticleId>,
    flags: Vec<u8>,
    mobile_tracker: Vec<u32>,
    mobile_count: u32,
}

impl LatticeState {
    /// Derives the environment states from the validated initial lattice.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::Cancelled`] when the caller's flag is set, and
    /// [`ContextError::InvalidInput`] if a lattice particle is outside the particle table.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let environments = dimensions.environment_count() as usize;
        let positions = dimensions.p() as usize;
        let particle_count = dimensions.pc() as usize;

        let mut particles = Vec::with_capacity(environments);
        let mut flags = vec![0_u8; environments];
        let mut mobile_tracker = vec![NO_MOBILE_TRACKER; environments];
        let mut mobile_count = 0_u32;

        for env in 0..environments {
            if env % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let particle = job.lattice.particles[env];
            if usize::from(particle.get()) >= particle_count {
                return Err(ContextError::InvalidInput {
                    what: "lattice particle",
                    detail: format!(
                        "environment {env} holds particle {} but the table has {particle_count} entries",
                        particle.get()
                    ),
                });
            }

            let position = env % positions;
            let is_void = particle.get() == VOID_PARTICLE;
            let mut env_flags = 0_u8;

            if !is_void && job.model.structure.positions[position].stable {
                env_flags |= STABLE;
            }

            if !is_void {
                env_flags |= MOBILE;
                mobile_tracker[env] = mobile_count;
                mobile_count = mobile_count.checked_add(1).ok_or(ContextError::Invariant {
                    what: "mobile tracker id overflow",
                })?;
                env_flags |= HAS_MOBILE_TRACKER;
            }

            flags[env] = env_flags;
            particles.push(particle);
        }

        Ok(Self {
            particles,
            flags,
            mobile_tracker,
            mobile_count,
        })
    }

    /// Returns the particle array.
    #[must_use]
    pub fn particles(&self) -> &[ParticleId] {
        &self.particles
    }

    /// Returns the derived flag array.
    #[must_use]
    pub fn flags(&self) -> &[u8] {
        &self.flags
    }

    /// Returns the mobile tracker id array ([`NO_MOBILE_TRACKER`] for absent ids).
    #[must_use]
    pub fn mobile_tracker(&self) -> &[u32] {
        &self.mobile_tracker
    }

    /// Returns the number of environments with a mobile tracker id.
    #[must_use]
    pub const fn mobile_count(&self) -> u32 {
        self.mobile_count
    }

    /// Returns the particle of one environment.
    #[must_use]
    pub fn particle(&self, env: EnvId) -> ParticleId {
        self.particles[env.index()]
    }

    /// Sets the particle of one environment without refreshing the derived flags or tracker id.
    ///
    /// State application belongs to Step 15; it updates the flags and tracker fields after the particle change.
    pub(crate) fn set_particle(&mut self, env: EnvId, particle: ParticleId) {
        self.particles[env.index()] = particle;
    }

    /// Returns the raw flags of one environment.
    #[must_use]
    pub fn flags_at(&self, env: EnvId) -> u8 {
        self.flags[env.index()]
    }

    /// Returns `true` when the environment is stable.
    #[must_use]
    pub fn is_stable(&self, env: EnvId) -> bool {
        self.flags_at(env) & STABLE != 0
    }

    /// Returns `true` when the environment is mobile.
    #[must_use]
    pub fn is_mobile(&self, env: EnvId) -> bool {
        self.flags_at(env) & MOBILE != 0
    }

    /// Returns the mobile tracker id of one environment, or `None` when it has none.
    #[must_use]
    pub fn mobile_tracker_id(&self, env: EnvId) -> Option<u32> {
        let id = self.mobile_tracker[env.index()];
        (id != NO_MOBILE_TRACKER).then_some(id)
    }

    /// Returns `true` when the lattice and environment arrays have the expected lengths.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, dimensions: &Dimensions) -> bool {
        let expected = dimensions.environment_count() as usize;
        self.particles.len() == expected
            && self.flags.len() == expected
            && self.mobile_tracker.len() == expected
    }

    /// Returns the summed allocator capacity of the three arenas.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        let particles =
            self.particles.capacity() as u64 * core::mem::size_of::<ParticleId>() as u64;
        let flags = self.flags.capacity() as u64;
        let tracker = self.mobile_tracker.capacity() as u64 * core::mem::size_of::<u32>() as u64;
        particles + flags + tracker
    }

    /// Checks the tracker id space (every id is assigned exactly once in ascending order).
    #[must_use]
    pub(crate) fn has_valid_trackers(&self) -> bool {
        let mut next = 0_u32;
        for env in 0..self.mobile_tracker.len() {
            let has_tracker = self.flags[env] & HAS_MOBILE_TRACKER != 0;
            let id = self.mobile_tracker[env];

            if has_tracker {
                if id != next {
                    return false;
                }
                let Some(advanced) = next.checked_add(1) else {
                    return false;
                };
                next = advanced;
            } else if id != NO_MOBILE_TRACKER {
                return false;
            }
        }

        next == self.mobile_count
    }
}

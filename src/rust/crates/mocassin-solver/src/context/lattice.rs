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

use mocassin_types::{JobPackage, ParticleId, SimulationKind};

use super::CancelProbe;
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;
use super::limits::{ContextLimits, MemoryEntry, MemoryPlan};

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
        particles: &[ParticleId],
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let environments = dimensions.environment_count() as usize;
        let positions = dimensions.p() as usize;
        let particle_count = dimensions.pc() as usize;

        let mut particles_out = Vec::with_capacity(environments);
        let mut flags = vec![0_u8; environments];
        let mut mobile_tracker = vec![NO_MOBILE_TRACKER; environments];
        let mut mobile_count = 0_u32;

        for env in 0..environments {
            if env % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let particle = particles[env];
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
            particles_out.push(particle);
        }

        Ok(Self {
            particles: particles_out,
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

/// The tracker index space of a KMC context.
///
/// Values belong to Step 18; this type only allocates and exposes the index space: the inverse mobile mapping
/// (`mobile_positions[id] = absolute environment`), the static tracker count, and the global tracker count.
/// MMC has no tracker index space (all zeros/empty).
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct TrackerIndex {
    /// The absolute environment of each mobile tracker id, ascending.
    pub mobile_positions: Vec<u32>,
    /// The number of static trackers per supercell cell.
    pub static_trackers_per_cell: u32,
    /// `A·B·C · static_trackers_per_cell`.
    pub static_tracker_count: u32,
    /// The number of compiled global trackers.
    pub global_tracker_count: u32,
}

impl TrackerIndex {
    /// Builds the index space and records its planned length before allocating the mapping.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::LimitExceeded`] when the static tracker count overflows `u32`,
    /// [`ContextError::Invariant`] for an inconsistent mobile id space, and cancellation.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        lattice: &LatticeState,
        plan: &mut MemoryPlan,
        limits: &ContextLimits,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        if job.kind == SimulationKind::Mmc {
            plan.set_entries(
                [MemoryEntry {
                    what: "trackers.mobile_positions",
                    elements: 0,
                    bytes: 0,
                }],
                limits,
            )?;
            return Ok(Self::default());
        }

        let mobile_count = lattice.mobile_count();
        plan.set_entries(
            [MemoryEntry {
                what: "trackers.mobile_positions",
                elements: u64::from(mobile_count),
                bytes: u64::from(mobile_count) * 4,
            }],
            limits,
        )?;

        let mut mobile_positions = Vec::with_capacity(mobile_count as usize);
        let mut next = 0_u32;
        for environment in 0..dimensions.environment_count() as usize {
            if environment % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }
            let env = EnvId::new(environment as u32);
            let id = lattice.mobile_tracker_id(env);
            match id {
                Some(id) => {
                    if id != next {
                        return Err(ContextError::Invariant {
                            what: "mobile tracker id order",
                        });
                    }
                    mobile_positions.push(environment as u32);
                    next += 1;
                }
                None => {
                    if lattice.flags_at(env) & super::lattice::HAS_MOBILE_TRACKER != 0 {
                        return Err(ContextError::Invariant {
                            what: "mobile tracker flag",
                        });
                    }
                }
            }
        }
        if next != mobile_count {
            return Err(ContextError::Invariant {
                what: "mobile tracker count",
            });
        }

        let static_trackers_per_cell = job.model.structure.static_trackers_per_cell;
        let static_tracker_count = u64::from(dimensions.cell_count())
            .checked_mul(u64::from(static_trackers_per_cell))
            .ok_or(ContextError::LimitExceeded {
                what: "static trackers",
                value: u64::MAX,
                limit: u64::from(u32::MAX),
            })?;
        let static_tracker_count =
            u32::try_from(static_tracker_count).map_err(|_| ContextError::LimitExceeded {
                what: "static trackers",
                value: static_tracker_count,
                limit: u64::from(u32::MAX),
            })?;
        let global_tracker_count = job.model.structure.global_tracker_count;
        if global_tracker_count != job.model.counts.global_tracker_count {
            return Err(ContextError::InvalidInput {
                what: "global tracker count",
                detail: "the structure and model counts disagree".to_owned(),
            });
        }

        Ok(Self {
            mobile_positions,
            static_trackers_per_cell,
            static_tracker_count,
            global_tracker_count,
        })
    }

    /// Returns the number of mobile trackers.
    #[must_use]
    pub fn mobile_count(&self) -> u32 {
        self.mobile_positions.len() as u32
    }

    /// Returns the static tracker id of a non-void particle, or `None` for void/out-of-range.
    #[must_use]
    pub fn static_tracker_id(
        &self,
        dimensions: &Dimensions,
        env: EnvId,
        particle: ParticleId,
    ) -> Option<u32> {
        if particle.get() == 0 || usize::from(particle.get()) >= dimensions.pc() as usize {
            return None;
        }
        let cell = dimensions.cell_of(env);
        let position = dimensions.position_of(env);
        let id = u64::from(cell) * u64::from(self.static_trackers_per_cell)
            + u64::from(position) * u64::from(dimensions.pc() - 1)
            + u64::from(particle.get() - 1);
        u32::try_from(id)
            .ok()
            .filter(|id| *id < self.static_tracker_count)
    }

    /// Returns the summed allocator capacity of the index array.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        self.mobile_positions.capacity() as u64 * 4
    }
}

// SPDX-License-Identifier: MIT

//! The KMC selection pools.
//!
//! A pool member is an **absolute supercell environment** plus a particle; the KMC loop selects a supercell
//! environment, while the direction count and the selection mask are P1-level tables. The pool id is P1-level
//! (`pool_of`), the member slot is supercell-addressed (`slot`), and each pool reserves the maximum number of
//! `(environment, particle)` combinations that could ever join it, so [`SelectionPools::set_selectable`] never
//! reallocates.
//!
//! **Active directions.** The `.msl` `jump_count`/`jump_mapping` tables are a union over every compiled
//! collection (the populated fixture mixes a KMC and an MMC collection), while a job runs one simulation. The
//! pool direction count, the selection relative index, and the jump-status build therefore use only the
//! directions of the job simulation's active collections; a KMC job never offers an MMC direction. This is a
//! recorded clarification of Step 14 §8 (the payload table is not per job kind).

use std::collections::BTreeMap;

use mocassin_types::{JobPackage, ParticleId, PositionId};

use super::CancelProbe;
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;
use super::lattice::LatticeState;
use super::limits::ContextLimits;

/// The `slot`/`pool_of` value of an absent combination.
pub const NO_SLOT: u32 = u32::MAX;

/// The KMC selection pools of one context.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct SelectionPools {
    /// One pool per distinct selectable direction count, ascending.
    pub pools: Vec<SelectionPool>,
    /// `E·pc` global slot map; `slot[env·pc + particle]` is the member slot, or [`NO_SLOT`].
    pub slot: Vec<u32>,
    /// `P·pc` P1 table; `pool_of[P·pc + particle]` is the pool id, or [`NO_SLOT`].
    pub pool_of: Vec<u32>,
    /// `Σ pool.members.len() × pool.direction_count`.
    pub selectable_jump_count: u64,
}

impl SelectionPools {
    /// Returns the empty pools of a context whose selection space is not built yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no pool member or slot map is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.pools.is_empty()
            && self.slot.is_empty()
            && self.pool_of.is_empty()
            && self.selectable_jump_count == 0
    }

    /// Builds the pools for a KMC job.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::NoSelectableJumps`] when no environment is selectable,
    /// [`ContextError::LimitExceeded`] for a pool count or selectable-count cap, [`ContextError::InvalidInput`]
    /// for an inconsistent simulation/collection reference, and cancellation.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        lattice: &LatticeState,
        apply_optimizers: bool,
        limits: &ContextLimits,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let shape = pool_shape(job, dimensions)?;
        if shape.counts.len() > limits.max_pools as usize {
            return Err(ContextError::LimitExceeded {
                what: "max_pools",
                value: shape.counts.len() as u64,
                limit: u64::from(limits.max_pools),
            });
        }
        let particles = dimensions.pc() as usize;
        let environments = dimensions.environment_count() as usize;

        let counts: Vec<u32> = shape.counts.keys().copied().collect();
        let mut slot = vec![NO_SLOT; environments * particles];
        let pool_of = shape.pool_of.clone();

        let mut pools: Vec<SelectionPool> = counts
            .iter()
            .map(|count| SelectionPool {
                direction_count: *count,
                members: Vec::with_capacity(*shape.counts.get(count).unwrap_or(&0) as usize),
            })
            .collect();

        let mut selectable_jump_count = 0_u64;
        for environment in 0..environments {
            if environment % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let env = EnvId::new(environment as u32);
            let position = dimensions.position_of(env) as usize;
            let particle = lattice.particle(env).get() as usize;
            let key = position * particles + particle;
            let pool_id = pool_of[key];
            if pool_id == NO_SLOT {
                continue;
            }

            let pool = &mut pools[pool_id as usize];
            let index = pool.members.len();
            debug_assert!(index < pool.members.capacity());
            pool.members.push(PoolMember {
                env,
                particle: lattice.particle(env),
            });
            slot[environment * particles + particle] = index as u32;
            selectable_jump_count += u64::from(pool.direction_count);
        }

        let mut this = Self {
            pools,
            slot,
            pool_of,
            selectable_jump_count,
        };

        if apply_optimizers {
            let mut optimizers = job.optimizers.clone();
            optimizers.sort_unstable_by_key(|optimizer| {
                (optimizer.position_id.get(), optimizer.particle_id.get())
            });
            for optimizer in optimizers {
                for environment in 0..environments {
                    let env = EnvId::new(environment as u32);
                    if PositionId::new(dimensions.position_of(env)) == optimizer.position_id
                        && lattice.particle(env) == optimizer.particle_id
                    {
                        this.remove_member(dimensions, env, optimizer.particle_id);
                    }
                }
            }
        }

        if this.selectable_jump_count == 0 {
            return Err(ContextError::NoSelectableJumps);
        }
        if this.selectable_jump_count > limits.max_selectable_jump_count {
            return Err(ContextError::LimitExceeded {
                what: "max_selectable_jump_count",
                value: this.selectable_jump_count,
                limit: limits.max_selectable_jump_count,
            });
        }

        Ok(this)
    }

    /// Returns the number of selectable environments (members).
    #[must_use]
    pub fn member_count(&self) -> usize {
        self.pools.iter().map(|pool| pool.members.len()).sum()
    }

    /// Sets the selectability of one `(environment, particle)` after a state change.
    ///
    /// `current` is the lattice particle now at `env`: the combination must be present when it is the current
    /// particle and compiled-selectable, and absent otherwise. The caller updates the lattice, then calls this
    /// for the departed and the arrived particle. A combination re-enters the pool after an optimizer removal
    /// (the optimizer is a start condition). The pool was reserved to its maximum combination count, so this
    /// never reallocates.
    pub(crate) fn set_selectable(
        &mut self,
        dimensions: &Dimensions,
        env: EnvId,
        particle: ParticleId,
        current: ParticleId,
    ) {
        let position = dimensions.position_of(env) as usize;
        let particles = dimensions.pc() as usize;
        let key = position * particles + particle.get() as usize;
        let pool_id = self.pool_of[key];
        let slot_key = env.index() * particles + particle.get() as usize;
        let present = self.slot[slot_key] != NO_SLOT;
        let wanted = current == particle && pool_id != NO_SLOT;

        match (wanted, present) {
            (true, false) => {
                let pool = &mut self.pools[pool_id as usize];
                let index = pool.members.len();
                debug_assert!(
                    index < pool.members.capacity(),
                    "pool capacity must cover every selectable combination"
                );
                pool.members.push(PoolMember { env, particle });
                self.slot[slot_key] = index as u32;
                self.selectable_jump_count += u64::from(pool.direction_count);
            }
            (false, true) => self.remove_member(dimensions, env, particle),
            _ => {}
        }
    }

    fn remove_member(&mut self, dimensions: &Dimensions, env: EnvId, particle: ParticleId) {
        let particles = dimensions.pc() as usize;
        let position = dimensions.position_of(env) as usize;
        let pool_id = self.pool_of[position * particles + particle.get() as usize];
        if pool_id == NO_SLOT {
            return;
        }
        let slot_key = env.index() * particles + particle.get() as usize;
        let index = self.slot[slot_key];
        if index == NO_SLOT {
            return;
        }

        let pool = &mut self.pools[pool_id as usize];
        let last = pool.members.len() - 1;
        pool.members.swap_remove(index as usize);
        self.slot[slot_key] = NO_SLOT;
        self.selectable_jump_count -= u64::from(pool.direction_count);

        if (index as usize) != last {
            let moved = pool.members[index as usize];
            let moved_key = moved.env.index() * particles + moved.particle.get() as usize;
            self.slot[moved_key] = index;
        }
    }

    /// Returns the summed allocator capacity of the pool arrays.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        let headers = self.pools.capacity() as u64 * core::mem::size_of::<SelectionPool>() as u64;
        let members: u64 = self
            .pools
            .iter()
            .map(|pool| pool.members.capacity() as u64 * core::mem::size_of::<PoolMember>() as u64)
            .sum();
        let slot = self.slot.capacity() as u64 * 4;
        let pool_of = self.pool_of.capacity() as u64 * 4;
        headers + members + slot + pool_of
    }

    /// Returns `true` when the pools and slot maps have a consistent shape.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, dimensions: &Dimensions) -> bool {
        if self.pools.is_empty() {
            return self.slot.is_empty()
                && self.pool_of.is_empty()
                && self.selectable_jump_count == 0;
        }

        let environments = dimensions.environment_count() as usize;
        let particles = dimensions.pc() as usize;
        let positions = dimensions.p() as usize;

        self.slot.len() == environments * particles
            && self.pool_of.len() == positions * particles
            && self.pools.windows(2).all(|pair| {
                pair[0].direction_count != 0 && pair[0].direction_count < pair[1].direction_count
            })
            && self
                .pools
                .last()
                .is_none_or(|pool| pool.direction_count != 0)
            && self.pools.iter().all(|pool| {
                pool.members.iter().all(|member| {
                    (member.env.get() as usize) < environments
                        && usize::from(member.particle.get()) < particles
                })
            })
            && self
                .pool_of
                .iter()
                .all(|pool| (*pool as usize) < self.pools.len() || *pool == NO_SLOT)
    }

    /// Returns `true` when every member matches the current lattice and slot/pool maps.
    #[must_use]
    pub(crate) fn is_consistent(&self, dimensions: &Dimensions, lattice: &LatticeState) -> bool {
        let particles = dimensions.pc() as usize;
        let mut total = 0_u64;

        for (pool_id, pool) in self.pools.iter().enumerate() {
            total += pool.members.len() as u64 * u64::from(pool.direction_count);
            for (index, member) in pool.members.iter().enumerate() {
                let position = dimensions.position_of(member.env) as usize;
                let key = position * particles + member.particle.get() as usize;
                if lattice.particle(member.env) != member.particle
                    || self.slot[member.env.index() * particles + member.particle.get() as usize]
                        != index as u32
                    || self.pool_of[key] as usize != pool_id
                {
                    return false;
                }
            }
        }

        total == self.selectable_jump_count
    }

    /// Returns the pools in ascending direction-count order.
    #[must_use]
    pub fn as_slice(&self) -> &[SelectionPool] {
        &self.pools
    }
}

/// One direction-count bucket.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct SelectionPool {
    /// The number of selectable jump directions shared by every member.
    pub direction_count: u32,
    /// The selectable `(environment, particle)` members in deterministic order.
    pub members: Vec<PoolMember>,
}

/// One selectable environment/particle combination.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct PoolMember {
    /// The absolute supercell environment.
    pub env: EnvId,
    /// The selectable particle.
    pub particle: ParticleId,
}

/// The pool capacities and P1 map derived from the compiled selection space.
pub(crate) struct PoolShape {
    /// Capacity per distinct selectable direction count.
    pub counts: BTreeMap<u32, u64>,
    /// Pool id per P1 `(position, particle)` key, or [`NO_SLOT`].
    pub pool_of: Vec<u32>,
}

/// Returns the active-direction mask of the job simulation.
///
/// The `.msl` direction tables are a union over every collection; only the directions whose collection belongs
/// to the job simulation's active collection set are selectable. A direction of the other simulation kind is
/// never offered.
pub(crate) fn active_direction_mask(job: &JobPackage) -> Result<Vec<bool>, ContextError> {
    let simulation = job
        .model
        .simulations
        .get(job.settings.simulation_index as usize)
        .ok_or(ContextError::InvalidInput {
            what: "simulation index",
            detail: format!(
                "job declares simulation {} but the model has {}",
                job.settings.simulation_index,
                job.model.simulations.len()
            ),
        })?;
    if simulation.kind != job.kind {
        return Err(ContextError::InvalidInput {
            what: "simulation kind",
            detail: "the selected simulation kind does not match the job".to_owned(),
        });
    }

    let mut active_collections = vec![false; job.model.transitions.collections.len()];
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
        if collection.kind != job.kind {
            return Err(ContextError::InvalidInput {
                what: "simulation collection kind",
                detail: format!("collection {collection_id} is not the job kind"),
            });
        }
        active_collections[*collection_id as usize] = true;
    }

    let mut active = vec![false; job.model.transitions.directions.len()];
    for (index, direction) in job.model.transitions.directions.iter().enumerate() {
        active[index] = active_collections
            .get(direction.collection_id.get() as usize)
            .copied()
            .unwrap_or(false);
    }
    Ok(active)
}

/// Derives the pool capacities and the P1 pool-id table.
pub(crate) fn pool_shape(
    job: &JobPackage,
    dimensions: &Dimensions,
) -> Result<PoolShape, ContextError> {
    let active = active_direction_mask(job)?;
    let positions = dimensions.p() as usize;
    let particles = dimensions.pc() as usize;
    let cells = u64::from(dimensions.cell_count());
    let transitions = &job.model.transitions;

    if transitions.jump_count.len() < positions * particles
        || transitions.jump_offsets.len() != transitions.jump_count.len() + 1
    {
        return Err(ContextError::InvalidInput {
            what: "jump count table",
            detail: "the jump count/offset lengths are inconsistent".to_owned(),
        });
    }

    let mut counts: BTreeMap<u32, u64> = BTreeMap::new();
    let mut filtered = vec![0_u32; positions * particles];
    for position in 0..positions {
        for particle in 0..particles {
            let key = position * particles + particle;
            let count = transitions.jump_count[key];
            let offset = transitions.jump_offsets[key] as usize;
            let end = offset + count as usize;
            let mapping =
                transitions
                    .jump_mapping
                    .get(offset..end)
                    .ok_or(ContextError::InvalidInput {
                        what: "jump mapping",
                        detail: "the mapping range is outside the table".to_owned(),
                    })?;
            let active_count = mapping
                .iter()
                .filter(|direction| active.get(**direction as usize).copied().unwrap_or(false))
                .count() as u32;
            filtered[key] = active_count;

            if active_count == 0 {
                continue;
            }
            let mask = job.model.structure.environments[position].selection_particle_mask;
            if mask & (1_u64 << particle) == 0 {
                continue;
            }
            *counts.entry(active_count).or_insert(0) += cells;
        }
    }

    let direction_counts: Vec<u32> = counts.keys().copied().collect();
    let mut pool_of = vec![NO_SLOT; positions * particles];
    for position in 0..positions {
        for particle in 0..particles {
            let key = position * particles + particle;
            let count = filtered[key];
            if count == 0 {
                continue;
            }
            let mask = job.model.structure.environments[position].selection_particle_mask;
            if mask & (1_u64 << particle) == 0 {
                continue;
            }
            let pool =
                direction_counts
                    .binary_search(&count)
                    .map_err(|_| ContextError::Invariant {
                        what: "pool count table",
                    })?;
            let pool =
                u32::try_from(pool).map_err(|_| ContextError::Invariant { what: "pool id" })?;
            pool_of[key] = pool;
        }
    }

    Ok(PoolShape { counts, pool_of })
}

#[cfg(test)]
mod tests {
    use super::{NO_SLOT, PoolMember, SelectionPools};

    #[test]
    fn a_pool_member_is_eight_bytes() {
        assert_eq!(core::mem::size_of::<PoolMember>(), 8);
    }

    #[test]
    fn empty_pools_are_valid() {
        let pools = SelectionPools::empty();
        assert!(pools.is_empty());
        assert_eq!(NO_SLOT, u32::MAX);
    }
}

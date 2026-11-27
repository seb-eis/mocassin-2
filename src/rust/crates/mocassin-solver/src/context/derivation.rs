// SPDX-License-Identifier: MIT

//! Per-environment derived-state refresh for committed particle changes.
//!
//! Step 14 froze the derived flags and assigned every non-void environment a sequential mobile tracker id.
//! State application keeps the flags in step with the particle (never a full-array scan) and permutes the ids so
//! each identity follows the particle that moved. Tracker values belong to Step 18; this module only maintains
//! the context's id space and its inverse mapping.

use mocassin_types::JobPackage;

use super::dimensions::{Dimensions, EnvId};
use super::jumps::MAX_PATH_NODES;
use super::lattice::{HAS_MOBILE_TRACKER, LatticeState, MOBILE, NO_MOBILE_TRACKER, STABLE};

/// The fixed path capacity as a `usize` array length.
const MAX_NODES: usize = MAX_PATH_NODES as usize;

/// Refreshes one changed environment's flags from the committed particle.
///
/// The flags follow the Step 14 rules exactly: `STABLE` needs a stable compiled site and a non-void particle,
/// `MOBILE` and `HAS_MOBILE_TRACKER` need a non-void particle. The tracker id is owned by the permutation pass
/// and is not rewritten here, so deriving the flags before the permutation cannot preserve a stale id.
pub(crate) fn derive_environment(
    job: &JobPackage,
    dimensions: &Dimensions,
    lattice: &mut LatticeState,
    env: EnvId,
) {
    let particle = lattice.particle(env);
    let position = dimensions.position_of(env);
    let is_void = particle.get() == 0;
    let stable = !is_void && job.model.structure.positions[position as usize].stable;

    let mut flags = 0_u8;
    if stable {
        flags |= STABLE;
    }
    if !is_void {
        flags |= MOBILE | HAS_MOBILE_TRACKER;
    }
    lattice.set_flags(env, flags);
}

/// Permutes the mobile tracker ids so each identity follows its particle through the committed path.
///
/// A node whose particle does not change keeps its identity: an ion that stays at its site has not moved. The
/// changed nodes form a closed source/destination set. Within it, each destination first takes the id of a
/// source that held the same particle value (a vacancy hop, a ring migration, or a two-particle swap); any
/// remaining pair — possible only for a path whose state codes do not conserve the particle multiset — is
/// paired in ascending node order. The rule's `tracker_order` is the Step 18 global-tracker order and is not
/// used. A void destination always has no id.
pub(crate) fn permute_mobile_trackers(
    lattice: &mut LatticeState,
    path: &[EnvId],
    old: &[u8],
    final_particles: &[u8],
) {
    let length = path
        .len()
        .min(old.len())
        .min(final_particles.len())
        .min(MAX_NODES);

    let mut old_ids = [NO_MOBILE_TRACKER; MAX_NODES];
    let mut assigned = [NO_MOBILE_TRACKER; MAX_NODES];
    let mut changed = [false; MAX_NODES];
    let mut source_used = [false; MAX_NODES];
    let mut destination_filled = [false; MAX_NODES];

    for node in 0..length {
        old_ids[node] = lattice
            .mobile_tracker_id(path[node])
            .unwrap_or(NO_MOBILE_TRACKER);
        assigned[node] = old_ids[node];
        changed[node] = old[node] != final_particles[node];
    }

    for destination in 0..length {
        if !changed[destination] {
            continue;
        }
        let source = (0..length).find(|source| {
            changed[*source]
                && !source_used[*source]
                && old[*source] == final_particles[destination]
        });
        if let Some(source) = source {
            source_used[source] = true;
            destination_filled[destination] = true;
            assigned[destination] = old_ids[source];
        } else {
            assigned[destination] = NO_MOBILE_TRACKER;
        }
    }

    let mut next_source = 0_usize;
    for destination in 0..length {
        if !changed[destination] || destination_filled[destination] {
            continue;
        }
        while next_source < length && (!changed[next_source] || source_used[next_source]) {
            next_source += 1;
        }
        if next_source < length {
            source_used[next_source] = true;
            assigned[destination] = old_ids[next_source];
            next_source += 1;
        }
    }

    for node in 0..length {
        let id = if final_particles[node] == 0 {
            NO_MOBILE_TRACKER
        } else {
            assigned[node]
        };
        lattice.set_mobile_tracker(path[node], id);
    }
}

/// Rebuilds the identity inverse after tracker ids have followed particle movement.
pub(crate) fn rebuild_mobile_positions(
    positions: &mut [u32],
    lattice: &LatticeState,
    path: &[EnvId],
) {
    for &environment in path {
        if let Some(id) = lattice.mobile_tracker_id(environment) {
            positions[id as usize] = environment.get();
        }
    }
}

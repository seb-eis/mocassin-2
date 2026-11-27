// SPDX-License-Identifier: MIT

//! Allocation-free KMC state application and exact incremental lattice-energy accounting.
//!
//! The commit order is fixed by the Step 15 design (§6): resolve the affected final clusters before the first
//! mutation, write particles and flags, permute the mobile tracker ids, update pool membership, distribute the
//! owner-major pair deltas, rebuild each affected cluster once, and accumulate the exact `ΔU`. Counters,
//! histograms, tracker values, and time are never touched here.

use mocassin_types::{JumpDirectionId, JumpRule, ParticleId, PositionId, SimulationKind};

use super::path::{MAX_PATH_NODES, resolve_rule_path};
use crate::context::derivation::{
    derive_environment, permute_mobile_trackers, rebuild_mobile_positions,
};
use crate::context::{
    ContextError, EnvId, NO_CLUSTER_CODE, SimulationContext, raw_has_void_member,
};

/// The six-bit occupation field of one cluster slot.
const PARTICLE_MASK: u64 = 0x3F;
/// The validated particle bound (`Dimensions` rejects `pc > 64`).
const MAX_PARTICLES: usize = 64;

/// Resolves one raw tuple, or returns the zero row for an unmapped centre particle.
fn resolve_row(ctx: &SimulationContext, instance: u32, raw: u64) -> Result<u32, ContextError> {
    let centre = ParticleId::new((raw & PARTICLE_MASK) as u8);
    if ctx.cluster_row(instance, centre).is_none() {
        return Ok(NO_CLUSTER_CODE);
    }
    ctx.cluster_lookup(instance, raw)
}

/// Resolves the instance's actual final tuple and every centre-particle row before the first mutation.
fn validate_final_rows(
    ctx: &SimulationContext,
    instance: u32,
    raw: u64,
) -> Result<(), ContextError> {
    let data = ctx
        .clusters
        .instance(instance)
        .ok_or(ContextError::Invariant {
            what: "cluster instance index",
        })?;
    if raw_has_void_member(raw, data.slot_count) {
        return Ok(());
    }
    let table = ctx
        .job
        .model
        .energy
        .cluster_tables
        .get(data.table as usize)
        .ok_or(ContextError::Invariant {
            what: "cluster table index",
        })?;
    let validate = |raw: u64| -> Result<(), ContextError> {
        let code_id = resolve_row(ctx, instance, raw)?;
        if code_id == NO_CLUSTER_CODE {
            return Ok(());
        }
        let energy = table
            .energies
            .get(code_id as usize)
            .ok_or(ContextError::Invariant {
                what: "cluster energy index",
            })?;
        if !energy.is_finite() {
            return Err(ContextError::Invariant {
                what: "cluster energy",
            });
        }
        Ok(())
    };
    validate(raw)?;
    for &particle in &table.particle_table_mapping {
        validate((raw & !PARTICLE_MASK) | u64::from(particle))?;
    }
    Ok(())
}

/// Builds the instance's raw tuple from the final state, with path-node `S2` values as slot overrides.
fn final_raw_code(
    ctx: &SimulationContext,
    instance: u32,
    cell: u32,
    path: &[EnvId],
    s2: &[u8],
) -> u64 {
    let data = ctx
        .clusters
        .instance(instance)
        .expect("the affected cluster instance exists");
    let (start, _) = ctx
        .clusters
        .slot_range(instance)
        .expect("the affected cluster slot range exists");
    let mut overrides = [(0_u8, ParticleId::new(0)); MAX_PATH_NODES];
    let mut count = 0_usize;
    for slot in 0..data.slot_count as usize {
        let environment = if slot == 0 {
            ctx.dimensions.cell_env(cell, data.centre_position)
        } else {
            ctx.dimensions.wrapped_env(
                cell,
                ctx.clusters.slot_cells[start + slot],
                ctx.clusters.slot_positions[start + slot],
            )
        };
        if let Some(node) = path.iter().position(|candidate| *candidate == environment) {
            overrides[count] = (slot as u8, ParticleId::new(s2[node]));
            count += 1;
        }
    }
    ctx.occupation_code(instance, cell, &overrides[..count])
}

/// Visits every affected `(instance, cell)` pair in the member-side and centre-side union.
///
/// The callback is invoked once per *candidate*; the caller deduplicates by comparing the stored final code.
/// Enumeration itself allocates nothing and holds no context slice across the callback.
fn for_each_affected(
    ctx: &mut SimulationContext,
    path: &[EnvId],
    changed: &[usize],
    mut visit: impl FnMut(&mut SimulationContext, u32, u32) -> Result<(), ContextError>,
) -> Result<(), ContextError> {
    for &node in changed {
        let environment = path[node];
        let position = PositionId::new(ctx.dimensions.position_of(environment));
        let centre_cell = ctx.dimensions.cell_of(environment);

        let centre_count = ctx.centres(position).len();
        for index in 0..centre_count {
            let instance = ctx.centres(position)[index];
            visit(ctx, instance, centre_cell)?;
        }

        let incoming_count = ctx.incoming(environment).len();
        for index in 0..incoming_count {
            let incoming = ctx.incoming(environment)[index];
            let owner =
                ctx.incoming_owner(environment, incoming)
                    .ok_or(ContextError::Invariant {
                        what: "incoming owner",
                    })?;
            let link = *ctx
                .link_range(PositionId::new(incoming.owner_position))
                .get(incoming.link_id as usize)
                .ok_or(ContextError::Invariant {
                    what: "incoming link index",
                })?;
            let owner_cell = ctx.dimensions.cell_of(owner);
            let start = link.cluster_start as usize;
            let end = start + link.cluster_len as usize;
            for association in start..end {
                let instance = ctx.links.cluster_assoc[association].instance;
                visit(ctx, instance, owner_cell)?;
            }
        }
    }
    Ok(())
}

/// Returns the particle the instance's centre held before the commit.
fn old_centre_particle(
    ctx: &SimulationContext,
    path: &[EnvId],
    old: &[u8],
    instance: u32,
    cell: u32,
) -> ParticleId {
    let centre_position = ctx
        .clusters
        .instance(instance)
        .expect("the affected cluster instance exists")
        .centre_position;
    let centre = ctx.dimensions.cell_env(cell, centre_position);
    match path.iter().position(|candidate| *candidate == centre) {
        Some(node) => ParticleId::new(old[node]),
        None => ctx.lattice.particle(centre),
    }
}

/// Rebuilds one affected instance's code, code id, and every allowed centre-particle energy row.
fn rebuild_instance(
    ctx: &mut SimulationContext,
    instance: u32,
    cell: u32,
    raw: u64,
) -> Result<(), ContextError> {
    let data = *ctx
        .clusters
        .instance(instance)
        .ok_or(ContextError::Invariant {
            what: "cluster instance index",
        })?;
    let cells = ctx.dimensions.cell_count() as usize;
    let index = instance as usize * cells + cell as usize;
    let start = ctx.clusters.energy_start[instance as usize] as usize;
    let total_rows = ctx.clusters.energy_start.last().copied().unwrap_or(0) as usize;

    let mut energies = [0.0_f64; MAX_PARTICLES];
    let (row_count, canonical) = {
        let table = ctx
            .job
            .model
            .energy
            .cluster_tables
            .get(data.table as usize)
            .ok_or(ContextError::Invariant {
                what: "cluster table index",
            })?;
        let row_count = table.particle_table_mapping.len();
        if row_count > MAX_PARTICLES || start + row_count > total_rows {
            return Err(ContextError::Invariant {
                what: "cluster energy row",
            });
        }
        let void_member = raw_has_void_member(raw, data.slot_count);
        for (row, &particle) in table.particle_table_mapping.iter().enumerate() {
            let code_id = if void_member {
                NO_CLUSTER_CODE
            } else {
                resolve_row(ctx, instance, (raw & !PARTICLE_MASK) | u64::from(particle))?
            };
            energies[row] = if code_id == NO_CLUSTER_CODE {
                0.0
            } else {
                let energy =
                    table
                        .energies
                        .get(code_id as usize)
                        .ok_or(ContextError::Invariant {
                            what: "cluster energy index",
                        })?;
                if !energy.is_finite() {
                    return Err(ContextError::Invariant {
                        what: "cluster energy",
                    });
                }
                energy * ctx.ev_to_kt()
            };
        }
        let canonical = if void_member {
            NO_CLUSTER_CODE
        } else {
            resolve_row(ctx, instance, raw)?
        };
        (row_count, canonical)
    };

    ctx.clusters.codes[index] = raw;
    ctx.clusters.code_ids[index] = canonical;
    for (row, energy) in energies.iter().copied().enumerate().take(row_count) {
        ctx.clusters.energy[cell as usize * total_rows + start + row] = energy;
    }
    Ok(())
}

/// Returns the exact per-link pair delta at `row` for a changed partner.
fn pair_delta(
    ctx: &SimulationContext,
    link: &crate::context::Link,
    row: u8,
    old: u8,
    new: u8,
) -> Result<f64, ContextError> {
    let table = ctx
        .job
        .model
        .energy
        .pair_tables
        .get(link.pair_id as usize)
        .ok_or(ContextError::Invariant {
            what: "pair table index",
        })?;
    let particles = ctx.dimensions.pc() as usize;
    let delta = (table.energies[row as usize * particles + new as usize]
        - table.energies[row as usize * particles + old as usize])
        * ctx.ev_to_kt();
    if !delta.is_finite() {
        return Err(ContextError::Invariant { what: "pair delta" });
    }
    Ok(delta)
}

/// Commits one resolved rule's S2 occupations in the frozen fixed order.
pub(crate) fn apply_state_change(
    ctx: &mut SimulationContext,
    selected: EnvId,
    direction: JumpDirectionId,
    rule: &JumpRule,
) -> Result<(), ContextError> {
    if ctx.job.kind != SimulationKind::Kmc {
        return Err(ContextError::InvalidInput {
            what: "state application",
            detail: "KMC state application requires a KMC context".to_owned(),
        });
    }

    let resolved = resolve_rule_path(ctx, selected, direction, rule)?;
    let path = resolved.nodes();
    let state0 = resolved.state0();
    let s2 = resolved.state2();
    let length = resolved.len();

    let mut old = [0_u8; MAX_PATH_NODES];
    let mut changed = [0_usize; MAX_PATH_NODES];
    let mut changed_count = 0_usize;
    for node in 0..length {
        old[node] = ctx.lattice.particle(path[node]).get();
        if ctx.lattice.particle(path[node]).get() != state0[node] {
            return Err(ContextError::InvalidInput {
                what: "transition state code",
                detail: "the rule does not match the current start occupation".to_owned(),
            });
        }
        if old[node] != s2[node] {
            changed[changed_count] = node;
            changed_count += 1;
        }
    }
    if changed_count == 0 {
        return Ok(());
    }

    // Pass A: read every before-value and weight unchanged-owner deltas by the owner's STABLE flag.
    let mut site_delta = 0.0_f64;
    let mut changed_pair_before = 0.0_f64;
    let mut unchanged_pair_delta = 0.0_f64;
    for &node in changed.iter().take(changed_count) {
        let environment = path[node];
        let position = PositionId::new(ctx.dimensions.position_of(environment));
        let stable_before = ctx.lattice.is_stable(environment);
        let stable_after =
            ctx.job.model.structure.positions[position.get() as usize].stable && s2[node] != 0;
        let site_before = ctx.site_energy(position, ParticleId::new(old[node]));
        let site_after = ctx.site_energy(position, ParticleId::new(s2[node]));
        site_delta += (if stable_after { site_after } else { 0.0 })
            - (if stable_before { site_before } else { 0.0 });
        if stable_before {
            changed_pair_before +=
                ctx.node_energy(environment, ParticleId::new(old[node])) - site_before;
        }

        let incoming_count = ctx.incoming(environment).len();
        for index in 0..incoming_count {
            let incoming = ctx.incoming(environment)[index];
            let owner =
                ctx.incoming_owner(environment, incoming)
                    .ok_or(ContextError::Invariant {
                        what: "incoming owner",
                    })?;
            let owner_changes = (0..length)
                .any(|candidate| path[candidate] == owner && old[candidate] != s2[candidate]);
            if owner_changes || !ctx.lattice.is_stable(owner) {
                continue;
            }
            let link = *ctx
                .link_range(PositionId::new(incoming.owner_position))
                .get(incoming.link_id as usize)
                .ok_or(ContextError::Invariant {
                    what: "incoming link index",
                })?;
            unchanged_pair_delta += pair_delta(
                ctx,
                &link,
                ctx.lattice.particle(owner).get(),
                old[node],
                s2[node],
            )?;
        }
    }

    // Resolve every affected final tuple and every centre row before the first mutation.
    for_each_affected(
        ctx,
        path,
        &changed[..changed_count],
        |ctx, instance, cell| {
            let raw = final_raw_code(ctx, instance, cell, path, s2);
            if ctx.cluster_code(instance, cell) != raw {
                validate_final_rows(ctx, instance, raw)?;
            }
            Ok(())
        },
    )?;

    // Step 2: particles and exactly one derived-state refresh per changed environment.
    for &node in changed.iter().take(changed_count) {
        let environment = path[node];
        ctx.set_particle(environment, ParticleId::new(s2[node]));
        derive_environment(&ctx.job, &ctx.dimensions, &mut ctx.lattice, environment);
    }

    // Step 3: mobile identities follow the changed path nodes.
    permute_mobile_trackers(&mut ctx.lattice, path, &old[..length], &s2[..length]);

    // Step 4: each changed environment leaves its old pool and joins its new one.
    for &node in changed.iter().take(changed_count) {
        let environment = path[node];
        ctx.set_selectable(environment, ParticleId::new(old[node]));
        ctx.set_selectable(environment, ParticleId::new(s2[node]));
    }

    // Step 5: distribute every owner's pair delta, including owners outside the path.
    for &node in changed.iter().take(changed_count) {
        let environment = path[node];
        let incoming_count = ctx.incoming(environment).len();
        for index in 0..incoming_count {
            let incoming = ctx.incoming(environment)[index];
            let owner =
                ctx.incoming_owner(environment, incoming)
                    .ok_or(ContextError::Invariant {
                        what: "incoming owner",
                    })?;
            let particles = ctx.dimensions.pc();
            for particle in 0..particles {
                let link = *ctx
                    .link_range(PositionId::new(incoming.owner_position))
                    .get(incoming.link_id as usize)
                    .ok_or(ContextError::Invariant {
                        what: "incoming link index",
                    })?;
                let delta = pair_delta(ctx, &link, particle as u8, old[node], s2[node])?;
                let index = owner.index() * particles as usize + particle as usize;
                ctx.energy.vectors[index] += delta;
            }
        }
    }

    // Step 6 and 7.1: rebuild each affected instance once (deduplicated by the stored final code) and
    // accumulate `C' - C` from the row captured immediately before the overwrite.
    let mut cluster_delta = 0.0_f64;
    for_each_affected(
        ctx,
        path,
        &changed[..changed_count],
        |ctx, instance, cell| {
            let raw = final_raw_code(ctx, instance, cell, path, s2);
            if ctx.cluster_code(instance, cell) == raw {
                return Ok(());
            }
            let before = ctx.cluster_energy(
                instance,
                cell,
                old_centre_particle(ctx, path, &old[..length], instance, cell),
            );
            rebuild_instance(ctx, instance, cell, raw)?;
            let centre = ctx
                .clusters
                .instance(instance)
                .ok_or(ContextError::Invariant {
                    what: "cluster instance index",
                })?
                .centre_position;
            let environment = ctx.dimensions.cell_env(cell, centre);
            let after = ctx.cluster_energy(instance, cell, ctx.lattice.particle(environment));
            cluster_delta += after - before;
            Ok(())
        },
    )?;

    // Step 7: changed rows use their after-values; unchanged stable owners already contributed their deltas.
    let mut changed_pair_after = 0.0_f64;
    for &node in changed.iter().take(changed_count) {
        let environment = path[node];
        if !ctx.lattice.is_stable(environment) {
            continue;
        }
        let position = PositionId::new(ctx.dimensions.position_of(environment));
        let site = ctx.site_energy(position, ParticleId::new(s2[node]));
        changed_pair_after += ctx.node_energy(environment, ParticleId::new(s2[node])) - site;
    }

    let delta_u = site_delta
        + 0.5 * (changed_pair_after - changed_pair_before + unchanged_pair_delta)
        + cluster_delta;
    if !delta_u.is_finite() {
        return Err(ContextError::Invariant {
            what: "lattice energy delta",
        });
    }
    let total = ctx.lattice_energy_ev + delta_u * ctx.kt_ev();
    if !total.is_finite() {
        return Err(ContextError::Invariant {
            what: "lattice energy",
        });
    }
    ctx.lattice_energy_ev = total;

    rebuild_mobile_positions(&mut ctx.trackers.mobile_positions, &ctx.lattice, path);
    ctx.check_lattice_energy();
    Ok(())
}

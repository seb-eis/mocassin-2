// SPDX-License-Identifier: MIT

//! The invariant sweep of a built context.
//!
//! [`check`] runs as the last initialization phase, after every arena is materialised and before the context is
//! published. It re-checks the arena shapes and the planned lengths, the KMC goals/normalization/time rules, the
//! MMC zero-state rule, the cluster energy rows and associations, the pool consistency, and the jump-status CSR
//! against the package. A failure returns [`ContextError::Invariant`] and no context escapes.

use super::{ContextError, EnvId, Goals, NO_CLUSTER_CODE, PositionId, SimulationContext};

/// Re-checks the built context against the package and the admitting plan.
pub(crate) fn check(context: &SimulationContext) -> Result<(), ContextError> {
    if !context.lattice.has_valid_shape(&context.dimensions) {
        return Err(ContextError::Invariant {
            what: "lattice arena length",
        });
    }

    if !context.lattice.has_valid_trackers() {
        return Err(ContextError::Invariant {
            what: "mobile tracker ids",
        });
    }

    if context.counters.per_particle.len() != context.dimensions.pc() as usize {
        return Err(ContextError::Invariant {
            what: "counter length",
        });
    }

    if !context.links.has_valid_shape(context.dimensions.p()) {
        return Err(ContextError::Invariant { what: "link CSR" });
    }

    if !context.clusters.has_valid_shape(&context.dimensions) {
        return Err(ContextError::Invariant {
            what: "cluster arena",
        });
    }

    check_planned_length(
        context,
        "lattice.particles",
        context.lattice.particles().len(),
    )?;
    check_planned_length(context, "lattice.flags", context.lattice.flags().len())?;
    check_planned_length(
        context,
        "lattice.mobile_tracker",
        context.lattice.mobile_tracker().len(),
    )?;
    check_planned_length(
        context,
        "counters.per_particle",
        context.counters.per_particle.len(),
    )?;
    check_planned_length(
        context,
        "links.owner_offsets",
        context.links.owner_offsets.len(),
    )?;
    check_planned_length(context, "links.links", context.links.links.len())?;
    check_planned_length(
        context,
        "links.inverse_offsets",
        context.links.inverse_offsets.len(),
    )?;
    check_planned_length(context, "links.inverse", context.links.inverse.len())?;
    check_planned_length(
        context,
        "links.cluster_assoc",
        context.links.cluster_assoc.len(),
    )?;
    check_planned_length(
        context,
        "clusters.instances",
        context.clusters.instances.len(),
    )?;
    check_planned_length(
        context,
        "clusters.centre_offsets",
        context.clusters.centre_offsets.len(),
    )?;
    check_planned_length(context, "clusters.centres", context.clusters.centres.len())?;
    check_planned_length(
        context,
        "clusters.slot_start",
        context.clusters.slot_start.len(),
    )?;
    check_planned_length(
        context,
        "clusters.slot_positions",
        context.clusters.slot_positions.len(),
    )?;
    check_planned_length(
        context,
        "clusters.slot_cells",
        context.clusters.slot_cells.len(),
    )?;
    check_planned_length(context, "clusters.codes", context.clusters.codes.len())?;
    check_planned_length(
        context,
        "clusters.code_ids",
        context.clusters.code_ids.len(),
    )?;
    check_planned_length(
        context,
        "clusters.energy_start",
        context.clusters.energy_start.len(),
    )?;
    check_planned_length(context, "clusters.energy", context.clusters.energy.len())?;
    check_planned_length(context, "energy.site", context.energy.site.len())?;
    check_planned_length(context, "energy.vectors", context.energy.vectors.len())?;
    check_planned_length(context, "pools.headers", context.pools.pools.len())?;
    check_planned_length(context, "pools.slot", context.pools.slot.len())?;
    check_planned_length(context, "pools.pool_of", context.pools.pool_of.len())?;
    check_planned_length(context, "jumps.offsets", context.jumps.offsets.len())?;
    check_planned_length(context, "jumps.links", context.jumps.links.len())?;
    check_planned_length(
        context,
        "trackers.mobile_positions",
        context.trackers.mobile_positions.len(),
    )?;

    if context.job.kind == mocassin_types::SimulationKind::Kmc {
        if context.trackers.mobile_positions.len() != context.lattice.mobile_count() as usize {
            return Err(ContextError::Invariant {
                what: "mobile position count",
            });
        }
        for (index, environment) in context.trackers.mobile_positions.iter().enumerate() {
            let env = EnvId::new(*environment);
            if context.lattice.mobile_tracker_id(env) != Some(index as u32) {
                return Err(ContextError::Invariant {
                    what: "mobile position mapping",
                });
            }
        }
        let expected_goals = Goals::for_job(&context.job, context.lattice.mobile_count())
            .map_err(|_| ContextError::Invariant { what: "goals" })?;
        if expected_goals != context.goals {
            return Err(ContextError::Invariant { what: "goals" });
        }
        if !context.normalization.k_norm.is_finite()
            || context.normalization.k_norm <= 0.0
            || !context.normalization.nu_max.is_finite()
            || context.normalization.nu_max <= 0.0
            || !context.normalization.dt.is_finite()
            || context.normalization.dt <= 0.0
        {
            return Err(ContextError::Invariant {
                what: "normalization",
            });
        }
    } else if !context.trackers.mobile_positions.is_empty()
        || context.goals != Goals::zeroed()
        || context.normalization.dt != 0.0
        || context.time_s != 0.0
    {
        return Err(ContextError::Invariant {
            what: "MMC runtime state",
        });
    }
    if !context.time_s.is_finite() || context.time_s < 0.0 {
        return Err(ContextError::Invariant {
            what: "simulated time",
        });
    }

    let planned_members = context
        .plan
        .planned_elements("pools.members")
        .unwrap_or_default();
    let pool_capacity: u64 = context
        .pools
        .pools
        .iter()
        .map(|pool| pool.members.capacity() as u64)
        .sum();
    if pool_capacity < planned_members {
        return Err(ContextError::Invariant {
            what: "pool member capacity",
        });
    }
    if !context.lattice_energy_ev.is_finite() {
        return Err(ContextError::Invariant {
            what: "lattice energy",
        });
    }

    let cells = context.dimensions.cell_count() as usize;
    let mut rows = 0_u32;

    for (index, instance) in context.clusters.instances.iter().enumerate() {
        let Some(table) = context
            .job
            .model
            .energy
            .cluster_tables
            .get(instance.table as usize)
        else {
            return Err(ContextError::Invariant {
                what: "cluster table index",
            });
        };
        if context.clusters.energy_start[index] != rows {
            return Err(ContextError::Invariant {
                what: "cluster energy rows",
            });
        }

        for cell in 0..cells {
            let id = context.clusters.code_ids[index * cells + cell];
            if id != NO_CLUSTER_CODE && id as usize >= table.occupation_codes.len() {
                return Err(ContextError::Invariant {
                    what: "cluster code id",
                });
            }
        }

        rows = rows
            .checked_add(table.particle_table_mapping.len() as u32)
            .ok_or(ContextError::Invariant {
                what: "cluster energy rows",
            })?;
    }

    if context.clusters.energy_start[context.clusters.instances.len()] != rows
        || (context.clusters.energy.len() as u64)
            != u64::from(context.dimensions.cell_count()) * u64::from(rows)
    {
        return Err(ContextError::Invariant {
            what: "cluster energy rows",
        });
    }
    let mut owner = 0_usize;
    for (link_id, link) in context.links.links.iter().enumerate() {
        while owner + 1 < context.links.owner_offsets.len()
            && link_id >= context.links.owner_offsets[owner + 1] as usize
        {
            owner += 1;
        }

        let end = link.cluster_start as usize + usize::from(link.cluster_len);
        for assoc_index in link.cluster_start as usize..end {
            let assoc = context.links.cluster_assoc[assoc_index];
            let Some(instance) = context.clusters.instances.get(assoc.instance as usize) else {
                return Err(ContextError::Invariant {
                    what: "cluster association instance",
                });
            };
            if instance.centre_position as usize != owner
                || assoc.slot == 0
                || assoc.slot >= instance.slot_count
            {
                return Err(ContextError::Invariant {
                    what: "cluster association slot",
                });
            }

            let slot = instance.slot_start as usize + usize::from(assoc.slot);
            if context.clusters.slot_positions[slot] != link.partner_position
                || context.clusters.slot_cells[slot] != link.offset
            {
                return Err(ContextError::Invariant {
                    what: "cluster association geometry",
                });
            }
        }
    }

    if !context.pools.has_valid_shape(&context.dimensions) {
        return Err(ContextError::Invariant {
            what: "selection pools",
        });
    }
    if !context
        .pools
        .is_consistent(&context.dimensions, &context.lattice)
    {
        return Err(ContextError::Invariant {
            what: "selection pool consistency",
        });
    }

    let statuses =
        u64::from(context.dimensions.p()) * u64::from(context.job.model.counts.direction_count);
    if !context.jumps.has_valid_shape(statuses) {
        return Err(ContextError::Invariant {
            what: "jump status CSR",
        });
    }
    check_jump_links(context, statuses)?;

    Ok(())
}

fn check_jump_links(context: &SimulationContext, statuses: u64) -> Result<(), ContextError> {
    let positions = context.dimensions.p() as usize;
    for status in 0..statuses as usize {
        let direction_index = status / positions;
        let position = (status % positions) as u32;
        // `Dimensions::from_job` rejects a direction-count mismatch, so the index is in range; the lookup keeps
        // a hand-built context from panicking if a future call path skips that guard.
        let Some(direction) = context
            .job
            .model
            .transitions
            .directions
            .get(direction_index)
        else {
            return Err(ContextError::Invariant {
                what: "jump status direction",
            });
        };
        let selected = context.dimensions.cell_env(0, position);
        let links = context.jumps.status_links(status);

        if !context.direction_is_active(direction_index as u32) {
            if !links.is_empty() {
                return Err(ContextError::Invariant {
                    what: "inactive jump status is not empty",
                });
            }
            continue;
        }

        for jump_link in links {
            if jump_link.sender_path_id == jump_link.receiver_path_id {
                return Err(ContextError::Invariant {
                    what: "jump link sender equals receiver",
                });
            }
            let Some(sender) = context.resolve_node(selected, direction, jump_link.sender_path_id)
            else {
                return Err(ContextError::Invariant {
                    what: "jump link sender node",
                });
            };
            let Some(receiver) =
                context.resolve_node(selected, direction, jump_link.receiver_path_id)
            else {
                return Err(ContextError::Invariant {
                    what: "jump link receiver node",
                });
            };
            let sender_position = context.dimensions.position_of(sender) as usize;
            let receiver_position = context.dimensions.position_of(receiver) as usize;
            if !context.job.model.structure.positions[sender_position].stable
                || !context.job.model.structure.positions[receiver_position].stable
            {
                return Err(ContextError::Invariant {
                    what: "jump link structural filter",
                });
            }
            let receiver_links = context.link_range(PositionId::new(receiver_position as u32));
            let Some(link) = receiver_links.get(jump_link.link_id as usize) else {
                return Err(ContextError::Invariant {
                    what: "jump link receiver link",
                });
            };
            if context.partner_of(receiver, link) != sender {
                return Err(ContextError::Invariant {
                    what: "jump link partner",
                });
            }
        }
    }

    Ok(())
}

fn check_planned_length(
    context: &SimulationContext,
    what: &'static str,
    actual: usize,
) -> Result<(), ContextError> {
    if context.plan.planned_elements(what) == Some(actual as u64) {
        Ok(())
    } else {
        Err(ContextError::Invariant { what })
    }
}

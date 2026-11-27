// SPDX-License-Identifier: MIT

//! PR-medium real-package acceptance and PR-fast read-only checks for Step 15 feature 1.

mod support;

use mocassin_solver::context::{ContextError, EnvId, PositionId};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::{JumpDirectionId, JumpRule, ParticleId, StateCode};

use support::energy::{assert_close, context, one_cell};
use support::{amend2, ceria, populated_kmc, with_model};

const MAX_PATH_NODES: usize = 10;

fn populate_energy_values(job: mocassin_types::JobPackage) -> mocassin_types::JobPackage {
    with_model(job, |model| {
        for (index, value) in model.energy.defect_background.iter_mut().enumerate() {
            *value = (index as f64 - 2.0) * 0.01;
        }
        for (table, pair) in model.energy.pair_tables.iter_mut().enumerate() {
            for (index, value) in pair.energies.iter_mut().enumerate() {
                *value = (table * 100 + index) as f64 * 0.001;
            }
        }
        for (index, value) in model.energy.cluster_tables.iter_mut().enumerate() {
            for (energy_index, energy) in value.energies.iter_mut().enumerate() {
                *energy = (index * 100 + energy_index) as f64 * 0.01;
            }
        }
    })
}

fn matching_application(
    ctx: &mocassin_solver::context::SimulationContext,
    rule: &JumpRule,
) -> Option<(EnvId, JumpDirectionId)> {
    let collection = ctx
        .job
        .model
        .transitions
        .collections
        .get(rule.collection_id.get() as usize)?;
    for &direction_id in &collection.direction_ids {
        let direction = ctx
            .job
            .model
            .transitions
            .directions
            .get(direction_id.get() as usize)?;
        let mut path = [EnvId::new(0); MAX_PATH_NODES];
        let length = ctx.resolve_path(EnvId::new(0), direction, &mut path);
        if length == 0 {
            continue;
        }
        for environment in 0..ctx.dimensions.environment_count() {
            let selected = EnvId::new(environment);
            let resolved = ctx.resolve_path(selected, direction, &mut path);
            if resolved == length
                && (0..length)
                    .all(|node| ctx.lattice.particle(path[node]).get() == rule.state0.node(node))
            {
                return Some((selected, direction_id));
            }
        }
    }
    None
}

fn direct_node_energy(
    ctx: &mocassin_solver::context::SimulationContext,
    env: EnvId,
    particle: u8,
    particles: &[ParticleId],
) -> f64 {
    let position = ctx.dimensions.position_of(env) as usize;
    let particle_count = ctx.dimensions.pc() as usize;
    let mut value = ctx.job.model.energy.defect_background
        [position * particle_count + usize::from(particle)]
        * ctx.ev_to_kt();
    for link in ctx.link_range(PositionId::new(position as u32)) {
        let partner = ctx.partner_of(env, link);
        let partner_particle = particles[partner.index()].get() as usize;
        value += ctx.job.model.energy.pair_tables[link.pair_id as usize].energies
            [usize::from(particle) * particle_count + partner_particle]
            * ctx.ev_to_kt();
    }
    value
}

fn direct_cluster_energy(
    ctx: &mocassin_solver::context::SimulationContext,
    centre: EnvId,
    particles: &[ParticleId],
) -> Result<f64, ContextError> {
    let position = PositionId::new(ctx.dimensions.position_of(centre));
    let cell = ctx.dimensions.cell_of(centre);
    let mut total = 0.0_f64;
    for &instance in ctx.centres(position) {
        let data = ctx
            .clusters
            .instance(instance)
            .ok_or(ContextError::Invariant {
                what: "test cluster instance",
            })?;
        let (start, _) = ctx
            .clusters
            .slot_range(instance)
            .ok_or(ContextError::Invariant {
                what: "test cluster slot range",
            })?;
        let mut raw = 0_u64;
        for slot in 0..data.slot_count as usize {
            let member = if slot == 0 {
                centre
            } else {
                ctx.dimensions.wrapped_env(
                    cell,
                    ctx.clusters.slot_cells[start + slot],
                    ctx.clusters.slot_positions[start + slot],
                )
            };
            raw |= u64::from(particles[member.index()].get()) << (6 * slot as u32);
        }
        if (0..data.slot_count).any(|slot| (raw >> (u32::from(slot) * 6)) & 0x3F == 0) {
            continue;
        }
        let centre_particle = ParticleId::new((raw & 0x3F) as u8);
        if ctx.cluster_row(instance, centre_particle).is_none() {
            continue;
        }
        let canonical = ctx.cluster_lookup(instance, raw)?;
        let table = &ctx.job.model.energy.cluster_tables[data.table as usize];
        total += table.energies[canonical as usize] * ctx.ev_to_kt();
    }
    Ok(total)
}

fn brute_force_path_energy(
    ctx: &mocassin_solver::context::SimulationContext,
    path: &[EnvId],
    state: StateCode,
) -> Result<f64, ContextError> {
    let mut particles = ctx.lattice.particles().to_vec();
    for (node, &env) in path.iter().enumerate() {
        particles[env.index()] = ParticleId::new(state.node(node));
    }
    let mut total = 0.0_f64;
    for (node, &env) in path.iter().enumerate() {
        total += direct_node_energy(ctx, env, state.node(node), &particles);
        total += direct_cluster_energy(ctx, env, &particles)?;
    }
    Ok(total)
}

fn brute_force_raw_s1(
    ctx: &mocassin_solver::context::SimulationContext,
    path: &[EnvId],
    state: StateCode,
) -> f64 {
    let mut total = 0.0_f64;
    for (node, &env) in path.iter().enumerate().skip(1) {
        total += direct_node_energy(ctx, env, state.node(node), ctx.lattice.particles());
    }
    total
}

fn assert_matches_brute_force(
    ctx: &mut mocassin_solver::context::SimulationContext,
    selected: EnvId,
    direction_id: JumpDirectionId,
    rule: &JumpRule,
) {
    let direction = &ctx.job.model.transitions.directions[direction_id.get() as usize];
    let mut path = [EnvId::new(0); MAX_PATH_NODES];
    let length = ctx.resolve_path(selected, direction, &mut path);
    assert!(length > 0);
    let path = &path[..length];
    let expected_s0 = brute_force_path_energy(ctx, path, rule.state0).expect("S0 recomputes");
    let expected_raw_s1 = brute_force_raw_s1(ctx, path, rule.state1);
    let expected_s2 = brute_force_path_energy(ctx, path, rule.state2).expect("S2 recomputes");
    let expected_s1 = expected_s0 + expected_raw_s1 + 0.5 * (expected_s2 - expected_s0);
    let expected_field = if ctx.field_scale() == 0.0 {
        0.0
    } else {
        let d = ctx.field_direction();
        let t = rule.charge_transport_vector;
        -0.5 * ctx.field_scale() * (t[0] * d[0] + t[1] * d[1] + t[2] * d[2])
    };

    let actual = EnergyEngine::new(ctx)
        .transition_energies(selected, direction_id, rule)
        .expect("the real rule evaluates");
    assert_close(actual.s0, expected_s0);
    assert_close(actual.s1, expected_s1);
    assert_close(actual.s2, expected_s2);
    assert_close(actual.field_term, expected_field);
    assert_close(
        actual.barrier_forward,
        expected_s1 - expected_s0 + expected_field,
    );
    assert_close(
        actual.barrier_backward,
        expected_s1 - expected_s2 - expected_field,
    );
}

#[test]
fn real_fixture_rules_match_an_independent_full_state_recomputation() {
    let jobs = [
        populate_energy_values(ceria()),
        populate_energy_values(one_cell(populated_kmc(), &[2, 0, 1])),
        populate_energy_values(one_cell(amend2(), &[2, 3, 1, 1, 2])),
        populate_energy_values(one_cell(amend2(), &[0, 2, 1, 1, 3])),
    ];
    let mut evaluated = 0_usize;
    for job in jobs {
        let mut ctx = context(job);
        let rules = ctx.job.model.transitions.rules.clone();
        for rule in &rules {
            assert!(rule.static_correction.is_none(), "v1 rules are dynamic");
            if let Some((selected, direction)) = matching_application(&ctx, rule) {
                assert_matches_brute_force(&mut ctx, selected, direction, rule);
                evaluated += 1;
            }
        }
    }
    assert!(evaluated >= 8, "expected real rules from each fixture");
}

#[test]
fn transition_evaluation_leaves_the_context_unchanged() {
    let mut ctx = context(populate_energy_values(one_cell(
        populated_kmc(),
        &[2, 0, 1],
    )));
    let rule = ctx.job.model.transitions.rules[0].clone();
    let before = (
        ctx.lattice.particles().to_vec(),
        ctx.lattice.flags().to_vec(),
        ctx.lattice.mobile_tracker().to_vec(),
        ctx.energy.vectors.clone(),
        ctx.clusters.codes.clone(),
        ctx.clusters.code_ids.clone(),
        ctx.clusters.energy.clone(),
        ctx.lattice_energy_ev,
        ctx.time_s,
    );
    let first = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("first evaluation");
    let second = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("second evaluation");
    let after = (
        ctx.lattice.particles().to_vec(),
        ctx.lattice.flags().to_vec(),
        ctx.lattice.mobile_tracker().to_vec(),
        ctx.energy.vectors.clone(),
        ctx.clusters.codes.clone(),
        ctx.clusters.code_ids.clone(),
        ctx.clusters.energy.clone(),
        ctx.lattice_energy_ev,
        ctx.time_s,
    );
    assert_eq!(first, second);
    assert_eq!(before, after);
}

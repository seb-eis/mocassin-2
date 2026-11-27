// SPDX-License-Identifier: MIT

//! PR-fast cluster-energy tests for Step 15 feature 1.

mod support;

use std::sync::Arc;

use mocassin_solver::context::{ContextError, EnvId, K_B_EV};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::JumpDirectionId;

use support::energy::{assert_close, cluster_only_job, context, one_cell, state_code};
use support::{amend2, pre_map, with_model};

#[test]
fn a_three_body_cluster_delta_is_built_once_from_the_final_two_member_state() {
    let mut job = cluster_only_job(&[2, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = Arc::make_mut(&mut job.model);
    // Only the initial and final tuples exist. Either one-member intermediate tuple is absent.
    model.energy.cluster_tables[0].occupation_codes = vec![4225, 8257];
    model.energy.cluster_tables[0].energies = vec![8.0, 2.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1];
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the final cluster tuple resolves without an intermediate lookup");

    assert_close(energies.s0, 8.0 * scale);
    assert_close(energies.s2, 2.0 * scale);
    assert_close(energies.s1, 5.0 * scale);
    assert_close(energies.barrier_forward, -3.0 * scale);
    assert_close(energies.barrier_backward, 3.0 * scale);
}

#[test]
fn a_cluster_centre_change_with_unchanged_members_uses_the_final_centre_row() {
    let mut job = cluster_only_job(&[1, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4161, 4162];
    model.energy.cluster_tables[0].energies = vec![3.0, 7.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[1, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 2, 1]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the centre-only cluster change evaluates");

    assert_close(energies.s0, 3.0 * scale);
    assert_close(energies.s2, 7.0 * scale);
    assert_close(energies.s1, 5.0 * scale);
}

#[test]
fn raw_s1_reads_the_site_pair_vector_without_folding_in_the_centre_cluster() {
    let mut job = cluster_only_job(&[2, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = Arc::make_mut(&mut job.model);
    model.energy.defect_background[4 + 1] = 1.0;
    model.energy.defect_background[4 + 2] = 9.0;
    model.energy.cluster_tables[0].energies.fill(5.0);
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 2, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let initial_cluster = ctx.cluster_energy(0, 0, mocassin_types::ParticleId::new(1));
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the cluster-centred raw-S1 read evaluates");

    let expected_s0 = scale + initial_cluster;
    let expected_s2 = scale + initial_cluster;
    let expected_s1 = expected_s0 + 9.0 * scale;
    assert_close(energies.s0, expected_s0);
    assert_close(energies.s2, expected_s2);
    assert_close(energies.s1, expected_s1);
}

#[test]
fn a_void_cluster_centre_contributes_zero_without_a_lookup() {
    let mut job = cluster_only_job(&[2, 0, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4225, 8257];
    model.energy.cluster_tables[0].energies = vec![0.0, 20.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1];
    model.transitions.rules[0].state0 = state_code(&[2, 0, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the void centre uses the zero row");

    assert_close(energies.s0, 0.0);
    assert_close(energies.s2, 20.0 * scale);
    assert_close(energies.s1, 10.0 * scale);
}

#[test]
fn a_void_cluster_member_contributes_zero_without_a_lookup() {
    let mut job = cluster_only_job(&[0, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4161];
    model.energy.cluster_tables[0].energies = vec![30.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1];
    model.transitions.rules[0].state0 = state_code(&[0, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 1]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the void member uses the zero row");

    assert_close(energies.s0, 0.0);
    assert_close(energies.s2, 30.0 * scale);
    assert_close(energies.s1, 15.0 * scale);
}

#[test]
fn a_run_time_non_canonical_non_void_tuple_fails_closed() {
    let job = with_model(pre_map(), |model| {
        model.transitions.directions[0].path[2] = [-1, 0, 0, 4];
    });
    let mut ctx = context(job);
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    // Current tuple has a void centre; S2 reaches raw 8386 = [2,3,2], absent from the pre-map table.
    rule.state0 = state_code(&[2, 0, 2]);
    rule.state1 = state_code(&[0, 1, 0]);
    rule.state2 = state_code(&[2, 2, 3]);
    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(1), JumpDirectionId::new(0), &rule)
        .expect_err("the pre-map non-canonical tuple is not guessed");
    assert!(matches!(
        error,
        ContextError::ExpansionMapAbsent {
            cluster_table: 0,
            code: 8386
        }
    ));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn a_present_expansion_map_missing_the_reached_tuple_is_a_data_error() {
    let job = with_model(amend2(), |model| {
        let expansions = model.energy.cluster_tables[0]
            .expansions
            .as_mut()
            .expect("the amendment-2 fixture has a map");
        let index = expansions
            .expanded_codes
            .iter()
            .position(|code| *code == 8386)
            .expect("raw 8386 starts in the map");
        expansions.expanded_codes.remove(index);
        expansions.canonical_indices.remove(index);
        model.transitions.directions[0].path[2] = [-1, 0, 0, 4];
    });
    let mut ctx = context(job);
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    rule.state0 = state_code(&[2, 0, 2]);
    rule.state1 = state_code(&[0, 1, 0]);
    rule.state2 = state_code(&[2, 2, 3]);
    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(1), JumpDirectionId::new(0), &rule)
        .expect_err("the missing raw tuple is not mapped to a neighbour");
    assert!(matches!(
        error,
        ContextError::MissingOccupationCode {
            cluster_table: 0,
            code: 8386
        }
    ));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn the_amendment_2_fixture_evaluates_all_eight_raw_cluster_assignments() {
    let assignments = [
        (8322_u64, 0_u32),
        (8323, 1),
        (8386, 2),
        (8387, 3),
        (12418, 2),
        (12419, 3),
        (12482, 4),
        (12483, 5),
    ];

    for (index, &(initial, initial_canonical)) in assignments.iter().enumerate() {
        let (final_raw, final_canonical) = assignments[(index + 1) % assignments.len()];
        let initial_slots = [
            (initial & 0x3F) as u8,
            ((initial >> 6) & 0x3F) as u8,
            ((initial >> 12) & 0x3F) as u8,
        ];
        let final_slots = [
            (final_raw & 0x3F) as u8,
            ((final_raw >> 6) & 0x3F) as u8,
            ((final_raw >> 12) & 0x3F) as u8,
        ];
        // Cluster slots are [P0 centre, P4 member, P1 member]; the path is [P1, P0, P4].
        let particles = [initial_slots[0], initial_slots[2], 1, 1, initial_slots[1]];
        let mut job = one_cell(amend2(), &particles);
        let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
        let model = Arc::make_mut(&mut job.model);
        for table in &mut model.energy.pair_tables {
            table.energies.fill(0.0);
        }
        model.energy.defect_background.fill(0.0);
        for (energy_index, energy) in model.energy.cluster_tables[0]
            .energies
            .iter_mut()
            .enumerate()
        {
            *energy = f64::from(energy_index as u32 + 1);
        }
        model.transitions.rules[0].state0 =
            state_code(&[initial_slots[2], initial_slots[0], initial_slots[1]]);
        model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
        model.transitions.rules[0].state2 =
            state_code(&[final_slots[2], final_slots[0], final_slots[1]]);

        let mut ctx = context(job);
        assert_eq!(ctx.cluster_code(0, 0), initial);
        assert_eq!(ctx.cluster_code_id(0, 0), Some(initial_canonical));
        let rule = ctx.job.model.transitions.rules[0].clone();
        let energies = EnergyEngine::new(&mut ctx)
            .transition_energies(EnvId::new(1), JumpDirectionId::new(0), &rule)
            .expect("the expansion map resolves the raw assignment");

        let initial_energy = f64::from(initial_canonical + 1) * scale;
        let final_energy = f64::from(final_canonical + 1) * scale;
        assert_close(energies.s0, initial_energy);
        assert_close(energies.s2, final_energy);
        assert_close(energies.s1, 0.5 * (initial_energy + final_energy));
    }
}

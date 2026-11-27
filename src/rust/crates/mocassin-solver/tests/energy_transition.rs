// SPDX-License-Identifier: MIT

//! PR-fast hand-computed tests for Step 15 feature-1 transition evaluation.

mod support;

use mocassin_solver::context::{EnvId, K_B_EV};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::{JumpDirectionId, PairLink, PositionId};

use support::energy::{assert_close, context, pair_only_job, site_only_job, state_code};

#[test]
fn a_three_site_migration_matches_the_hand_computed_s0_raw_s1_and_s2() {
    let mut job = site_only_job(&[2, 0, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = std::sync::Arc::make_mut(&mut job.model);
    // Forward path occupations: S0 = [2,1,1], S1 = [0,1,0], S2 = [1,1,2].
    // Site energies (eV): P0 = {void:4, A:1, B:2}, P1 = {A:3}, P2 = {void:4, A:5, B:10}.
    for (position, particle, energy) in [
        (0, 0, 4.0),
        (0, 1, 1.0),
        (0, 2, 2.0),
        (1, 1, 3.0),
        (2, 0, 4.0),
        (2, 1, 5.0),
        (2, 2, 10.0),
    ] {
        model.energy.defect_background[position * 4 + particle] = energy;
    }

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the real three-site rule evaluates");

    // S0 = 2 + 0 + 5 = 7; raw S1 = 3 + 4 = 7; S2 = 1 + 0 + 10 = 11.
    // conf = 4, S1 = 7 + 7 + 2 = 16, b01 = 9, b10 = 5.
    assert_close(energies.s0, 7.0 * scale);
    assert_close(energies.s1, 16.0 * scale);
    assert_close(energies.s2, 11.0 * scale);
    assert_close(energies.barrier_forward, 9.0 * scale);
    assert_close(energies.barrier_backward, 5.0 * scale);
    assert_eq!(energies.field_term, 0.0);
}

#[test]
fn reversing_the_three_site_migration_swaps_the_forward_and_backward_barriers() {
    let mut job = site_only_job(&[1, 0, 2]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = std::sync::Arc::make_mut(&mut job.model);
    for (position, particle, energy) in [
        (0, 0, 4.0),
        (0, 1, 1.0),
        (0, 2, 2.0),
        (1, 1, 3.0),
        (2, 0, 4.0),
        (2, 1, 5.0),
        (2, 2, 10.0),
    ] {
        model.energy.defect_background[position * 4 + particle] = energy;
    }

    let mut ctx = context(job);
    // The inverse direction uses the same endpoint codes in reverse path order.
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(2), JumpDirectionId::new(1), &rule)
        .expect("the reverse three-site rule evaluates");

    // Reverse S0 = 11, raw S1 = 7, S2 = 7, conf = -4, S1 = 16.
    assert_close(energies.s0, 11.0 * scale);
    assert_close(energies.s1, 16.0 * scale);
    assert_close(energies.s2, 7.0 * scale);
    assert_close(energies.barrier_forward, 5.0 * scale);
    assert_close(energies.barrier_backward, 9.0 * scale);
}

#[test]
fn a_two_particle_swap_matches_the_hand_computed_pair_delta() {
    let mut job = pair_only_job(&[1, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = std::sync::Arc::make_mut(&mut job.model);
    model.structure.environments[0].pairs = vec![PairLink {
        target_position: PositionId::new(2),
        offset: [0, 0, 0],
        pair_id: 0,
    }];
    model.energy.pair_tables[0].energies[4 + 1] = 1.0;
    model.energy.pair_tables[0].energies[2 * 4 + 2] = 4.0;
    model.transitions.directions[0].path = vec![[0, 0, 0, 0], [0, 0, 0, 2]];
    model.transitions.directions[0].movement = vec![[0.5, 0.0, 0.0], [-0.5, 0.0, 0.0]];
    model.transitions.directions[0].path_vector = [0.0; 3];
    model.transitions.directions[0].length = 0.0;
    model.transitions.rules[0].state0 = state_code(&[1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 0]);
    model.transitions.rules[0].state2 = state_code(&[2, 2]);
    model.transitions.rules[0].tracker_order = vec![0xFF, 0xFF];

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the two-particle swap evaluates");

    // The owner P0 -> P2 table contributes 1 eV before and 4 eV after the swap.
    assert_close(energies.s0, scale);
    assert_close(energies.s2, 4.0 * scale);
    assert_close(energies.s1, 2.5 * scale);
    assert_close(energies.barrier_forward, 1.5 * scale);
    assert_close(energies.barrier_backward, -1.5 * scale);
}

#[test]
fn an_asymmetric_one_directional_link_uses_the_spectator_owners_table_and_row() {
    let mut job = pair_only_job(&[2, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = std::sync::Arc::make_mut(&mut job.model);
    // Only the unstable spectator P1 owns a link, pointing at the changed sender P0. There is no reverse.
    model.structure.environments[1].pairs = vec![PairLink {
        target_position: PositionId::new(0),
        offset: [0, 0, 0],
        pair_id: 0,
    }];
    for (row, column, energy) in [(1, 2, 3.0), (1, 1, 7.0), (2, 1, 13.0), (2, 2, 100.0)] {
        model.energy.pair_tables[0].energies[row * 4 + column] = energy;
    }
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 0, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the one-directional asymmetric link evaluates");

    // P1 stays at particle 1: its owner-row correction is t[1][1] - t[1][2] = 7 - 3 = 4 eV.
    assert_close(energies.s0, 3.0 * scale);
    assert_close(energies.s2, 7.0 * scale);
    assert_close(energies.s1, 5.0 * scale);
    assert_close(energies.barrier_forward, 2.0 * scale);
    assert_close(energies.barrier_backward, -2.0 * scale);
    // The sender-major/transposed shortcut would use 13 - 100 and produce -84 eV instead.
    assert!((energies.s2 - (-84.0 * scale)).abs() > scale);
}

#[test]
fn duplicate_periodic_image_links_to_one_changed_sender_all_contribute() {
    let mut job = pair_only_job(&[2, 1, 1]);
    let scale = 1.0 / (K_B_EV * job.settings.temperature_k);
    let model = std::sync::Arc::make_mut(&mut job.model);
    model.structure.environments[1].pairs = vec![
        PairLink {
            target_position: PositionId::new(0),
            offset: [0, 0, 0],
            pair_id: 0,
        },
        PairLink {
            target_position: PositionId::new(0),
            offset: [1, 0, 0],
            pair_id: 1,
        },
    ];
    // In a one-cell supercell both images resolve to P0. Their distinct tables contribute 4 + 5 eV.
    model.energy.pair_tables[0].energies[4 + 2] = 1.0;
    model.energy.pair_tables[0].energies[4 + 1] = 5.0;
    model.energy.pair_tables[1].energies[4 + 2] = 2.0;
    model.energy.pair_tables[1].energies[4 + 1] = 7.0;
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 0, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let rule = ctx.job.model.transitions.rules[0].clone();
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("both periodic images evaluate");

    assert_close(energies.s0, 3.0 * scale);
    assert_close(energies.s2, 12.0 * scale);
    assert_close(energies.s1, 7.5 * scale);
    assert_close(energies.barrier_forward, 4.5 * scale);
    assert_close(energies.barrier_backward, -4.5 * scale);
}

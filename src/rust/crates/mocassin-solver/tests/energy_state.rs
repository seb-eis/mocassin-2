// SPDX-License-Identifier: MIT

//! PR-fast applied-state acceptance for Step 15 feature 2.

mod support;

use std::sync::Arc;

use mocassin_solver::context::{
    ContextError, EnvId, HAS_MOBILE_TRACKER, MOBILE, NO_SLOT, PoolMember, STABLE, SimulationContext,
};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::{JumpDirectionId, PairLink, ParticleId, PositionId};

use support::energy::{assert_close, cluster_only_job, context, state_code};

/// Asserts the pool slot map, the pool member list, and the particle all agree for one environment.
fn assert_pool_membership(ctx: &SimulationContext, env: u32) {
    let pc = ctx.dimensions.pc() as usize;
    let particle = ctx.lattice.particle(EnvId::new(env)).get() as usize;
    let position = ctx.dimensions.position_of(EnvId::new(env)) as usize;
    let pool_id = ctx.pools.pool_of[position * pc + particle];
    let slot = ctx.pools.slot[env as usize * pc + particle];
    if pool_id == NO_SLOT || particle == 0 {
        assert_eq!(slot, NO_SLOT);
        return;
    }
    assert_ne!(slot, NO_SLOT);
    assert!(
        ctx.pools.pools[pool_id as usize]
            .members
            .contains(&PoolMember {
                env: EnvId::new(env),
                particle: ParticleId::new(particle as u8),
            })
    );
}

/// The populated fixture's real cluster has slot 0 at P1 and slots 1/2 at P0/P2.
fn three_body_job(particles: &[u8]) -> mocassin_types::JobPackage {
    let mut job = cluster_only_job(particles);
    let model = Arc::make_mut(&mut job.model);
    for position in &mut model.structure.positions {
        position.stable = true;
    }
    job
}

#[test]
fn a_centre_only_change_rebuilds_the_instance_and_keeps_the_identities_in_place() {
    // Tier: PR-fast. Seam: public apply plus public context state. Initial [1,2,1] has the centre at P1
    // (particle 2); the final [1,1,1] changes only that centre, so the two members and their ions stay.
    let mut job = three_body_job(&[1, 2, 1]);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4161, 4162];
    model.energy.cluster_tables[0].energies = vec![3.0, 7.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[1, 2, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 1]);
    // Global indices are deliberately unrelated to the mobile ids; the engine must not use them.
    model.transitions.rules[0].tracker_order = vec![7, 6, 5];

    let mut ctx = context(job);
    let initial_energy = ctx.lattice_energy_ev;
    let initial_time = ctx.time_s();
    let initial_mcs = ctx.counters.mcs;
    let initial_ids = [
        ctx.lattice.mobile_tracker_id(EnvId::new(0)),
        ctx.lattice.mobile_tracker_id(EnvId::new(1)),
        ctx.lattice.mobile_tracker_id(EnvId::new(2)),
    ];
    let initial_positions = ctx.mobile_positions().to_vec();
    let rule = ctx.job.model.transitions.rules[0].clone();

    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the centre-only state applies");

    assert_eq!(
        ctx.lattice.particles(),
        &[ParticleId::new(1), ParticleId::new(1), ParticleId::new(1)]
    );
    for env in 0..3_u32 {
        assert_eq!(
            ctx.lattice.flags_at(EnvId::new(env)),
            STABLE | MOBILE | HAS_MOBILE_TRACKER
        );
        assert_pool_membership(&ctx, env);
    }
    // The members did not move, so no identity may move either; tracker_order is not the permutation source.
    assert_eq!(
        [
            ctx.lattice.mobile_tracker_id(EnvId::new(0)),
            ctx.lattice.mobile_tracker_id(EnvId::new(1)),
            ctx.lattice.mobile_tracker_id(EnvId::new(2)),
        ],
        initial_ids
    );
    assert_eq!(ctx.mobile_positions(), initial_positions.as_slice());

    assert_eq!(ctx.cluster_code(0, 0), 4161);
    assert_eq!(ctx.cluster_code_id(0, 0), Some(0));
    assert_close(
        ctx.cluster_energy(0, 0, ParticleId::new(1)),
        3.0 * ctx.ev_to_kt(),
    );
    assert_close(
        ctx.cluster_energy(0, 0, ParticleId::new(2)),
        7.0 * ctx.ev_to_kt(),
    );
    // The pre-commit row was the centre-particle-2 row (7 eV); the committed row is 3 eV.
    assert_close(ctx.lattice_energy_ev - initial_energy, -4.0);
    assert_eq!(ctx.time_s(), initial_time);
    assert_eq!(ctx.counters.mcs, initial_mcs);
    ctx.check_lattice_energy();
    assert_close(
        ctx.lattice_energy_ev,
        ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
    );
}

#[test]
fn a_conserving_migration_permutes_the_trackers_and_updates_every_owner() {
    // Tier: PR-fast. Initial [2,1,1] -> [1,1,2]: the P0 particle moves to P2 and the P2 particle to P0; the
    // unchanged P1 keeps its identity. The cluster instance changes both members and never the centre.
    let mut job = three_body_job(&[2, 1, 1]);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4225, 4226, 8257, 8258];
    model.energy.cluster_tables[0].energies = vec![8.0, 8.0, 2.0, 2.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);
    model.transitions.rules[0].tracker_order = vec![7, 6, 5];

    let mut ctx = context(job);
    let initial_energy = ctx.lattice_energy_ev;
    let initial_time = ctx.time_s();
    let initial_mcs = ctx.counters.mcs;
    let ids = [
        ctx.lattice.mobile_tracker_id(EnvId::new(0)).expect("P0 id"),
        ctx.lattice.mobile_tracker_id(EnvId::new(1)).expect("P1 id"),
        ctx.lattice.mobile_tracker_id(EnvId::new(2)).expect("P2 id"),
    ];
    let rule = ctx.job.model.transitions.rules[0].clone();

    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the conserving migration applies");

    assert_eq!(
        ctx.lattice.particles(),
        &[ParticleId::new(1), ParticleId::new(1), ParticleId::new(2)]
    );
    for env in 0..3_u32 {
        assert_eq!(
            ctx.lattice.flags_at(EnvId::new(env)),
            STABLE | MOBILE | HAS_MOBILE_TRACKER
        );
        assert_pool_membership(&ctx, env);
    }
    // id 0 follows the P0 particle to P2, id 2 follows the P2 particle to P0, id 1 stays at P1.
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(0)), Some(ids[2]));
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(1)), Some(ids[1]));
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(2)), Some(ids[0]));
    assert_eq!(ctx.mobile_positions()[ids[0] as usize], 2);
    assert_eq!(ctx.mobile_positions()[ids[1] as usize], 1);
    assert_eq!(ctx.mobile_positions()[ids[2] as usize], 0);

    assert_eq!(ctx.cluster_code(0, 0), 8257);
    assert_eq!(ctx.cluster_code_id(0, 0), Some(2));
    assert_close(
        ctx.cluster_energy(0, 0, ParticleId::new(1)),
        2.0 * ctx.ev_to_kt(),
    );
    assert_close(ctx.lattice_energy_ev - initial_energy, -6.0);
    assert_eq!(ctx.time_s(), initial_time);
    assert_eq!(ctx.counters.mcs, initial_mcs);
    ctx.check_lattice_energy();
    assert_close(
        ctx.lattice_energy_ev,
        ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
    );
}

#[test]
fn a_centre_and_member_change_rebuilds_the_shared_instance_once() {
    // Tier: PR-fast. Initial [2,1,1] -> [1,2,1] changes the centre (P1) and the P0 member of the same
    // instance, so the member-side and centre-side routes name the same `(instance, cell)`; it must be rebuilt
    // once and its `C' - C` counted once.
    let mut job = three_body_job(&[2, 1, 1]);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4161, 4162, 4225, 4226];
    model.energy.cluster_tables[0].energies = vec![3.0, 5.0, 8.0, 9.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 2, 1]);

    let mut ctx = context(job);
    let initial_energy = ctx.lattice_energy_ev;
    let ids = [
        ctx.lattice.mobile_tracker_id(EnvId::new(0)).expect("P0 id"),
        ctx.lattice.mobile_tracker_id(EnvId::new(1)).expect("P1 id"),
        ctx.lattice.mobile_tracker_id(EnvId::new(2)).expect("P2 id"),
    ];
    let rule = ctx.job.model.transitions.rules[0].clone();

    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the shared-instance migration applies");

    assert_eq!(
        ctx.lattice.particles(),
        &[ParticleId::new(1), ParticleId::new(2), ParticleId::new(1)]
    );
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(0)), Some(ids[1]));
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(1)), Some(ids[0]));
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(2)), Some(ids[2]));
    assert_eq!(ctx.mobile_positions()[ids[0] as usize], 1);
    assert_eq!(ctx.mobile_positions()[ids[1] as usize], 0);
    assert_eq!(ctx.mobile_positions()[ids[2] as usize], 2);

    assert_eq!(ctx.cluster_code(0, 0), 4162);
    assert_eq!(ctx.cluster_code_id(0, 0), Some(1));
    assert_close(
        ctx.cluster_energy(0, 0, ParticleId::new(2)),
        5.0 * ctx.ev_to_kt(),
    );
    // 8 eV (centre-1 row of 4225) -> 5 eV (centre-2 row of 4162), counted once.
    assert_close(ctx.lattice_energy_ev - initial_energy, -3.0);
    ctx.check_lattice_energy();
    assert_close(
        ctx.lattice_energy_ev,
        ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
    );
}

#[test]
fn a_vacancy_hop_clears_the_departed_identity_and_flags() {
    // Tier: PR-fast. Initial [1,0,1] -> [0,1,1]: the void moves from P1 to P0, so P0 loses its particle and
    // its tracker id while P1 gains both; P2 is a spectator.
    let mut job = three_body_job(&[1, 0, 1]);
    let model = Arc::make_mut(&mut job.model);
    model.energy.cluster_tables[0].occupation_codes = vec![4161, 4162];
    model.energy.cluster_tables[0].energies = vec![3.0, 7.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[1, 0, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[0, 1, 1]);

    let mut ctx = context(job);
    let initial_energy = ctx.lattice_energy_ev;
    let spectator = ctx.lattice.mobile_tracker_id(EnvId::new(2)).expect("P2 id");
    let rule = ctx.job.model.transitions.rules[0].clone();

    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the vacancy hop applies");

    assert_eq!(
        ctx.lattice.particles(),
        &[ParticleId::new(0), ParticleId::new(1), ParticleId::new(1)]
    );
    assert_eq!(ctx.lattice.flags_at(EnvId::new(0)), 0);
    assert_eq!(
        ctx.lattice.flags_at(EnvId::new(1)),
        STABLE | MOBILE | HAS_MOBILE_TRACKER
    );
    assert_eq!(
        ctx.lattice.flags_at(EnvId::new(2)),
        STABLE | MOBILE | HAS_MOBILE_TRACKER
    );
    assert_eq!(ctx.lattice.mobile_tracker_id(EnvId::new(0)), None);
    assert_eq!(
        ctx.lattice.mobile_tracker_id(EnvId::new(2)),
        Some(spectator)
    );
    let moved = ctx
        .lattice
        .mobile_tracker_id(EnvId::new(1))
        .expect("the moved identity lands at P1");
    assert_eq!(ctx.mobile_positions()[moved as usize], 1);
    assert_eq!(ctx.mobile_positions()[spectator as usize], 2);
    for env in 0..3_u32 {
        assert_pool_membership(&ctx, env);
    }

    // The final tuple has a void member, so it is the empty cluster (no lookup, zero row).
    assert_eq!(ctx.cluster_code(0, 0), 4097);
    assert_eq!(ctx.cluster_code_id(0, 0), None);
    assert_close(ctx.cluster_energy(0, 0, ParticleId::new(1)), 0.0);
    assert_eq!(ctx.lattice_energy_ev, initial_energy);
    ctx.check_lattice_energy();
}

#[test]
fn state_application_rejects_a_non_kmc_context() {
    // Tier: PR-fast. The commit path indexes the KMC mobile id space; an MMC context must fail closed.
    let mut ctx = context(support::populated_mmc());
    let rule = ctx.job.model.transitions.rules[0].clone();
    let error = EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect_err("KMC state application requires a KMC context");
    assert!(matches!(error, ContextError::InvalidInput { .. }));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn owner_updates_cover_an_owner_outside_the_path() {
    // Tier: PR-fast. Seam: public apply and the owner-major node vector.
    let mut job = cluster_only_job(&[2, 1, 1]);
    let model = Arc::make_mut(&mut job.model);
    for environment in &mut model.structure.environments {
        environment.pairs.clear();
        environment.clusters.clear();
    }
    model.energy.cluster_tables.clear();
    model.counts.cluster_table_count = 0;
    model.structure.environments[1].pairs.push(PairLink {
        target_position: PositionId::new(0),
        offset: [0, 0, 0],
        pair_id: 0,
    });
    model.energy.pair_tables[0].energies[4 + 2] = 1.0;
    model.energy.pair_tables[0].energies[4 + 1] = 5.0;
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let scale = ctx.ev_to_kt();
    let before = ctx.node_energy(EnvId::new(1), ParticleId::new(1));
    let rule = ctx.job.model.transitions.rules[0].clone();
    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the external owner receives the partner delta");
    assert_close(
        ctx.node_energy(EnvId::new(1), ParticleId::new(1)),
        before + 4.0 * scale,
    );
}

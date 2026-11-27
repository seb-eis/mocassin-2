// SPDX-License-Identifier: MIT

//! PR-fast incremental-versus-full lattice-energy properties for Step 15 feature 2.

mod support;

use std::sync::Arc;

use mocassin_solver::context::{ContextError, EnvId};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::{JumpDirectionId, JumpRule, ParticleId, PositionId};

use support::energy::{assert_close, cluster_only_job, context, multi_cell, state_code};

/// Three cells with the same start occupation; P1 is structurally unstable, so it is an unchanged owner
/// outside the stable pair rows while its cluster term still counts.
fn property_job() -> mocassin_types::JobPackage {
    let mut job = multi_cell(
        cluster_only_job(&[2, 1, 1]),
        &[&[2, 1, 1], &[2, 1, 1], &[2, 1, 1]],
    );
    let model = Arc::make_mut(&mut job.model);
    for table in &mut model.energy.pair_tables {
        for (index, energy) in table.energies.iter_mut().enumerate() {
            *energy = ((index % 7) as f64 - 3.0) * 0.125;
        }
    }
    for (index, energy) in model.energy.defect_background.iter_mut().enumerate() {
        *energy = ((index % 5) as f64 - 2.0) * 0.0625;
    }
    model.structure.positions[1].stable = false;
    // Centre-1 and centre-2 rows for both member tuples [2,1] and [1,2].
    model.energy.cluster_tables[0].occupation_codes = vec![4225, 4226, 8257, 8258];
    model.energy.cluster_tables[0].energies = vec![8.0, 8.0, 2.0, 2.0];
    model.energy.cluster_tables[0].particle_table_mapping = vec![1, 2];
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);
    job
}

fn reverse(rule: &JumpRule) -> JumpRule {
    let mut reverse = rule.clone();
    reverse.state0 = rule.state2;
    reverse.state2 = rule.state0;
    reverse
}

#[test]
fn incremental_energy_matches_full_recomputation_after_every_accepted_step() {
    // Tier: PR-fast. Seam: public apply followed by the independent public full recomputation. The loop
    // alternates a migration and its inverse across all three cells, so the cluster row and pair-delta
    // bookkeeping is exercised repeatedly and in a non-zero cell.
    let mut ctx = context(property_job());
    assert_close(
        ctx.lattice_energy_ev,
        ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
    );
    let forward = ctx.job.model.transitions.rules[0].clone();
    let backward = reverse(&forward);
    let mut applied = 0_u32;

    for step in 0..30_u32 {
        let cell = step % 3;
        let selected = EnvId::new(cell * 3);
        let rule = if ctx.lattice.particle(selected).get() == 2 {
            &forward
        } else {
            &backward
        };
        let before_particles = ctx.lattice.particles().to_vec();
        let before_time = ctx.time_s();
        let before_mcs = ctx.counters.mcs;
        EnergyEngine::new(&mut ctx)
            .apply_state_change(selected, JumpDirectionId::new(0), rule)
            .unwrap_or_else(|error| panic!("step {step} failed: {error}"));
        applied += 1;
        assert_ne!(ctx.lattice.particles(), before_particles);
        assert_eq!(ctx.time_s(), before_time);
        assert_eq!(ctx.counters.mcs, before_mcs);
        assert_close(
            ctx.lattice_energy_ev,
            ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
        );
        ctx.check_lattice_energy();
    }
    assert_eq!(applied, 30);
}

#[test]
fn an_unstable_unchanged_owner_has_zero_lattice_energy_weight() {
    // Tier: PR-fast. Hand oracle: the only pair interaction is owned by the unstable, unchanged P1, so its
    // 99 eV vector delta is applied to the owner row but contributes no pair weight to U (sigma_O = 0).
    let mut job = cluster_only_job(&[2, 1, 1]);
    let model = Arc::make_mut(&mut job.model);
    for environment in &mut model.structure.environments {
        environment.pairs.clear();
        environment.clusters.clear();
    }
    model.energy.cluster_tables.clear();
    model.counts.cluster_table_count = 0;
    model.structure.positions[1].stable = false;
    model.structure.environments[1]
        .pairs
        .push(mocassin_types::PairLink {
            target_position: PositionId::new(0),
            offset: [0, 0, 0],
            pair_id: 0,
        });
    model.energy.pair_tables[0].energies[4 + 1] = 100.0;
    model.energy.pair_tables[0].energies[4 + 2] = 1.0;
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 1, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    let before = ctx.lattice_energy_ev;
    let owner_before = ctx.node_energy(EnvId::new(1), ParticleId::new(1));
    let rule = ctx.job.model.transitions.rules[0].clone();
    EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the unstable owner delta is excluded from U");
    assert_close(
        ctx.node_energy(EnvId::new(1), ParticleId::new(1)),
        owner_before + 99.0 * ctx.ev_to_kt(),
    );
    assert_close(ctx.lattice_energy_ev, before);
    assert_close(
        ctx.lattice_energy_ev,
        ctx.recompute_lattice_energy_kt() * ctx.kt_ev(),
    );
}

#[test]
fn a_failed_final_cluster_resolution_leaves_the_context_unchanged() {
    // Tier: PR-fast. Resolve-before-mutate failure injection on a real package/model.
    let mut ctx = context(property_job());
    let model = Arc::make_mut(&mut ctx.job.model);
    model.energy.cluster_tables[0]
        .occupation_codes
        .retain(|code| *code != 8257);
    model.energy.cluster_tables[0].energies.remove(2);
    let particles = ctx.lattice.particles().to_vec();
    let flags = ctx.lattice.flags().to_vec();
    let trackers = ctx.lattice.mobile_tracker().to_vec();
    let mobile_positions = ctx.mobile_positions().to_vec();
    let vectors = ctx.energy.vectors.clone();
    let code = ctx.cluster_code(0, 0);
    let total = ctx.lattice_energy_ev;
    let rule = ctx.job.model.transitions.rules[0].clone();
    let error = EnergyEngine::new(&mut ctx)
        .apply_state_change(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect_err("the missing final tuple must fail before mutation");
    assert!(matches!(error, ContextError::ExpansionMapAbsent { .. }));
    assert_eq!(ctx.lattice.particles(), particles);
    assert_eq!(ctx.lattice.flags(), flags);
    assert_eq!(ctx.lattice.mobile_tracker(), trackers);
    assert_eq!(ctx.mobile_positions(), mobile_positions);
    assert_eq!(ctx.energy.vectors, vectors);
    assert_eq!(ctx.cluster_code(0, 0), code);
    assert_eq!(ctx.lattice_energy_ev, total);
}

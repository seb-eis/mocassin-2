// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-5 goals, counters, tracker index, and normalization.

mod support;

use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, K_B_EV, ParticleId, SimulationContext,
};
use mocassin_types::JobPackage;

use support::{amend1_net10, ceria, initialize, populated_kmc, populated_mmc, with_model};

fn expect_invalid(job: JobPackage, what: &'static str, exit: u8) {
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("the invalid job is rejected");
    assert_eq!(error.exit_code().code(), exit);
    match error {
        ContextError::InvalidInput { what: actual, .. } => assert_eq!(actual, what),
        ContextError::NonFiniteFactor { what: actual } => assert_eq!(actual, what),
        other => panic!("unexpected error {other}"),
    }
}

#[test]
fn the_populated_goals_and_block_match_the_hand_values() {
    let job = populated_kmc();
    assert_eq!(job.settings.pre_run_mcsp, 50);
    assert_eq!(job.settings.target_mcsp, 100);
    let context = initialize(job);

    assert_eq!(context.lattice.mobile_count(), 16);
    assert_eq!(
        context.goals,
        mocassin_solver::context::Goals {
            prerun: 800,
            main: 1600,
            total: 2400,
            cycles_per_block: 24,
        }
    );
    assert_eq!(context.counters.per_particle.len(), 4);
    assert!(
        context
            .counters
            .per_particle
            .iter()
            .all(|counter| *counter == Default::default())
    );
    assert_eq!(context.counters.cycles, 0);
    assert_eq!(context.counters.mcs, 0);
}

#[test]
fn the_msl_v1_ceria_job_matches_the_acceptance_values() {
    let context = initialize(ceria());

    assert_eq!(context.lattice.mobile_count(), 648);
    assert_eq!(context.pools.selectable_jump_count, 432);
    assert_eq!(
        context.goals,
        mocassin_solver::context::Goals {
            prerun: 162_000,
            main: 97_200,
            total: 259_200,
            cycles_per_block: 2_592,
        }
    );
    assert_eq!(context.normalization.dof, 2);
    assert_eq!(context.normalization.nu_max, 1.0e13);
    assert!((context.normalization.dt - 2.0 / (1.0e13 * 432.0)).abs() < 1e-20);
}

#[test]
fn the_amend_1_net10_ceria_job_matches_the_same_acceptance_values() {
    let context = initialize(amend1_net10());

    assert_eq!(context.dimensions.lattice_size(), [6, 6, 6]);
    assert_eq!(context.dimensions.p(), 4);
    assert_eq!(context.dimensions.pc(), 5);
    assert_eq!(context.dimensions.environment_count(), 864);
    assert_eq!(context.lattice.mobile_count(), 648);
    assert_eq!(context.pools.selectable_jump_count, 432);
    assert_eq!(
        context.goals,
        mocassin_solver::context::Goals {
            prerun: 162_000,
            main: 97_200,
            total: 259_200,
            cycles_per_block: 2_592,
        }
    );
    assert_eq!(context.goals.cycles_per_block, 2_592);
    assert_eq!(context.normalization.dof, 2);
    assert_eq!(context.normalization.nu_max, 1.0e13);
    assert!((context.normalization.dt - 2.0 / (1.0e13 * 432.0)).abs() < 1e-20);

    // Tracker counts: one mobile id per non-void environment, 16 static trackers per cell, 2 globals.
    assert_eq!(context.mobile_positions().len(), 648);
    assert_eq!(
        context.mobile_positions().len(),
        context.lattice.mobile_count() as usize
    );
    assert_eq!(context.trackers.static_trackers_per_cell, 16);
    assert_eq!(context.trackers.static_tracker_count, 3_456);
    assert_eq!(
        context.trackers.static_tracker_count,
        context.dimensions.cell_count() * 16
    );
    assert_eq!(context.trackers.global_tracker_count, 2);
    assert_eq!(
        context.trackers.global_tracker_count,
        context.job.model.structure.global_tracker_count
    );
    assert_eq!(
        context.static_tracker_mapping().len(),
        context.dimensions.p() as usize * context.dimensions.pc() as usize
    );
    assert_eq!(
        context.global_tracker_mapping().len(),
        context.job.model.counts.collection_count as usize * context.dimensions.pc() as usize
    );

    // The compatibility copy is logically identical to `msl-v1`.
    let msl_v1 = initialize(ceria());
    assert_eq!(context.goals, msl_v1.goals);
    assert_eq!(context.normalization, msl_v1.normalization);
    assert_eq!(context.trackers, msl_v1.trackers);
    assert_eq!(context.pools, msl_v1.pools);
    assert_eq!(context.lattice_energy_ev, msl_v1.lattice_energy_ev);
}

#[test]
fn the_goal_round_up_and_block_size_follow_the_design() {
    let job = populated_kmc();
    let mut settings = job.clone();
    settings.settings.pre_run_mcsp = 0;
    settings.settings.target_mcsp = 1;
    let context = initialize(settings);
    assert_eq!(context.goals.prerun, 0);
    assert_eq!(context.goals.main, 16);
    assert_eq!(context.goals.total, 100);
    assert_eq!(context.goals.cycles_per_block, 1);

    let mut settings = job;
    settings.settings.pre_run_mcsp = 3;
    settings.settings.target_mcsp = 4;
    let context = initialize(settings);
    assert_eq!(context.goals.prerun, 48);
    assert_eq!(context.goals.main, 64);
    assert_eq!(context.goals.total, 200);
    assert_eq!(context.goals.cycles_per_block, 2);
}

#[test]
fn a_zero_total_goal_is_rejected() {
    let mut job = populated_kmc();
    job.settings.pre_run_mcsp = 0;
    job.settings.target_mcsp = 0;
    expect_invalid(job, "total goal", 12);
}

#[test]
fn a_goal_overflow_is_rejected() {
    let mut job = populated_kmc();
    job.settings.target_mcsp = u64::MAX;
    expect_invalid(job, "goal overflow", 12);

    let mut job = populated_kmc();
    job.settings.pre_run_mcsp = u64::MAX;
    job.settings.target_mcsp = 1;
    expect_invalid(job, "goal overflow", 12);
}

#[test]
fn a_job_without_mobile_particles_is_rejected() {
    let mut job = populated_kmc();
    for particle in &mut job.lattice.particles {
        *particle = ParticleId::new(0);
    }
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("no non-void environment");
    assert!(matches!(error, ContextError::NoMobileParticles));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn the_populated_normalization_and_time_step_match_the_hand_values() {
    let context = initialize(populated_kmc());

    assert_eq!(context.normalization.k_fix, 1.0);
    assert_eq!(context.normalization.k_auto, 1.0);
    assert_eq!(context.normalization.raw_max, 0.0);
    assert_eq!(context.normalization.k_norm, 1.0);
    assert_eq!(context.normalization.nu_max, 1.0e13);
    assert_eq!(context.normalization.dof, 2);
    assert!((context.normalization.dt - 2.0 / (1.0e13 * 16.0)).abs() < 1e-20);
    assert_eq!(context.dt(), context.normalization.dt);
    assert_eq!(context.time_s(), 0.0);
    assert_eq!(
        context.origin,
        mocassin_solver::context::ContextOrigin::Fresh
    );
    assert_eq!(context.state_flags, 0);
}

#[test]
fn a_normalization_energy_gives_the_exponential_k_fix() {
    let mut job = populated_kmc();
    job.settings.normalization_energy_ev = Some(0.05);
    let context = initialize(job);
    let expected = (0.05 / (K_B_EV * 773.0)).exp();

    assert!((context.normalization.k_fix - expected).abs() < 1e-12);
    assert!((context.normalization.k_norm - expected).abs() < 1e-12);
    assert!((context.normalization.dt - 2.0 * expected / (1.0e13 * 16.0)).abs() < 1e-20);
}

#[test]
fn a_job_frequency_cap_overrides_the_rule_maximum() {
    let mut job = populated_kmc();
    job.settings.max_attempt_frequency_hz = Some(2.0e13);
    let context = initialize(job);
    assert_eq!(context.normalization.nu_max, 2.0e13);
    assert!((context.normalization.dt - 2.0 / (2.0e13 * 16.0)).abs() < 1e-20);
}

#[test]
fn a_zero_or_nan_nu_max_is_rejected() {
    for cap in [0.0, f64::NAN, -1.0] {
        let mut job = populated_kmc();
        job.settings.max_attempt_frequency_hz = Some(cap);
        expect_invalid(job, "nu_max", 13);
    }

    let job = with_model(populated_kmc(), |model| {
        for rule in &mut model.transitions.rules {
            rule.frequency_hz = 0.0;
        }
    });
    expect_invalid(job, "nu_max", 13);
}

#[test]
fn a_non_finite_k_fix_is_rejected() {
    let mut job = populated_kmc();
    job.settings.normalization_energy_ev = Some(f64::MAX);
    expect_invalid(job, "K_fix", 13);
}

#[test]
fn an_mmc_context_has_no_goals_time_or_normalization() {
    let context = initialize(populated_mmc());

    assert_eq!(context.goals, mocassin_solver::context::Goals::zeroed());
    assert_eq!(context.normalization.k_fix, 1.0);
    assert_eq!(context.normalization.k_auto, 1.0);
    assert_eq!(context.normalization.raw_max, 0.0);
    assert_eq!(context.normalization.k_norm, 1.0);
    assert_eq!(context.normalization.nu_max, 1.0);
    assert_eq!(context.normalization.dof, 1);
    assert_eq!(context.normalization.dt, 0.0);
    assert_eq!(context.time_s(), 0.0);
    assert!(context.trackers.mobile_positions.is_empty());
    assert_eq!(context.trackers.static_tracker_count, 0);
    assert_eq!(context.trackers.global_tracker_count, 0);
}

#[test]
fn the_mobile_mapping_is_the_inverse_of_the_tracker_ids() {
    let context = initialize(populated_kmc());

    assert_eq!(context.mobile_positions().len(), 16);
    for (id, environment) in context.mobile_positions().iter().enumerate() {
        assert_eq!(
            context.lattice.mobile_tracker_id(EnvId::new(*environment)),
            Some(id as u32)
        );
        assert_eq!(
            context.lattice.mobile_tracker_id(EnvId::new(*environment)),
            Some(id as u32)
        );
    }
    for environment in 0..context.dimensions.environment_count() {
        let env = EnvId::new(environment);
        if context.lattice.particle(env) == ParticleId::new(0) {
            assert_eq!(context.lattice.mobile_tracker_id(env), None);
            assert!(!context.mobile_positions().contains(&environment));
        }
    }
}

#[test]
fn the_static_and_global_tracker_index_space_is_exposed() {
    let context = initialize(populated_kmc());
    let model = &context.job.model;
    let per_cell = model.structure.static_trackers_per_cell;
    let cells = context.dimensions.cell_count();

    assert_eq!(context.trackers.static_trackers_per_cell, per_cell);
    assert_eq!(context.trackers.static_tracker_count, cells * per_cell);
    assert_eq!(
        context.trackers.global_tracker_count,
        model.structure.global_tracker_count
    );
    assert_eq!(
        context.static_tracker_mapping().len(),
        model.transitions.static_tracker_mapping.len()
    );
    assert_eq!(
        context.global_tracker_mapping().len(),
        model.transitions.global_tracker_mapping.len()
    );

    let env = EnvId::new(0);
    assert_eq!(context.static_tracker_id(env, ParticleId::new(1)), Some(0));
    assert_eq!(context.static_tracker_id(env, ParticleId::new(0)), None);

    let cell_one_position_two = context.dimensions.cell_env(1, 2);
    let expected = per_cell + 2 * (context.dimensions.pc() - 1);
    assert_eq!(
        context.static_tracker_id(cell_one_position_two, ParticleId::new(1)),
        Some(expected)
    );

    let report = context.memory_report();
    let entry = report
        .entries()
        .iter()
        .find(|entry| entry.what == "trackers.mobile_positions")
        .expect("tracker entry");
    assert_eq!(entry.elements, 16);
}

#[test]
fn the_goals_and_trackers_are_deterministic() {
    let first = initialize(populated_kmc());
    let second = initialize(populated_kmc());
    assert_eq!(first.goals, second.goals);
    assert_eq!(first.trackers, second.trackers);
    assert_eq!(first.normalization, second.normalization);
    assert_eq!(
        first.rng.export_state().to_bytes(),
        second.rng.export_state().to_bytes()
    );
    assert_eq!(first.lattice_energy_ev, second.lattice_energy_ev);
}

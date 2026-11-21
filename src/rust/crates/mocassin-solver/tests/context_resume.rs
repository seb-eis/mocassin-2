// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-5 resume contract.

mod support;

use mocassin_solver::context::{
    ContextError, ContextOptions, ContextOrigin, InitOutcome, NormalizationState, ResumePhase,
    ResumeState, SimulationContext,
};
use mocassin_types::ParticleId;

use support::{initialize, populated_kmc, populated_mmc};

fn fresh_resume(context: &SimulationContext) -> ResumeState {
    ResumeState {
        phase: ResumePhase::Run,
        model_signature: context.job.model.signature.clone(),
        seed_hash: mocassin_rng::seed_hash(&context.job.seed),
        job_id: context.job.job_id,
        kind: context.job.kind,
        lattice_size: context.job.lattice.lattice_size,
        position_count: context.job.lattice.position_count,
        particle_count: context.dimensions.pc(),
        particles: context.lattice.particles().to_vec(),
        counters: context.counters.per_particle.clone(),
        cycles: context.counters.cycles,
        mcs: context.counters.mcs,
        simulated_time_s: context.time_s,
        normalization: NormalizationState {
            k_fix: context.normalization.k_fix,
            k_auto: context.normalization.k_auto,
            raw_max: context.normalization.raw_max,
            k_norm: context.normalization.k_norm,
        },
        rng_state: context.rng.export_state(),
        state_flags: 0,
        mobile_positions: if context.kind == mocassin_types::SimulationKind::Kmc {
            Some(context.mobile_positions().to_vec())
        } else {
            None
        },
    }
}

fn resume_options(resume: ResumeState) -> ContextOptions {
    ContextOptions {
        resume: Some(resume),
        ..ContextOptions::default()
    }
}

fn initialize_with_resume(
    job: mocassin_types::JobPackage,
    resume: ResumeState,
) -> SimulationContext {
    match SimulationContext::initialize(job, &resume_options(resume), None).expect("resumes") {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a Run state is not completed"),
    }
}

fn expect_mismatch(mutate: impl FnOnce(&mut ResumeState), what: &'static str) {
    let context = initialize(populated_kmc());
    let mut resume = fresh_resume(&context);
    mutate(&mut resume);
    let error = SimulationContext::initialize(context.job.clone(), &resume_options(resume), None)
        .expect_err("the mismatch is rejected");
    match error {
        ContextError::ResumeMismatch { what: actual } => assert_eq!(actual, what),
        other => panic!("unexpected error {other}"),
    }
}

#[test]
fn a_resume_restores_the_state_exactly() {
    let mut fresh = initialize(populated_kmc());
    let first_draw = fresh.rng.next_u32();
    let _ = first_draw;

    let mut resume = fresh_resume(&fresh);
    resume.phase = ResumePhase::Prerun;
    resume.counters[0].skip = 3;
    resume.counters[0].success = 1;
    resume.cycles = 10;
    resume.mcs = 5;
    resume.simulated_time_s = 2.5;
    resume.normalization = NormalizationState {
        k_fix: 1.0,
        k_auto: 0.125,
        raw_max: 8.0,
        k_norm: 0.125,
    };
    resume.state_flags = 0x1234;
    let next_expected = fresh.rng.next_u32();

    let restored = initialize_with_resume(fresh.job.clone(), resume.clone());

    assert_eq!(restored.origin, ContextOrigin::Resumed);
    assert_eq!(restored.state_flags, 0x1234);
    assert_eq!(restored.lattice.particles(), resume.particles.as_slice());
    assert_eq!(
        restored.mobile_positions(),
        resume.mobile_positions.as_ref().unwrap()
    );
    assert_eq!(restored.counters.per_particle, resume.counters);
    assert_eq!(restored.counters.cycles, 10);
    assert_eq!(restored.counters.mcs, 5);
    assert_eq!(restored.time_s(), 2.5);
    assert_eq!(restored.normalization.k_fix, 1.0);
    assert_eq!(restored.normalization.k_auto, 0.125);
    assert_eq!(restored.normalization.raw_max, 8.0);
    assert_eq!(restored.normalization.k_norm, 0.125);
    assert!((restored.dt() - 2.0 * 0.125 / (1.0e13 * 16.0)).abs() < 1e-20);
    assert_eq!(restored.goals, fresh.goals);
    assert_eq!(restored.lattice_energy_ev, fresh.lattice_energy_ev);
    assert_eq!(restored.trackers, fresh.trackers);

    let mut restored = restored;
    assert_eq!(restored.rng.next_u32(), next_expected);
}

#[test]
fn the_resumed_lattice_drives_the_energy_pools_goals_and_tracker_index() {
    let fresh = initialize(populated_kmc());
    let mut resume = fresh_resume(&fresh);
    resume.particles[0] = ParticleId::new(0);
    resume.mobile_positions = Some(
        resume
            .particles
            .iter()
            .enumerate()
            .filter(|(_, particle)| particle.get() != 0)
            .map(|(environment, _)| environment as u32)
            .collect(),
    );

    let restored = initialize_with_resume(fresh.job.clone(), resume.clone());

    assert_eq!(restored.lattice.particles(), resume.particles.as_slice());
    assert_eq!(restored.lattice.mobile_count(), 15);
    assert_eq!(restored.mobile_positions().len(), 15);
    assert_eq!(restored.pools.member_count(), 15);
    assert_eq!(restored.pools.selectable_jump_count, 15);
    assert_eq!(
        restored.goals,
        mocassin_solver::context::Goals {
            prerun: 750,
            main: 1500,
            total: 2300,
            cycles_per_block: 23,
        }
    );
    assert!(
        (restored.lattice_energy_ev - restored.recompute_lattice_energy_kt() * restored.kt_ev())
            .abs()
            < 1e-12
    );
    assert_eq!(
        restored
            .memory_report()
            .entries()
            .iter()
            .find(|entry| entry.what == "trackers.mobile_positions")
            .expect("tracker entry")
            .elements,
        15
    );
    assert_eq!(restored.job.lattice.particles[0], ParticleId::new(1));
}

#[test]
fn a_completed_state_returns_the_already_completed_outcome() {
    let fresh = initialize(populated_kmc());
    let mut resume = fresh_resume(&fresh);
    resume.phase = ResumePhase::Completed;
    resume.mcs = 4242;

    let outcome = SimulationContext::initialize(fresh.job.clone(), &resume_options(resume), None)
        .expect("the completed state is recognized");
    match outcome {
        InitOutcome::AlreadyCompleted { mcs, total_goal } => {
            assert_eq!(mcs, 4242);
            assert_eq!(total_goal, fresh.goals.total);
        }
        InitOutcome::Ready(_) => panic!("a completed state must not resume"),
    }
}

#[test]
fn a_resume_without_a_stored_mapping_is_accepted() {
    let fresh = initialize(populated_kmc());
    let mut resume = fresh_resume(&fresh);
    resume.mobile_positions = None;
    let restored = initialize_with_resume(fresh.job.clone(), resume);
    assert_eq!(restored.mobile_positions().len(), 16);
}

#[test]
fn an_mmc_resume_has_no_time_or_goals() {
    let fresh = initialize(populated_mmc());
    let mut resume = fresh_resume(&fresh);
    resume.simulated_time_s = 5.0;
    let restored = initialize_with_resume(fresh.job.clone(), resume);

    assert_eq!(restored.origin, ContextOrigin::Resumed);
    assert_eq!(restored.time_s(), 0.0);
    assert_eq!(restored.goals, mocassin_solver::context::Goals::zeroed());
    assert_eq!(restored.normalization.k_fix, 1.0);
    assert_eq!(restored.normalization.k_norm, 1.0);
    assert_eq!(restored.normalization.dt, 0.0);
    assert!(restored.trackers.mobile_positions.is_empty());
}

#[test]
fn resume_binding_mismatches_are_rejected() {
    expect_mismatch(|resume| resume.model_signature.push('0'), "model signature");
    expect_mismatch(|resume| resume.seed_hash.push('0'), "seed hash");
    expect_mismatch(|resume| resume.job_id += 1, "job id");
    expect_mismatch(
        |resume| resume.kind = mocassin_types::SimulationKind::Mmc,
        "kind",
    );
    expect_mismatch(|resume| resume.lattice_size = [2, 2, 1], "lattice size");
    expect_mismatch(|resume| resume.position_count = 4, "position count");
    expect_mismatch(|resume| resume.particle_count = 5, "particle count");
    expect_mismatch(
        |resume| {
            resume.particles.pop();
        },
        "particle array length",
    );
    expect_mismatch(
        |resume| {
            resume.particles[0] = ParticleId::new(63);
        },
        "particle index",
    );
    expect_mismatch(
        |resume| resume.simulated_time_s = f64::NAN,
        "simulated time",
    );
    expect_mismatch(|resume| resume.simulated_time_s = -1.0, "simulated time");
    expect_mismatch(
        |resume| {
            resume.counters.pop();
        },
        "counter array length",
    );
    expect_mismatch(|resume| resume.normalization.k_fix = 0.0, "K_fix");
    expect_mismatch(|resume| resume.normalization.k_auto = f64::NAN, "K_auto");
    expect_mismatch(|resume| resume.normalization.k_norm = -1.0, "K_norm");
    expect_mismatch(|resume| resume.normalization.raw_max = -1.0, "RawMax");
    expect_mismatch(
        |resume| {
            resume.mobile_positions = Some(vec![0; 16]);
        },
        "mobile mapping",
    );
}

#[test]
fn a_completed_state_still_validates_the_mapping() {
    let fresh = initialize(populated_kmc());
    let mut resume = fresh_resume(&fresh);
    resume.phase = ResumePhase::Completed;
    resume.mobile_positions = Some(vec![0; 16]);
    let error = SimulationContext::initialize(fresh.job.clone(), &resume_options(resume), None)
        .expect_err("the mapping mismatch is rejected");
    assert!(matches!(
        error,
        ContextError::ResumeMismatch {
            what: "mobile mapping"
        }
    ));
}

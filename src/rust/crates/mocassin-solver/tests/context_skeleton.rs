// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-1 context skeleton.
//!
//! The tests use the real .NET-written `msl-v1` package unchanged: no fake job, no fake model. They assert the
//! dimension contract, the lattice/flag/mobile-tracker arenas, the empty later-feature containers, the zeroed
//! counters, the memory report, the limits, and the cancellation outcome.

use std::path::Path;
use std::sync::Arc;

use mocassin_msl::Package;
use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, InitOutcome, SimulationContext,
};
use mocassin_types::{JobPackage, ReaderLimits, SimulationKind};

fn fixture_job(job_id: u32) -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl");
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the msl-v1 fixture opens")
        .load_job(job_id)
        .expect("the fixture job maps")
}

fn initialize(job: JobPackage, options: &ContextOptions) -> SimulationContext {
    match SimulationContext::initialize(job, options, None).expect("the job initializes") {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a fresh job is not already completed"),
    }
}

#[test]
fn the_msl_v1_ceria_job_initializes_the_skeleton_context() {
    let job = fixture_job(1);
    let expected_particles = job.lattice.particles.clone();
    let expected_position_count = job.model.counts.position_count;
    let expected_particle_count = job.model.counts.particle_count;

    let context = initialize(job, &ContextOptions::default());

    assert_eq!(context.kind, SimulationKind::Kmc);
    assert_eq!(context.dimensions.lattice_size(), [6, 6, 6]);
    assert_eq!(context.dimensions.p(), 4);
    assert_eq!(context.dimensions.pc(), 5);
    assert_eq!(context.dimensions.cell_count(), 216);
    assert_eq!(context.dimensions.environment_count(), 864);
    assert_eq!(context.dimensions.p(), expected_position_count);
    assert_eq!(context.dimensions.pc(), expected_particle_count);

    assert_eq!(context.lattice.particles(), expected_particles.as_slice());
    assert_eq!(context.lattice.flags().len(), 864);
    assert_eq!(context.lattice.mobile_tracker().len(), 864);
}

#[test]
fn the_environment_flags_follow_the_documented_rules() {
    let context = initialize(fixture_job(1), &ContextOptions::default());
    let positions = context.dimensions.p() as usize;
    let mut expected_tracker = 0_u32;
    let mut mobile_count = 0_u32;

    for env in 0..context.dimensions.environment_count() {
        let id = EnvId::new(env);
        let particle = context.lattice.particle(id);
        let is_void = particle.get() == 0;
        let position = &context.job.model.structure.positions[env as usize % positions];
        let expected_stable = !is_void && position.stable;

        assert_eq!(context.lattice.is_stable(id), expected_stable, "env {env}");
        assert_eq!(context.lattice.is_mobile(id), !is_void, "env {env}");

        if is_void {
            assert_eq!(context.lattice.mobile_tracker_id(id), None, "env {env}");
        } else {
            assert_eq!(
                context.lattice.mobile_tracker_id(id),
                Some(expected_tracker),
                "env {env}"
            );
            expected_tracker += 1;
            mobile_count += 1;
        }
    }

    assert_eq!(context.lattice.mobile_count(), mobile_count);
    assert_eq!(mobile_count, expected_tracker);
    assert!(mobile_count > 0, "the fixture has mobile particles");
}

#[test]
fn the_later_feature_containers_are_empty_and_the_counters_are_zeroed() {
    let context = initialize(fixture_job(1), &ContextOptions::default());

    // The link and cluster CSRs are built, but this table-free ceria package has no link, instance, or code.
    assert!(context.links.is_empty());
    assert_eq!(
        context.links.owner_offsets.len(),
        context.dimensions.p() as usize + 1
    );
    assert_eq!(
        context.links.inverse_offsets.len(),
        context.dimensions.p() as usize + 1
    );
    assert!(context.clusters.is_empty());
    assert_eq!(
        context.clusters.centre_offsets.len(),
        context.dimensions.p() as usize + 1
    );
    assert_eq!(context.clusters.slot_start, [0]);
    assert!(!context.pools.is_empty());
    assert_eq!(
        context.pools.pool_of.len(),
        context.dimensions.p() as usize * context.dimensions.pc() as usize
    );
    assert_eq!(
        context.pools.slot.len(),
        context.dimensions.environment_count() as usize * context.dimensions.pc() as usize
    );
    assert!(context.pools.selectable_jump_count > 0);

    assert_eq!(
        context.jumps.status_count(),
        context.dimensions.p() as usize * context.job.model.counts.direction_count as usize
    );
    assert!(!context.jumps.offsets.is_empty());
    assert_eq!(
        context.counters.per_particle.len(),
        context.dimensions.pc() as usize
    );
    assert!(
        context
            .counters
            .per_particle
            .iter()
            .all(|entry| *entry == Default::default())
    );
    assert_eq!(context.counters.cycles, 0);
    assert_eq!(context.counters.mcs, 0);
}

#[test]
fn the_memory_report_is_internally_consistent() {
    let context = initialize(fixture_job(1), &ContextOptions::default());
    let report = context.memory_report();

    assert_eq!(
        report.total_bytes(),
        report
            .entries()
            .iter()
            .map(|entry| entry.bytes)
            .sum::<u64>()
    );
    assert!(report.total_bytes() > 0);
    assert!(
        report.capacity_bytes() >= report.total_bytes(),
        "capacity {} < planned {}",
        report.capacity_bytes(),
        report.total_bytes()
    );
}

#[test]
fn a_limit_violation_is_rejected_before_allocation() {
    let mut options = ContextOptions::default();
    options.limits.max_positions = 863;

    let error = SimulationContext::initialize(fixture_job(1), &options, None)
        .expect_err("864 environments exceed the cap");

    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_positions",
            value: 864,
            limit: 863,
        }
    ));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn an_exact_total_cap_admits_the_job() {
    let job = fixture_job(1);
    let total = initialize(job.clone(), &ContextOptions::default())
        .memory_report()
        .total_bytes();
    let mut options = ContextOptions::default();
    options.limits.max_context_bytes = total;

    let context = initialize(job, &options);
    assert_eq!(context.memory_report().total_bytes(), total);
}

#[test]
fn the_compiled_site_stability_flag_is_honoured() {
    let mut job = fixture_job(1);
    let positions = job.model.counts.position_count;
    let environments = job.lattice.particles.len();
    let unstable = (0..positions)
        .find(|position| {
            (*position as usize..environments)
                .step_by(positions as usize)
                .any(|env| job.lattice.particles[env].get() != 0)
        })
        .expect("a non-void environment exists");

    let mut model = (*job.model).clone();
    model.structure.positions[unstable as usize].stable = false;
    job.model = Arc::new(model);

    let context = initialize(job, &ContextOptions::default());
    let mut checked = 0;

    for env in (unstable as usize..context.dimensions.environment_count() as usize)
        .step_by(positions as usize)
    {
        let id = EnvId::new(env as u32);
        if context.lattice.particle(id).get() != 0 {
            assert!(!context.lattice.is_stable(id), "env {env}");
            checked += 1;
        }
    }

    assert!(
        checked > 0,
        "the unstable position carried a non-void particle"
    );
}

#[test]
fn a_truncated_lattice_is_rejected_as_invalid_input() {
    let mut job = fixture_job(1);
    job.lattice.particles.pop();

    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("a short lattice is invalid");

    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "lattice length",
            ..
        }
    ));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn a_particle_outside_the_table_is_rejected_as_invalid_input() {
    let mut job = fixture_job(1);
    job.lattice.particles[0] = mocassin_types::ParticleId::new(63);

    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("particle 63 is outside the five-entry table");

    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "lattice particle",
            ..
        }
    ));
}

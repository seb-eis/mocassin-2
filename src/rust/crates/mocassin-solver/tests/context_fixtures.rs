// SPDX-License-Identifier: MIT

//! Step 14 feature-6 acceptance over the new real-writer fixtures.

use std::path::Path;
use std::sync::Arc;

use mocassin_msl::Package;
use mocassin_solver::context::{
    ContextOptions, InitOutcome, ParticleId, PositionId, SimulationContext,
};
use mocassin_types::{JobPackage, ReaderLimits};

fn fixture_job(package: &str, file: &str) -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(package)
        .join(file);
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the fixture opens")
        .load_job(1)
        .expect("the fixture job maps")
}

fn initialize(job: JobPackage) -> SimulationContext {
    match SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect("the fixture initializes")
    {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a fresh job is not completed"),
    }
}

fn summary(context: &SimulationContext) -> String {
    let mut links_per_owner = vec![0_usize; context.dimensions.p() as usize];
    for position in 0..context.dimensions.p() {
        links_per_owner[position as usize] = context.link_range(PositionId::new(position)).len();
    }
    let total_links: usize = links_per_owner.iter().sum();
    let site_sum: f64 = context.energy.site.iter().sum();
    let vector_sum: f64 = context.energy.vectors.iter().sum();
    let cluster_sum: f64 = context.clusters.energy.iter().sum();
    let pool_sizes: Vec<(u32, usize)> = context
        .pools
        .pools
        .iter()
        .map(|pool| (pool.direction_count, pool.members.len()))
        .collect();
    let jump_sizes: Vec<usize> = (0..context.jumps.status_count())
        .map(|status| context.jump_status(status).len())
        .collect();

    format!(
        "kind={:?}\ncell={}x{}x{} p={} pc={} envs={}\nmobile={} selectable={} members={}\nlinks={} per_owner={:?}\nclusters instances={} codes={} energy_rows={} cluster_sum={:.6}\nsite={} vector={} energy_ev={:.9}\npools={:?} jumps={:?}\ngoals={:?}\nnormalization={:?}\ntrackers mobile={} static={} global={} time={} origin={:?} flags={}",
        context.kind,
        context.dimensions.a(),
        context.dimensions.b(),
        context.dimensions.c(),
        context.dimensions.p(),
        context.dimensions.pc(),
        context.dimensions.environment_count(),
        context.lattice.mobile_count(),
        context.pools.selectable_jump_count,
        context.pools.member_count(),
        total_links,
        links_per_owner,
        context.clusters.instances.len(),
        context.clusters.codes.len(),
        context.clusters.energy.len(),
        cluster_sum,
        site_sum,
        vector_sum,
        context.lattice_energy_ev,
        pool_sizes,
        jump_sizes,
        context.goals,
        context.normalization,
        context.trackers.mobile_positions.len(),
        context.trackers.static_tracker_count,
        context.trackers.global_tracker_count,
        context.time_s(),
        context.origin,
        context.state_flags,
    )
}

#[test]
fn the_ten_cubed_ceria_fixture_initializes_and_matches_its_computed_values() {
    let context = initialize(fixture_job(
        "step-14-ceria-10x10x10",
        "step-14-ceria-10x10x10.msl",
    ));

    assert_eq!(context.dimensions.lattice_size(), [10, 10, 10]);
    assert_eq!(context.dimensions.p(), 4);
    assert_eq!(context.dimensions.pc(), 5);
    assert_eq!(context.dimensions.environment_count(), 4000);
    assert_eq!(context.lattice.particles().len(), 4000);

    let mobile = u64::from(context.lattice.mobile_count());
    assert!(mobile > 0);
    assert_eq!(context.goals.prerun, 250 * mobile);
    assert_eq!(context.goals.main, 150 * mobile);
    let sum = context.goals.prerun + context.goals.main;
    let total = sum.div_ceil(100) * 100;
    assert_eq!(context.goals.total, total);
    assert_eq!(context.goals.cycles_per_block, total / 100);
    assert!(context.pools.selectable_jump_count > 0);
    assert_eq!(context.normalization.dof, 2);
    assert_eq!(context.normalization.nu_max, 1.0e13);
    assert!(
        (context.dt() - 2.0 / (1.0e13 * context.pools.selectable_jump_count as f64)).abs() < 1e-20
    );
    assert_eq!(context.lattice_energy_ev, 0.0);
    assert_eq!(
        context.recompute_lattice_energy_kt() * context.kt_ev(),
        context.lattice_energy_ev
    );
    assert_eq!(
        context.mobile_positions().len(),
        context.lattice.mobile_count() as usize
    );
    assert_eq!(
        context
            .memory_report()
            .entries()
            .iter()
            .find(|entry| entry.what == "trackers.mobile_positions")
            .expect("tracker entry")
            .elements,
        mobile
    );

    let first = summary(&context);
    let second = initialize(fixture_job(
        "step-14-ceria-10x10x10",
        "step-14-ceria-10x10x10.msl",
    ));
    assert_eq!(first, summary(&second));
    assert_eq!(context.memory_report(), second.memory_report());
    assert_eq!(context.lattice_energy_ev, second.lattice_energy_ev);
    assert_eq!(context.goals, second.goals);
    assert_eq!(context.normalization, second.normalization);
    assert_eq!(
        context.rng.export_state().to_bytes(),
        second.rng.export_state().to_bytes()
    );
    assert_eq!(context.state_flags, second.state_flags);

    insta::assert_snapshot!("step14_ceria_10x10x10", first);
}

#[test]
fn the_multi_centre_fixture_has_four_instances_and_a_per_instance_energy() {
    let job = fixture_job(
        "step-14-multi-centre-cluster",
        "step-14-multi-centre-cluster.msl",
    );
    let context = initialize(job.clone());

    assert_eq!(context.dimensions.p(), 12);
    assert_eq!(context.job.model.structure.site_count, 3);
    assert_eq!(context.clusters.instances.len(), 4);
    assert!(context.links_are_consistent());

    let expected_centres = [0_u32, 5, 6, 11];
    let expected_slots = [[0_u32, 1, 2], [5, 4, 3], [6, 7, 8], [11, 10, 9]];
    for (index, instance) in context.clusters.instances.iter().enumerate() {
        assert_eq!(instance.centre_position, expected_centres[index]);
        assert_eq!(instance.slot_count, 3);
        let start = instance.slot_start as usize;
        assert_eq!(
            &context.clusters.slot_positions[start..start + 3],
            &expected_slots[index]
        );
        assert_eq!(
            context.centres(PositionId::new(instance.centre_position)),
            [index as u32]
        );
        for cell in 0..context.dimensions.cell_count() {
            assert_eq!(context.cluster_code(index as u32, cell), 4161);
            assert_eq!(context.cluster_code_id(index as u32, cell), Some(0));
            assert_eq!(
                context.cluster_energy(index as u32, cell, ParticleId::new(1)),
                0.0
            );
        }
    }

    // A distinct synthetic table proves the per-instance code -> energy mapping; the sample tables are zero.
    let mut model = (*job.model).clone();
    model.energy.cluster_tables[0].energies =
        (0..18).map(|index| (index as f64 + 1.0) * 0.5).collect();
    let job = JobPackage {
        model: Arc::new(model),
        ..job
    };
    let context = initialize(job);
    let scale = context.ev_to_kt();
    for instance in 0..4 {
        assert_eq!(
            context.cluster_energy(instance, 0, ParticleId::new(1)),
            0.5 * scale
        );
        assert_eq!(
            context.cluster_energy(instance, 0, ParticleId::new(2)),
            1.0 * scale
        );
    }
}

trait ClusterProbe {
    fn links_are_consistent(&self) -> bool;
}

impl ClusterProbe for SimulationContext {
    fn links_are_consistent(&self) -> bool {
        self.clusters.instances.len() == 4
            && self.clusters.instances.iter().all(|instance| {
                let start = instance.slot_start as usize;
                instance.slot_count == 3
                    && self.clusters.slot_positions[start] == instance.centre_position
                    && self.clusters.slot_cells[start] == [0, 0, 0]
            })
    }
}

#[test]
fn the_fixtures_are_deterministic_across_two_initializations() {
    let first = initialize(fixture_job(
        "step-14-multi-centre-cluster",
        "step-14-multi-centre-cluster.msl",
    ));
    let second = initialize(fixture_job(
        "step-14-multi-centre-cluster",
        "step-14-multi-centre-cluster.msl",
    ));
    assert_eq!(first.clusters, second.clusters);
    assert_eq!(first.links, second.links);
    assert_eq!(first.energy, second.energy);
    assert_eq!(first.goals, second.goals);
    assert_eq!(first.normalization, second.normalization);
    assert_eq!(first.state_flags, second.state_flags);
}

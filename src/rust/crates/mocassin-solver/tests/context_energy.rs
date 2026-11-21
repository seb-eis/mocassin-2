// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-3 energy runtime and exact initial lattice energy.

mod support;

use std::sync::Arc;

use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, K_B_EV, PositionId, SimulationContext,
};
use mocassin_types::{
    ClusterLink, ClusterTable, EnergyModel, EnvironmentModel, InitialLattice, JobPackage,
    JobSettings, ModelCounts, PairLink, PairTable, ParameterizedModel, ParticleId, ParticleTable,
    Position, SimulationKind, SimulationModel, SimulationSettings, SolverJobFlags, StructureModel,
    TrackerModels, TransitionModel,
};

use support::{amend2, centered, ceria, initialize, populated_kmc, with_model};

const TOY_TEMPERATURE: f64 = 1000.0;
const TOY_FIELD_VM: f64 = 1.0e7;

fn toy_job(with_cluster: bool) -> JobPackage {
    let particles = 2_u32;
    let pc = particles as usize;
    let mut pair = vec![0.0; pc * pc];
    pair[pc + 1] = 3.0;

    let (cluster_tables, clusters) = if with_cluster {
        (
            vec![ClusterTable {
                object_id: "toy-cluster".to_owned(),
                occupation_codes: vec![65],
                energies: vec![10.0],
                particle_table_mapping: vec![1],
                expansions: None,
            }],
            vec![
                ClusterLink {
                    cluster_id: 0,
                    byte_position: 0,
                },
                ClusterLink {
                    cluster_id: 0,
                    byte_position: 1,
                },
            ],
        )
    } else {
        (Vec::new(), Vec::new())
    };

    let model = ParameterizedModel {
        format_version: 1,
        signature: "toy-signature".to_owned(),
        model_signature: "toy-model".to_owned(),
        symmetry_contract_version: "symmetry-1".to_owned(),
        space_group_data_version: "toy".to_owned(),
        space_group_data_sha256: "0".repeat(64),
        particle: ParticleTable {
            charges: vec![0.0, 0.0],
            symbols: vec!["void".to_owned(), "A".to_owned()],
            is_vacancy: vec![false, false],
        },
        structure: StructureModel {
            cell_vectors_ang: [[1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]],
            positions: vec![Position {
                vector4: [0, 0, 0, 1],
                site_index: 0,
                stable: true,
                allowed_mask: 0b10,
            }],
            site_count: 1,
            environments: vec![EnvironmentModel {
                selection_particle_mask: 0b10,
                pairs: vec![PairLink {
                    target_position: PositionId::new(0),
                    offset: [1, 0, 0],
                    pair_id: 0,
                }],
                clusters,
                update_particle_ids: vec![1],
            }],
            interaction_range: [1, 0, 0, 0],
            static_trackers_per_cell: 0,
            global_tracker_count: 0,
        },
        energy: EnergyModel {
            pair_tables: vec![PairTable {
                object_id: "toy-pair".to_owned(),
                energies: pair,
            }],
            cluster_tables,
            defect_background: vec![0.0, 2.0],
        },
        transitions: TransitionModel {
            collections: Vec::new(),
            directions: Vec::new(),
            rules: Vec::new(),
            jump_count: vec![0; pc],
            jump_offsets: vec![0; pc + 1],
            jump_mapping: Vec::new(),
            static_tracker_mapping: vec![-1; pc],
            global_tracker_mapping: Vec::new(),
        },
        simulations: vec![SimulationModel {
            id: "toy-simulation".to_owned(),
            kind: SimulationKind::Mmc,
            jump_collection_ids: Vec::new(),
            default_settings: SimulationSettings {
                temperature_k: TOY_TEMPERATURE,
                target_mcsp: 100,
                write_out_count: 1,
                pre_run_mcsp: 0,
                time_limit_s: None,
                min_success_rate: None,
                electric_field_vm: TOY_FIELD_VM,
                electric_field_direction: [1.0, 0.0, 0.0],
                normalization_energy_ev: None,
                max_attempt_frequency_hz: None,
                break_tolerance: None,
                break_sample_length_mcsp: None,
                break_sample_interval_mcsp: None,
                result_sample_mcsp: None,
                flags: 0,
            },
            tracker_models: TrackerModels {
                global_tracker_indices: Vec::new(),
                static_tracker_indices: Vec::new(),
            },
        }],
        counts: ModelCounts {
            particle_count: particles,
            position_count: 1,
            site_count: 1,
            environment_count: 1,
            pair_table_count: 1,
            cluster_table_count: u32::from(with_cluster),
            collection_count: 0,
            direction_count: 0,
            rule_count: 0,
            simulation_count: 1,
            global_tracker_count: 0,
            static_trackers_per_cell: 0,
        },
    };

    JobPackage {
        job_id: 1,
        kind: SimulationKind::Mmc,
        model: Arc::new(model),
        lattice: InitialLattice {
            signature: "toy-lattice".to_owned(),
            lattice_size: [2, 1, 1],
            position_count: 1,
            particles: vec![ParticleId::new(1); 2],
        },
        settings: JobSettings {
            simulation_index: 0,
            temperature_k: TOY_TEMPERATURE,
            target_mcsp: 100,
            write_out_count: 1,
            pre_run_mcsp: 0,
            time_limit_s: None,
            min_success_rate: None,
            electric_field_vm: TOY_FIELD_VM,
            electric_field_direction: [1.0, 0.0, 0.0],
            normalization_energy_ev: None,
            max_attempt_frequency_hz: None,
            break_tolerance: None,
            break_sample_length_mcsp: None,
            break_sample_interval_mcsp: None,
            result_sample_mcsp: None,
            flags: SolverJobFlags::MMC_SIMULATION,
        },
        seed: [0; 32],
        optimizers: Vec::new(),
        extensions: Vec::new(),
        instruction: None,
        doping_info: String::new(),
    }
}

fn close(left: f64, right: f64) -> bool {
    (left - right).abs() <= 1e-9 * (1.0 + right.abs())
}

fn ev_to_kt(temperature_k: f64) -> f64 {
    1.0 / (K_B_EV * temperature_k)
}

#[test]
fn the_toy_site_and_pair_total_matches_the_hand_computation() {
    let context = initialize(toy_job(false));
    let scale = ev_to_kt(TOY_TEMPERATURE);

    assert_eq!(context.dimensions.environment_count(), 2);
    assert_eq!(context.energy.site.len(), 2);
    assert_eq!(context.energy.vectors.len(), 4);
    assert!(close(context.ev_to_kt(), scale));
    assert!(close(context.kt_ev(), K_B_EV * TOY_TEMPERATURE));

    // site(A) = 2 eV, pair(A, A) = 3 eV, two stable environments in two cells: 4 + 0.5 * 6 = 7 eV.
    assert!(close(
        context.node_energy(EnvId::new(0), ParticleId::new(1)),
        5.0 * scale
    ));
    assert!(close(
        context.node_energy(EnvId::new(1), ParticleId::new(1)),
        5.0 * scale
    ));
    assert!(close(
        context.node_energy(EnvId::new(0), ParticleId::new(0)),
        0.0
    ));
    assert!(close(
        context.site_energy(PositionId::new(0), ParticleId::new(1)),
        2.0 * scale
    ));
    assert!(close(context.lattice_energy_ev, 7.0));
    assert!(close(context.recompute_lattice_energy_kt(), 7.0 * scale));
    context.check_lattice_energy();

    assert!(close(
        context.field_scale(),
        TOY_FIELD_VM * 1e-10 / (K_B_EV * TOY_TEMPERATURE)
    ));
    assert_eq!(context.field_direction(), [1.0, 0.0, 0.0]);
    assert_eq!(
        context.cell_vectors(),
        [[1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]]
    );
}

#[test]
fn the_toy_cluster_total_counts_each_instance_once_per_cell() {
    let context = initialize(toy_job(true));
    let scale = ev_to_kt(TOY_TEMPERATURE);

    assert_eq!(context.clusters.instances.len(), 1);
    assert_eq!(context.clusters.instances[0].slot_count, 2);
    for cell in 0..2 {
        assert!(close(
            context.cluster_energy(0, cell, ParticleId::new(1)),
            10.0 * scale
        ));
        assert!(close(
            context.cluster_energy(0, cell, ParticleId::new(0)),
            0.0
        ));
    }

    // 7 eV site+pair + 10 eV cluster in each of the two cells.
    assert!(close(context.lattice_energy_ev, 27.0));
    assert!(close(context.recompute_lattice_energy_kt(), 27.0 * scale));
    context.check_lattice_energy();
}

#[test]
fn the_real_fixture_totals_match_the_site_backgrounds() {
    for (job, expected_ev) in [
        (ceria(), 0.0),
        (centered(), 0.0),
        (populated_kmc(), -3.2),
        (amend2(), -8.0),
    ] {
        let context = initialize(job);
        assert!(
            close(context.lattice_energy_ev, expected_ev),
            "expected {expected_ev} eV, found {}",
            context.lattice_energy_ev
        );
        assert_eq!(
            context.energy.site.len(),
            context.dimensions.position_particle_count() as usize
        );
        assert_eq!(
            context.energy.vectors.len(),
            context.dimensions.environment_count() as usize * context.dimensions.pc() as usize
        );
        assert!(close(
            context.recompute_lattice_energy_kt() * context.kt_ev(),
            context.lattice_energy_ev
        ));
        context.check_lattice_energy();
    }
}

#[test]
fn non_zero_pair_and_cluster_tables_match_a_brute_force_recomputation() {
    let mut job = populated_kmc();
    let positions = job.lattice.position_count as usize;
    let cells = job.lattice.particles.len() / positions;
    for cell in 0..cells {
        job.lattice.particles[cell * positions + 1] = ParticleId::new(1);
    }

    let job = with_model(job, |model| {
        for (index, value) in model.energy.defect_background.iter_mut().enumerate() {
            *value = (index as f64 - 2.0) * 0.01;
        }
        for (table, pair) in model.energy.pair_tables.iter_mut().enumerate() {
            for (index, value) in pair.energies.iter_mut().enumerate() {
                *value = (table * 100 + index) as f64 * 0.001;
            }
        }
        for (index, value) in model.energy.cluster_tables[0]
            .energies
            .iter_mut()
            .enumerate()
        {
            *value = (index as f64 + 1.0) * 0.1;
        }
    });

    let context = initialize(job);
    let dimensions = &context.dimensions;
    let particles = dimensions.pc() as usize;
    let scale = context.ev_to_kt();
    let model = &context.job.model;

    let mut expected_ev = 0.0;
    for environment in 0..dimensions.environment_count() {
        let env = EnvId::new(environment);
        let position = dimensions.position_of(env) as usize;
        let cell = dimensions.cell_of(env);
        let particle = context.lattice.particle(env).get() as usize;

        for particle_id in 0..dimensions.pc() {
            let particle = ParticleId::new(particle_id as u8);
            let mut value =
                model.energy.defect_background[position * particles + particle_id as usize] * scale;
            for link in context.link_range(PositionId::new(position as u32)) {
                let partner = dimensions.wrapped_env(cell, link.offset, link.partner_position);
                let partner_particle = context.lattice.particle(partner).get() as usize;
                value += model.energy.pair_tables[link.pair_id as usize].energies
                    [particle_id as usize * particles + partner_particle]
                    * scale;
            }
            assert!(close(context.node_energy(env, particle), value));
        }

        if context.lattice.is_stable(env) {
            expected_ev += model.energy.defect_background[position * particles + particle];
            for link in context.link_range(PositionId::new(position as u32)) {
                let partner = dimensions.wrapped_env(cell, link.offset, link.partner_position);
                let partner_particle = context.lattice.particle(partner).get() as usize;
                expected_ev += 0.5
                    * model.energy.pair_tables[link.pair_id as usize].energies
                        [particle * particles + partner_particle];
            }
        }
    }

    for (instance, instance_data) in context.clusters.instances.iter().enumerate() {
        let table = &model.energy.cluster_tables[instance_data.table as usize];
        for cell in 0..dimensions.cell_count() {
            for particle in 0..dimensions.pc() {
                let particle_id = ParticleId::new(particle as u8);
                let expected = context
                    .cluster_row(instance as u32, particle_id)
                    .map(|_row| {
                        let code = context.cluster_code(instance as u32, cell);
                        let raw = (code & !0x3F) | u64::from(particle_id.get());
                        let id = context
                            .cluster_lookup(instance as u32, raw)
                            .expect("the mutated table resolves");
                        table.energies[id as usize] * scale
                    })
                    .unwrap_or(0.0);
                assert!(close(
                    context.cluster_energy(instance as u32, cell, particle_id),
                    expected
                ));
            }
            let centre = dimensions.cell_env(cell, instance_data.centre_position);
            let particle = context.lattice.particle(centre);
            if context.cluster_row(instance as u32, particle).is_some() {
                let code = context.cluster_code(instance as u32, cell);
                let raw = (code & !0x3F) | u64::from(particle.get());
                let id = context
                    .cluster_lookup(instance as u32, raw)
                    .expect("lookup");
                expected_ev += table.energies[id as usize];
            }
        }
    }

    assert!(
        close(context.lattice_energy_ev, expected_ev),
        "expected {expected_ev} eV, found {}",
        context.lattice_energy_ev
    );
    assert!(close(
        context.recompute_lattice_energy_kt() * context.kt_ev(),
        context.lattice_energy_ev
    ));
    context.check_lattice_energy();
}

#[test]
fn a_non_canonical_cluster_tuple_uses_the_canonical_energy() {
    let mut job = amend2();
    let positions = job.lattice.position_count as usize;
    let cells = job.lattice.particles.len() / positions;
    for cell in 0..cells {
        job.lattice.particles[cell * positions] = ParticleId::new(2);
        job.lattice.particles[cell * positions + 1] = ParticleId::new(2);
        job.lattice.particles[cell * positions + 4] = ParticleId::new(3);
    }

    let job = with_model(job, |model| {
        model.energy.cluster_tables[0].energies = vec![1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    });
    let context = initialize(job);
    let scale = context.ev_to_kt();

    for cell in 0..context.dimensions.cell_count() {
        // slot 0 = 2, slot 1 = 3, slot 2 = 2 -> raw 8386 -> canonical index 2.
        assert!(close(
            context.cluster_energy(0, cell, ParticleId::new(2)),
            3.0 * scale
        ));
        // slot 0 = 3 -> raw 8387 -> canonical index 3.
        assert!(close(
            context.cluster_energy(0, cell, ParticleId::new(3)),
            4.0 * scale
        ));
        assert!(close(
            context.cluster_energy(0, cell, ParticleId::new(0)),
            0.0
        ));
        assert_eq!(context.cluster_code_id(0, cell), Some(2));
    }

    // Stable sites: P1 (particle 2) contributes -0.5 eV per cell, P4 (particle 3) has no site entry;
    // the cluster contributes the canonical 3 eV per cell: -4.0 + 24.0 = 20.0 eV.
    assert!(close(context.lattice_energy_ev, 20.0));
    context.check_lattice_energy();
}

#[test]
fn the_energy_runtime_is_deterministic() {
    let first = initialize(populated_kmc());
    let second = initialize(populated_kmc());

    assert_eq!(first.energy, second.energy);
    assert_eq!(first.lattice_energy_ev, second.lattice_energy_ev);
    assert_eq!(
        first.recompute_lattice_energy_kt(),
        second.recompute_lattice_energy_kt()
    );
}

#[test]
fn the_energy_vector_cap_rejects_before_allocation() {
    let mut options = ContextOptions::default();
    options.limits.max_energy_vector_bytes = 863;
    let error = SimulationContext::initialize(populated_kmc(), &options, None)
        .expect_err("the vectors need 864 bytes");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_energy_vector_bytes",
            value: 864,
            limit: 863,
        }
    ));
    assert_eq!(error.exit_code().code(), 12);

    let mut exact = ContextOptions::default();
    exact.limits.max_energy_vector_bytes = 864;
    assert!(SimulationContext::initialize(populated_kmc(), &exact, None).is_ok());
}

#[test]
fn a_non_finite_temperature_or_field_is_rejected() {
    type SettingsMutation = fn(&mut JobPackage);
    let cases: [(&str, SettingsMutation); 4] = [
        ("temperature", |job| job.settings.temperature_k = 0.0),
        ("temperature", |job| {
            job.settings.temperature_k = f64::INFINITY
        }),
        ("electric field", |job| {
            job.settings.electric_field_vm = f64::INFINITY;
        }),
        ("field direction", |job| {
            job.settings.electric_field_direction = [f64::NAN, 0.0, 0.0];
        }),
    ];

    for (what, mutate) in cases {
        let mut job = toy_job(false);
        mutate(&mut job);
        let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
            .expect_err("the factor is not finite");
        match error {
            ContextError::NonFiniteFactor { what: actual } => assert_eq!(actual, what),
            other => panic!("unexpected error {other}"),
        }
        assert_eq!(error.exit_code().code(), 13);
    }
}

// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-4 KMC jump status.

mod support;

use std::sync::Arc;

use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, PositionId, SimulationContext,
};
use mocassin_types::{
    EnergyModel, EnvironmentModel, InitialLattice, JobPackage, JobSettings, JumpCollection,
    JumpCollectionId, JumpDirection, JumpDirectionId, ModelCounts, PairLink, PairTable,
    ParameterizedModel, ParticleId, ParticleTable, Position, SimulationKind, SimulationModel,
    SimulationSettings, SolverJobFlags, StructureModel, TrackerModels, TransitionModel,
};

use support::{amend2, centered, initialize, populated_kmc, populated_mmc, with_model};

fn payload_status(
    job: &JobPackage,
    dimensions: &mocassin_solver::context::Dimensions,
    direction: &JumpDirection,
    position: u32,
) -> Vec<(u8, u8, u32)> {
    let selected = dimensions.cell_env(0, position);
    let nodes: Vec<EnvId> = (0..direction.path.len())
        .map(|node| {
            if node == 0 {
                selected
            } else {
                let encoding = direction.path[node];
                dimensions.wrapped_env(
                    dimensions.cell_of(selected),
                    [encoding[0], encoding[1], encoding[2]],
                    u32::try_from(encoding[3]).expect("a valid path position"),
                )
            }
        })
        .collect();
    let stable =
        |env: EnvId| job.model.structure.positions[dimensions.position_of(env) as usize].stable;

    let mut status = Vec::new();
    for sender in 0..nodes.len() {
        if !stable(nodes[sender]) {
            continue;
        }
        for receiver in 0..nodes.len() {
            if sender == receiver || !stable(nodes[receiver]) {
                continue;
            }

            let receiver_position = dimensions.position_of(nodes[receiver]) as usize;
            let mut pairs = job.model.structure.environments[receiver_position]
                .pairs
                .clone();
            pairs.sort_by(|left, right| {
                (left.pair_id, left.target_position.get(), left.offset).cmp(&(
                    right.pair_id,
                    right.target_position.get(),
                    right.offset,
                ))
            });

            for (local, pair) in pairs.iter().enumerate() {
                let partner = dimensions.wrapped_env(
                    dimensions.cell_of(nodes[receiver]),
                    pair.offset,
                    pair.target_position.get(),
                );
                if partner == nodes[sender] {
                    status.push((sender as u8, receiver as u8, local as u32));
                    break;
                }
            }
        }
    }

    status
}

fn status_tuples(
    context: &SimulationContext,
    position: PositionId,
    direction: u32,
) -> Vec<(u8, u8, u32)> {
    context
        .jump_links(position, direction)
        .iter()
        .map(|link| (link.sender_path_id, link.receiver_path_id, link.link_id))
        .collect()
}

#[test]
fn the_populated_kmc_jump_status_matches_a_brute_force_recomputation() {
    let job = populated_kmc();
    let context = initialize(job.clone());
    let positions = context.dimensions.p();
    let directions = context.job.model.transitions.directions.len();

    assert_eq!(
        context.jumps.status_count(),
        positions as usize * directions
    );
    assert_eq!(context.jumps.links.len(), 4);

    for direction_index in 0..directions as u32 {
        let direction = &job.model.transitions.directions[direction_index as usize];
        for position in 0..positions {
            let actual = status_tuples(&context, PositionId::new(position), direction_index);
            if !context.direction_is_active(direction_index) {
                assert!(actual.is_empty(), "inactive direction {direction_index}");
                continue;
            }
            let expected = payload_status(&job, &context.dimensions, direction, position);
            assert_eq!(
                actual, expected,
                "direction {direction_index} position {position}"
            );
        }
    }

    assert!(context.jump_status(6).is_empty());
    assert!(context.jump_status(11).is_empty());
    assert!(context.direction_is_active(0));
    assert!(!context.direction_is_active(2));
}

#[test]
fn the_amend_2_jump_status_matches_the_brute_force_recomputation() {
    let job = amend2();
    let context = initialize(job.clone());
    let positions = context.dimensions.p();

    for direction_index in 0..4_u32 {
        let direction = &job.model.transitions.directions[direction_index as usize];
        for position in 0..positions {
            let expected = payload_status(&job, &context.dimensions, direction, position);
            assert_eq!(
                status_tuples(&context, PositionId::new(position), direction_index),
                expected,
                "direction {direction_index} position {position}"
            );
        }
    }

    assert_eq!(context.jumps.links.len(), 8);
}

#[test]
fn an_mmc_context_has_no_jump_status() {
    for job in [populated_mmc(), centered()] {
        let context = initialize(job);
        assert!(context.jumps.is_empty());
        assert_eq!(context.jumps.status_count(), 0);
        assert!(context.jump_status(0).is_empty());
    }
}

#[test]
fn the_receiver_filter_is_structural_not_initial_occupation() {
    let unstable = initialize(populated_kmc());
    assert!(
        unstable
            .jumps
            .links
            .iter()
            .all(|link| link.sender_path_id != 1 && link.receiver_path_id != 1)
    );

    let stable = with_model(populated_kmc(), |model| {
        model.structure.positions[1].stable = true;
    });
    let context = initialize(stable);

    let links = context.jump_links(PositionId::new(0), 0);
    assert!(
        links
            .iter()
            .any(|link| link.sender_path_id == 1 || link.receiver_path_id == 1)
    );
    assert_eq!(
        context.lattice.particle(context.dimensions.cell_env(0, 1)),
        ParticleId::new(0),
        "the included receiver is void at initialization"
    );
}

#[test]
fn receiver_links_carry_the_cluster_associations() {
    let job = with_model(populated_kmc(), |model| {
        model.structure.positions[1].stable = true;
    });
    let context = initialize(job);
    let links = context.jump_links(PositionId::new(0), 0);

    let receiver_one = links
        .iter()
        .find(|link| link.receiver_path_id == 1 && link.sender_path_id == 0)
        .expect("P1 receives the P0 sender");
    let receiver_env = context
        .resolve_node(
            context.dimensions.cell_env(0, 0),
            &context.job.model.transitions.directions[0],
            receiver_one.receiver_path_id,
        )
        .expect("receiver node");
    let receiver_links = context.link_range(PositionId::new(
        context.dimensions.position_of(receiver_env),
    ));
    let link = &receiver_links[receiver_one.link_id as usize];
    assert_eq!(link.cluster_len, 1);
    let assoc = context.links.cluster_assoc[link.cluster_start as usize];
    assert_eq!(assoc.instance, 0);
    assert_eq!(assoc.slot, 1);

    let receiver_one_from_p2 = links
        .iter()
        .find(|link| link.receiver_path_id == 1 && link.sender_path_id == 2)
        .expect("P1 receives the P2 sender");
    let link = &receiver_links[receiver_one_from_p2.link_id as usize];
    let assoc = context.links.cluster_assoc[link.cluster_start as usize];
    assert_eq!(assoc.slot, 2);
}

#[test]
fn resolve_node_replaces_the_selected_bucket_and_wraps_relative_cells() {
    let job = amend2();
    let context = initialize(job);
    let direction = &context.job.model.transitions.directions[1];
    let selected = context.dimensions.cell_env(0, 4);

    assert_eq!(context.path_len(direction), 3);
    assert_eq!(context.resolve_node(selected, direction, 0), Some(selected));
    assert_eq!(
        context.resolve_node(selected, direction, 1),
        Some(context.dimensions.cell_env(4, 0))
    );
    assert_eq!(
        context.resolve_node(selected, direction, 2),
        Some(context.dimensions.cell_env(0, 1))
    );
    assert_eq!(context.resolve_node(selected, direction, 3), None);

    let mut path = [EnvId::new(0); 10];
    assert_eq!(context.resolve_path(selected, direction, &mut path), 3);
    assert_eq!(path[0], selected);
    assert_eq!(path[1], context.dimensions.cell_env(4, 0));
    assert_eq!(path[2], context.dimensions.cell_env(0, 1));

    let mut too_short = [EnvId::new(0); 1];
    assert_eq!(context.resolve_path(selected, direction, &mut too_short), 0);

    let centered = initialize(centered());
    let cross_bucket = &centered.job.model.transitions.directions[3];
    let selected = centered.dimensions.cell_env(0, 2);
    assert_eq!(cross_bucket.position_id, 6);
    assert_eq!(
        centered.resolve_node(selected, cross_bucket, 0),
        Some(selected)
    );
    assert_eq!(
        centered.resolve_node(selected, cross_bucket, 1),
        Some(centered.dimensions.cell_env(0, 7))
    );
}

#[test]
fn the_jump_status_caps_reject_before_allocation() {
    let job = populated_kmc();

    let mut options = ContextOptions::default();
    options.limits.max_status_count = 11;
    let error = SimulationContext::initialize(job.clone(), &options, None)
        .expect_err("12 statuses exceed the cap");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_status_count",
            value: 12,
            limit: 11,
        }
    ));

    let mut options = ContextOptions::default();
    options.limits.max_jump_links = 3;
    let error = SimulationContext::initialize(job.clone(), &options, None)
        .expect_err("4 jump links exceed the cap");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_jump_links",
            value: 4,
            limit: 3,
        }
    ));

    let mut options = ContextOptions::default();
    options.limits.max_jump_status_bytes = 83;
    let error = SimulationContext::initialize(job, &options, None)
        .expect_err("84 status bytes exceed the cap");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_jump_status_bytes",
            value: 84,
            limit: 83,
        }
    ));
}

#[test]
fn an_invalid_jump_path_is_rejected() {
    let bad_position = with_model(populated_kmc(), |model| {
        model.transitions.directions[0].path[1][3] = 3;
    });
    let error = SimulationContext::initialize(bad_position, &ContextOptions::default(), None)
        .expect_err("the path node position is outside the cell");
    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "jump path position",
            ..
        }
    ));

    let too_long = with_model(populated_kmc(), |model| {
        model.transitions.directions[0].path = vec![[0, 0, 0, 0]; 11];
    });
    let error = SimulationContext::initialize(too_long, &ContextOptions::default(), None)
        .expect_err("an 11-node path exceeds max_path_nodes");
    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "jump path",
            ..
        }
    ));
}

#[test]
fn the_jump_status_is_deterministic() {
    let first = initialize(populated_kmc());
    let second = initialize(populated_kmc());
    assert_eq!(first.jumps, second.jumps);
}

fn ten_node_job() -> JobPackage {
    let positions = 10_u32;
    let pc = 2_u32;
    let particle_count = 2_u32;

    let environments = (0..positions)
        .map(|position| EnvironmentModel {
            selection_particle_mask: 0b10,
            pairs: (0..positions)
                .filter(|target| *target != position)
                .map(|target| PairLink {
                    target_position: PositionId::new(target),
                    offset: [0, 0, 0],
                    pair_id: 0,
                })
                .collect(),
            clusters: Vec::new(),
            update_particle_ids: vec![1],
        })
        .collect();

    let direction = JumpDirection {
        object_id: "ten-node".to_owned(),
        position_id: 0,
        collection_id: JumpCollectionId::new(0),
        path: (0..10).map(|node| [0, 0, 0, node]).collect(),
        movement: vec![[0.0; 3]; 10],
        path_vector: [0.0; 3],
        length: 0.0,
        is_inverse: false,
    };

    let mut jump_count = vec![0_u32; positions as usize * pc as usize];
    jump_count[1] = 1;
    let mut jump_offsets = vec![0_u32; jump_count.len() + 1];
    for (index, count) in jump_count.iter().enumerate() {
        jump_offsets[index + 1] = jump_offsets[index] + count;
    }

    let model = ParameterizedModel {
        format_version: 1,
        signature: "ten-node".to_owned(),
        model_signature: "ten-node".to_owned(),
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
            positions: (0..positions)
                .map(|position| Position {
                    vector4: [0, 0, 0, position as i32],
                    site_index: 0,
                    stable: true,
                    allowed_mask: 0b10,
                })
                .collect(),
            site_count: 1,
            environments,
            interaction_range: [0, 0, 0, 0],
            static_trackers_per_cell: 0,
            global_tracker_count: 0,
        },
        energy: EnergyModel {
            pair_tables: vec![PairTable {
                object_id: "ten-node-pair".to_owned(),
                energies: vec![0.0; (pc * pc) as usize],
            }],
            cluster_tables: Vec::new(),
            defect_background: vec![0.0; (positions * pc) as usize],
        },
        transitions: TransitionModel {
            collections: vec![JumpCollection {
                kind: SimulationKind::Kmc,
                transition_id: "ten-node".to_owned(),
                mobile_particle_mask: 0b10,
                rule_ids: Vec::new(),
                direction_ids: vec![JumpDirectionId::new(0)],
            }],
            directions: vec![direction],
            rules: Vec::new(),
            jump_count,
            jump_offsets,
            jump_mapping: vec![0],
            static_tracker_mapping: vec![-1; (positions * pc) as usize],
            global_tracker_mapping: Vec::new(),
        },
        simulations: vec![SimulationModel {
            id: "ten-node".to_owned(),
            kind: SimulationKind::Kmc,
            jump_collection_ids: vec![0],
            default_settings: SimulationSettings {
                temperature_k: 1000.0,
                target_mcsp: 100,
                write_out_count: 1,
                pre_run_mcsp: 0,
                time_limit_s: None,
                min_success_rate: None,
                electric_field_vm: 0.0,
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
            particle_count,
            position_count: positions,
            site_count: 1,
            environment_count: positions,
            pair_table_count: 1,
            cluster_table_count: 0,
            collection_count: 1,
            direction_count: 1,
            rule_count: 0,
            simulation_count: 1,
            global_tracker_count: 0,
            static_trackers_per_cell: 0,
        },
    };

    JobPackage {
        job_id: 1,
        kind: SimulationKind::Kmc,
        model: Arc::new(model),
        lattice: InitialLattice {
            signature: "ten-node".to_owned(),
            lattice_size: [1, 1, 1],
            position_count: positions,
            particles: vec![ParticleId::new(1); positions as usize],
        },
        settings: JobSettings {
            simulation_index: 0,
            temperature_k: 1000.0,
            target_mcsp: 100,
            write_out_count: 1,
            pre_run_mcsp: 0,
            time_limit_s: None,
            min_success_rate: None,
            electric_field_vm: 0.0,
            electric_field_direction: [1.0, 0.0, 0.0],
            normalization_energy_ev: None,
            max_attempt_frequency_hz: None,
            break_tolerance: None,
            break_sample_length_mcsp: None,
            break_sample_interval_mcsp: None,
            result_sample_mcsp: None,
            flags: SolverJobFlags::KMC_SIMULATION,
        },
        seed: [0; 32],
        optimizers: Vec::new(),
        extensions: Vec::new(),
        instruction: None,
        doping_info: String::new(),
    }
}

#[test]
fn a_ten_node_direction_stays_within_the_derived_bound() {
    let context = initialize(ten_node_job());
    let links = context.jump_links(PositionId::new(0), 0);

    assert_eq!(
        context.path_len(&context.job.model.transitions.directions[0]),
        10
    );
    assert_eq!(links.len(), 90);
    assert!(links.len() <= 10 * 9);
    for link in links {
        assert_ne!(link.sender_path_id, link.receiver_path_id);
        assert!(link.sender_path_id < 10);
        assert!(link.receiver_path_id < 10);
    }
    assert_eq!(context.pools.selectable_jump_count, 1);
}

// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-2 cluster runtime and cluster-code resolution.

mod support;

use mocassin_solver::context::{
    ClusterInstance, ContextError, ContextOptions, Dimensions, EnvId, NO_CLUSTER_CODE, PositionId,
    SimulationContext,
};
use mocassin_types::{ClusterLink, ClusterTable, JobPackage, PairLink, ParticleId};

use support::{amend2, centered, initialize, populated_kmc, populated_mmc, pre_map, with_model};

fn raw_code_from_geometry(context: &SimulationContext, instance: u32, cell: u32) -> u64 {
    let instance_data = &context.clusters.instances[instance as usize];
    let start = instance_data.slot_start as usize;
    let mut code = 0_u64;

    for slot in 0..instance_data.slot_count {
        let environment = if slot == 0 {
            context
                .dimensions
                .cell_env(cell, instance_data.centre_position)
        } else {
            let index = start + usize::from(slot);
            context.dimensions.wrapped_env(
                cell,
                context.clusters.slot_cells[index],
                context.clusters.slot_positions[index],
            )
        };
        code |= u64::from(context.lattice.particle(environment).get()) << (u32::from(slot) * 6);
    }

    code
}

fn expect_geometry_error(job: JobPackage, slot: u8, centre: u32) {
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("the hostile geometry fails closed");
    match error {
        ContextError::ClusterGeometry {
            cluster_table,
            instance,
            slot: actual_slot,
            ..
        } => {
            assert_eq!(cluster_table, 0);
            assert_eq!(actual_slot, slot);
            assert_eq!(instance, centre);
        }
        other => panic!("unexpected error {other}"),
    }
}

#[test]
fn the_populated_cluster_resolves_one_instance_and_its_associations() {
    let context = initialize(populated_kmc());

    assert_eq!(context.clusters.instances.len(), 1);
    assert_eq!(
        context.clusters.instances[0],
        ClusterInstance {
            table: 0,
            centre_position: 1,
            slot_start: 0,
            slot_count: 3,
        }
    );
    assert_eq!(context.clusters.slot_positions, [1, 0, 2]);
    assert_eq!(
        context.clusters.slot_cells,
        [[0, 0, 0], [0, 0, 0], [0, 0, 0]]
    );
    assert_eq!(context.centres(PositionId::new(1)), [0]);
    assert!(context.centres(PositionId::new(0)).is_empty());
    assert_eq!(context.clusters.energy.len(), 16);
    assert!(context.clusters.energy.iter().all(|value| *value == 0.0));

    let p1 = context.link_range(PositionId::new(1));
    assert_eq!(p1.len(), 2);
    assert_eq!(p1[0].cluster_start, 0);
    assert_eq!(p1[0].cluster_len, 1);
    assert_eq!(p1[1].cluster_start, 1);
    assert_eq!(p1[1].cluster_len, 1);
    assert_eq!(context.links.cluster_assoc.len(), 2);
    assert_eq!(context.links.cluster_assoc[0].instance, 0);
    assert_eq!(context.links.cluster_assoc[0].slot, 1);
    assert_eq!(context.links.cluster_assoc[1].instance, 0);
    assert_eq!(context.links.cluster_assoc[1].slot, 2);
}

#[test]
fn the_initial_codes_match_a_brute_force_rebuild() {
    for job in [populated_kmc(), amend2()] {
        let context = initialize(job);
        let cells = context.dimensions.cell_count();

        for instance in 0..context.clusters.instances.len() as u32 {
            for cell in 0..cells {
                let expected = raw_code_from_geometry(&context, instance, cell);
                assert_eq!(context.cluster_code(instance, cell), expected);
                assert_eq!(context.occupation_code(instance, cell, &[]), expected);
            }
        }
    }
}

#[test]
fn the_centre_particle_selects_the_cluster_row() {
    let mut job = populated_kmc();
    let positions = job.lattice.position_count as usize;
    let cells = job.lattice.particles.len() / positions;
    for cell in 0..cells {
        job.lattice.particles[cell * positions + 1] = ParticleId::new(1);
    }
    let context = initialize(job);

    assert_eq!(context.cluster_code(0, 0), 4161);
    assert_eq!(context.cluster_code_id(0, 0), Some(0));
    assert_eq!(context.cluster_row(0, ParticleId::new(1)), Some(0));
    assert_eq!(context.cluster_row(0, ParticleId::new(2)), Some(1));
    assert_eq!(context.cluster_row(0, ParticleId::new(0)), None);
    assert_eq!(context.cluster_row(0, ParticleId::new(3)), None);
    assert_eq!(context.cluster_row(9, ParticleId::new(1)), None);
}

#[test]
fn the_mapped_centre_particle_resolves_the_energy_row_lookup() {
    let context = initialize(populated_kmc());
    assert_eq!(context.cluster_lookup(0, 4161).expect("lookup"), 0);
    assert_eq!(context.cluster_lookup(0, 4162).expect("lookup"), 1);
    assert!(matches!(
        context.cluster_lookup(0, 8386),
        Err(ContextError::ExpansionMapAbsent { .. })
    ));
    assert!(matches!(
        context.cluster_lookup(0, 0),
        Err(ContextError::Invariant { .. })
    ));
}

#[test]
fn the_amendment_2_fixture_resolves_all_eight_raw_assignments() {
    let context = initialize(amend2());
    let expected = [
        (8322, 0),
        (8323, 1),
        (8386, 2),
        (8387, 3),
        (12418, 2),
        (12419, 3),
        (12482, 4),
        (12483, 5),
    ];

    for (raw, canonical) in expected {
        assert_eq!(
            context.cluster_lookup(0, raw).expect("lookup"),
            canonical,
            "raw {raw}"
        );
    }

    assert_eq!(context.cluster_code(0, 0), 8320);
    assert_eq!(context.cluster_code_id(0, 0), None);
    assert_eq!(context.clusters.slot_positions, [0, 4, 1]);
    assert_eq!(
        context.clusters.slot_cells,
        [[0, 0, 0], [-1, 0, 0], [0, 0, 0]]
    );
}

#[test]
fn the_pre_map_fixture_fails_closed_for_the_non_canonical_tuples() {
    let context = initialize(pre_map());

    assert_eq!(context.cluster_lookup(0, 8322).expect("lookup"), 0);
    assert_eq!(context.cluster_lookup(0, 12418).expect("lookup"), 2);
    for raw in [8386, 8387] {
        match context.cluster_lookup(0, raw) {
            Err(ContextError::ExpansionMapAbsent {
                cluster_table,
                code,
            }) => {
                assert_eq!(cluster_table, 0);
                assert_eq!(code, raw);
            }
            other => panic!("raw {raw}: unexpected {other:?}"),
        }
    }
}

#[test]
fn a_pre_map_package_whose_reached_tuple_is_non_canonical_fails_at_initialization() {
    let mut job = pre_map();
    let positions = job.lattice.position_count as usize;
    let cells = job.lattice.particles.len() / positions;
    for cell in 0..cells {
        job.lattice.particles[cell * positions + 4] = ParticleId::new(3);
    }

    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("the reached raw tuple is non-canonical without a map");
    match error {
        ContextError::ExpansionMapAbsent {
            cluster_table,
            code,
        } => {
            assert_eq!(cluster_table, 0);
            assert_eq!(code, 8386);
        }
        other => panic!("unexpected error {other}"),
    }

    let mut with_map = amend2();
    for cell in 0..cells {
        with_map.lattice.particles[cell * positions + 4] = ParticleId::new(3);
    }
    let context = initialize(with_map);
    assert_eq!(context.cluster_code(0, 0), 8384);
    assert_eq!(context.cluster_code_id(0, 0), None);
    assert_eq!(context.cluster_lookup(0, 8386).expect("lookup"), 2);
}

#[test]
fn a_present_map_missing_a_tuple_is_a_missing_occupation_code() {
    let job = with_model(amend2(), |model| {
        let expansions = model.energy.cluster_tables[0]
            .expansions
            .as_mut()
            .expect("the amendment-2 table has a map");
        let index = expansions
            .expanded_codes
            .iter()
            .position(|code| *code == 8386)
            .expect("8386 is in the map");
        expansions.expanded_codes.remove(index);
        expansions.canonical_indices.remove(index);
    });
    let context = initialize(job);

    match context.cluster_lookup(0, 8386) {
        Err(ContextError::MissingOccupationCode {
            cluster_table,
            code,
        }) => {
            assert_eq!(cluster_table, 0);
            assert_eq!(code, 8386);
        }
        other => panic!("unexpected {other:?}"),
    }
    assert_eq!(context.cluster_lookup(0, 8387).expect("lookup"), 3);
}

fn multi_centre_job() -> JobPackage {
    let mut job = centered();
    for particle in &mut job.lattice.particles {
        *particle = ParticleId::new(1);
    }

    with_model(job, |model| {
        model.energy.cluster_tables.push(ClusterTable {
            object_id: "synthetic-multi-centre".to_owned(),
            occupation_codes: vec![65],
            energies: vec![0.0],
            particle_table_mapping: vec![1],
            expansions: None,
        });
        model.counts.cluster_table_count = 1;
        for centre in [0, 3, 4, 7] {
            model.structure.environments[centre]
                .clusters
                .push(ClusterLink {
                    cluster_id: 0,
                    byte_position: 0,
                });
        }
        model.structure.environments[1].clusters.push(ClusterLink {
            cluster_id: 0,
            byte_position: 1,
        });
    })
}

#[test]
fn a_multi_centre_table_yields_one_instance_per_slot_zero_link() {
    let context = initialize(multi_centre_job());

    assert_eq!(context.clusters.instances.len(), 4);
    let expected_slots = [[0_u32, 1_u32], [3, 2], [4, 5], [7, 6]];
    for (index, expected) in expected_slots.into_iter().enumerate() {
        let instance = &context.clusters.instances[index];
        assert_eq!(instance.centre_position, expected[0]);
        assert_eq!(instance.slot_count, 2);
        let start = instance.slot_start as usize;
        assert_eq!(
            &context.clusters.slot_positions[start..start + 2],
            &expected
        );
        assert_eq!(
            context.centres(PositionId::new(expected[0])),
            [index as u32]
        );
    }

    for instance in 0..4 {
        for cell in 0..context.dimensions.cell_count() {
            assert_eq!(context.cluster_code(instance, cell), 65);
            assert_eq!(context.cluster_code_id(instance, cell), Some(0));
        }
    }
    assert_eq!(context.links.cluster_assoc.len(), 4);
    assert_eq!(context.cluster_lookup(0, 65).expect("lookup"), 0);

    let assoc_links: Vec<_> = (0..context.dimensions.p())
        .flat_map(|position| {
            let position_id = PositionId::new(position);
            context
                .link_range(position_id)
                .iter()
                .enumerate()
                .filter(|(_, link)| link.cluster_len == 1)
                .map(move |(local, link)| (position, local as u32, link.cluster_start))
        })
        .collect();
    assert_eq!(assoc_links.len(), 4);
    for (position, local, assoc) in assoc_links {
        let instance = context.links.cluster_assoc[assoc as usize].instance;
        assert_eq!(
            context.clusters.instances[instance as usize].centre_position,
            position
        );
        assert_eq!(context.links.cluster_assoc[assoc as usize].slot, 1);
        assert_eq!(
            context.clusters.slot_positions
                [context.clusters.instances[instance as usize].slot_start as usize + 1],
            context.link_range(PositionId::new(position))[local as usize].partner_position
        );
    }
}

#[test]
fn a_forward_link_may_carry_associations_from_several_instances() {
    let job = with_model(populated_kmc(), |model| {
        model.energy.cluster_tables.push(ClusterTable {
            object_id: "synthetic-shared-link".to_owned(),
            occupation_codes: vec![65, 66],
            energies: vec![0.0, 0.0],
            particle_table_mapping: vec![1, 2],
            expansions: None,
        });
        model.counts.cluster_table_count = 2;
        model.structure.environments[1].clusters.push(ClusterLink {
            cluster_id: 1,
            byte_position: 0,
        });
        model.structure.environments[0].clusters.push(ClusterLink {
            cluster_id: 1,
            byte_position: 1,
        });
    });
    let context = initialize(job);

    assert_eq!(context.clusters.instances.len(), 2);
    let p1 = context.link_range(PositionId::new(1));
    let to_p0 = p1
        .iter()
        .position(|link| link.partner_position == 0)
        .expect("P1 links to P0");
    let to_p2 = p1
        .iter()
        .position(|link| link.partner_position == 2)
        .expect("P1 links to P2");

    assert_eq!(p1[to_p0].cluster_len, 2);
    assert_eq!(p1[to_p2].cluster_len, 1);
    let start = p1[to_p0].cluster_start as usize;
    assert_eq!(context.links.cluster_assoc[start].instance, 0);
    assert_eq!(context.links.cluster_assoc[start].slot, 1);
    assert_eq!(context.links.cluster_assoc[start + 1].instance, 1);
    assert_eq!(context.links.cluster_assoc[start + 1].slot, 1);
    assert_eq!(
        context.links.cluster_assoc[p1[to_p2].cluster_start as usize].instance,
        0
    );
}

#[test]
fn hostile_cluster_geometry_fails_closed() {
    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[0].clusters.push(ClusterLink {
                cluster_id: 0,
                byte_position: 0,
            });
        }),
        0,
        0,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[0]
                .clusters
                .retain(|link| !(link.cluster_id == 0 && link.byte_position == 1));
        }),
        1,
        0,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[0].clusters.push(ClusterLink {
                cluster_id: 0,
                byte_position: 1,
            });
        }),
        1,
        0,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[1]
                .pairs
                .retain(|pair| pair.target_position.get() != 0);
        }),
        1,
        1,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[1].pairs.push(PairLink {
                target_position: PositionId::new(0),
                offset: [1, 0, 0],
                pair_id: 0,
            });
        }),
        1,
        1,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[0].clusters.push(ClusterLink {
                cluster_id: 0,
                byte_position: 9,
            });
        }),
        9,
        0,
    );

    expect_geometry_error(
        with_model(populated_kmc(), |model| {
            model.structure.environments[2]
                .clusters
                .retain(|link| !(link.cluster_id == 0 && link.byte_position == 2));
            model.structure.environments[0].clusters.push(ClusterLink {
                cluster_id: 0,
                byte_position: 2,
            });
        }),
        2,
        1,
    );
}

#[test]
fn a_multi_centre_member_image_must_exist_and_be_unique() {
    let absent = with_model(multi_centre_job(), |model| {
        model.structure.environments[4]
            .pairs
            .retain(|pair| pair.target_position.get() != 5);
    });
    expect_geometry_error(absent, 1, 4);

    let ambiguous = with_model(multi_centre_job(), |model| {
        model.structure.environments[4].pairs.push(PairLink {
            target_position: PositionId::new(6),
            offset: [0, 0, 0],
            pair_id: 2,
        });
    });
    expect_geometry_error(ambiguous, 1, 4);
}

#[test]
fn a_cluster_link_to_a_missing_table_is_rejected() {
    let job = with_model(populated_kmc(), |model| {
        model.structure.environments[0].clusters.push(ClusterLink {
            cluster_id: 9,
            byte_position: 0,
        });
    });
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("cluster table 9 does not exist");
    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "cluster link table",
            ..
        }
    ));
}

#[test]
fn the_mmc_job_builds_the_same_links_and_clusters() {
    let kmc = initialize(populated_kmc());
    let mmc = initialize(populated_mmc());

    assert_eq!(kmc.links, mmc.links);
    assert_eq!(kmc.clusters, mmc.clusters);
    assert_eq!(mmc.kind, mocassin_types::SimulationKind::Mmc);
}

#[test]
fn a_hostile_slot_geometry_does_not_panic_or_hang() {
    // A table with slot 0 only is a one-slot instance: the code is the centre particle alone.
    let job = with_model(populated_kmc(), |model| {
        for environment in &mut model.structure.environments {
            environment
                .clusters
                .retain(|link| link.cluster_id != 0 || link.byte_position == 0);
        }
        model.energy.cluster_tables[0].occupation_codes = vec![0, 1, 2, 3];
        model.energy.cluster_tables[0].energies = vec![0.0; 4];
        model.energy.cluster_tables[0].particle_table_mapping = vec![0, 1, 2, 3];
    });
    let context = initialize(job);
    assert_eq!(context.clusters.instances.len(), 1);
    assert_eq!(context.clusters.instances[0].slot_count, 1);
    assert_eq!(context.dimensions.environment_count(), 24);
    assert_eq!(context.clusters.energy.len(), 8 * 4);
    let _: Option<EnvId> = None;
}

#[test]
fn the_dimensions_are_the_contract_for_the_cluster_cells() {
    let context = initialize(populated_kmc());
    let dimensions: &Dimensions = &context.dimensions;
    assert_eq!(dimensions.cell_count(), 8);
    assert_eq!(dimensions.p(), 3);
    assert_eq!(
        dimensions.wrapped_env(7, [1, 0, 0], 0),
        dimensions.cell_env(3, 0)
    );
    assert_eq!(
        dimensions.wrapped_env(0, [-1, 0, 0], 1),
        dimensions.cell_env(4, 1)
    );
    assert_eq!(NO_CLUSTER_CODE, u32::MAX);
}

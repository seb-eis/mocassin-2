// SPDX-License-Identifier: MIT

//! Model mapping and invariant tests.

use super::*;

#[test]
fn the_fixture_model_maps() {
    let model = map_ok(&model_dto());
    assert_eq!(model.counts.particle_count, 5);
    assert_eq!(model.counts.position_count, 4);
    assert_eq!(model.counts.rule_count, 4);
    assert_eq!(model.transitions.jump_offsets[6], 0);
    assert_eq!(model.transitions.jump_offsets[7], 1);
    assert_eq!(model.transitions.jump_offsets[20], 4);
}

#[test]
fn void_nodes_are_exempt_from_the_union_check() {
    let model = map_ok(&model_dto());
    // The fixture's state0/state2 carry void at node 1, and no allowed mask contains particle 0.
    assert_eq!(model.transitions.rules[0].state0.node(1), 0);
    assert!(
        model
            .structure
            .positions
            .iter()
            .all(|position| position.allowed_mask & 1 == 0)
    );
}

#[test]
fn a_collection_union_admits_a_particle_only_one_direction_allows() {
    let mut payload = model_dto();
    // Node 1 of direction 0 is position 2 (mask {1,2}); give direction 1 position 0 (mask {3,4}).
    payload.transitions.jump_directions[1].path[1] = [0, 0, 0, 0];
    // Particle 3 is admitted only by direction 1 at node 1; the union check accepts it by contract.
    let state1 = payload.transitions.jump_rules[0].state1;
    payload.transitions.jump_rules[0].state1 = (state1 & !(0x3F << 6)) | (3 << 6);

    let model = map_ok(&payload);
    assert_eq!(model.transitions.rules[0].state1.node(1), 3);

    // Without the mask difference the same particle is outside the union.
    let mut control = model_dto();
    let state1 = control.transitions.jump_rules[0].state1;
    control.transitions.jump_rules[0].state1 = (state1 & !(0x3F << 6)) | (3 << 6);
    let error = model_from_payload(&control, &schema(), &limits()).expect_err("union");
    assert!(error.to_string().contains("not admitted"), "{error}");
}

#[test]
fn a_direction_may_be_mapped_into_another_start_bucket() {
    let mut payload = model_dto();
    // Bucket 6 (position 1, particle 1) and bucket 16 (position 3, particle 1) each have one direction.
    // Swap them so each bucket refers to a direction whose representative start is the other position.
    payload.transitions.jump_mapping[0] = 1;
    payload.transitions.jump_mapping[2] = 0;

    let model = map_ok(&payload);
    let particle_count = model.counts.particle_count as usize;
    let direction_at_bucket = |bucket: usize| -> usize {
        let start = model.transitions.jump_offsets[bucket] as usize;
        model.transitions.jump_mapping[start] as usize
    };
    let bucket_six = 6;
    let bucket_sixteen = 16;
    assert_ne!(
        model.transitions.directions[direction_at_bucket(bucket_six)].position_id,
        (bucket_six / particle_count) as u32
    );
    assert_ne!(
        model.transitions.directions[direction_at_bucket(bucket_sixteen)].position_id,
        (bucket_sixteen / particle_count) as u32
    );
}

#[test]
fn a_particle_outside_the_union_is_rejected_even_when_mass_and_charge_hold() {
    let mut payload = model_dto();
    payload.transitions.jump_rules[0].state0 =
        (payload.transitions.jump_rules[0].state0 & !0x3F) | 4;
    payload.transitions.jump_rules[0].state2 =
        (payload.transitions.jump_rules[0].state2 & !0x3F) | 4;
    let error = model_from_payload(&payload, &schema(), &limits()).expect_err("union");
    assert!(error.to_string().contains("not admitted"), "{error}");
}

#[test]
fn a_non_conserved_symbol_multiset_is_rejected() {
    let error = rejected(|payload| {
        // state2 becomes Vc, ∅, Vc while state0 remains Vc, ∅, O.
        payload.transitions.jump_rules[0].state2 =
            (payload.transitions.jump_rules[0].state2 & !0x3F) | 2;
    });
    assert!(error.to_string().contains("symbols"), "{error}");
}

#[test]
fn a_non_conserved_charge_is_rejected() {
    let error = rejected(|payload| {
        // Use the two Ce particles (same symbol, different charge) and admit them in the masks.
        payload.structure.positions[1].particle_ids = vec![3, 4];
        payload.structure.positions[3].particle_ids = vec![3, 4];
        let rule = &mut payload.transitions.jump_rules[0];
        rule.state0 = 3 | (3 << 12);
        rule.state2 = 4 | (4 << 12);
    });
    assert!(error.to_string().contains("charges"), "{error}");
}

#[test]
fn a_nonzero_transport_vector_without_a_charge_change_is_rejected() {
    let error = rejected(|payload| {
        let rule = &mut payload.transitions.jump_rules[0];
        rule.state2 = rule.state0;
        rule.charge_transport_vector = [5.0, 0.0, 0.0];
    });
    assert!(
        error.to_string().contains("zero transport vector"),
        "{error}"
    );
}

#[test]
fn a_path_vector_that_does_not_match_the_movement_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].movement[0] = [1.0, 0.0, 0.0];
    });
    assert!(error.to_string().contains("movement"), "{error}");
}

#[test]
fn a_length_that_does_not_match_the_path_vector_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].length = 1.0;
    });
    assert!(error.to_string().contains("path-vector norm"), "{error}");
}

#[test]
fn a_path_that_does_not_start_at_the_representative_position_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].path[0] = [0, 0, 0, 2];
    });
    assert!(error.to_string().contains("node zero"), "{error}");
}

#[test]
fn a_path_shorter_than_two_nodes_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].path.truncate(1);
        payload.transitions.jump_directions[0].movement.truncate(1);
    });
    assert!(error.to_string().contains("2..="), "{error}");
}

#[test]
fn directions_in_one_collection_must_share_a_path_length() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[1].path.truncate(2);
        payload.transitions.jump_directions[1].movement.truncate(2);
    });
    assert!(error.to_string().contains("same path length"), "{error}");
}

#[test]
fn a_path_node_position_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].path[1] = [0, 0, 0, 99];
    });
    assert!(error.to_string().contains("path"), "{error}");
}

#[test]
fn a_state_particle_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].state0 =
            (payload.transitions.jump_rules[0].state0 & !0x3F) | 63;
    });
    assert!(error.to_string().contains("state0"), "{error}");
}

#[test]
fn state_bits_above_the_path_length_are_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].state0 |= 1 << 36;
    });
    assert!(error.to_string().contains("bits above"), "{error}");
}

#[test]
fn a_tracker_order_length_mismatch_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].tracker_order.pop();
    });
    assert!(error.to_string().contains("tracker_order"), "{error}");
}

#[test]
fn a_tracker_index_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].tracker_order[0] = 99;
    });
    assert!(error.to_string().contains("tracker_order"), "{error}");
}

#[test]
fn a_rule_id_that_is_not_the_dense_index_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[1].rule_id = 9;
    });
    assert!(error.to_string().contains("rule_id"), "{error}");
}

#[test]
fn a_negative_frequency_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].frequency_hz = -1.0;
    });
    assert!(error.to_string().contains("frequency"), "{error}");
}

#[test]
fn a_non_finite_transport_vector_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].charge_transport_vector = [f64::NAN, 0.0, 0.0];
    });
    assert!(error.to_string().contains("finite"), "{error}");
}

#[test]
fn a_non_finite_static_correction_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_rules[0].static_correction = Some([f64::INFINITY, 0.0, 0.0]);
    });
    assert!(error.to_string().contains("finite"), "{error}");
}

#[test]
fn collection_ids_out_of_range_are_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_collections[0].rule_ids[0] = 99;
    });
    assert!(error.to_string().contains("rule_ids"), "{error}");

    let error = rejected(|payload| {
        payload.transitions.jump_collections[0].direction_ids[0] = 99;
    });
    assert!(error.to_string().contains("direction_ids"), "{error}");
}

#[test]
fn a_collection_without_directions_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_collections[0]
            .direction_ids
            .clear();
    });
    assert!(error.to_string().contains("direction list"), "{error}");
}

#[test]
fn a_direction_collection_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_directions[0].collection_id = 9;
    });
    assert!(error.to_string().contains("collection_id"), "{error}");
}

#[test]
fn a_jump_mapping_direction_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_mapping[0] = 99;
    });
    assert!(error.to_string().contains("jump_mapping"), "{error}");
}

#[test]
fn a_duplicate_direction_in_one_bucket_is_rejected() {
    let error = rejected(|payload| {
        // Widen bucket 6 to two entries and duplicate its direction in the mapping.
        payload.transitions.jump_count[6] = 2;
        let duplicate = payload.transitions.jump_mapping[0];
        payload.transitions.jump_mapping.insert(1, duplicate);
    });
    assert!(error.to_string().contains("twice"), "{error}");
}

#[test]
fn a_jump_count_length_mismatch_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_count.pop();
    });
    assert!(error.to_string().contains("jump_count"), "{error}");
}

#[test]
fn a_static_tracker_mapping_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.static_tracker_mapping[1] = 99;
    });
    assert!(
        error.to_string().contains("static_tracker_mapping"),
        "{error}"
    );
}

#[test]
fn a_global_tracker_mapping_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.global_tracker_mapping[0] = 99;
    });
    assert!(
        error.to_string().contains("global_tracker_mapping"),
        "{error}"
    );
}

#[test]
fn a_selection_mask_above_the_particle_count_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.environments[0].selection_particle_mask |= 1 << 10;
    });
    assert!(error.to_string().contains("mask"), "{error}");
}

#[test]
fn a_position_site_index_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.positions[0].site_index = 9;
    });
    assert!(error.to_string().contains("site_index"), "{error}");
}

#[test]
fn a_position_vector_that_is_not_the_base_position_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.positions[1].vector4 = [0, 0, 0, 2];
    });
    assert!(error.to_string().contains("vector4"), "{error}");
}

#[test]
fn position_particle_ids_must_be_ascending_and_in_range() {
    let error = rejected(|payload| {
        payload.structure.positions[1].particle_ids = vec![2, 1];
    });
    assert!(error.to_string().contains("ascending"), "{error}");

    let error = rejected(|payload| {
        payload.structure.positions[1].particle_ids = vec![1, 9];
    });
    assert!(error.to_string().contains("particle_ids"), "{error}");
}

#[test]
fn a_static_tracker_count_mismatch_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.static_trackers_per_cell = 15;
    });
    assert!(
        error.to_string().contains("static_trackers_per_cell"),
        "{error}"
    );
}

#[test]
fn a_negative_interaction_range_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.interaction_range[0] = -1;
    });
    assert!(error.to_string().contains("interaction_range"), "{error}");
}

#[test]
fn a_pair_link_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        add_pair_table(payload);
        payload.structure.environments[0]
            .pairs
            .push(PairLinkPayload {
                target_position: 99,
                offset: [0, 0, 0],
                energy_id: 0,
            });
    });
    assert!(error.to_string().contains("target_position"), "{error}");
}

#[test]
fn a_pair_offset_above_the_interaction_range_is_rejected() {
    let error = rejected(|payload| {
        add_pair_table(payload);
        payload.structure.environments[0]
            .pairs
            .push(PairLinkPayload {
                target_position: 1,
                offset: [4, 0, 0],
                energy_id: 0,
            });
    });
    assert!(error.to_string().contains("offset"), "{error}");
}

#[test]
fn a_pair_energy_id_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.environments[0]
            .pairs
            .push(PairLinkPayload {
                target_position: 1,
                offset: [0, 0, 0],
                energy_id: 9,
            });
    });
    assert!(error.to_string().contains("energy_id"), "{error}");
}

#[test]
fn a_cluster_link_out_of_range_is_rejected() {
    let error = rejected(|payload| {
        payload.structure.environments[0]
            .clusters
            .push(ClusterLinkPayload {
                cluster_id: 9,
                byte_position: 0,
            });
    });
    assert!(error.to_string().contains("cluster_id"), "{error}");
}

#[test]
fn a_cluster_byte_position_above_nine_is_rejected() {
    let error = rejected(|payload| {
        add_cluster_table(payload, vec![0], vec![1, 2]);
        payload.structure.environments[0]
            .clusters
            .push(ClusterLinkPayload {
                cluster_id: 0,
                byte_position: 10,
            });
    });
    assert!(error.to_string().contains("byte_position"), "{error}");
}

#[test]
fn a_pair_table_length_and_finiteness_are_checked() {
    let error = rejected(|payload| {
        add_pair_table(payload);
        payload.energy.pair_tables[0].energies.pop();
    });
    assert!(error.to_string().contains("energies"), "{error}");

    let error = rejected(|payload| {
        add_pair_table(payload);
        payload.energy.pair_tables[0].energies[0] = f64::NAN;
    });
    assert!(error.to_string().contains("finite"), "{error}");
}

#[test]
fn cluster_codes_must_be_ascending_and_fit_the_particle_count() {
    let error = rejected(|payload| {
        add_cluster_table(payload, vec![2, 1], vec![1, 2]);
    });
    assert!(error.to_string().contains("ascending"), "{error}");

    let error = rejected(|payload| {
        add_cluster_table(payload, vec![5], vec![1, 2]);
    });
    assert!(error.to_string().contains("occupation_codes"), "{error}");

    let error = rejected(|payload| {
        add_cluster_table(payload, vec![1 << 63], vec![1, 2]);
    });
    assert!(error.to_string().contains("six-bit"), "{error}");
}

#[test]
fn a_cluster_mapping_must_be_ascending_and_in_range() {
    let error = rejected(|payload| {
        add_cluster_table(payload, vec![0], vec![2, 1]);
    });
    assert!(
        error.to_string().contains("particle_table_mapping"),
        "{error}"
    );

    let error = rejected(|payload| {
        add_cluster_table(payload, vec![0], vec![1, 9]);
    });
    assert!(
        error.to_string().contains("particle_table_mapping"),
        "{error}"
    );
}

#[test]
fn a_defect_background_length_mismatch_is_rejected() {
    let error = rejected(|payload| {
        payload.energy.defect_background.pop();
    });
    assert!(error.to_string().contains("defect_background"), "{error}");
}

#[test]
fn the_particle_table_is_checked() {
    let error = rejected(|payload| {
        payload.particle.count = 0;
    });
    assert!(error.to_string().contains("particle count"), "{error}");

    let error = rejected(|payload| {
        payload.particle.charges.pop();
    });
    assert!(error.to_string().contains("charges"), "{error}");

    let error = rejected(|payload| {
        payload.particle.charges[0] = 1.0;
    });
    assert!(error.to_string().contains("void"), "{error}");

    let error = rejected(|payload| {
        payload.particle.is_vacancy[0] = true;
    });
    assert!(error.to_string().contains("vacancy"), "{error}");

    let error = rejected(|payload| {
        payload.particle.charges[1] = f64::NAN;
    });
    assert!(error.to_string().contains("finite"), "{error}");
}

#[test]
fn malformed_identities_are_rejected() {
    let error = rejected(|payload| {
        payload.signature = "z".repeat(64);
    });
    assert!(error.to_string().contains("hexadecimal"), "{error}");

    let error = rejected(|payload| {
        payload.model_signature = "a".repeat(63);
    });
    assert!(error.to_string().contains("hexadecimal"), "{error}");
}

#[test]
fn provenance_must_match_schema_info() {
    let error = rejected(|payload| {
        payload.space_group_data_sha256 = "a".repeat(64);
    });
    assert!(error.to_string().contains("provenance"), "{error}");

    let error = rejected(|payload| {
        payload.symmetry_contract_version.clear();
    });
    assert!(error.to_string().contains("non-empty"), "{error}");
}

#[test]
fn empty_payload_and_schema_provenance_is_rejected() {
    let mut schema = schema();
    schema.symmetry_contract_version.clear();
    schema.space_group_data_version.clear();
    schema.space_group_data_sha256.clear();
    let mut payload = model_dto();
    payload.symmetry_contract_version.clear();
    payload.space_group_data_version.clear();
    payload.space_group_data_sha256.clear();

    let error = model_from_payload(&payload, &schema, &limits())
        .expect_err("empty provenance must not satisfy the match by being empty on both sides");
    assert!(error.to_string().contains("non-empty"), "{error}");
}

#[test]
fn simulation_settings_are_checked() {
    for mutate in [
        (|payload: &mut ModelPayload| payload.simulations[0].default_settings.temperature_k = 0.0)
            as fn(&mut ModelPayload),
        |payload: &mut ModelPayload| payload.simulations[0].default_settings.target_mcsp = 0,
        |payload: &mut ModelPayload| {
            payload.simulations[0].default_settings.min_success_rate = Some(-1.0);
        },
        |payload: &mut ModelPayload| {
            payload.simulations[0]
                .default_settings
                .electric_field_direction = [2.0, 0.0, 0.0];
        },
        |payload: &mut ModelPayload| {
            payload.simulations[0]
                .default_settings
                .max_attempt_frequency_hz = Some(0.0);
        },
        |payload: &mut ModelPayload| {
            payload.simulations[0].default_settings.break_tolerance = Some(1.0);
        },
        |payload: &mut ModelPayload| payload.simulations[0].default_settings.flags = 0x10,
    ] {
        let error = rejected(mutate);
        assert_eq!(error.kind(), ErrorClass::Model);
    }
}

#[test]
fn simulation_tracker_lists_are_checked() {
    let error = rejected(|payload| {
        payload.simulations[0]
            .tracker_models
            .global_tracker_indices
            .swap(0, 1);
    });
    assert!(error.to_string().contains("ascending"), "{error}");

    let error = rejected(|payload| {
        payload.simulations[0]
            .tracker_models
            .static_tracker_indices
            .push(99);
    });
    assert!(
        error.to_string().contains("static_tracker_indices"),
        "{error}"
    );
}

#[test]
fn a_simulation_collection_kind_mismatch_is_rejected() {
    let error = rejected(|payload| {
        payload.transitions.jump_collections[0].kind = "mmc".to_owned();
    });
    assert!(
        error.to_string().contains("different simulation kind"),
        "{error}"
    );
}

#[test]
fn mmc_state1_must_be_zero() {
    let mut payload = model_dto();
    make_mmc(&mut payload);
    assert!(model_from_payload(&payload, &schema(), &limits()).is_ok());

    payload.transitions.jump_rules[0].state1 = 64;
    let error = model_from_payload(&payload, &schema(), &limits()).expect_err("state1");
    assert!(error.to_string().contains("state1 must be zero"), "{error}");
}

#[test]
fn mmc_rejects_a_nonempty_tracker_list() {
    let mut payload = model_dto();
    make_mmc(&mut payload);
    payload.simulations[0]
        .tracker_models
        .global_tracker_indices
        .push(0);
    let error = model_from_payload(&payload, &schema(), &limits()).expect_err("trackers");
    assert!(error.to_string().contains("MMC"), "{error}");
}

#[test]
fn mmc_rejects_a_pre_run() {
    let mut payload = model_dto();
    make_mmc(&mut payload);
    payload.simulations[0].default_settings.pre_run_mcsp = 10;
    let error = model_from_payload(&payload, &schema(), &limits()).expect_err("pre-run");
    assert!(error.to_string().contains("pre_run_mcsp"), "{error}");
}

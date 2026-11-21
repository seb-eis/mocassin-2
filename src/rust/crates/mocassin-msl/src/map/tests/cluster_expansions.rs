// SPDX-License-Identifier: MIT

//! Cluster occupation code expansion validation and lookup tests.

use mocassin_types::payload::CodeExpansionPayload;
use mocassin_types::{ClusterCodeLookup, ErrorClass};

use super::{fixture_model, limits, map_ok, model_from_payload, schema};

fn new_fixture() -> mocassin_types::payload::ModelPayload {
    fixture_model("step-11-amend-2/payloads/model-1.bin")
}

fn pre_map_fixture() -> mocassin_types::payload::ModelPayload {
    fixture_model("amend-2-pre-map/payloads/model-1.bin")
}

fn rejected(mutate: impl FnOnce(&mut CodeExpansionPayload)) -> crate::PackageError {
    let mut payload = new_fixture();
    mutate(
        payload.energy.cluster_tables[0]
            .code_expansions
            .as_mut()
            .expect("the new fixture has a map"),
    );
    let error = model_from_payload(&payload, &schema(), &limits())
        .expect_err("the mutated expansion map must be rejected");
    assert_eq!(error.kind(), ErrorClass::Package, "{error}");
    assert_eq!(error.exit_code(), mocassin_types::ExitCode::PackageError);
    error
}

#[test]
fn the_runtime_table_resolves_all_eight_raw_codes() {
    let model = map_ok(&new_fixture());
    let table = &model.energy.cluster_tables[0];

    for (raw, expected) in [
        (8322, 0),
        (8323, 1),
        (8386, 2),
        (8387, 3),
        (12418, 2),
        (12419, 3),
        (12482, 4),
        (12483, 5),
    ] {
        assert_eq!(table.lookup(raw), ClusterCodeLookup::Canonical(expected));
    }
    assert_eq!(table.lookup(9999), ClusterCodeLookup::MissingFromMap(9999));
}

#[test]
fn the_absent_map_resolves_canonical_codes_and_fails_closed_otherwise() {
    let model = map_ok(&pre_map_fixture());
    let table = &model.energy.cluster_tables[0];

    for (index, code) in table.occupation_codes.iter().enumerate() {
        assert_eq!(
            table.lookup(*code),
            ClusterCodeLookup::Canonical(index as u32)
        );
    }
    for raw in [8386, 8387] {
        assert_eq!(
            table.lookup(raw),
            ClusterCodeLookup::ExpansionMapAbsent(raw)
        );
        assert_eq!(table.lookup(raw).index(), None);
    }
}

#[test]
fn a_non_ascending_map_is_rejected() {
    let error = rejected(|expansion| expansion.expanded_codes.swap(0, 1));
    assert!(
        error
            .to_string()
            .contains("energy.cluster_tables.code_expansions"),
        "{error}"
    );
    assert!(error.to_string().contains("strictly ascending"), "{error}");
}

#[test]
fn an_out_of_range_canonical_index_is_rejected() {
    let error = rejected(|expansion| expansion.canonical_indices[0] = 99);
    assert!(error.to_string().contains("canonical index"), "{error}");
}

#[test]
fn a_code_with_bits_above_the_slots_is_rejected() {
    let error = rejected(|expansion| {
        let last = expansion.expanded_codes.len() - 1;
        expansion.expanded_codes[last] = 1 << 60;
    });
    assert!(error.to_string().contains("bits above"), "{error}");
}

#[test]
fn a_code_with_a_particle_outside_the_count_is_rejected() {
    let error = rejected(|expansion| expansion.expanded_codes[0] = 4 << 6);
    assert!(error.to_string().contains("particle"), "{error}");
}

#[test]
fn a_missing_canonical_self_entry_is_rejected() {
    let error = rejected(|expansion| {
        expansion.expanded_codes.remove(0);
        expansion.canonical_indices.remove(0);
    });
    assert!(
        error.to_string().contains("missing from expanded_codes"),
        "{error}"
    );
}

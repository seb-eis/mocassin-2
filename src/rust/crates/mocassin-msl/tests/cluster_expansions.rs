// SPDX-License-Identifier: MIT

//! Cluster occupation code expansion map acceptance and hostile-input tests.

mod support;

use std::fs;

use mocassin_msl::{Package, PackageError, decode_model_payload};
use mocassin_types::payload::ModelPayload;
use mocassin_types::{ClusterCodeLookup, ReaderLimits};

use support::fixture_file;

fn package_path(package: &str, name: &str) -> std::path::PathBuf {
    fixture_file(package, name)
}

fn raw_model(package: &str) -> ModelPayload {
    let bytes = fs::read(package_path(&format!("{package}/payloads"), "model-1.bin"))
        .expect("read the model payload");
    rmp_serde::from_slice(&bytes).expect("decode the model payload")
}

fn encode(payload: &ModelPayload) -> Vec<u8> {
    rmp_serde::to_vec_named(payload).expect("encode the model payload")
}

fn rejected(mutate: impl FnOnce(&mut ModelPayload), limits: &ReaderLimits) -> PackageError {
    let mut payload = raw_model("step-11-amend-2");
    mutate(&mut payload);
    decode_model_payload(&encode(&payload), limits)
        .expect_err("the mutated payload must be rejected")
}

#[test]
fn the_new_fixture_resolves_all_eight_raw_codes() {
    let package = Package::open_readonly(
        package_path("step-11-amend-2", "step-11-amend-2.msl"),
        ReaderLimits::default(),
    )
    .expect("the fixture opens");
    let model = package.load_model(1).expect("the model maps");
    let table = &model.energy.cluster_tables[0];

    assert_eq!(
        table.occupation_codes,
        vec![8322, 8323, 12418, 12419, 12482, 12483]
    );

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
        assert_eq!(
            table.lookup(raw),
            ClusterCodeLookup::Canonical(expected),
            "raw code {raw}"
        );
    }

    assert_eq!(table.lookup(9999), ClusterCodeLookup::MissingFromMap(9999));
}

#[test]
fn the_pre_map_fixture_fails_closed_for_non_canonical_codes() {
    let package = Package::open_readonly(
        package_path("amend-2-pre-map", "amend-2-pre-map.msl"),
        ReaderLimits::default(),
    )
    .expect("the pre-map fixture opens");
    let model = package.load_model(1).expect("the model maps");
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
fn the_scan_accepts_the_absent_optional_key() {
    let bytes = fs::read(package_path("amend-2-pre-map/payloads", "model-1.bin"))
        .expect("read the pre-map model payload");
    let decoded = decode_model_payload(&bytes, &ReaderLimits::default())
        .expect("the absent optional key is accepted");
    assert!(decoded.energy.cluster_tables[0].code_expansions.is_none());
}

#[test]
fn the_cumulative_expansion_budget_is_enforced_before_the_typed_decode() {
    let bytes = fs::read(package_path("step-11-amend-2/payloads", "model-1.bin"))
        .expect("read the model payload");
    let rejected = ReaderLimits {
        max_cluster_expansion_bytes: 95,
        ..ReaderLimits::default()
    };
    let error = decode_model_payload(&bytes, &rejected).expect_err("the budget rejects");
    assert!(matches!(error, PackageError::PayloadTooLarge { .. }));
    assert_eq!(error.exit_code(), mocassin_types::ExitCode::PackageError);

    let accepted = ReaderLimits {
        max_cluster_expansion_bytes: 96,
        ..ReaderLimits::default()
    };
    decode_model_payload(&bytes, &accepted).expect("the exact cost is accepted");
}

#[test]
fn an_unequal_pair_is_rejected_with_the_field_path() {
    let error = rejected(
        |payload| {
            payload.energy.cluster_tables[0]
                .code_expansions
                .as_mut()
                .unwrap()
                .canonical_indices
                .pop();
        },
        &ReaderLimits::default(),
    );
    assert!(
        error
            .to_string()
            .contains("energy.cluster_tables[0].code_expansions"),
        "{error}"
    );
    assert_eq!(error.exit_code(), mocassin_types::ExitCode::PackageError);
}

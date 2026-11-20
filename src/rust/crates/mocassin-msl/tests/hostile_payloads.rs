// SPDX-License-Identifier: MIT

//! Hostile and malformed payload tests: the reader must fail cleanly and within its bounds.

mod support;

use std::fs;

use mocassin_msl::{decode_lattice_payload, decode_model_payload, decompress_payload};
use mocassin_types::{ErrorClass, ReaderLimits};

use support::{msgpack, payloads, raw_payload};

fn expect_package_error(error: &mocassin_msl::PackageError) {
    assert_eq!(error.kind(), ErrorClass::Package);
}

#[test]
fn a_duplicate_known_key_is_rejected() {
    let mut bytes = msgpack::map(2);
    bytes.extend(msgpack::string("format_version"));
    bytes.extend(msgpack::uint(1));
    bytes.extend(msgpack::string("format_version"));
    bytes.extend(msgpack::uint(1));

    let error = decode_model_payload(&bytes, &ReaderLimits::default()).expect_err("duplicate key");
    expect_package_error(&error);
    assert!(error.to_string().contains("duplicate key"));
}

#[test]
fn a_missing_required_key_is_rejected() {
    let error = decode_model_payload(&msgpack::map(0), &ReaderLimits::default())
        .expect_err("an empty map has no required keys");
    expect_package_error(&error);
    assert!(error.to_string().contains("required key"));
}

#[test]
fn a_non_map_document_is_rejected() {
    let error = decode_model_payload(&[0x01], &ReaderLimits::default())
        .expect_err("the document is not a map");
    expect_package_error(&error);
    assert!(error.to_string().contains("expected a map"));
}

#[test]
fn trailing_bytes_are_rejected() {
    let mut model = fs::read(raw_payload("model-1.bin")).expect("read model payload");
    model.push(0x00);

    let error = decode_model_payload(&model, &ReaderLimits::default()).expect_err("trailing byte");
    expect_package_error(&error);
    assert!(error.to_string().contains("trailing bytes"));
}

#[test]
fn a_truncated_payload_is_rejected() {
    let model = fs::read(raw_payload("model-1.bin")).expect("read model payload");
    let half = &model[..model.len() / 2];

    let error = decode_model_payload(half, &ReaderLimits::default()).expect_err("truncated input");
    expect_package_error(&error);
}

#[test]
fn a_declared_length_beyond_the_input_is_rejected() {
    let mut bytes = msgpack::map(1);
    bytes.extend(msgpack::string("unknown"));
    bytes.extend(msgpack::array(u32::MAX));

    let error = decode_model_payload(&bytes, &ReaderLimits::default())
        .expect_err("the array declares more values than the input holds");
    expect_package_error(&error);
    assert!(error.to_string().contains("declared length"));
}

#[test]
fn excessive_nesting_is_rejected() {
    let mut bytes = msgpack::map(1);
    bytes.extend(msgpack::string("unknown"));
    bytes.extend(core::iter::repeat_n(0x91, 20));
    bytes.push(0x00);

    let error =
        decode_model_payload(&bytes, &ReaderLimits::default()).expect_err("nesting is too deep");
    expect_package_error(&error);
    assert!(error.to_string().contains("nesting depth"));
}

#[test]
fn the_element_budget_is_enforced() {
    let model = fs::read(raw_payload("model-1.bin")).expect("read model payload");
    let limits = ReaderLimits {
        max_model_elements: 8,
        ..ReaderLimits::default()
    };

    let error = decode_model_payload(&model, &limits).expect_err("budget");
    assert!(matches!(
        error,
        mocassin_msl::PackageError::ElementBudgetExceeded { .. }
    ));
    expect_package_error(&error);
}

#[test]
fn the_depth_limit_is_configurable() {
    let model = fs::read(raw_payload("model-1.bin")).expect("read model payload");
    let limits = ReaderLimits {
        max_messagepack_depth: 2,
        ..ReaderLimits::default()
    };

    let error = decode_model_payload(&model, &limits).expect_err("depth limit");
    expect_package_error(&error);
}

#[test]
fn a_decompression_bomb_is_rejected_before_allocation() {
    let bomb = zstd::bulk::compress(&vec![0_u8; 4 * 1024 * 1024], 19).expect("compress the bomb");
    let limit = 1024;

    let error = decompress_payload("model", &bomb, limit).expect_err("the declared size is huge");
    assert!(matches!(
        error,
        mocassin_msl::PackageError::PayloadTooLarge {
            declared,
            limit: 1024,
            ..
        } if declared == 4 * 1024 * 1024
    ));
    expect_package_error(&error);
}

#[test]
fn a_frame_without_a_content_size_is_streamed_with_a_hard_limit() {
    let payload = vec![0_u8; 100_000];
    let frame = zstd::stream::encode_all(&payload[..], 3).expect("encode a streaming frame");
    assert!(
        zstd::decompressed_size(&frame).is_none(),
        "the streaming encoder omits the content size"
    );

    let error =
        decompress_payload("model", &frame, 10_000).expect_err("the output exceeds the limit");
    assert!(matches!(
        error,
        mocassin_msl::PackageError::PayloadTooLarge { .. }
    ));

    let decoded = decompress_payload("model", &frame, 200_000).expect("the output fits");
    assert_eq!(decoded, payload);
}

#[test]
fn a_truncated_frame_and_an_empty_frame_are_rejected() {
    let raw = fs::read(raw_payload("model-1.bin")).expect("read model payload");
    let frame = zstd::bulk::compress(&raw, 3).expect("compress the model payload");
    let truncated = &frame[..frame.len() - 8];

    let error = decompress_payload("model", truncated, 512 * 1024 * 1024)
        .expect_err("the frame is truncated");
    expect_package_error(&error);
    assert!(error.to_string().contains("invalid model payload frame"));

    let empty = decompress_payload("model", &[], 512 * 1024 * 1024).expect_err("empty payload");
    expect_package_error(&empty);
}

#[test]
fn the_lattice_guard_rejects_an_oversized_supercell() {
    let payload = payloads::lattice(256, 256, 256, &[0x04], &[], true);

    let error = decode_lattice_payload(&payload, &ReaderLimits::default())
        .expect_err("the supercell exceeds max_positions");
    expect_package_error(&error);
    assert!(error.to_string().contains("exceeds the reader limit"));
}

#[test]
fn the_lattice_particle_count_must_match_the_size_product() {
    let payload = payloads::lattice(1, 1, 2, &[0x02], &[0, 1, 2], true);

    let error = decode_lattice_payload(&payload, &ReaderLimits::default())
        .expect_err("three particles cannot fill four sites");
    expect_package_error(&error);
    assert!(error.to_string().contains("expected 4"));
}

#[test]
fn a_lattice_particle_above_a_byte_is_rejected() {
    let mut payload = payloads::lattice(1, 1, 1, &[0x01], &[], false);
    // The particle array is the last map value: replace the empty array with one element holding 256.
    assert_eq!(payload.last(), Some(&0x90), "an empty array is expected");
    payload.pop();
    payload.extend(msgpack::array(1));
    payload.extend(msgpack::uint(256));

    let error = decode_lattice_payload(&payload, &ReaderLimits::default())
        .expect_err("a particle above u8::MAX is rejected");
    expect_package_error(&error);
}

#[test]
fn every_per_field_count_cap_is_enforced() {
    let bytes = fs::read(support::fixture_file(
        "step-13-populated/payloads",
        "model-1.bin",
    ))
    .expect("read the populated model payload");
    let defaults = ReaderLimits::default();
    assert!(
        decode_model_payload(&bytes, &defaults).is_ok(),
        "the fixture is within the documented defaults"
    );

    let cases = [
        (
            "jump rules",
            ReaderLimits {
                max_rules: 7,
                ..defaults
            },
        ),
        (
            "jump directions",
            ReaderLimits {
                max_directions: 3,
                ..defaults
            },
        ),
        (
            "jump collections",
            ReaderLimits {
                max_collections: 1,
                ..defaults
            },
        ),
        (
            "pair tables",
            ReaderLimits {
                max_pair_tables: 5,
                ..defaults
            },
        ),
        (
            "cluster tables",
            ReaderLimits {
                max_cluster_tables: 0,
                ..defaults
            },
        ),
        (
            "pair-link total",
            ReaderLimits {
                max_pair_links: 5,
                ..defaults
            },
        ),
        (
            "pair links per position",
            ReaderLimits {
                max_pair_links_per_position: 1,
                ..defaults
            },
        ),
        (
            "cluster links per position",
            ReaderLimits {
                max_cluster_links_per_position: 0,
                ..defaults
            },
        ),
    ];

    for (field, limits) in cases {
        let error = decode_model_payload(&bytes, &limits)
            .expect_err("the lowered cap must reject the fixture");
        assert!(
            error.to_string().contains(field) && error.to_string().contains("reader limit"),
            "{field}: {error}"
        );
    }

    let raised = ReaderLimits {
        max_rules: 8,
        max_directions: 4,
        max_collections: 2,
        max_pair_tables: 6,
        max_cluster_tables: 1,
        max_pair_links: 6,
        max_pair_links_per_position: 2,
        max_cluster_links_per_position: 1,
        ..defaults
    };
    assert!(
        decode_model_payload(&bytes, &raised).is_ok(),
        "raised limits must accept the same payload"
    );
}

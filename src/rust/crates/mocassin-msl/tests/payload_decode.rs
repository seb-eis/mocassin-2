// SPDX-License-Identifier: MIT

//! Typed decode tests over the raw payloads and the two lattice encodings.

mod support;

use std::fs;

use mocassin_msl::{decode_job_payload, decode_lattice_payload, decode_model_payload};
use mocassin_types::ReaderLimits;

use support::{msgpack, payloads::lattice, raw_payload};

#[test]
fn the_raw_model_and_job_payloads_decode() {
    let limits = ReaderLimits::default();

    let model = decode_model_payload(
        &fs::read(raw_payload("model-1.bin")).expect("read model payload"),
        &limits,
    )
    .expect("the raw model payload decodes");
    assert_eq!(model.format_version, 1);
    assert_eq!(model.particle.count, 5);
    assert_eq!(model.structure.position_count, 4);
    assert_eq!(model.structure.positions.len(), 4);
    assert_eq!(model.structure.environments.len(), 4);
    assert!(model.energy.pair_tables.is_empty());
    assert!(model.energy.cluster_tables.is_empty());
    assert_eq!(model.transitions.jump_collections.len(), 1);
    assert_eq!(model.transitions.jump_directions.len(), 2);
    assert_eq!(model.transitions.jump_rules.len(), 4);
    assert_eq!(model.simulations.len(), 1);

    let targets = [150_u64, 150, 200, 200, 200, 200];
    for (index, expected) in targets.into_iter().enumerate() {
        let name = format!("job-{}.bin", index + 1);
        let job = decode_job_payload(
            &fs::read(raw_payload(&name)).expect("read job payload"),
            &limits,
        )
        .expect("the raw job payload decodes");
        assert_eq!(job.format_version, 1);
        assert_eq!(job.kind, "kmc");
        assert_eq!(job.target_mcsp, expected, "{name}");
        assert_eq!(job.flags, 0x72, "{name}");
    }
}

#[test]
fn the_raw_lattice_payloads_decode() {
    let limits = ReaderLimits::default();
    let expected = [
        ([6_u32, 6, 6], 864_usize),
        ([6, 6, 6], 864),
        ([8, 8, 8], 2048),
        ([8, 8, 8], 2048),
        ([6, 6, 6], 864),
        ([8, 8, 8], 2048),
    ];

    for (index, (size, length)) in expected.into_iter().enumerate() {
        let name = format!("lattice-{}.bin", index + 1);
        let lattice = decode_lattice_payload(
            &fs::read(raw_payload(&name)).expect("read lattice payload"),
            &limits,
        )
        .expect("the raw lattice payload decodes");
        assert_eq!(lattice.format_version, 1);
        assert_eq!(lattice.lattice_size, size, "{name}");
        assert_eq!(lattice.position_count, 4, "{name}");
        assert_eq!(lattice.lattice.len(), length, "{name}");
        assert_eq!(
            Some(length as u64),
            lattice.expected_particle_count(),
            "{name}"
        );
        assert!(lattice.lattice.iter().all(|particle| *particle < 5));
    }
}

#[test]
fn the_lattice_accepts_the_v1_array_and_the_canonical_bin() {
    let limits = ReaderLimits::default();
    let particles = [0_u8, 1, 2, 3];

    let from_array = decode_lattice_payload(&lattice(1, 1, 2, &[0x02], &particles, false), &limits)
        .expect("the array form decodes");
    assert_eq!(from_array.lattice, particles);
    assert_eq!(from_array.lattice_size, [1, 1, 2]);
    assert_eq!(from_array.position_count, 2);

    let from_bin = decode_lattice_payload(&lattice(1, 1, 2, &[0x02], &particles, true), &limits)
        .expect("the binary form decodes");
    assert_eq!(from_bin.lattice, particles);
    assert_eq!(from_bin, from_array);
}

#[test]
fn a_small_integer_is_accepted_at_any_stored_width() {
    let limits = ReaderLimits::default();
    let particles = [0_u8, 1, 2, 3];

    // position_count = 2 stored as uint16 instead of a positive fixint.
    let lattice = decode_lattice_payload(
        &lattice(1, 1, 2, &[0xcd, 0x00, 0x02], &particles, false),
        &limits,
    )
    .expect("a wider integer decodes");
    assert_eq!(lattice.position_count, 2);
}

#[test]
fn unknown_keys_are_ignored() {
    let limits = ReaderLimits::default();
    let particles = [0_u8, 1, 2, 3];

    let mut bytes = msgpack::map(5);
    bytes.extend(msgpack::string("format_version"));
    bytes.extend(msgpack::uint(1));
    bytes.extend(msgpack::string("lattice_size"));
    bytes.extend(msgpack::array(3));
    for value in [1_u32, 1, 2] {
        bytes.extend(msgpack::uint(u64::from(value)));
    }
    bytes.extend(msgpack::string("position_count"));
    bytes.extend(msgpack::uint(2));
    bytes.extend(msgpack::string("lattice"));
    bytes.extend(msgpack::bin(&particles));
    bytes.extend(msgpack::string("an_unknown_key"));
    bytes.extend(msgpack::uint(7));

    let lattice = decode_lattice_payload(&bytes, &limits).expect("unknown keys are ignored");
    assert_eq!(lattice.lattice, particles);
}

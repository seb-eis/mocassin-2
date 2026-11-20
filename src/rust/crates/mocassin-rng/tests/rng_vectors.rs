// SPDX-License-Identifier: MIT

//! Cross-language RNG vector tests over `tests/contracts/rng-vectors.json`.

mod support;

use std::fs;

use mocassin_rng::{Rng, RngStateV1, derive_job_seed, derive_lattice_seed, seed_hash};
use support::json::Json;

fn vectors() -> Json {
    Json::parse(&fs::read_to_string(support::vectors_path()).expect("read the RNG vector document"))
}

fn from_hex(value: &str) -> Vec<u8> {
    let mut bytes = Vec::with_capacity(value.len() / 2);
    let (pairs, _) = value.as_bytes().as_chunks::<2>();
    for pair in pairs {
        let text = core::str::from_utf8(pair).expect("ASCII hex");
        bytes.push(u8::from_str_radix(text, 16).expect("hex pair"));
    }
    bytes
}

fn seed(value: &Json) -> [u8; 32] {
    from_hex(value.as_str()).try_into().expect("a 32-byte seed")
}

fn to_hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
}

fn word(value: &Json) -> u32 {
    u32::from_str_radix(value.as_str(), 16).expect("a 32-bit hexadecimal word")
}

fn wide(value: &Json) -> u64 {
    u64::from_str_radix(value.as_str(), 16).expect("a 64-bit hexadecimal word")
}

#[test]
fn the_document_names_the_step_13_algorithm_and_domains() {
    let document = vectors();
    assert_eq!(document.get("format").as_str(), "mocassin.rng-vectors");
    assert_eq!(document.get("formatVersion").as_u64(), 1);
    assert_eq!(document.get("algorithm").as_str(), "chacha12-v1");
    assert_eq!(
        document.get("domains").get("jobSeed").as_str(),
        "mocassin/job/v1"
    );
    assert_eq!(
        document.get("domains").get("latticeSeed").as_str(),
        "mocassin/lattice/v1"
    );
    assert_eq!(
        document.get("domains").get("rngFork").as_str(),
        "mocassin/rng-fork/v1"
    );
}

#[test]
fn the_seed_derivations_match_the_cross_language_vectors() {
    let document = vectors();
    let seeds = document.get("seeds");
    let master = seed(seeds.get("masterHex"));

    for entry in seeds.get("job").as_array() {
        let derived = derive_job_seed(
            &master,
            u32::try_from(entry.get("collection").as_u64()).expect("u32"),
            u32::try_from(entry.get("config").as_u64()).expect("u32"),
            u32::try_from(entry.get("job").as_u64()).expect("u32"),
        );
        assert_eq!(to_hex(&derived), entry.get("expectedHex").as_str());
    }

    for entry in seeds.get("lattice").as_array() {
        let derived = derive_lattice_seed(
            &seed(entry.get("jobSeedHex")),
            &seed(entry.get("requestHashHex")),
        );
        assert_eq!(to_hex(&derived), entry.get("expectedHex").as_str());
    }

    for entry in seeds.get("seedHash").as_array() {
        assert_eq!(
            seed_hash(&seed(entry.get("seedHex"))),
            entry.get("expectedHex").as_str()
        );
    }
}

#[test]
fn the_draw_vectors_match() {
    let document = vectors();
    let draws = document.get("draws");
    let initial = seed(draws.get("seedHex"));

    let mut rng = Rng::from_seed(initial);
    for expected in draws.get("first16U32").as_array() {
        assert_eq!(rng.next_u32(), word(expected));
    }

    let mut rng = Rng::from_seed(initial);
    for expected in draws.get("first8U64").as_array() {
        assert_eq!(rng.next_u64(), wide(expected));
    }

    let mut rng = Rng::from_seed(initial);
    for expected in draws.get("first4F64").as_array() {
        assert_eq!(rng.next_f64().to_bits(), expected.as_f64().to_bits());
    }

    for upper in [2_u32, 10, 1024] {
        let mut rng = Rng::from_seed(initial);
        for expected in draws.get("bounded").get(&upper.to_string()).as_array() {
            assert_eq!(
                u64::from(rng.next_bounded(upper)),
                expected.as_u64(),
                "upper={upper}"
            );
        }
    }

    let mut rng = Rng::from_seed(initial);
    for _ in 0..62 {
        let _ = rng.next_u32();
    }
    for expected in draws.get("bufferBoundaryAfter62").as_array() {
        assert_eq!(rng.next_u32(), word(expected));
    }
}

#[test]
fn the_fork_vector_matches_and_the_parent_is_unmoved() {
    let document = vectors();
    let fork = document.get("fork");
    let initial = seed(fork.get("seedHex"));
    let domain = from_hex(fork.get("domainHex").as_str());

    let mut parent = Rng::from_seed(initial);
    let before = parent.export_state();
    assert_eq!(before.stream(), fork.get("stream").as_u64());
    assert_eq!(before.word_pos(), u128::from(fork.get("wordPos").as_u64()));
    let mut child = parent.fork(&domain);
    assert_eq!(
        parent.export_state(),
        before,
        "fork must not advance the parent"
    );

    for expected in fork.get("childFirst8U32").as_array() {
        assert_eq!(child.next_u32(), word(expected));
    }
}

#[test]
fn the_state_vectors_round_trip_and_resume() {
    let document = vectors();
    let state = document.get("state");
    let initial = seed(state.get("seedHex"));
    let draws = usize::try_from(state.get("drawsBeforeExport").as_u64()).expect("usize");

    let mut rng = Rng::from_seed(initial);
    for _ in 0..draws {
        let _ = rng.next_u32();
    }
    let exported = rng.export_state();
    assert_eq!(exported.as_bytes().len(), 64);
    assert_eq!(to_hex(exported.as_bytes()), state.get("stateHex").as_str());

    let mut resumed = Rng::from_state(&exported).expect("restore");
    for expected in state.get("next8U32").as_array() {
        assert_eq!(resumed.next_u32(), word(expected));
    }

    let nonzero = state.get("nonzeroStream");
    let bytes = from_hex(nonzero.get("stateHex").as_str());
    assert_eq!(bytes.len(), 64);
    let nonzero_state = RngStateV1::from_bytes(&bytes).expect("a valid nonzero-stream state");
    assert_eq!(nonzero_state.stream(), nonzero.get("stream").as_u64());
    assert_eq!(
        nonzero_state.word_pos(),
        u128::from(nonzero.get("wordPos").as_u64())
    );
    let mut resumed = Rng::from_state(&nonzero_state).expect("restore the nonzero stream");
    for expected in nonzero.get("next8U32").as_array() {
        assert_eq!(resumed.next_u32(), word(expected));
    }
}

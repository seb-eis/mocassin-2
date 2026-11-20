// SPDX-License-Identifier: MIT

//! Frozen golden vectors for the Step 13 ChaCha12 contract.
//!
//! The expected words were produced by an independent ChaCha12 implementation (12 rounds, 64-bit counter,
//! 64-bit stream/nonce) and cross-checked against the documented Step 10 seed vectors. A change to the draw
//! order, the seed derivation, the fork derivation, or the state layout fails this test; such a change is a
//! format break, not a refactor.

use mocassin_rng::{Rng, RngStateV1, derive_job_seed, derive_lattice_seed, seed_hash};

const MASTER: [u8; 32] = [
    0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
    0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
];

const CERIA_JOB_SEED: &str = "b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e";
const CERIA_REQUEST_HASH: &str = "5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295";

fn from_hex(value: &str) -> [u8; 32] {
    let mut bytes = [0u8; 32];
    let (pairs, _) = value.as_bytes().as_chunks::<2>();
    for (index, chunk) in pairs.iter().enumerate() {
        let text = core::str::from_utf8(chunk.as_slice()).expect("ASCII hex");
        bytes[index] = u8::from_str_radix(text, 16).expect("hex pair");
    }
    bytes
}

fn to_hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
}

#[test]
fn seed_derivation_matches_the_step_10_vectors() {
    assert_eq!(
        to_hex(&derive_job_seed(&MASTER, 0, 0, 0)),
        "884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7"
    );
    assert_eq!(
        to_hex(&derive_job_seed(&MASTER, 1, 2, 3)),
        "172da959820807561e234261342a48e26076c59e1af34a01487919d16a7daccf"
    );
    assert_eq!(
        to_hex(&derive_lattice_seed(
            &from_hex(CERIA_JOB_SEED),
            &from_hex(CERIA_REQUEST_HASH)
        )),
        "a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2"
    );
    assert_eq!(
        seed_hash(&[0u8; 32]),
        "66687aadf862bd776c8fc18b8e9f8e20089714856ee233b3902a591d0d5f2925"
    );
}

#[test]
fn the_first_sixteen_words_match_the_independent_chacha12_vector() {
    let mut rng = Rng::from_seed(MASTER);
    let expected = [
        0xfff9_31f2_u32,
        0x5ec6_7ad1,
        0x25f3_0544,
        0xaa40_e9d7,
        0x1f60_1349,
        0xbc46_bec2,
        0xc3ca_c3e9,
        0x361a_1ad9,
        0x08b3_4059,
        0x9f7c_85c2,
        0x54e2_d629,
        0x9ad4_2885,
        0x0a1b_2b61,
        0x165d_76e6,
        0xfbae_85e5,
        0x7988_3646,
    ];
    for (index, word) in expected.into_iter().enumerate() {
        assert_eq!(rng.next_u32(), word, "word {index}");
    }
}

#[test]
fn the_buffer_boundary_matches_the_independent_vector() {
    let mut rng = Rng::from_seed(MASTER);
    for _ in 0..62 {
        let _ = rng.next_u32();
    }

    // Words 62..66: the last two words of the fourth ChaCha12 block and the first two of the fifth.
    let expected = [0x5a25_3879_u32, 0x4b62_6621, 0x56e9_6aa5, 0x5d53_7674];
    for word in expected {
        assert_eq!(rng.next_u32(), word);
    }
}

#[test]
fn next_u64_combines_two_words_low_first() {
    let mut pair = Rng::from_seed(MASTER);
    let low = pair.next_u32();
    let high = pair.next_u32();
    let expected = u64::from(low) | (u64::from(high) << 32);

    let mut wide = Rng::from_seed(MASTER);
    assert_eq!(wide.next_u64(), expected);
    assert_eq!(
        expected, 0x5ec6_7ad1_fff9_31f2,
        "the independent vector's first u64"
    );
}

#[test]
fn next_f64_uses_the_top_53_bits() {
    let mut rng = Rng::from_seed(MASTER);
    let expected = [
        0.370_216_060_895_448_56_f64,
        0.665_053_000_475_335_7,
        0.735_454_485_314_562_8,
        0.211_335_828_938_710_6,
    ];
    for value in expected {
        assert_eq!(rng.next_f64(), value);
        assert!((0.0..1.0).contains(&value));
    }
}

#[test]
fn bounded_draws_match_the_independent_vector() {
    let cases: [(u32, [u32; 8]); 3] = [
        (2, [1, 0, 0, 1, 0, 1, 1, 0]),
        (10, [9, 3, 1, 6, 1, 7, 7, 2]),
        (1024, [1023, 379, 151, 681, 125, 753, 783, 216]),
    ];

    for (upper, expected) in cases {
        let mut rng = Rng::from_seed(MASTER);
        for (index, value) in expected.into_iter().enumerate() {
            let drawn = rng.next_bounded(upper);
            assert_eq!(drawn, value, "upper={upper} draw {index}");
            assert!(drawn < upper);
        }
    }
}

#[test]
fn a_zero_bound_consumes_no_word() {
    let mut rng = Rng::from_seed(MASTER);
    let before = rng.export_state();
    assert_eq!(rng.next_bounded(0), 0);
    assert_eq!(rng.export_state(), before);
}

#[test]
fn fork_matches_the_independent_derivation() {
    let mut parent = Rng::from_seed(MASTER);
    let mut child = parent.fork(&[0x41; 32]);
    let expected = [
        0x989d_10fe_u32,
        0x8382_6b54,
        0x6703_9c18,
        0x54f6_1693,
        0xd07a_991d,
        0xdd54_e406,
        0xc5f3_79e9,
        0x0957_e3ba,
    ];
    for word in expected {
        assert_eq!(child.next_u32(), word);
    }
}

#[test]
fn the_state_blob_is_frozen_after_seventeen_words() {
    let mut rng = Rng::from_seed(MASTER);
    for _ in 0..17 {
        let _ = rng.next_u32();
    }
    let state = rng.export_state();

    // 64-byte `MCR1` blob: version 1, stream 0, word position 17, and the master key.
    assert_eq!(state.stream(), 0);
    assert_eq!(state.word_pos(), 17);
    assert_eq!(
        to_hex(state.as_bytes()),
        "4d43523101000000000000000000000011000000000000000000000000000000000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"
    );

    let mut resumed = Rng::from_state(&state).expect("restore");
    let expected_next = [
        0xe072_3b83_u32,
        0x5215_5cdb,
        0x4683_d13d,
        0xeb0c_8e35,
        0x4864_1b2e,
        0x3230_9d04,
        0x1685_ee7e,
        0xa35e_c622,
    ];
    for word in expected_next {
        assert_eq!(resumed.next_u32(), word);
    }
}

#[test]
fn restoring_a_state_reproduces_the_continuation_exactly() {
    let mut reference = Rng::from_seed(MASTER);
    for _ in 0..1000 {
        let _ = reference.next_u64();
    }
    let state = RngStateV1::from_bytes(reference.export_state().as_bytes()).expect("valid");

    let mut resumed = Rng::from_state(&state).expect("restore");
    for _ in 0..64 {
        assert_eq!(resumed.next_u32(), reference.next_u32());
    }
}

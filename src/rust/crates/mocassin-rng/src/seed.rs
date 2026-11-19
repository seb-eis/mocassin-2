// SPDX-License-Identifier: MIT

//! Deterministic seed derivation for the MOCASSIN solver.
//!
//! The formulas are fixed by the Step 10 build contract and reproduced here for verification and tests:
//!
//! ```text
//! derive_job_seed(master, collection, config, job)
//!   = SHA-256(master[32] || u32_le(collection) || u32_le(config) || u32_le(job)
//!             || UTF8("mocassin/job/v1"))
//! derive_lattice_seed(job_seed, lattice_request_hash)
//!   = SHA-256(UTF8("mocassin/lattice/v1") || job_seed[32] || lattice_request_hash[32])
//! ```
//!
//! The .NET build derives and persists the job seed; the solver uses the package seed unless `--seed` overrides
//! it for one process (Step 13 design §4.1). The solver never re-derives a seed silently.

use sha2::{Digest, Sha256};

/// Length of a master seed, job seed, lattice seed, or lattice-request hash in bytes.
pub const SEED_BYTES: usize = 32;

/// Terminal domain-separation string of a job seed.
pub const JOB_SEED_DOMAIN: &str = "mocassin/job/v1";

/// Prefix domain-separation string of a lattice seed.
pub const LATTICE_SEED_DOMAIN: &str = "mocassin/lattice/v1";

/// Derives the 32-byte job seed from the master seed and the three zero-based indices.
///
/// The index fields are written as fixed little-endian `u32` values, in the documented order.
#[must_use]
pub fn derive_job_seed(
    master: &[u8; SEED_BYTES],
    collection: u32,
    config: u32,
    job: u32,
) -> [u8; SEED_BYTES] {
    let mut hasher = Sha256::new();
    hasher.update(master);
    hasher.update(collection.to_le_bytes());
    hasher.update(config.to_le_bytes());
    hasher.update(job.to_le_bytes());
    hasher.update(JOB_SEED_DOMAIN.as_bytes());
    hasher.finalize().into()
}

/// Derives the 32-byte lattice seed from a job seed and the canonical lattice-request hash.
#[must_use]
pub fn derive_lattice_seed(
    job_seed: &[u8; SEED_BYTES],
    lattice_request_hash: &[u8; SEED_BYTES],
) -> [u8; SEED_BYTES] {
    let mut hasher = Sha256::new();
    hasher.update(LATTICE_SEED_DOMAIN.as_bytes());
    hasher.update(job_seed);
    hasher.update(lattice_request_hash);
    hasher.finalize().into()
}

/// Returns `SHA-256(seed)` as 64 lowercase hexadecimal characters.
///
/// This is the only seed value that may appear in a summary or a log; the raw seed must never be logged.
#[must_use]
pub fn seed_hash(seed: &[u8; SEED_BYTES]) -> String {
    to_lower_hex(&Sha256::digest(seed))
}

fn to_lower_hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
}

#[cfg(test)]
mod tests {
    use super::{SEED_BYTES, derive_job_seed, derive_lattice_seed, seed_hash};

    const MASTER: [u8; SEED_BYTES] = [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
        0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d,
        0x1e, 0x1f,
    ];

    const CERIA_JOB_SEED: &str = "b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e";
    const CERIA_REQUEST_HASH: &str =
        "5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295";

    fn from_hex(value: &str) -> [u8; SEED_BYTES] {
        let mut bytes = [0u8; SEED_BYTES];
        let (pairs, _) = value.as_bytes().as_chunks::<2>();
        for (index, chunk) in pairs.iter().enumerate() {
            let text = core::str::from_utf8(chunk.as_slice()).expect("ASCII hex");
            bytes[index] = u8::from_str_radix(text, 16).expect("hex pair");
        }
        bytes
    }

    fn to_hex(bytes: &[u8]) -> String {
        super::to_lower_hex(bytes)
    }

    #[test]
    fn job_seed_matches_the_step_10_golden_vector() {
        let seed = derive_job_seed(&MASTER, 0, 0, 0);
        assert_eq!(
            to_hex(&seed),
            "884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7"
        );
    }

    #[test]
    fn job_seed_indices_use_fixed_little_endian_fields() {
        let seed = derive_job_seed(&MASTER, 1, 2, 3);
        assert_eq!(
            to_hex(&seed),
            "172da959820807561e234261342a48e26076c59e1af34a01487919d16a7daccf"
        );
    }

    #[test]
    fn lattice_seed_matches_the_step_10_golden_vector() {
        let seed = derive_lattice_seed(&from_hex(CERIA_JOB_SEED), &from_hex(CERIA_REQUEST_HASH));
        assert_eq!(
            to_hex(&seed),
            "a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2"
        );
    }

    #[test]
    fn derivation_is_deterministic_and_separated_by_every_field() {
        assert_eq!(
            derive_job_seed(&MASTER, 4, 5, 6),
            derive_job_seed(&MASTER, 4, 5, 6)
        );

        let base = derive_job_seed(&MASTER, 0, 0, 0);
        assert_ne!(base, derive_job_seed(&MASTER, 1, 0, 0));
        assert_ne!(base, derive_job_seed(&MASTER, 0, 1, 0));
        assert_ne!(base, derive_job_seed(&MASTER, 0, 0, 1));

        let job = from_hex(CERIA_JOB_SEED);
        let request = from_hex(CERIA_REQUEST_HASH);
        let lattice = derive_lattice_seed(&job, &request);
        assert_ne!(lattice, derive_lattice_seed(&MASTER, &request));
        assert_ne!(lattice, derive_lattice_seed(&job, &MASTER));
        assert_ne!(base, lattice);
    }

    #[test]
    fn seed_hash_is_sha256_lowercase_hex() {
        assert_eq!(
            seed_hash(&[0u8; SEED_BYTES]),
            "66687aadf862bd776c8fc18b8e9f8e20089714856ee233b3902a591d0d5f2925"
        );
        assert_eq!(seed_hash(&MASTER).len(), 64);
    }
}

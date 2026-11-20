// SPDX-License-Identifier: MIT

//! The ChaCha12 draw stream.
//!
//! [`Rng`] wraps `rand_chacha::ChaCha12Rng` and fixes the draw semantics from the Step 13 design §4.2. The
//! wrapper never exposes the crate's buffered block or memory layout; the replayable state is the first-party
//! [`RngStateV1`] blob. All randomness in the solver comes from this type: no thread-local generator, no
//! time-based seed, and no operating-system random source.

use core::fmt;

use rand::{Rng as _, SeedableRng};
use rand_chacha::ChaCha12Rng;
use sha2::{Digest, Sha256};

use crate::state::{RngStateError, RngStateV1};

/// Algorithm identifier bound to `.mcs` `rngAlgorithm`.
pub const RNG_ALGORITHM: &str = "chacha12-v1";

/// Domain-separation prefix of the fork derivation.
pub const RNG_FORK_DOMAIN: &str = "mocassin/rng-fork/v1";

/// Longest allowed fork domain in bytes.
pub const MAX_FORK_DOMAIN_BYTES: usize = 255;

/// Largest word position the ChaCha12 68-bit counter supports (`2^68`).
const MAX_WORD_POSITION: u128 = 1_u128 << 68;

/// Deterministic ChaCha12 stream for one simulation or extension.
pub struct Rng {
    inner: ChaCha12Rng,
}

impl Rng {
    /// Creates a stream from a 32-byte seed at stream 0, word position 0.
    #[must_use]
    pub fn from_seed(seed: [u8; 32]) -> Self {
        Self {
            inner: ChaCha12Rng::from_seed(seed),
        }
    }

    /// Restores a stream from a validated [`RngStateV1`].
    ///
    /// Resuming mid-block repopulates the internal buffer and skips to the exact 32-bit word, so the next draw
    /// is identical to an uninterrupted stream.
    ///
    /// # Errors
    ///
    /// Rejects a state whose word position does not fit the 68-bit counter. [`RngStateV1::from_bytes`] already
    /// checks the bound, so this is a defensive re-check.
    pub fn from_state(state: &RngStateV1) -> Result<Self, RngStateError> {
        if state.word_pos() >= MAX_WORD_POSITION {
            return Err(RngStateError::WordPositionOutOfRange {
                word_pos: state.word_pos(),
            });
        }

        let mut inner = ChaCha12Rng::from_seed(*state.key());
        inner.set_stream(state.stream());
        inner.set_word_pos(state.word_pos());
        Ok(Self { inner })
    }

    /// Exports the replayable state of this stream.
    ///
    /// The blob contains the raw key. Treat it as secret material; see [`RngStateV1`].
    #[must_use]
    pub fn export_state(&self) -> RngStateV1 {
        RngStateV1::from_parts(
            self.inner.get_seed(),
            self.inner.get_stream(),
            self.inner.get_word_pos(),
        )
    }

    /// Returns one uniform `u32`; consumes one 32-bit word.
    #[inline]
    pub fn next_u32(&mut self) -> u32 {
        self.inner.next_u32()
    }

    /// Returns one uniform `u64`; consumes two words, low word first.
    #[inline]
    pub fn next_u64(&mut self) -> u64 {
        let low = u64::from(self.next_u32());
        let high = u64::from(self.next_u32());
        low | (high << 32)
    }

    /// Returns a uniform double in `[0, 1)`; consumes two words.
    ///
    /// The value uses the 53 high bits of a `u64`, so every representable double in `[0, 1)` is equally likely
    /// at the 2^-53 resolution.
    #[inline]
    pub fn next_f64(&mut self) -> f64 {
        /// `2^-53`.
        const SCALE: f64 = 1.0 / 9_007_199_254_740_992.0;

        ((self.next_u64() >> 11) as f64) * SCALE
    }

    /// Returns a uniform integer in `[0, upper)` using Lemire multiply-and-reject.
    ///
    /// The result is unbiased and half-open. `upper == 0` returns `0` and consumes no word. A rejected draw
    /// consumes one extra word per attempt; for the small upper bounds used by the solver the expected number
    /// of attempts is below 1.01.
    ///
    /// # Panics
    ///
    /// This function does not panic for any `u32` input.
    #[inline]
    pub fn next_bounded(&mut self, upper: u32) -> u32 {
        if upper == 0 {
            return 0;
        }

        let threshold = upper.wrapping_neg() % upper;
        let mut product = u64::from(self.next_u32()) * u64::from(upper);
        let mut low = product as u32;
        while low < threshold {
            product = u64::from(self.next_u32()) * u64::from(upper);
            low = product as u32;
        }

        (product >> 32) as u32
    }

    /// Derives a new independent stream from this one and a domain string.
    ///
    /// The hash is `SHA-256("mocassin/rng-fork/v1" || 0x00 || key[32] || stream_le64 || word_pos_le128 ||
    /// domain)`. The parent is not advanced. The child starts at stream 0, word position 0.
    ///
    /// # Panics
    ///
    /// Panics when the domain is empty or longer than [`MAX_FORK_DOMAIN_BYTES`]. Use [`Rng::try_fork`] for a
    /// caller-supplied domain that may be untrusted.
    #[must_use]
    pub fn fork(&mut self, domain: &[u8]) -> Self {
        self.try_fork(domain)
            .expect("the fork domain must be non-empty and at most 255 bytes")
    }

    /// Fallible form of [`Rng::fork`] for caller-supplied domains.
    ///
    /// # Errors
    ///
    /// Rejects an empty domain and a domain longer than [`MAX_FORK_DOMAIN_BYTES`].
    pub fn try_fork(&mut self, domain: &[u8]) -> Result<Self, InvalidForkDomain> {
        if domain.is_empty() {
            return Err(InvalidForkDomain::Empty);
        }

        if domain.len() > MAX_FORK_DOMAIN_BYTES {
            return Err(InvalidForkDomain::TooLong { len: domain.len() });
        }

        let mut hasher = Sha256::new();
        hasher.update(RNG_FORK_DOMAIN.as_bytes());
        hasher.update([0u8]);
        hasher.update(self.inner.get_seed());
        hasher.update(self.inner.get_stream().to_le_bytes());
        hasher.update(self.inner.get_word_pos().to_le_bytes());
        hasher.update(domain);
        let digest: [u8; 32] = hasher.finalize().into();

        Ok(Self::from_seed(digest))
    }
}

impl fmt::Debug for Rng {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter
            .debug_struct("Rng")
            .field("algorithm", &RNG_ALGORITHM)
            .field("stream", &self.inner.get_stream())
            .field("word_pos", &self.inner.get_word_pos())
            .finish()
    }
}

/// A fork domain violates the derivation contract.
#[derive(Clone, Debug, PartialEq, Eq, thiserror::Error)]
pub enum InvalidForkDomain {
    /// The domain is empty.
    #[error("the fork domain must not be empty")]
    Empty,
    /// The domain is longer than 255 bytes.
    #[error("the fork domain is {len} bytes; the maximum is {max}", max = MAX_FORK_DOMAIN_BYTES)]
    TooLong {
        /// Length of the rejected domain.
        len: usize,
    },
}

#[cfg(test)]
mod tests {
    use super::{InvalidForkDomain, Rng, RngStateV1};
    use crate::state::RNG_STATE_BYTES;

    const MASTER: [u8; 32] = [
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e,
        0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d,
        0x1e, 0x1f,
    ];

    #[test]
    fn export_uses_the_documented_64_byte_layout() {
        let mut rng = Rng::from_seed(MASTER);
        let _ = rng.next_u32();
        let state = rng.export_state();
        let bytes = state.as_bytes();

        assert_eq!(bytes.len(), RNG_STATE_BYTES);
        assert_eq!(&bytes[0..4], b"MCR1");
        assert_eq!(
            u32::from_le_bytes(bytes[4..8].try_into().expect("4 bytes")),
            1
        );
        assert_eq!(
            u64::from_le_bytes(bytes[8..16].try_into().expect("8 bytes")),
            0
        );
        assert_eq!(
            u128::from_le_bytes(bytes[16..32].try_into().expect("16 bytes")),
            1
        );
        assert_eq!(&bytes[32..64], MASTER.as_slice());
    }

    #[test]
    fn resume_at_block_offsets_matches_the_uninterrupted_stream() {
        for drawn in [0_usize, 15, 16, 17, 1000] {
            let mut reference = Rng::from_seed(MASTER);
            for _ in 0..drawn {
                let _ = reference.next_u32();
            }

            let state = reference.export_state();
            assert_eq!(state.word_pos(), drawn as u128);

            let mut resumed = Rng::from_state(&state).expect("restore");
            for _ in 0..8 {
                assert_eq!(resumed.next_u32(), reference.next_u32());
            }
        }
    }

    #[test]
    fn a_nonzero_stream_resumes_at_the_exact_word() {
        // Independent ChaCha12 vector: key = MASTER, stream = 7, word position 21 (block 1, offset 5).
        let mut bytes = Rng::from_seed(MASTER).export_state().to_bytes();
        bytes[8..16].copy_from_slice(&7_u64.to_le_bytes());
        bytes[16..32].copy_from_slice(&21_u128.to_le_bytes());

        let state = RngStateV1::from_bytes(&bytes).expect("valid patched state");
        let mut rng = Rng::from_state(&state).expect("restore");

        let expected = [
            0x7d91_22fe_u32,
            0x2f4c_f32a,
            0x1b02_bdf7,
            0x2c38_3c16,
            0x1f45_85ee,
            0x57e5_f4cb,
            0x85e8_4032,
            0xdc20_6c88,
        ];
        for word in expected {
            assert_eq!(rng.next_u32(), word);
        }
    }

    #[test]
    fn fork_does_not_advance_the_parent() {
        let mut rng = Rng::from_seed(MASTER);
        let _ = rng.next_u32();
        let before = rng.export_state();

        let _child = rng.fork(b"extension:test");
        assert_eq!(rng.export_state(), before);
    }

    #[test]
    fn fork_rejects_an_empty_or_oversized_domain() {
        let mut rng = Rng::from_seed(MASTER);
        assert_eq!(
            rng.try_fork(b"").expect_err("empty domain"),
            InvalidForkDomain::Empty
        );
        assert_eq!(
            rng.try_fork(&[0x41; 256]).expect_err("oversized domain"),
            InvalidForkDomain::TooLong { len: 256 }
        );
        assert!(rng.try_fork(&[0x41; 255]).is_ok());
    }

    #[test]
    #[should_panic(expected = "the fork domain must be non-empty")]
    fn fork_panics_on_an_empty_domain() {
        let mut rng = Rng::from_seed(MASTER);
        let _ = rng.fork(b"");
    }

    #[test]
    fn fork_separates_the_child_stream() {
        let mut parent = Rng::from_seed(MASTER);
        let mut first = parent.fork(b"extension:a");
        let mut second = parent.fork(b"extension:b");

        assert_ne!(first.next_u32(), second.next_u32());
        assert_ne!(first.export_state(), second.export_state());
        assert_ne!(
            first.export_state().to_bytes(),
            parent.export_state().to_bytes()
        );
    }

    #[test]
    fn debug_output_does_not_expose_the_key() {
        let rng = Rng::from_seed(MASTER);
        let debug = format!("{rng:?}");
        assert!(debug.contains("chacha12-v1"));
        assert!(!debug.contains("0001020304050607"), "{debug}");
        assert!(!debug.contains("00010203"), "{debug}");

        let state_debug = format!("{:?}", rng.export_state());
        assert!(state_debug.contains("<redacted>"));
    }
}

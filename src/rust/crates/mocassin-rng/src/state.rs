// SPDX-License-Identifier: MIT

//! The first-party 64-byte `RngStateV1` resume blob.
//!
//! The layout is fixed by the Step 13 data-contract decision note and is bound to `.mcs`
//! `rngAlgorithm = "chacha12-v1"` and `rngState`:
//!
//! | Offset | Size | Field |
//! | ---: | ---: | --- |
//! | 0 | 4 | magic `0x4D 0x43 0x52 0x31` (`"MCR1"`) |
//! | 4 | 4 | `format_version` (`u32` little-endian, value 1) |
//! | 8 | 8 | `stream` (`u64` little-endian) |
//! | 16 | 16 | `word_pos` (`u128` little-endian) |
//! | 32 | 32 | ChaCha12 key / seed (`[u8; 32]`) |
//!
//! The blob carries the raw 32-byte key by design. A `.mcs` is therefore as sensitive as the seed: never log
//! `rngState` or the key, and expose only `seedHash` in summaries. [`RngStateV1`] deliberately exposes no key
//! accessor and redacts the key in its `Debug` output.

use core::fmt;

/// Byte length of an encoded [`RngStateV1`].
pub const RNG_STATE_BYTES: usize = 64;

/// Magic bytes at the start of an encoded [`RngStateV1`]: `"MCR1"`.
pub const RNG_STATE_MAGIC: [u8; 4] = *b"MCR1";

/// Current [`RngStateV1`] format version.
pub const RNG_STATE_VERSION: u32 = 1;

/// Largest word position the ChaCha12 68-bit counter supports (`2^68`).
const MAX_WORD_POSITION: u128 = 1_u128 << 68;

/// Validated 64-byte ChaCha12 resume state.
///
/// A value can only be built from bytes that pass [`RngStateV1::from_bytes`] or by exporting a live stream, so
/// holding one means the magic, version, and word-position bound were checked.
#[derive(Clone, PartialEq, Eq)]
pub struct RngStateV1 {
    bytes: [u8; RNG_STATE_BYTES],
}

impl RngStateV1 {
    /// Decodes and validates a 64-byte state blob.
    ///
    /// # Errors
    ///
    /// Rejects a wrong length, a bad magic, an unsupported version, and a word position whose 32-bit-word
    /// offset does not fit the ChaCha12 68-bit counter.
    pub fn from_bytes(bytes: &[u8]) -> Result<Self, RngStateError> {
        if bytes.len() != RNG_STATE_BYTES {
            return Err(RngStateError::BadLength {
                found: bytes.len(),
                expected: RNG_STATE_BYTES,
            });
        }

        let mut raw = [0u8; RNG_STATE_BYTES];
        raw.copy_from_slice(bytes);

        if raw[0..4] != RNG_STATE_MAGIC {
            return Err(RngStateError::BadMagic {
                found: [raw[0], raw[1], raw[2], raw[3]],
            });
        }

        let version = u32::from_le_bytes([raw[4], raw[5], raw[6], raw[7]]);
        if version != RNG_STATE_VERSION {
            return Err(RngStateError::UnsupportedVersion { found: version });
        }

        let word_pos = u128::from_le_bytes(raw[16..32].try_into().expect("16-byte slice"));
        if word_pos >= MAX_WORD_POSITION {
            return Err(RngStateError::WordPositionOutOfRange { word_pos });
        }

        Ok(Self { bytes: raw })
    }

    /// Returns the encoded 64-byte blob.
    #[must_use]
    pub const fn as_bytes(&self) -> &[u8; RNG_STATE_BYTES] {
        &self.bytes
    }

    /// Returns a copy of the encoded 64-byte blob.
    #[must_use]
    pub const fn to_bytes(&self) -> [u8; RNG_STATE_BYTES] {
        self.bytes
    }

    /// Returns the ChaCha12 stream identifier.
    #[must_use]
    pub fn stream(&self) -> u64 {
        u64::from_le_bytes(self.bytes[8..16].try_into().expect("8-byte slice"))
    }

    /// Returns the 32-bit-word offset inside the stream.
    #[must_use]
    pub fn word_pos(&self) -> u128 {
        u128::from_le_bytes(self.bytes[16..32].try_into().expect("16-byte slice"))
    }

    /// Returns the ChaCha12 key.
    ///
    /// Crate-internal: no public accessor exists, so callers cannot log the key by accident. Only the stream
    /// implementation and the state tests read it.
    pub(crate) fn key(&self) -> &[u8; 32] {
        self.bytes[32..64].try_into().expect("32-byte slice")
    }

    /// Builds a state from a live stream.
    ///
    /// The ChaCha12 word position is a 68-bit value by construction; the bound is asserted in debug builds and
    /// re-checked by [`RngStateV1::from_bytes`] when the blob is read back.
    pub(crate) fn from_parts(seed: [u8; 32], stream: u64, word_pos: u128) -> Self {
        debug_assert!(
            word_pos < MAX_WORD_POSITION,
            "word position exceeds 68 bits"
        );

        let mut bytes = [0u8; RNG_STATE_BYTES];
        bytes[0..4].copy_from_slice(&RNG_STATE_MAGIC);
        bytes[4..8].copy_from_slice(&RNG_STATE_VERSION.to_le_bytes());
        bytes[8..16].copy_from_slice(&stream.to_le_bytes());
        bytes[16..32].copy_from_slice(&word_pos.to_le_bytes());
        bytes[32..64].copy_from_slice(&seed);
        Self { bytes }
    }
}

impl fmt::Debug for RngStateV1 {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter
            .debug_struct("RngStateV1")
            .field("stream", &self.stream())
            .field("word_pos", &self.word_pos())
            .field("key", &"<redacted>")
            .finish()
    }
}

/// A state blob does not match the `RngStateV1` layout.
#[derive(Clone, Debug, PartialEq, Eq, thiserror::Error)]
pub enum RngStateError {
    /// The encoded blob is not 64 bytes.
    #[error("RNG state must be {expected} bytes, found {found}")]
    BadLength {
        /// Number of bytes supplied.
        found: usize,
        /// Required number of bytes.
        expected: usize,
    },
    /// The magic bytes are not `"MCR1"`.
    #[error("RNG state magic must be \"MCR1\", found {found:02x?}")]
    BadMagic {
        /// The four bytes that were read.
        found: [u8; 4],
    },
    /// The format version is not the supported version.
    #[error("unsupported RNG state format version {found}; expected {expected}", expected = RNG_STATE_VERSION)]
    UnsupportedVersion {
        /// The version that was read.
        found: u32,
    },
    /// The word position does not fit the ChaCha12 68-bit counter.
    #[error("RNG word position {word_pos} does not fit the 68-bit ChaCha12 counter")]
    WordPositionOutOfRange {
        /// The rejected word position.
        word_pos: u128,
    },
}

#[cfg(test)]
mod tests {
    use super::{RNG_STATE_BYTES, RNG_STATE_MAGIC, RNG_STATE_VERSION, RngStateError, RngStateV1};

    fn canonical() -> [u8; RNG_STATE_BYTES] {
        let mut bytes = [0u8; RNG_STATE_BYTES];
        bytes[0..4].copy_from_slice(&RNG_STATE_MAGIC);
        bytes[4..8].copy_from_slice(&RNG_STATE_VERSION.to_le_bytes());
        bytes[8..16].copy_from_slice(&7_u64.to_le_bytes());
        bytes[16..32].copy_from_slice(&1234_u128.to_le_bytes());
        for (index, byte) in bytes[32..64].iter_mut().enumerate() {
            *byte = index as u8;
        }
        bytes
    }

    #[test]
    fn a_canonical_blob_decodes_its_fields() {
        let state = RngStateV1::from_bytes(&canonical()).expect("valid state");
        assert_eq!(state.stream(), 7);
        assert_eq!(state.word_pos(), 1234);
        assert_eq!(state.key().to_vec(), canonical()[32..64].to_vec());
        assert_eq!(state.to_bytes(), canonical());
        assert_eq!(state.as_bytes(), &canonical());
    }

    #[test]
    fn a_wrong_length_is_rejected() {
        for length in [0, 63, 65, 128] {
            let error = RngStateV1::from_bytes(&vec![0u8; length]).expect_err("length must fail");
            assert_eq!(
                error,
                RngStateError::BadLength {
                    found: length,
                    expected: RNG_STATE_BYTES,
                }
            );
        }
    }

    #[test]
    fn a_bad_magic_is_rejected() {
        let mut bytes = canonical();
        bytes[0] = b'X';
        let error = RngStateV1::from_bytes(&bytes).expect_err("magic must fail");
        assert_eq!(error, RngStateError::BadMagic { found: *b"XCR1" });
        assert!(error.to_string().contains("MCR1"));
    }

    #[test]
    fn an_unsupported_version_is_rejected() {
        let mut bytes = canonical();
        bytes[4..8].copy_from_slice(&2_u32.to_le_bytes());
        let error = RngStateV1::from_bytes(&bytes).expect_err("version must fail");
        assert_eq!(error, RngStateError::UnsupportedVersion { found: 2 });
        assert!(error.to_string().contains("version"));
    }

    #[test]
    fn a_word_position_at_or_above_two_to_the_sixty_eighth_is_rejected() {
        for word_pos in [1_u128 << 68, (1_u128 << 68) + 1, u128::MAX] {
            let mut bytes = canonical();
            bytes[16..32].copy_from_slice(&word_pos.to_le_bytes());
            let error = RngStateV1::from_bytes(&bytes).expect_err("word position must fail");
            assert_eq!(error, RngStateError::WordPositionOutOfRange { word_pos });
        }

        let mut bytes = canonical();
        bytes[16..32].copy_from_slice(&((1_u128 << 68) - 1).to_le_bytes());
        assert!(RngStateV1::from_bytes(&bytes).is_ok());
    }

    #[test]
    fn debug_output_redacts_the_key() {
        let state = RngStateV1::from_bytes(&canonical()).expect("valid state");
        let debug = format!("{state:?}");
        assert!(debug.contains("stream: 7"));
        assert!(debug.contains("word_pos: 1234"));
        assert!(debug.contains("<redacted>"));
        assert!(!debug.contains("0001020304050607"), "{debug}");
    }
}

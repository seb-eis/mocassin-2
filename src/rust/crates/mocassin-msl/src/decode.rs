// SPDX-License-Identifier: MIT

//! Zstd frame handling and the bounded typed decode.
//!
//! Every `.msl` payload is one standard Zstd frame whose contents are a MessagePack map. The reader checks the
//! declared decompressed size (or streams with a hard limit when the frame declares none) before it allocates,
//! then scans the document with the bounded payload scan, and only then decodes into the `mocassin-types` DTOs.

use std::io::Read;

use mocassin_types::ReaderLimits;
use mocassin_types::payload::{JobPayload, LatticePayload, ModelPayload};

use crate::error::PackageError;
use crate::scan;

/// The only supported package and payload encoding version in v1.
const SUPPORTED_VERSION: u32 = 1;

/// Decompresses one payload frame, enforcing `limit` on the decompressed size.
///
/// A frame that records its content size is rejected before decompression when the size exceeds `limit`. A
/// frame without a recorded size is streamed and the output is bounded while it grows, so a decompression bomb
/// cannot allocate past the limit.
pub fn decompress_payload(
    section: &'static str,
    compressed: &[u8],
    limit: u64,
) -> Result<Vec<u8>, PackageError> {
    if compressed.is_empty() {
        return Err(PackageError::InvalidFrame {
            section,
            detail: "the payload is empty".to_owned(),
        });
    }

    match zstd::decompressed_size(compressed) {
        Some(declared) => {
            if declared > limit {
                return Err(PackageError::PayloadTooLarge {
                    section,
                    declared,
                    limit,
                });
            }

            let capacity =
                usize::try_from(declared).map_err(|_| PackageError::PayloadTooLarge {
                    section,
                    declared,
                    limit: limit.min(usize::MAX as u64),
                })?;
            let output = zstd::bulk::decompress(compressed, capacity).map_err(|error| {
                PackageError::InvalidFrame {
                    section,
                    detail: error.to_string(),
                }
            })?;
            if output.len() as u64 != declared {
                return Err(PackageError::InvalidFrame {
                    section,
                    detail: format!(
                        "the frame declares {declared} bytes but decompressed to {}",
                        output.len()
                    ),
                });
            }
            Ok(output)
        }
        None => {
            let mut decoder = zstd::stream::read::Decoder::new(compressed).map_err(|error| {
                PackageError::InvalidFrame {
                    section,
                    detail: error.to_string(),
                }
            })?;

            let reserve = limit.min(64 * 1024) as usize;
            let mut output = Vec::with_capacity(reserve);
            let mut buffer = [0_u8; 16 * 1024];
            loop {
                let read =
                    decoder
                        .read(&mut buffer)
                        .map_err(|error| PackageError::InvalidFrame {
                            section,
                            detail: error.to_string(),
                        })?;
                if read == 0 {
                    break;
                }

                let grown = output.len() as u64 + read as u64;
                if grown > limit {
                    return Err(PackageError::PayloadTooLarge {
                        section,
                        declared: grown,
                        limit,
                    });
                }
                output.extend_from_slice(&buffer[..read]);
            }
            Ok(output)
        }
    }
}

/// Scans and decodes a decompressed model payload.
///
/// # Errors
///
/// Returns a scan, decode, budget, or unsupported-version error. No allocation happens before the scan passes.
pub fn decode_model_payload(
    payload: &[u8],
    limits: &ReaderLimits,
) -> Result<ModelPayload, PackageError> {
    let facts = scan::scan_model(payload, limits)?;
    require_version(facts.format_version)?;
    rmp_serde::from_slice(payload).map_err(|error| PackageError::Decode {
        section: "model",
        detail: error.to_string(),
    })
}

/// Scans and decodes a decompressed job payload.
///
/// # Errors
///
/// Returns a scan, decode, budget, or unsupported-version error.
pub fn decode_job_payload(
    payload: &[u8],
    limits: &ReaderLimits,
) -> Result<JobPayload, PackageError> {
    let facts = scan::scan_job(payload, limits)?;
    require_version(facts.format_version)?;
    rmp_serde::from_slice(payload).map_err(|error| PackageError::Decode {
        section: "job",
        detail: error.to_string(),
    })
}

/// Scans and decodes a decompressed lattice payload.
///
/// # Errors
///
/// Returns a scan, decode, guard, budget, or unsupported-version error.
pub fn decode_lattice_payload(
    payload: &[u8],
    limits: &ReaderLimits,
) -> Result<LatticePayload, PackageError> {
    let facts = scan::scan_lattice(payload, limits)?;
    require_version(facts.format_version)?;
    rmp_serde::from_slice(payload).map_err(|error| PackageError::Decode {
        section: "lattice",
        detail: error.to_string(),
    })
}

fn require_version(found: Option<u64>) -> Result<(), PackageError> {
    let found = found.unwrap_or(0);
    if found == u64::from(SUPPORTED_VERSION) {
        return Ok(());
    }

    Err(PackageError::SchemaVersion {
        found: u32::try_from(found).unwrap_or(u32::MAX),
        supported: SUPPORTED_VERSION..=SUPPORTED_VERSION,
    })
}

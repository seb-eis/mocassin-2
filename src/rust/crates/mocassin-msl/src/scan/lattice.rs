// SPDX-License-Identifier: MIT

//! The lattice-payload schema walk and its length guard.

use mocassin_types::ReaderLimits;

use super::facts::{LatticeFacts, validate_lattice};
use super::{Cursor, scan_map};
use crate::error::PackageError;

const LATTICE_KEYS: &[&str] = &[
    "format_version",
    "lattice_size",
    "position_count",
    "lattice",
];

/// Scans a decompressed lattice payload and applies the `a × b × c × position_count` guard.
pub(crate) fn scan_lattice(
    payload: &[u8],
    limits: &ReaderLimits,
) -> Result<LatticeFacts, PackageError> {
    // The guard allows at most `max_positions` particles; the extra slack covers the map and its keys.
    let element_budget = u64::from(limits.max_positions) + 64;
    let mut cursor = Cursor::new(
        payload,
        element_budget,
        limits.max_messagepack_depth,
        "lattice",
    );
    let mut facts = LatticeFacts::default();

    scan_map(&mut cursor, LATTICE_KEYS, |cursor, key| match key {
        "format_version" => {
            facts.format_version = Some(cursor.read_u64()?);
            Ok(())
        }
        "lattice_size" => {
            let length = cursor.read_array_len()?;
            if length != 3 {
                return Err(cursor.scan_error(format!(
                    "lattice_size must hold three values, found {length}"
                )));
            }
            for slot in &mut facts.lattice_size {
                *slot = cursor.read_u32()?;
            }
            cursor.leave();
            Ok(())
        }
        "position_count" => {
            facts.position_count = Some(u64::from(cursor.read_u32()?));
            Ok(())
        }
        "lattice" => {
            facts.lattice_len = Some(cursor.read_u8_sequence_len()?);
            Ok(())
        }
        _ => unreachable!(),
    })?;

    validate_lattice(
        &facts,
        limits.max_lattice_payload_bytes,
        u64::from(limits.max_positions),
        u64::from(limits.max_position_count),
    )?;
    cursor.finish()?;
    Ok(facts)
}

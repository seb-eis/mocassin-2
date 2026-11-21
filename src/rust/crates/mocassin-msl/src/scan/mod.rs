// SPDX-License-Identifier: MIT

//! Bounded MessagePack scanning for package payloads.
//!
//! The scan is the "validate before allocation" gate (Step 13 design §3.4). It walks a decompressed payload
//! once without building a document tree, enforces the element and depth budgets and every declared length,
//! rejects duplicate known keys and trailing bytes, and records the facts needed to cross-check the section
//! lengths. Only after the scan passes is the payload decoded into its typed DTO.

mod cursor;
mod facts;
mod job;
mod lattice;
mod model;

pub(crate) use cursor::Cursor;
pub(crate) use job::scan_job;
pub(crate) use lattice::scan_lattice;
pub(crate) use model::scan_model;

use crate::error::PackageError;

/// Iterates a schema map, rejects duplicate known keys, requires every required key, and skips unknown keys.
///
/// `required.len() + optional.len()` must be at most 128; every schema map in the payload is far below that.
/// An optional key participates in the duplicate mask but is not required by the post-loop check.
pub(crate) fn scan_map_optional<'a, F>(
    cursor: &mut Cursor<'a>,
    required: &'static [&'static str],
    optional: &'static [&'static str],
    mut handler: F,
) -> Result<(), PackageError>
where
    F: FnMut(&mut Cursor<'a>, &'static str) -> Result<(), PackageError>,
{
    debug_assert!(
        required.len() + optional.len() <= 128,
        "the duplicate mask holds 128 keys"
    );

    let length = cursor.read_map_len()?;
    let mut seen: u128 = 0;

    for _ in 0..length {
        let key = cursor.read_str()?;
        if let Some(index) = required.iter().position(|candidate| *candidate == key) {
            let bit = 1_u128 << index;
            if seen & bit != 0 {
                return Err(cursor.scan_error(format!("duplicate key \"{key}\"")));
            }
            seen |= bit;
            handler(cursor, required[index])?;
        } else if let Some(index) = optional.iter().position(|candidate| *candidate == key) {
            let bit = 1_u128 << (required.len() + index);
            if seen & bit != 0 {
                return Err(cursor.scan_error(format!("duplicate key \"{key}\"")));
            }
            seen |= bit;
            handler(cursor, optional[index])?;
        } else {
            cursor.skip_value()?;
        }
    }

    cursor.leave();

    let required_mask = if required.is_empty() {
        0
    } else if required.len() == 128 {
        u128::MAX
    } else {
        (1_u128 << required.len()) - 1
    };
    if seen & required_mask != required_mask {
        let missing = required
            .iter()
            .enumerate()
            .find(|(index, _)| seen & (1_u128 << index) == 0)
            .map(|(_, key)| *key)
            .unwrap_or("?");
        return Err(cursor.scan_error(format!("required key \"{missing}\" is absent")));
    }

    Ok(())
}

/// Iterates a schema map, rejects duplicate known keys, requires every known key, and skips unknown keys.
pub(crate) fn scan_map<'a, F>(
    cursor: &mut Cursor<'a>,
    known: &'static [&'static str],
    handler: F,
) -> Result<(), PackageError>
where
    F: FnMut(&mut Cursor<'a>, &'static str) -> Result<(), PackageError>,
{
    scan_map_optional(cursor, known, &[], handler)
}

/// Reads an array length, skips its values, and leaves the container.
pub(crate) fn skip_array(cursor: &mut Cursor<'_>) -> Result<u64, PackageError> {
    let length = cursor.read_array_len()?;
    cursor.skip_n(length)?;
    cursor.leave();
    Ok(length)
}

/// Reads an array of `rows` arrays of `columns` values and skips them.
pub(crate) fn scan_fixed_matrix(
    cursor: &mut Cursor<'_>,
    rows: u64,
    columns: u64,
) -> Result<(), PackageError> {
    let row_count = cursor.read_array_len()?;
    if row_count != rows {
        return Err(cursor.scan_error(format!(
            "expected a {rows}x{columns} matrix, found {row_count} rows"
        )));
    }
    for _ in 0..row_count {
        let column_count = cursor.read_array_len()?;
        if column_count != columns {
            return Err(cursor.scan_error(format!(
                "expected a {rows}x{columns} matrix, found {column_count} columns"
            )));
        }
        cursor.skip_n(column_count)?;
        cursor.leave();
    }
    cursor.leave();
    Ok(())
}

/// Rejects a count above a per-field cap.
pub(crate) fn check_cap(
    cursor: &Cursor<'_>,
    what: &str,
    value: u64,
    limit: u64,
) -> Result<(), PackageError> {
    if value > limit {
        return Err(cursor.scan_error(format!("{what} {value} exceeds the reader limit {limit}")));
    }
    Ok(())
}

/// Maps a payload kind string to its canonical spelling, or `None` for an unknown spelling.
pub(crate) fn kind_name(value: &str) -> Option<&'static str> {
    match value {
        "kmc" => Some("kmc"),
        "mmc" => Some("mmc"),
        _ => None,
    }
}

#[cfg(test)]
mod tests {
    use super::{Cursor, scan_map, scan_map_optional};

    const NO_REQUIRED: &[&str] = &[];
    const OPTIONAL_CODE_EXPANSIONS: &[&str] = &["code_expansions"];
    const NESTED_KEYS: &[&str] = &["expanded_codes", "canonical_indices"];

    fn write_string(bytes: &mut Vec<u8>, value: &str) {
        assert!(value.len() < 32);
        bytes.push(0xa0 | value.len() as u8);
        bytes.extend_from_slice(value.as_bytes());
    }

    #[test]
    fn a_duplicate_optional_key_is_rejected_at_scan_time() {
        let mut bytes = vec![0x82];
        write_string(&mut bytes, "code_expansions");
        bytes.push(0xc0);
        write_string(&mut bytes, "code_expansions");
        bytes.push(0xc0);

        let mut cursor = Cursor::new(&bytes, 100, 8, "model");
        let error = scan_map_optional(
            &mut cursor,
            NO_REQUIRED,
            OPTIONAL_CODE_EXPANSIONS,
            |cursor, _| cursor.skip_value(),
        )
        .expect_err("the duplicate optional key is rejected");
        assert!(error.to_string().contains("duplicate key"), "{error}");
    }

    #[test]
    fn a_duplicate_nested_key_is_rejected_at_scan_time() {
        let mut bytes = vec![0x82];
        write_string(&mut bytes, "expanded_codes");
        bytes.push(0xc0);
        write_string(&mut bytes, "expanded_codes");
        bytes.push(0xc0);

        let mut cursor = Cursor::new(&bytes, 100, 8, "model");
        let error = scan_map(&mut cursor, NESTED_KEYS, |cursor, _| cursor.skip_value())
            .expect_err("the duplicate nested key is rejected");
        assert!(error.to_string().contains("duplicate key"), "{error}");
    }
}

// SPDX-License-Identifier: MIT

//! The decoded payload → validated runtime mapping.
//!
//! Every index, length, finite value, kind, flag, and tracker value is checked in the submodules; the
//! scientific invariants (occupation union, mass/charge conservation, movement/path vector, transport vector)
//! are checked by [`crate::validate::validate_scientific`]. A failure is a structured [`PackageError`], never a
//! panic.

mod job;
mod model;
mod transition;

pub(crate) use job::{job_from_payload, lattice_from_payload};
pub(crate) use model::model_from_payload;

use crate::error::PackageError;
use mocassin_types::SimulationKind;

fn parse_kind(what: &'static str, value: &str) -> Result<SimulationKind, PackageError> {
    value.parse().map_err(|_| PackageError::InvalidValue {
        what,
        detail: format!("unknown simulation kind \"{value}\""),
    })
}

fn u32_len(value: usize) -> Result<u32, PackageError> {
    u32::try_from(value).map_err(|_| PackageError::InvalidValue {
        what: "payload counts",
        detail: "a section count does not fit a 32-bit unsigned value".to_owned(),
    })
}

fn require_column_u64(column: &'static str, row: u64, payload: u64) -> Result<(), PackageError> {
    if row == payload {
        Ok(())
    } else {
        Err(PackageError::ColumnMismatch {
            column,
            column_value: row.to_string(),
            payload_value: payload.to_string(),
        })
    }
}

fn require_column_u32(column: &'static str, row: u32, payload: u32) -> Result<(), PackageError> {
    if row == payload {
        Ok(())
    } else {
        Err(PackageError::ColumnMismatch {
            column,
            column_value: row.to_string(),
            payload_value: payload.to_string(),
        })
    }
}

fn require_column_option_u64(
    column: &'static str,
    row: Option<u64>,
    payload: Option<u64>,
) -> Result<(), PackageError> {
    if row == payload {
        Ok(())
    } else {
        Err(PackageError::ColumnMismatch {
            column,
            column_value: format!("{row:?}"),
            payload_value: format!("{payload:?}"),
        })
    }
}

fn require_column_f64(column: &'static str, row: f64, payload: f64) -> Result<(), PackageError> {
    if row == payload {
        Ok(())
    } else {
        Err(PackageError::ColumnMismatch {
            column,
            column_value: row.to_string(),
            payload_value: payload.to_string(),
        })
    }
}

fn require_column_option_f64(
    column: &'static str,
    row: Option<f64>,
    payload: Option<f64>,
) -> Result<(), PackageError> {
    if row == payload {
        Ok(())
    } else {
        Err(PackageError::ColumnMismatch {
            column,
            column_value: format!("{row:?}"),
            payload_value: format!("{payload:?}"),
        })
    }
}

#[cfg(test)]
mod tests;

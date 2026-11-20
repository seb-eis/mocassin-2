// SPDX-License-Identifier: MIT

//! Structured reader errors and their solver exit classes.
//!
//! Every reader failure names the package section it came from, and a failure tied to a job or model is wrapped
//! in a [`PackageContext`] so a diagnostic can name both. [`PackageError::kind`] maps the variant onto the
//! shared [`ErrorClass`], and `mocassin-cli` turns that class into the canonical solver exit code.

use core::fmt;
use core::ops::RangeInclusive;

use mocassin_types::{ErrorClass, ExitCode};

/// A validated `.msl` read failed.
#[derive(Debug, thiserror::Error)]
pub enum PackageError {
    /// The path is missing, unreadable, a directory, or another file-system failure.
    #[error("I/O error: {0}")]
    Io(#[from] std::io::Error),

    /// SQLite rejected an open, pragma, query, or value conversion.
    #[error("SQLite error: {0}")]
    Sqlite(#[from] rusqlite::Error),

    /// The package schema or payload version is outside the supported range.
    #[error("unsupported package format version {found}; supported {supported:?}")]
    SchemaVersion {
        /// The version that was found.
        found: u32,
        /// The supported range.
        supported: RangeInclusive<u32>,
    },

    /// A required table, column, row, or `schema_info` key is absent.
    #[error("missing {what} \"{name}\"")]
    MissingSchemaElement {
        /// The element kind, for example `"table"`, `"column"`, or `"schema_info key"`.
        what: &'static str,
        /// The element name.
        name: &'static str,
    },

    /// The `schema_info` rows are malformed or internally inconsistent.
    #[error("invalid schema_info: {0}")]
    InvalidSchemaInfo(String),

    /// The compressed payload is not a usable Zstd frame.
    #[error("invalid {section} payload frame: {detail}")]
    InvalidFrame {
        /// The package section.
        section: &'static str,
        /// The failure detail.
        detail: String,
    },

    /// A payload declares more decompressed or guarded bytes than the limit allows.
    #[error("{section} payload declares {declared} bytes, above the {limit}-byte limit")]
    PayloadTooLarge {
        /// The package section.
        section: &'static str,
        /// The declared or observed size.
        declared: u64,
        /// The applied limit.
        limit: u64,
    },

    /// The bounded scan consumed more MessagePack elements than the section budget allows.
    #[error("{section} payload exceeds the {limit}-element scan budget")]
    ElementBudgetExceeded {
        /// The package section.
        section: &'static str,
        /// The applied element budget.
        limit: u64,
    },

    /// The bounded scan rejected the document shape.
    #[error("{section} payload scan failed: {detail}")]
    Scan {
        /// The package section.
        section: &'static str,
        /// The failure detail.
        detail: String,
    },

    /// The typed decode rejected the document.
    #[error("{section} payload decode failed: {detail}")]
    Decode {
        /// The package section.
        section: &'static str,
        /// The failure detail.
        detail: String,
    },

    /// A stored signature or identity does not match the recomputed or declared value.
    #[error("{what} signature mismatch: expected {expected}, found {actual}")]
    SignatureMismatch {
        /// The signed element.
        what: &'static str,
        /// The expected value.
        expected: String,
        /// The found value.
        actual: String,
    },

    /// An index is outside its permitted range.
    #[error("invalid index {what} = {value}; the limit is {limit}")]
    InvalidIndex {
        /// The index name.
        what: &'static str,
        /// The rejected value.
        value: u64,
        /// The exclusive upper bound.
        limit: u64,
    },

    /// A derived or declared length disagrees with the expected length.
    #[error("length mismatch for {what}: expected {expected}, found {found}")]
    LengthMismatch {
        /// The length name.
        what: &'static str,
        /// The expected length.
        expected: u64,
        /// The found length.
        found: u64,
    },

    /// A value is structurally valid but violates a model invariant.
    #[error("invalid value for {what}: {detail}")]
    InvalidValue {
        /// The value name.
        what: &'static str,
        /// The failure detail.
        detail: String,
    },

    /// A SQLite column disagrees with the decoded payload.
    #[error("column {column} holds {column_value} but the payload holds {payload_value}")]
    ColumnMismatch {
        /// The column name, for example `"jobs.kind"`.
        column: &'static str,
        /// The column value.
        column_value: String,
        /// The payload value.
        payload_value: String,
    },

    /// The requested job id is not present in the package.
    #[error("job {job_id} is not present in the package")]
    JobNotFound {
        /// The requested job id.
        job_id: u32,
    },

    /// A job row references a model or lattice row that is absent.
    #[error("job {job_id} references a missing {what} row")]
    DanglingReference {
        /// The job whose reference is dangling.
        job_id: u32,
        /// The missing section, for example `"model"` or `"lattice"`.
        what: &'static str,
    },

    /// The requested option or package feature is not supported by this solver version.
    #[error("unsupported: {0}")]
    Unsupported(String),
}

impl PackageError {
    /// Maps the error onto the shared solver error class.
    #[must_use]
    pub fn kind(&self) -> ErrorClass {
        match self {
            Self::Io(_) => ErrorClass::Io,
            Self::Sqlite(_)
            | Self::SchemaVersion { .. }
            | Self::MissingSchemaElement { .. }
            | Self::InvalidSchemaInfo(_)
            | Self::InvalidFrame { .. }
            | Self::PayloadTooLarge { .. }
            | Self::ElementBudgetExceeded { .. }
            | Self::Scan { .. }
            | Self::Decode { .. }
            | Self::DanglingReference { .. } => ErrorClass::Package,
            Self::SignatureMismatch { .. }
            | Self::InvalidIndex { .. }
            | Self::LengthMismatch { .. }
            | Self::InvalidValue { .. }
            | Self::ColumnMismatch { .. } => ErrorClass::Model,
            Self::JobNotFound { .. } | Self::Unsupported(_) => ErrorClass::InvalidArguments,
        }
    }

    /// Returns the canonical solver exit code for this error.
    #[must_use]
    pub fn exit_code(&self) -> ExitCode {
        self.kind().exit_code()
    }
}

/// A [`PackageError`] bound to the job/model/section that produced it.
#[derive(Debug)]
pub struct PackageContext {
    /// The job id, when the failure happened while loading a job.
    pub job_id: Option<u32>,
    /// The SQLite model id, or `-1` when no model is involved.
    pub model_id: i64,
    /// The package section, for example `"model"`, `"job"`, or `"lattice"`.
    pub section: &'static str,
    /// The underlying error.
    pub source: PackageError,
}

impl PackageContext {
    /// Builds a context around a reader error.
    #[must_use]
    pub const fn new(
        job_id: Option<u32>,
        model_id: i64,
        section: &'static str,
        source: PackageError,
    ) -> Self {
        Self {
            job_id,
            model_id,
            section,
            source,
        }
    }

    /// Returns the solver error class of the underlying error.
    #[must_use]
    pub fn kind(&self) -> ErrorClass {
        self.source.kind()
    }

    /// Returns the canonical solver exit code of the underlying error.
    #[must_use]
    pub fn exit_code(&self) -> ExitCode {
        self.source.exit_code()
    }
}

impl fmt::Display for PackageContext {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self.job_id {
            Some(job_id) => write!(formatter, "job {job_id} ")?,
            None => formatter.write_str("package ")?,
        }
        write!(
            formatter,
            "model {} [{}]: {}",
            self.model_id, self.section, self.source
        )
    }
}

impl std::error::Error for PackageContext {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        Some(&self.source)
    }
}

#[cfg(test)]
mod tests {
    use std::ops::RangeInclusive;

    use mocassin_types::{ErrorClass, ExitCode};

    use super::{PackageContext, PackageError};

    fn io_error() -> PackageError {
        PackageError::Io(std::io::Error::new(std::io::ErrorKind::NotFound, "missing"))
    }

    #[test]
    fn every_package_and_schema_error_is_exit_eleven() {
        let errors = [
            PackageError::SchemaVersion {
                found: 2,
                supported: RangeInclusive::new(1, 1),
            },
            PackageError::MissingSchemaElement {
                what: "table",
                name: "models",
            },
            PackageError::InvalidSchemaInfo("bad".to_owned()),
            PackageError::InvalidFrame {
                section: "model",
                detail: "bad".to_owned(),
            },
            PackageError::PayloadTooLarge {
                section: "model",
                declared: 10,
                limit: 5,
            },
            PackageError::ElementBudgetExceeded {
                section: "model",
                limit: 5,
            },
            PackageError::Scan {
                section: "model",
                detail: "bad".to_owned(),
            },
            PackageError::Decode {
                section: "model",
                detail: "bad".to_owned(),
            },
        ];

        for error in errors {
            assert_eq!(error.kind(), ErrorClass::Package);
            assert_eq!(error.exit_code(), ExitCode::PackageError);
        }
    }

    #[test]
    fn every_model_error_is_exit_twelve() {
        let errors = [
            PackageError::SignatureMismatch {
                what: "model",
                expected: "a".to_owned(),
                actual: "b".to_owned(),
            },
            PackageError::InvalidIndex {
                what: "position",
                value: 9,
                limit: 4,
            },
            PackageError::LengthMismatch {
                what: "charges",
                expected: 5,
                found: 4,
            },
            PackageError::InvalidValue {
                what: "temperature_k",
                detail: "not finite".to_owned(),
            },
            PackageError::ColumnMismatch {
                column: "jobs.kind",
                column_value: "kmc".to_owned(),
                payload_value: "mmc".to_owned(),
            },
        ];

        for error in errors {
            assert_eq!(error.kind(), ErrorClass::Model);
            assert_eq!(error.exit_code(), ExitCode::ModelError);
        }
    }

    #[test]
    fn argument_and_io_errors_use_their_canonical_codes() {
        assert_eq!(
            PackageError::JobNotFound { job_id: 7 }.kind(),
            ErrorClass::InvalidArguments
        );
        assert_eq!(
            PackageError::JobNotFound { job_id: 7 }.exit_code(),
            ExitCode::InvalidArguments
        );
        assert_eq!(
            PackageError::Unsupported("batch".to_owned()).kind(),
            ErrorClass::InvalidArguments
        );
        assert_eq!(io_error().kind(), ErrorClass::Io);
        assert_eq!(io_error().exit_code(), ExitCode::IoError);
        assert_eq!(
            PackageError::Sqlite(rusqlite::Error::QueryReturnedNoRows).kind(),
            ErrorClass::Package
        );
    }

    #[test]
    fn the_context_names_the_job_model_and_section() {
        let context = PackageContext::new(
            Some(3),
            4,
            "lattice",
            PackageError::InvalidValue {
                what: "lattice_size",
                detail: "zero".to_owned(),
            },
        );

        let text = context.to_string();
        assert!(text.contains("job 3"), "{text}");
        assert!(text.contains("model 4"), "{text}");
        assert!(text.contains("[lattice]"), "{text}");
        assert!(text.contains("lattice_size"), "{text}");
        assert_eq!(context.kind(), ErrorClass::Model);
        assert!(std::error::Error::source(&context).is_some());

        let package_only = PackageContext::new(
            None,
            -1,
            "schema",
            PackageError::MissingSchemaElement {
                what: "table",
                name: "jobs",
            },
        );
        assert!(package_only.to_string().starts_with("package model -1"));
    }
}

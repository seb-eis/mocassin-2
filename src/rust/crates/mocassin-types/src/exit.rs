// SPDX-License-Identifier: MIT

//! The canonical `mocassin-sim` process exit codes and the error classes that map to them.
//!
//! The table is fixed by [`01-target-architecture.md` §7.3] and mirrored by `docs/reference/cli.md`, the Step 13
//! page, and `solver-architecture.md`. It is the solver process contract, not the .NET `mocassin` table. Code 5
//! is reserved for multi-job/batch partial success until Step 19 and is never emitted by the Step 13 inspection
//! path.
//!
//! [`01-target-architecture.md` §7.3]: https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/01-target-architecture.md

use core::fmt;

/// Canonical solver process exit codes.
///
/// The discriminants are the process exit codes; do not renumber them.
#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(u8)]
pub enum ExitCode {
    /// 0 — completed, including a successful dry run.
    Completed = 0,
    /// 5 — multi-job/batch partial success (at least one job failed, others completed); reserved until Step 19.
    PartialSuccess = 5,
    /// 10 — invalid arguments, unknown job, unsupported option, extension requested, or a parse error.
    InvalidArguments = 10,
    /// 11 — package error: unusable SQLite, schema/version mismatch, missing element, invalid frame,
    /// decode/scan/size error.
    PackageError = 11,
    /// 12 — model/data error: signature mismatch, invalid index/length/value, column/payload mismatch.
    ModelError = 12,
    /// 13 — numerical failure or internal invariant violation, including a caught panic.
    InternalError = 13,
    /// 14 — run aborted by a configured limit.
    LimitAborted = 14,
    /// 15 — run cancelled.
    Cancelled = 15,
    /// 16 — I/O error: missing or unreadable path.
    IoError = 16,
}

impl ExitCode {
    /// Every defined code in ascending order.
    pub const ALL: [Self; 9] = [
        Self::Completed,
        Self::PartialSuccess,
        Self::InvalidArguments,
        Self::PackageError,
        Self::ModelError,
        Self::InternalError,
        Self::LimitAborted,
        Self::Cancelled,
        Self::IoError,
    ];

    /// Returns the numeric process exit code.
    #[must_use]
    pub const fn code(self) -> u8 {
        self as u8
    }

    /// Returns the canonical one-line meaning from the contract table.
    #[must_use]
    pub const fn meaning(self) -> &'static str {
        match self {
            Self::Completed => "completed",
            Self::PartialSuccess => "partial success for multi-job/batch commands",
            Self::InvalidArguments => "invalid arguments",
            Self::PackageError => "package error",
            Self::ModelError => "model/data error",
            Self::InternalError => "numerical failure or internal invariant violation",
            Self::LimitAborted => "aborted by a configured limit",
            Self::Cancelled => "cancelled",
            Self::IoError => "I/O error",
        }
    }
}

impl fmt::Display for ExitCode {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.code(), formatter)
    }
}

impl From<ExitCode> for u8 {
    fn from(code: ExitCode) -> Self {
        code.code()
    }
}

impl TryFrom<u8> for ExitCode {
    type Error = UnknownExitCode;

    fn try_from(code: u8) -> Result<Self, Self::Error> {
        match code {
            0 => Ok(Self::Completed),
            5 => Ok(Self::PartialSuccess),
            10 => Ok(Self::InvalidArguments),
            11 => Ok(Self::PackageError),
            12 => Ok(Self::ModelError),
            13 => Ok(Self::InternalError),
            14 => Ok(Self::LimitAborted),
            15 => Ok(Self::Cancelled),
            16 => Ok(Self::IoError),
            other => Err(UnknownExitCode(other)),
        }
    }
}

/// The numeric code is not in the canonical solver exit table.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct UnknownExitCode(u8);

impl UnknownExitCode {
    /// Returns the rejected code.
    #[must_use]
    pub const fn code(self) -> u8 {
        self.0
    }
}

impl fmt::Display for UnknownExitCode {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(
            formatter,
            "unknown solver exit code {}; expected 0, 5, or 10-16",
            self.0
        )
    }
}

impl std::error::Error for UnknownExitCode {}

/// The error class a solver failure belongs to, before it is mapped to a process exit code.
///
/// `mocassin-msl` classifies a `PackageError` with this enum and the CLI converts it with
/// [`ErrorClass::exit_code`]; the engine uses the remaining variants for limit aborts, cancellation, and caught
/// internal violations. The enum is plain data, so the types crate keeps its no-I/O, no-SQLite dependency set.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum ErrorClass {
    /// The operation completed successfully.
    Success,
    /// At least one batch job failed while others completed (reserved until Step 19).
    PartialSuccess,
    /// The command line, job selector, or requested feature is invalid or unsupported.
    InvalidArguments,
    /// The package is unreadable, malformed, corrupt, or of an unsupported version.
    Package,
    /// The payload decoded but violates a model/data invariant or signature.
    Model,
    /// A numerical failure or internal invariant violation, including a caught panic.
    Internal,
    /// A configured limit aborted the run.
    LimitAborted,
    /// The run was cancelled.
    Cancelled,
    /// A file-system operation failed.
    Io,
}

impl ErrorClass {
    /// Maps the class to its canonical process exit code.
    #[must_use]
    pub const fn exit_code(self) -> ExitCode {
        match self {
            Self::Success => ExitCode::Completed,
            Self::PartialSuccess => ExitCode::PartialSuccess,
            Self::InvalidArguments => ExitCode::InvalidArguments,
            Self::Package => ExitCode::PackageError,
            Self::Model => ExitCode::ModelError,
            Self::Internal => ExitCode::InternalError,
            Self::LimitAborted => ExitCode::LimitAborted,
            Self::Cancelled => ExitCode::Cancelled,
            Self::Io => ExitCode::IoError,
        }
    }
}

impl From<ErrorClass> for ExitCode {
    fn from(class: ErrorClass) -> Self {
        class.exit_code()
    }
}

impl fmt::Display for ErrorClass {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter.write_str(self.exit_code().meaning())
    }
}

#[cfg(test)]
mod tests {
    use super::{ErrorClass, ExitCode};

    #[test]
    fn the_table_is_exactly_zero_five_and_ten_to_sixteen() {
        let codes: Vec<u8> = ExitCode::ALL.iter().map(|code| code.code()).collect();
        assert_eq!(codes, vec![0, 5, 10, 11, 12, 13, 14, 15, 16]);
    }

    #[test]
    fn every_code_round_trips_through_its_numeric_value() {
        for code in ExitCode::ALL {
            assert_eq!(u8::from(code), code.code());
            assert_eq!(ExitCode::try_from(code.code()), Ok(code));
        }
    }

    #[test]
    fn codes_outside_the_table_are_rejected() {
        for code in [1, 2, 3, 4, 6, 7, 8, 9, 17, 255] {
            let error = ExitCode::try_from(code).expect_err("code must be rejected");
            assert_eq!(error.code(), code);
            assert!(error.to_string().contains(&code.to_string()));
        }
    }

    #[test]
    fn meanings_are_unique_and_match_the_contract_phrases() {
        let mut meanings: Vec<&str> = ExitCode::ALL.iter().map(|code| code.meaning()).collect();
        assert_eq!(meanings.len(), 9);
        meanings.sort_unstable();
        meanings.dedup();
        assert_eq!(meanings.len(), 9, "every code needs a distinct meaning");

        assert_eq!(ExitCode::Completed.meaning(), "completed");
        assert_eq!(
            ExitCode::PartialSuccess.meaning(),
            "partial success for multi-job/batch commands"
        );
        assert_eq!(ExitCode::InvalidArguments.meaning(), "invalid arguments");
        assert_eq!(ExitCode::PackageError.meaning(), "package error");
        assert_eq!(ExitCode::ModelError.meaning(), "model/data error");
        assert_eq!(
            ExitCode::InternalError.meaning(),
            "numerical failure or internal invariant violation"
        );
        assert_eq!(
            ExitCode::LimitAborted.meaning(),
            "aborted by a configured limit"
        );
        assert_eq!(ExitCode::Cancelled.meaning(), "cancelled");
        assert_eq!(ExitCode::IoError.meaning(), "I/O error");
    }

    #[test]
    fn display_is_the_numeric_code() {
        assert_eq!(ExitCode::Completed.to_string(), "0");
        assert_eq!(ExitCode::PartialSuccess.to_string(), "5");
        assert_eq!(ExitCode::IoError.to_string(), "16");
    }

    #[test]
    fn every_error_class_maps_to_its_documented_code() {
        let rows = [
            (ErrorClass::Success, ExitCode::Completed),
            (ErrorClass::PartialSuccess, ExitCode::PartialSuccess),
            (ErrorClass::InvalidArguments, ExitCode::InvalidArguments),
            (ErrorClass::Package, ExitCode::PackageError),
            (ErrorClass::Model, ExitCode::ModelError),
            (ErrorClass::Internal, ExitCode::InternalError),
            (ErrorClass::LimitAborted, ExitCode::LimitAborted),
            (ErrorClass::Cancelled, ExitCode::Cancelled),
            (ErrorClass::Io, ExitCode::IoError),
        ];
        for (class, expected) in rows {
            assert_eq!(class.exit_code(), expected);
            assert_eq!(ExitCode::from(class), expected);
        }
    }

    #[test]
    fn error_class_display_reuses_the_exit_meaning() {
        assert_eq!(ErrorClass::Package.to_string(), "package error");
        assert_eq!(ErrorClass::Io.to_string(), "I/O error");
    }
}

// SPDX-License-Identifier: MIT

//! The simulation-context error contract and its exit-code mapping.
//!
//! Every failure while building a context is one [`ContextError`] variant. The mapping to the canonical solver
//! exit table ([`01-target-architecture.md` §7.3]) is fixed here so the CLI, the solver, and the tests agree:
//!
//! - a model/data error is exit 12: the package is structurally valid but the job cannot be represented or is
//!   internally inconsistent ([`ContextError::InvalidInput`], [`ContextError::ClusterGeometry`],
//!   [`ContextError::MissingOccupationCode`], [`ContextError::ExpansionMapAbsent`],
//!   [`ContextError::NoSelectableJumps`], [`ContextError::NoMobileParticles`],
//!   [`ContextError::ResumeMismatch`], [`ContextError::LimitExceeded`]);
//! - a numerical failure or internal invariant violation is exit 13 ([`ContextError::NonFiniteFactor`],
//!   [`ContextError::RngState`], [`ContextError::Invariant`]);
//! - a cancelled initialization is exit 15 ([`ContextError::Cancelled`]).
//!
//! `LimitExceeded` is deliberately **not** exit 14. Exit 14 is the *run-time* "aborted by a configured limit"
//! (cycle target, wall clock, rate) produced by the KMC/MMC engine; a job whose context cannot be represented
//! is a data error detected before the first cycle. See the Step 14 page §13 and
//! [`docs/planning/01-target-architecture.md`](../../../../docs/planning/01-target-architecture.md) §7.3.
//!
//! [`01-target-architecture.md` §7.3]: https://github.com/seb-eis/mocassin-2/blob/main/docs/planning/01-target-architecture.md

use mocassin_rng::RngStateError;
use mocassin_types::ExitCode;

/// A failure while building or validating a [`super::SimulationContext`].
#[derive(Debug, thiserror::Error)]
pub enum ContextError {
    /// A validated package field is not usable by the context.
    #[error("invalid context input `{what}`: {detail}")]
    InvalidInput {
        /// The field or contract that failed.
        what: &'static str,
        /// The offending value and the reason, without package-specific private data.
        detail: String,
    },
    /// A cluster slot set, centre, or member image cannot be determined exactly.
    #[error(
        "cluster table {cluster_table} instance {instance} slot {slot} has an invalid geometry: {detail}"
    )]
    ClusterGeometry {
        /// The shared cluster-table index.
        cluster_table: u32,
        /// The instance id, or the centre position when the instance is not built yet.
        instance: u32,
        /// The slot whose image is absent or ambiguous.
        slot: u8,
        /// The reason the geometry is rejected.
        detail: String,
    },
    /// A raw cluster tuple is absent from a present expansion map; the package is internally inconsistent.
    #[error("cluster table {cluster_table} has no expansion for raw occupation code {code:#x}")]
    MissingOccupationCode {
        /// The shared cluster-table index.
        cluster_table: u32,
        /// The rejected raw slot-tuple code.
        code: u64,
    },
    /// No expansion map exists and the raw cluster tuple is not canonical; the package predates the map.
    #[error(
        "cluster table {cluster_table} has no code expansion map and its raw occupation code {code:#x} is \
         not canonical; rebuild the package with the current writer"
    )]
    ExpansionMapAbsent {
        /// The shared cluster-table index.
        cluster_table: u32,
        /// The non-canonical raw slot-tuple code.
        code: u64,
    },
    /// A KMC job has no selectable jump directions.
    #[error("the KMC job has no selectable jump directions")]
    NoSelectableJumps,
    /// A KMC job has no potentially mobile particles.
    #[error("the KMC job has no mobile particles")]
    NoMobileParticles,
    /// A resume state does not match the package, the job, or the lattice.
    #[error("the resume state does not match the job: {what}")]
    ResumeMismatch {
        /// The check that failed (signature, seed hash, id, kind, dimensions, counts, mapping).
        what: &'static str,
    },
    /// A context limit or the memory plan rejects the job before allocation.
    #[error("context limit `{what}` exceeded: value {value} > limit {limit}")]
    LimitExceeded {
        /// The limit that was exceeded.
        what: &'static str,
        /// The planned value; `u64::MAX` when checked arithmetic overflowed.
        value: u64,
        /// The configured limit.
        limit: u64,
    },
    /// A temperature-derived factor (`kT`, `K_fix`, `K_norm`, `nu_max`, or `dt`) is not finite and positive.
    #[error("the normalization factor `{what}` is not finite and positive")]
    NonFiniteFactor {
        /// The factor that failed.
        what: &'static str,
    },
    /// The random-number resume blob is invalid.
    #[error("the RNG resume state is invalid: {0}")]
    RngState(#[source] RngStateError),
    /// A debug/release invariant violation in a derived structure.
    #[error("context invariant violated: {what}")]
    Invariant {
        /// The invariant that failed.
        what: &'static str,
    },
    /// The caller's cancellation flag was set during initialization.
    #[error("context initialization was cancelled")]
    Cancelled,
}

impl ContextError {
    /// Returns the canonical process exit code for this error.
    #[must_use]
    pub const fn exit_code(&self) -> ExitCode {
        match self {
            Self::InvalidInput { .. }
            | Self::ClusterGeometry { .. }
            | Self::MissingOccupationCode { .. }
            | Self::ExpansionMapAbsent { .. }
            | Self::NoSelectableJumps
            | Self::NoMobileParticles
            | Self::ResumeMismatch { .. }
            | Self::LimitExceeded { .. } => ExitCode::ModelError,
            Self::NonFiniteFactor { .. } | Self::RngState(_) | Self::Invariant { .. } => {
                ExitCode::InternalError
            }
            Self::Cancelled => ExitCode::Cancelled,
        }
    }
}

#[cfg(test)]
mod tests {
    use mocassin_rng::{RngStateError, RngStateV1};
    use mocassin_types::ExitCode;

    use super::ContextError;

    fn invalid_rng_state() -> RngStateError {
        RngStateV1::from_bytes(&[0u8; 64]).expect_err("zero bytes are not a state")
    }

    #[test]
    fn data_errors_map_to_exit_12() {
        let errors = [
            ContextError::InvalidInput {
                what: "lattice length",
                detail: "expected 864 particles".to_owned(),
            },
            ContextError::ClusterGeometry {
                cluster_table: 0,
                instance: 1,
                slot: 2,
                detail: "ambiguous member image".to_owned(),
            },
            ContextError::MissingOccupationCode {
                cluster_table: 0,
                code: 0x42,
            },
            ContextError::ExpansionMapAbsent {
                cluster_table: 0,
                code: 0x42,
            },
            ContextError::NoSelectableJumps,
            ContextError::NoMobileParticles,
            ContextError::ResumeMismatch {
                what: "model signature",
            },
            ContextError::LimitExceeded {
                what: "max_context_bytes",
                value: 10,
                limit: 5,
            },
        ];

        for error in errors {
            assert_eq!(error.exit_code(), ExitCode::ModelError, "{error}");
            assert_eq!(error.exit_code().code(), 12);
        }
    }

    #[test]
    fn numerical_and_invariant_errors_map_to_exit_13() {
        let errors = [
            ContextError::NonFiniteFactor { what: "kT" },
            ContextError::RngState(invalid_rng_state()),
            ContextError::Invariant {
                what: "link CSR length",
            },
        ];

        for error in errors {
            assert_eq!(error.exit_code(), ExitCode::InternalError, "{error}");
            assert_eq!(error.exit_code().code(), 13);
        }
    }

    #[test]
    fn cancellation_maps_to_exit_15() {
        assert_eq!(ContextError::Cancelled.exit_code(), ExitCode::Cancelled);
        assert_eq!(ContextError::Cancelled.exit_code().code(), 15);
    }
}

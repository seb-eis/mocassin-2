// SPDX-License-Identifier: MIT

//! Read-only access to immutable MOCASSIN `.msl` packages.
//!
//! The crate opens a package read-only, validates its identity, schema, and provenance keys, and decodes one
//! model, job, and lattice payload within the [`mocassin_types::ReaderLimits`] envelope. Every payload is
//! Zstd-compressed MessagePack; the reader bounds the decompressed size, scans the document without allocating
//! before it decodes into the `mocassin_types::payload` mirrors, and rejects duplicate known keys, trailing
//! bytes, and over-budget documents.
//!
//! The package is never written and never read through a writable connection. Full payload-to-runtime mapping,
//! index/finite/semantic validation, and canonical signature verification belong to the next Step 13 feature;
//! this crate stops after the bounded, typed decode and the stored `format_version` cross-checks.

#![forbid(unsafe_op_in_unsafe_fn)]

mod decode;
mod error;
mod package;
mod scan;

pub use decode::{
    decode_job_payload, decode_lattice_payload, decode_model_payload, decompress_payload,
};
pub use error::{PackageContext, PackageError};
pub use package::{APPLICATION_ID, JobRow, LoadedJob, Package, SchemaInfo};

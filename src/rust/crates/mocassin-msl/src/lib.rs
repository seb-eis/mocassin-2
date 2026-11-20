// SPDX-License-Identifier: MIT

//! Read-only access to immutable MOCASSIN `.msl` packages.
//!
//! The crate opens a package read-only, validates its identity, schema, and provenance keys, decodes one model,
//! job, and lattice payload within the [`mocassin_types::ReaderLimits`] envelope, maps them onto the validated
//! runtime types, and verifies both stored signatures.
//!
//! Every payload is Zstd-compressed MessagePack. The reader bounds the decompressed size, scans the document
//! without allocating before it decodes into the `mocassin_types::payload` mirrors, rejects duplicate known
//! keys, trailing bytes, and over-budget documents, then checks every index, length, finite value, kind, flag,
//! tracker value, and scientific invariant. The package is never written and never read through a writable
//! connection.
//!
//! [`Package::load_job`] returns a [`mocassin_types::JobPackage`]; the engine consumes it unchanged.

#![forbid(unsafe_op_in_unsafe_fn)]

mod decode;
mod error;
mod map;
mod package;
mod projection;
mod scan;
mod validate;

pub use decode::{
    decode_job_payload, decode_lattice_payload, decode_model_payload, decompress_payload,
};
pub use error::{PackageContext, PackageError};
pub use package::{APPLICATION_ID, Package, SchemaInfo};
pub use projection::{
    LATTICE_DOMAIN, PARAMETERIZATION_DOMAIN, lattice_projection, lattice_signature,
    parameterization_projection, parameterization_signature,
};

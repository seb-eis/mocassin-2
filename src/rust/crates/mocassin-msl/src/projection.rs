// SPDX-License-Identifier: MIT

//! The ADR 0010 canonical MessagePack projection and the two stored signatures.
//!
//! The projection uses forced-width headers only: array32 (`0xdd`), str32 (`0xdb`), bin32 (`0xc6`), fixed
//! uint64 (`0xcf`), fixed int64 (`0xd3`), and fixed float64 (`0xcb`). Every projected float is quantized to 12
//! decimal places with round-half-away-from-zero, using the same expression as the .NET
//! `CanonicalProjectionWriter`, and negative zero is normalized to positive zero.
//!
//! The parameterization signature is `SHA-256(UTF8("mocassin/parameterization-signature/v1") || 0x00 ||
//! projection)`; the lattice signature uses `"mocassin/lattice-signature/v1"`. Both are lowercase hexadecimal.

use mocassin_types::InitialLattice;
use mocassin_types::ParameterizedModel;
use sha2::{Digest, Sha256};

use crate::error::PackageError;

/// Domain prefix of the parameterization signature.
pub const PARAMETERIZATION_DOMAIN: &str = "mocassin/parameterization-signature/v1";

/// Domain prefix of the lattice signature.
pub const LATTICE_DOMAIN: &str = "mocassin/lattice-signature/v1";

/// Projection format version written by v1.
const PROJECTION_VERSION: u64 = 1;

/// A destination for canonical projection bytes.
trait Sink {
    fn write(&mut self, bytes: &[u8]);
}

#[derive(Default)]
struct VecSink {
    bytes: Vec<u8>,
}

impl Sink for VecSink {
    fn write(&mut self, bytes: &[u8]) {
        self.bytes.extend_from_slice(bytes);
    }
}

struct HashSink {
    hasher: Sha256,
}

impl Sink for HashSink {
    fn write(&mut self, bytes: &[u8]) {
        self.hasher.update(bytes);
    }
}

impl HashSink {
    fn finish(self) -> String {
        to_lower_hex(&self.hasher.finalize())
    }
}

/// Writes the forced-width projection into a sink.
struct Projection<S: Sink> {
    sink: S,
}

impl<S: Sink> Projection<S> {
    fn new(sink: S) -> Self {
        Self { sink }
    }

    fn array_header(&mut self, count: u32) {
        let mut header = [0_u8; 5];
        header[0] = 0xdd;
        header[1..].copy_from_slice(&count.to_be_bytes());
        self.sink.write(&header);
    }

    fn string(&mut self, value: &str) {
        let bytes = value.as_bytes();
        let mut header = [0_u8; 5];
        header[0] = 0xdb;
        header[1..].copy_from_slice(&(bytes.len() as u32).to_be_bytes());
        self.sink.write(&header);
        self.sink.write(bytes);
    }

    fn uint64(&mut self, value: u64) {
        let mut bytes = [0_u8; 9];
        bytes[0] = 0xcf;
        bytes[1..].copy_from_slice(&value.to_be_bytes());
        self.sink.write(&bytes);
    }

    fn float64(&mut self, value: f64) -> Result<(), PackageError> {
        let quantized = quantize(value)?;
        let mut bytes = [0_u8; 9];
        bytes[0] = 0xcb;
        bytes[1..].copy_from_slice(&quantized.to_bits().to_be_bytes());
        self.sink.write(&bytes);
        Ok(())
    }

    fn double_array(&mut self, values: &[f64]) -> Result<(), PackageError> {
        self.array_header(u32::try_from(values.len()).map_err(|_| too_large())?);
        for value in values {
            self.float64(*value)?;
        }
        Ok(())
    }

    fn nested_double_arrays<'a, I>(&mut self, tables: I) -> Result<(), PackageError>
    where
        I: IntoIterator<Item = &'a [f64]>,
    {
        let tables: Vec<&[f64]> = tables.into_iter().collect();
        self.array_header(u32::try_from(tables.len()).map_err(|_| too_large())?);
        for table in tables {
            self.double_array(table)?;
        }
        Ok(())
    }

    fn particle_bin(&mut self, particles: &[mocassin_types::ParticleId]) {
        let mut header = [0_u8; 5];
        header[0] = 0xc6;
        header[1..].copy_from_slice(&(particles.len() as u32).to_be_bytes());
        self.sink.write(&header);

        let mut chunk = [0_u8; 4096];
        let mut filled = 0;
        for particle in particles {
            chunk[filled] = particle.get();
            filled += 1;
            if filled == chunk.len() {
                self.sink.write(&chunk);
                filled = 0;
            }
        }
        if filled > 0 {
            self.sink.write(&chunk[..filled]);
        }
    }
}

fn too_large() -> PackageError {
    PackageError::InvalidValue {
        what: "canonical projection",
        detail: "a projected array does not fit a 32-bit header length".to_owned(),
    }
}

/// Quantizes a projected double exactly as the .NET writer does.
fn quantize(value: f64) -> Result<f64, PackageError> {
    if !value.is_finite() {
        return Err(PackageError::InvalidValue {
            what: "canonical projection",
            detail: "the projection contains a non-finite value".to_owned(),
        });
    }

    let scaled = value * 1e12;
    if !scaled.is_finite() {
        return Err(PackageError::InvalidValue {
            what: "canonical projection",
            detail: "the value is too large to quantize to 12 decimal places".to_owned(),
        });
    }

    let rounded = if scaled >= 0.0 {
        (scaled + 0.5).floor()
    } else {
        (scaled - 0.5).ceil()
    };
    let result = rounded / 1e12;
    if !result.is_finite() {
        return Err(PackageError::InvalidValue {
            what: "canonical projection",
            detail: "the quantized value is not finite".to_owned(),
        });
    }

    Ok(if result == 0.0 { 0.0 } else { result })
}

fn to_lower_hex(bytes: &[u8]) -> String {
    use core::fmt::Write as _;

    let mut text = String::with_capacity(bytes.len() * 2);
    for byte in bytes {
        let _ = write!(text, "{byte:02x}");
    }
    text
}

fn write_parameterization<S: Sink>(
    projection: &mut Projection<S>,
    model: &ParameterizedModel,
) -> Result<(), PackageError> {
    projection.array_header(6);
    projection.uint64(PROJECTION_VERSION);
    projection.string(&model.model_signature);
    projection.nested_double_arrays(
        model
            .energy
            .pair_tables
            .iter()
            .map(|table| table.energies.as_slice()),
    )?;
    projection.nested_double_arrays(
        model
            .energy
            .cluster_tables
            .iter()
            .map(|table| table.energies.as_slice()),
    )?;
    projection.double_array(&model.energy.defect_background)?;
    let frequencies: Vec<f64> = model
        .transitions
        .rules
        .iter()
        .map(|rule| rule.frequency_hz)
        .collect();
    projection.double_array(&frequencies)?;
    Ok(())
}

fn write_lattice<S: Sink>(
    projection: &mut Projection<S>,
    model_signature: &str,
    lattice: &InitialLattice,
) {
    projection.array_header(5);
    projection.uint64(PROJECTION_VERSION);
    projection.string(model_signature);
    projection.array_header(3);
    for size in lattice.lattice_size {
        projection.uint64(u64::from(size));
    }
    projection.uint64(u64::from(lattice.position_count));
    projection.particle_bin(&lattice.particles);
}

/// Returns the canonical parameterization projection bytes (diagnostics and golden tests only).
///
/// # Errors
///
/// Returns an error when a projected value is non-finite or cannot be quantized.
pub fn parameterization_projection(model: &ParameterizedModel) -> Result<Vec<u8>, PackageError> {
    let mut projection = Projection::new(VecSink::default());
    write_parameterization(&mut projection, model)?;
    Ok(projection.sink.bytes)
}

/// Computes the canonical parameterization signature.
///
/// # Errors
///
/// Returns an error when a projected value is non-finite or cannot be quantized.
pub fn parameterization_signature(model: &ParameterizedModel) -> Result<String, PackageError> {
    let mut hasher = Sha256::new();
    hasher.update(PARAMETERIZATION_DOMAIN.as_bytes());
    hasher.update([0_u8]);
    let mut projection = Projection::new(HashSink { hasher });
    write_parameterization(&mut projection, model)?;
    Ok(projection.sink.finish())
}

/// Returns the canonical lattice projection bytes (diagnostics and golden tests only).
#[must_use]
pub fn lattice_projection(model_signature: &str, lattice: &InitialLattice) -> Vec<u8> {
    let mut projection = Projection::new(VecSink::default());
    write_lattice(&mut projection, model_signature, lattice);
    projection.sink.bytes
}

/// Computes the canonical lattice signature.
#[must_use]
pub fn lattice_signature(model_signature: &str, lattice: &InitialLattice) -> String {
    let mut hasher = Sha256::new();
    hasher.update(LATTICE_DOMAIN.as_bytes());
    hasher.update([0_u8]);
    let mut projection = Projection::new(HashSink { hasher });
    write_lattice(&mut projection, model_signature, lattice);
    projection.sink.finish()
}

#[cfg(test)]
mod tests {
    use super::quantize;

    #[test]
    fn quantization_matches_the_documented_rule() {
        assert_eq!(quantize(0.0).expect("finite"), 0.0);
        assert_eq!(quantize(-0.0).expect("finite"), 0.0);
        assert_eq!(
            quantize(1.234_567_890_123_456).expect("finite"),
            1.234_567_890_123
        );
        assert_eq!(
            quantize(-1.234_567_890_123_456).expect("finite"),
            -1.234_567_890_123
        );
        assert_eq!(quantize(0.5e-12).expect("finite"), 1e-12);
        assert_eq!(quantize(-0.5e-12).expect("finite"), -1e-12);
        assert!(quantize(f64::NAN).is_err());
        assert!(quantize(f64::INFINITY).is_err());
        assert!(quantize(f64::MAX).is_err());
    }

    #[test]
    fn negative_zero_is_positive_zero_in_the_projection() {
        let positive = quantize(0.0).expect("finite").to_bits();
        let negative = quantize(-0.0).expect("finite").to_bits();
        assert_eq!(positive, negative);
    }
}

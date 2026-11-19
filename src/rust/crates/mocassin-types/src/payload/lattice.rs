// SPDX-License-Identifier: MIT

//! The initial-lattice payload.

use core::fmt;

use serde::de::{SeqAccess, Visitor};
use serde::{Deserialize, Deserializer};

/// The `.msl` initial-lattice payload.
///
/// The payload is only the initial state; the pointer order is the documented row-major
/// `index = ((a * B + b) * C + c) * position_count + P`.
#[derive(Debug, Clone, PartialEq, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct LatticePayload {
    /// The payload-encoding version.
    pub format_version: u32,
    /// The supercell size `[a, b, c]` in unit cells.
    pub lattice_size: [u32; 3],
    /// The number of canonical P1 positions per unit cell.
    pub position_count: u32,
    /// The compiled particle index of every linearized supercell position.
    ///
    /// The v1 .NET writer stores the array as a MessagePack array of `u8`; the canonical projection uses `bin`.
    /// The reader accepts both and normalizes to a byte vector.
    #[serde(deserialize_with = "deserialize_particles")]
    pub lattice: Vec<u8>,
}

/// Accepts the v1 array-of-`u8` form and the canonical `bin` form.
fn deserialize_particles<'de, D>(deserializer: D) -> Result<Vec<u8>, D::Error>
where
    D: Deserializer<'de>,
{
    deserializer.deserialize_any(ParticlesVisitor)
}

struct ParticlesVisitor;

impl<'de> Visitor<'de> for ParticlesVisitor {
    type Value = Vec<u8>;

    fn expecting(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter.write_str("a MessagePack array of u8 or a binary value")
    }

    fn visit_bytes<E>(self, value: &[u8]) -> Result<Self::Value, E>
    where
        E: serde::de::Error,
    {
        Ok(value.to_vec())
    }

    fn visit_byte_buf<E>(self, value: Vec<u8>) -> Result<Self::Value, E>
    where
        E: serde::de::Error,
    {
        Ok(value)
    }

    fn visit_seq<A>(self, mut sequence: A) -> Result<Self::Value, A::Error>
    where
        A: SeqAccess<'de>,
    {
        // The reader's scan already bounded the declared length; cap the eager reservation anyway.
        let capacity = sequence.size_hint().unwrap_or(0).min(4096);
        let mut particles = Vec::with_capacity(capacity);
        while let Some(particle) = sequence.next_element::<u8>()? {
            particles.push(particle);
        }
        Ok(particles)
    }
}

impl LatticePayload {
    /// Recomputes the expected particle count `a * b * c * position_count` in checked arithmetic.
    ///
    /// # Errors
    ///
    /// Returns `None` when the product overflows a `u64`.
    #[must_use]
    pub fn expected_particle_count(&self) -> Option<u64> {
        let [a, b, c] = self.lattice_size.map(u64::from);
        a.checked_mul(b)?
            .checked_mul(c)?
            .checked_mul(u64::from(self.position_count))
    }
}

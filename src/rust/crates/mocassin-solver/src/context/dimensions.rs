// SPDX-License-Identifier: MIT

//! The supercell dimension contract: `[A,B,C]`, `P`, `pc`, and the checked index/wrap helpers.
//!
//! Two dense index spaces exist and are never interchanged:
//!
//! - [`PositionId`](mocassin_types::PositionId) is the canonical P1 position `0..P` inside one unit cell; P1-
//!   keyed tables (site energies, the link-owner CSR, `jump_count`, masks) use it.
//! - [`EnvId`] is an absolute supercell environment `0..A·B·C·P`; pools, jump links, energy-vector accessors,
//!   and partner resolution use it.
//!
//! The linear order is the contract from `02-data-contracts.md`:
//! `index(a, b, c, q) = q + c·P + b·P·C + a·P·C·B`. It is row-major over cells, so
//! `cell = index / P` and `position = index % P`, and the absolute environment of cell `cell` and position
//! `position` is `cell·P + position`.

use core::fmt;

use mocassin_types::JobPackage;

use super::error::ContextError;

/// Largest linear environment index the context accepts: the signed 32-bit range.
///
/// `A·B·C·P ≤ i32::MAX` keeps every index addressable by the `i32` arithmetic of the payload and by the
/// `u32` arena indices without a lossy cast.
const MAX_ENVIRONMENTS: u64 = i32::MAX as u64;

/// The validated supercell dimensions of one job.
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct Dimensions {
    a: u32,
    b: u32,
    c: u32,
    p: u32,
    pc: u32,
    cell_count: u32,
    environment_count: u32,
}

impl Dimensions {
    /// Validates the extents and derives the checked counts.
    ///
    /// # Errors
    ///
    /// - `InvalidInput` when an extent or count is zero or the particle count exceeds 64 (the six-bit state
    ///   codes and `u64` masks bound the model to 64 particles);
    /// - `LimitExceeded { what: "positions" }` when `A·B·C·P` overflows or exceeds `i32::MAX`.
    pub fn new(
        lattice_size: [u32; 3],
        position_count: u32,
        particle_count: u32,
    ) -> Result<Self, ContextError> {
        for (axis, value) in ["a", "b", "c"].into_iter().zip(lattice_size) {
            if value == 0 {
                return Err(ContextError::InvalidInput {
                    what: "lattice size",
                    detail: format!("supercell extent {axis} is zero"),
                });
            }
        }

        if position_count == 0 {
            return Err(ContextError::InvalidInput {
                what: "position count",
                detail: "the unit cell has no P1 position".to_owned(),
            });
        }

        if particle_count == 0 {
            return Err(ContextError::InvalidInput {
                what: "particle count",
                detail: "the particle table is empty".to_owned(),
            });
        }

        if particle_count > 64 {
            return Err(ContextError::InvalidInput {
                what: "particle count",
                detail: format!(
                    "the context holds at most 64 particles (six-bit state codes), found {particle_count}"
                ),
            });
        }

        let [a, b, c] = lattice_size;
        let cell_count = u64::from(a)
            .checked_mul(u64::from(b))
            .and_then(|value| value.checked_mul(u64::from(c)))
            .ok_or_else(|| positions_limit(u64::MAX))?;
        let environment_count = cell_count
            .checked_mul(u64::from(position_count))
            .ok_or_else(|| positions_limit(u64::MAX))?;

        if environment_count > MAX_ENVIRONMENTS {
            return Err(positions_limit(environment_count));
        }

        Ok(Self {
            a,
            b,
            c,
            p: position_count,
            pc: particle_count,
            cell_count: u32::try_from(cell_count).map_err(|_| positions_limit(cell_count))?,
            environment_count: u32::try_from(environment_count)
                .map_err(|_| positions_limit(environment_count))?,
        })
    }

    /// Derives the dimensions from a validated job and checks the model vectors the context indexes.
    ///
    /// The phase-0 memory plan reads `structure.environments[position]` for every P1 position and the invariant
    /// sweep reads `directions[direction_index]` for every compiled direction, so both vectors and the P1
    /// position count are checked here, before any phase indexes them. A hand-built job with a short vector is
    /// therefore rejected as invalid input instead of panicking.
    ///
    /// # Errors
    ///
    /// See [`Dimensions::new`]; additionally `InvalidInput` when the model counts, the structure vectors, or the
    /// direction vector disagree with the declared counts.
    pub fn from_job(job: &JobPackage) -> Result<Self, ContextError> {
        let dimensions = Self::new(
            job.lattice.lattice_size,
            job.lattice.position_count,
            job.model.counts.particle_count,
        )?;

        let positions = dimensions.p as usize;
        let structure = &job.model.structure;
        if job.model.counts.position_count != dimensions.p
            || structure.positions.len() != positions
            || structure.environments.len() != positions
        {
            return Err(ContextError::InvalidInput {
                what: "position count",
                detail: format!(
                    "the model counts imply {} positions, the structure has {} positions and {} environments",
                    dimensions.p,
                    structure.positions.len(),
                    structure.environments.len()
                ),
            });
        }

        let directions = job.model.transitions.directions.len();
        if job.model.counts.direction_count as usize != directions {
            return Err(ContextError::InvalidInput {
                what: "direction count",
                detail: format!(
                    "the model declares {} directions but the transitions hold {directions}",
                    job.model.counts.direction_count
                ),
            });
        }

        Ok(dimensions)
    }

    /// Returns the `a` supercell extent.
    #[must_use]
    pub const fn a(&self) -> u32 {
        self.a
    }

    /// Returns the `b` supercell extent.
    #[must_use]
    pub const fn b(&self) -> u32 {
        self.b
    }

    /// Returns the `c` supercell extent.
    #[must_use]
    pub const fn c(&self) -> u32 {
        self.c
    }

    /// Returns the number of canonical P1 positions per unit cell.
    #[must_use]
    pub const fn p(&self) -> u32 {
        self.p
    }

    /// Returns the number of compiled particles per cell, including void.
    #[must_use]
    pub const fn pc(&self) -> u32 {
        self.pc
    }

    /// Returns the supercell size `[a, b, c]`.
    #[must_use]
    pub const fn lattice_size(&self) -> [u32; 3] {
        [self.a, self.b, self.c]
    }

    /// Returns `A·B·C`.
    #[must_use]
    pub const fn cell_count(&self) -> u32 {
        self.cell_count
    }

    /// Returns `A·B·C·P`.
    #[must_use]
    pub const fn environment_count(&self) -> u32 {
        self.environment_count
    }

    /// Returns the total P1-keyed table length `P·pc`.
    #[must_use]
    pub const fn position_particle_count(&self) -> u64 {
        self.p as u64 * self.pc as u64
    }

    /// Returns `true` when `env` addresses a supercell environment.
    #[must_use]
    pub const fn contains_env(&self, env: u32) -> bool {
        env < self.environment_count
    }

    /// Returns the row-major linear environment index.
    ///
    /// # Panics
    ///
    /// Debug builds panic when an argument is out of range; release builds are defined only for validated
    /// arguments (the constructor bounds the total).
    #[must_use]
    pub fn linear_index(&self, a: u32, b: u32, c: u32, position: u32) -> u32 {
        debug_assert!(a < self.a && b < self.b && c < self.c && position < self.p);
        position + c * self.p + b * self.p * self.c + a * self.p * self.c * self.b
    }

    /// Returns the absolute environment of a linear cell index and a P1 position.
    #[must_use]
    pub fn cell_env(&self, cell: u32, position: u32) -> EnvId {
        debug_assert!(cell < self.cell_count && position < self.p);
        EnvId(cell * self.p + position)
    }

    /// Returns the row-major linear cell index of `[a, b, c]`.
    #[must_use]
    pub const fn linear_cell(&self, coords: [u32; 3]) -> u32 {
        debug_assert!(coords[0] < self.a && coords[1] < self.b && coords[2] < self.c);
        (coords[0] * self.b + coords[1]) * self.c + coords[2]
    }

    /// Returns the cell index of `cell` shifted by `offset` and wrapped component-wise into the supercell.
    #[must_use]
    pub fn wrapped_cell_index(&self, cell: u32, offset: [i32; 3]) -> u32 {
        self.wrapped_cell_index_i64(cell, offset.map(i64::from))
    }

    /// Returns the cell index of `cell` shifted by a 64-bit offset and wrapped component-wise.
    ///
    /// The `i64` form is used by the inverse routing index, which subtracts an `i32` offset: negating
    /// `i32::MIN` in 32 bits would overflow, so the subtraction is widened first.
    #[must_use]
    pub fn wrapped_cell_index_i64(&self, cell: u32, offset: [i64; 3]) -> u32 {
        let coords = self.cell_coords(cell);
        let wrapped = [
            (i64::from(coords[0]) + offset[0]).rem_euclid(i64::from(self.a)) as u32,
            (i64::from(coords[1]) + offset[1]).rem_euclid(i64::from(self.b)) as u32,
            (i64::from(coords[2]) + offset[2]).rem_euclid(i64::from(self.c)) as u32,
        ];
        self.linear_cell(wrapped)
    }

    /// Returns the absolute environment of `cell` shifted by `offset`, at `position`.
    #[must_use]
    pub fn wrapped_env(&self, cell: u32, offset: [i32; 3], position: u32) -> EnvId {
        self.cell_env(self.wrapped_cell_index(cell, offset), position)
    }

    /// Returns the supercell cell of an absolute environment.
    #[must_use]
    pub const fn cell_of(&self, env: EnvId) -> u32 {
        debug_assert!(self.contains_env(env.0));
        env.0 / self.p
    }

    /// Returns the P1 position of an absolute environment.
    #[must_use]
    pub const fn position_of(&self, env: EnvId) -> u32 {
        debug_assert!(self.contains_env(env.0));
        env.0 % self.p
    }

    /// Splits a linear environment index into its cells and P1 position.
    #[must_use]
    pub fn split(&self, env: EnvId) -> ([u32; 3], u32) {
        (self.cell_coords(self.cell_of(env)), self.position_of(env))
    }

    /// Splits a linear cell index into `[a, b, c]`.
    #[must_use]
    pub fn cell_coords(&self, cell: u32) -> [u32; 3] {
        debug_assert!(cell < self.cell_count);
        let per_a = self.b * self.c;
        let a = cell / per_a;
        let rest = cell % per_a;
        [a, rest / self.c, rest % self.c]
    }

    /// Wraps one cell coordinate plus an image offset into `0..extent`.
    ///
    /// The addition is evaluated in `i64` with `rem_euclid`, so an `i32::MIN`/`i32::MAX` payload offset cannot
    /// overflow and wraps exactly. `extent` must be non-zero (guaranteed by [`Dimensions::new`]).
    #[must_use]
    pub const fn wrap_component(coordinate: u32, offset: i32, extent: u32) -> u32 {
        (coordinate as i64 + offset as i64).rem_euclid(extent as i64) as u32
    }

    /// Wraps a cell plus a relative image offset component-wise.
    #[must_use]
    pub fn wrap_cell(&self, cell: [u32; 3], offset: [i32; 3]) -> [u32; 3] {
        [
            Self::wrap_component(cell[0], offset[0], self.a),
            Self::wrap_component(cell[1], offset[1], self.b),
            Self::wrap_component(cell[2], offset[2], self.c),
        ]
    }
}

/// A dense absolute supercell environment `0..A·B·C·P`.
#[derive(Clone, Copy, Debug, Default, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(transparent)]
pub struct EnvId(u32);

impl EnvId {
    /// Wraps a validated absolute environment index.
    #[must_use]
    pub const fn new(value: u32) -> Self {
        Self(value)
    }

    /// Returns the dense index.
    #[must_use]
    pub const fn get(self) -> u32 {
        self.0
    }

    /// Returns the dense index as a `usize` for arena indexing.
    #[must_use]
    pub const fn index(self) -> usize {
        self.0 as usize
    }
}

impl fmt::Display for EnvId {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        fmt::Display::fmt(&self.0, formatter)
    }
}

impl From<EnvId> for u32 {
    fn from(id: EnvId) -> Self {
        id.0
    }
}

fn positions_limit(value: u64) -> ContextError {
    ContextError::LimitExceeded {
        what: "positions",
        value,
        limit: MAX_ENVIRONMENTS,
    }
}

#[cfg(test)]
mod tests {
    use super::{Dimensions, EnvId};
    use crate::context::ContextError;

    fn ceria() -> Dimensions {
        Dimensions::new([6, 6, 6], 4, 5).expect("a 6x6x6 ceria supercell is valid")
    }

    #[test]
    fn the_counts_are_derived_and_checked() {
        let dimensions = ceria();
        assert_eq!(dimensions.lattice_size(), [6, 6, 6]);
        assert_eq!(dimensions.cell_count(), 216);
        assert_eq!(dimensions.environment_count(), 864);
        assert_eq!(dimensions.position_particle_count(), 20);
    }

    #[test]
    fn linear_indices_round_trip_in_the_documented_order() {
        let dimensions = ceria();

        for a in 0..dimensions.a() {
            for b in 0..dimensions.b() {
                for c in 0..dimensions.c() {
                    for position in 0..dimensions.p() {
                        let env = dimensions.linear_index(a, b, c, position);
                        let id = EnvId::new(env);
                        assert_eq!(dimensions.split(id), ([a, b, c], position));
                        assert_eq!(dimensions.cell_env(env / dimensions.p(), position), id);
                    }
                }
            }
        }
    }

    #[test]
    fn cell_indices_and_wrapped_offsets_round_trip() {
        let dimensions = ceria();
        for a in 0..dimensions.a() {
            for b in 0..dimensions.b() {
                for c in 0..dimensions.c() {
                    let cell = dimensions.linear_cell([a, b, c]);
                    assert_eq!(dimensions.cell_coords(cell), [a, b, c]);
                    assert_eq!(dimensions.wrapped_cell_index(cell, [0, 0, 0]), cell);
                    assert_eq!(
                        dimensions.wrapped_cell_index(cell, [1, -1, i32::MAX]),
                        dimensions.linear_cell([
                            (i64::from(a) + 1).rem_euclid(i64::from(dimensions.a())) as u32,
                            (i64::from(b) - 1).rem_euclid(i64::from(dimensions.b())) as u32,
                            (i64::from(c) + i64::from(i32::MAX))
                                .rem_euclid(i64::from(dimensions.c()))
                                as u32,
                        ])
                    );
                }
            }
        }
    }

    #[test]
    fn row_major_cell_coordinates_match_the_payload_order() {
        let dimensions = ceria();
        assert_eq!(dimensions.cell_coords(0), [0, 0, 0]);
        assert_eq!(dimensions.cell_coords(1), [0, 0, 1]);
        assert_eq!(dimensions.cell_coords(6), [0, 1, 0]);
        assert_eq!(dimensions.cell_coords(36), [1, 0, 0]);
        assert_eq!(dimensions.cell_coords(215), [5, 5, 5]);
    }

    #[test]
    fn wrap_handles_every_i32_offset_without_overflow() {
        for (offset, expected) in [
            (0, 0),
            (1, 1),
            (-1, 5),
            (5, 5),
            (6, 0),
            (-6, 0),
            (i32::MAX, 1),
            (i32::MIN, 4),
        ] {
            let wrapped = Dimensions::wrap_component(0, offset, 6);
            let oracle = (i64::from(offset)).rem_euclid(6);
            assert_eq!(i64::from(wrapped), oracle, "offset {offset}");
            assert_eq!(wrapped, expected, "offset {offset}");
        }

        let dimensions = ceria();
        assert_eq!(
            dimensions.wrap_cell([5, 5, 5], [i32::MAX, i32::MIN, 1]),
            [0, 3, 0]
        );
        assert_eq!(
            dimensions.wrapped_cell_index_i64(0, [-i64::from(i32::MIN), 0, 0]),
            dimensions.linear_cell([(2_147_483_648_i64).rem_euclid(6) as u32, 0, 0])
        );
    }

    #[test]
    fn a_zero_extent_is_rejected() {
        for size in [[0, 1, 1], [1, 0, 1], [1, 1, 0]] {
            let error = Dimensions::new(size, 1, 1).expect_err("a zero extent is invalid");
            assert!(matches!(
                error,
                ContextError::InvalidInput {
                    what: "lattice size",
                    ..
                }
            ));
        }

        let error = Dimensions::new([1, 1, 1], 0, 1).expect_err("a zero P1 count is invalid");
        assert!(matches!(
            error,
            ContextError::InvalidInput {
                what: "position count",
                ..
            }
        ));
    }

    #[test]
    fn the_particle_count_is_bounded_by_the_six_bit_state_code() {
        for pc in [0, 65] {
            let error = Dimensions::new([1, 1, 1], 1, pc).expect_err("pc must be 1..=64");
            assert!(matches!(
                error,
                ContextError::InvalidInput {
                    what: "particle count",
                    ..
                }
            ));
        }
    }

    #[test]
    fn an_overflowing_or_too_large_supercell_is_a_limit_error() {
        let overflow = Dimensions::new([u32::MAX, u32::MAX, u32::MAX], 1, 1)
            .expect_err("the product overflows u64");
        assert!(matches!(
            overflow,
            ContextError::LimitExceeded {
                what: "positions",
                value: u64::MAX,
                ..
            }
        ));

        let too_large =
            Dimensions::new([46_341, 46_341, 1], 1, 1).expect_err("the product exceeds i32::MAX");
        assert!(matches!(
            too_large,
            ContextError::LimitExceeded {
                what: "positions",
                ..
            }
        ));
    }
}

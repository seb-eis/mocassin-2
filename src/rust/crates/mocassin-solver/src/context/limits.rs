// SPDX-License-Identifier: MIT

//! Context limits and the phase-0 memory plan.
//!
//! [`MemoryPlan::plan`] runs before the first arena allocation. It derives every array size with checked `u64`
//! arithmetic, compares the result with the configured limits, and rejects an unrepresentable job with
//! [`ContextError::LimitExceeded`] (exit 12) instead of discovering the size after an allocation attempt. A
//! passing plan is the only input to [`super::SimulationContext::initialize`]'s allocation phases, and
//! [`MemoryReport`] reports the plan's **planned lengths** times `size_of`, so `plan == report` is an exact
//! equality on lengths. Allocator capacity is a separate diagnostic (`capacity_bytes ≥ total_bytes`), not the
//! equality target.
//!
//! Step 14 grows the plan with its features: feature 1 plans the lattice/environment arrays and the counter
//! block; feature 2 adds the link, cluster, and cluster-energy arrays; feature 4 adds the pool and jump-status
//! arrays. The other limits in [`ContextLimits`] are public from feature 1 so later features and callers share
//! one value type, and each is enforced by the phase that plans its array.

use core::mem::size_of;

use mocassin_types::JobPackage;

use super::dimensions::Dimensions;
use super::error::ContextError;

/// Overridable library limits for one context.
///
/// The defaults are the Step 14 design's budgets (§2); a caller may tighten any of them, while a package that
/// needs a larger context must be rejected rather than silently over-allocated.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ContextLimits {
    /// Maximum `A·B·C·P` environments (default 500,000; also bounded by `i32::MAX`).
    pub max_positions: u64,
    /// Maximum compiled particles `pc` (default 64; six-bit state codes, `u64` masks).
    pub max_particles: u32,
    /// Maximum payload pair links `N` (default 16,000,000).
    pub max_pair_links: u64,
    /// Maximum links on one P1 position (default 65,536).
    pub max_links_per_position: u32,
    /// Maximum referenced cluster tables (default 1,000,000).
    pub max_cluster_tables: u32,
    /// Maximum cluster instances `K` (default 100,000; one per slot-0 link).
    pub max_cluster_instances: u32,
    /// Maximum distinct selectable direction counts (default 65,536).
    pub max_pools: u32,
    /// Maximum selectable jump count the RNG may draw against (default `u32::MAX`).
    pub max_selectable_jump_count: u64,
    /// Maximum path nodes of one jump direction (default 10, the state-code bound).
    pub max_path_nodes: u8,
    /// Byte cap for the site table plus the site+pair vectors (default 512 MiB).
    pub max_energy_vector_bytes: u64,
    /// Byte cap for forward links, the inverse index, and cluster associations (default 768 MiB).
    pub max_link_bytes: u64,
    /// Byte cap for every cluster array except the per-instance energy (default 256 MiB).
    pub max_cluster_state_bytes: u64,
    /// Byte cap for the per-instance cluster energy array (default 256 MiB).
    pub max_cluster_energy_bytes: u64,
    /// Byte cap for pool members, the slot map, and `pool_of` (default 256 MiB).
    pub max_pool_bytes: u64,
    /// Maximum jump statuses `P·D` (default 4,000,000; also bounds the status build).
    pub max_status_count: u64,
    /// Maximum jump links `J` (default 8,000,000).
    pub max_jump_links: u64,
    /// Byte cap for jump-status offsets and links (default 256 MiB).
    pub max_jump_status_bytes: u64,
    /// Byte cap for the sum of every planned array (default 1 GiB); the binding total.
    pub max_context_bytes: u64,
}

impl ContextLimits {
    /// Returns the Step 14 design budgets.
    #[must_use]
    pub const fn new() -> Self {
        Self {
            max_positions: 500_000,
            max_particles: 64,
            max_pair_links: 16_000_000,
            max_links_per_position: 65_536,
            max_cluster_tables: 1_000_000,
            max_cluster_instances: 100_000,
            max_pools: 65_536,
            max_selectable_jump_count: u32::MAX as u64,
            max_path_nodes: 10,
            max_energy_vector_bytes: 512 * 1024 * 1024,
            max_link_bytes: 768 * 1024 * 1024,
            max_cluster_state_bytes: 256 * 1024 * 1024,
            max_cluster_energy_bytes: 256 * 1024 * 1024,
            max_pool_bytes: 256 * 1024 * 1024,
            max_status_count: 4_000_000,
            max_jump_links: 8_000_000,
            max_jump_status_bytes: 256 * 1024 * 1024,
            max_context_bytes: 1024 * 1024 * 1024,
        }
    }
}

impl Default for ContextLimits {
    fn default() -> Self {
        Self::new()
    }
}

/// One planned array or arena component.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct MemoryEntry {
    /// The stable diagnostic name of the array, for example `lattice.particles`.
    pub what: &'static str,
    /// The planned element count.
    pub elements: u64,
    /// The planned byte length: `elements × size_of::<E>()`.
    pub bytes: u64,
}

/// The phase-0 allocation plan of one context.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct MemoryPlan {
    entries: Vec<MemoryEntry>,
    total_bytes: u64,
}

impl MemoryPlan {
    /// Plans every array of the context for a validated job.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::LimitExceeded`] when a dimension product overflows, when a count exceeds its
    /// configured cap, or when the summed bytes exceed `max_context_bytes`.
    pub fn plan(job: &JobPackage, limits: &ContextLimits) -> Result<Self, ContextError> {
        let dimensions = Dimensions::from_job(job)?;
        Self::for_dimensions(&dimensions, limits)
    }

    /// Plans the arrays for already-derived dimensions.
    ///
    /// # Errors
    ///
    /// See [`MemoryPlan::plan`].
    pub(crate) fn for_dimensions(
        dimensions: &Dimensions,
        limits: &ContextLimits,
    ) -> Result<Self, ContextError> {
        let environments = u64::from(dimensions.environment_count());
        let particles = u64::from(dimensions.pc());

        if environments > limits.max_positions {
            return Err(ContextError::LimitExceeded {
                what: "max_positions",
                value: environments,
                limit: limits.max_positions,
            });
        }

        if particles > u64::from(limits.max_particles) {
            return Err(ContextError::LimitExceeded {
                what: "max_particles",
                value: particles,
                limit: u64::from(limits.max_particles),
            });
        }

        let entries = vec![
            entry(
                "lattice.particles",
                environments,
                1,
                limits.max_context_bytes,
            )?,
            entry("lattice.flags", environments, 1, limits.max_context_bytes)?,
            entry(
                "lattice.mobile_tracker",
                environments,
                4,
                limits.max_context_bytes,
            )?,
            entry(
                "counters.per_particle",
                particles,
                size_of::<super::goals::ParticleCounters>() as u64,
                limits.max_context_bytes,
            )?,
            entry(
                "counters.scalars",
                2,
                size_of::<u64>() as u64,
                limits.max_context_bytes,
            )?,
        ];

        let mut total_bytes = 0_u64;
        for planned in &entries {
            total_bytes = checked_add(total_bytes, planned.bytes, "max_context_bytes", u64::MAX)?;
        }

        if total_bytes > limits.max_context_bytes {
            return Err(ContextError::LimitExceeded {
                what: "max_context_bytes",
                value: total_bytes,
                limit: limits.max_context_bytes,
            });
        }

        Ok(Self {
            entries,
            total_bytes,
        })
    }

    /// Returns the planned entries in insertion order.
    #[must_use]
    pub fn entries(&self) -> &[MemoryEntry] {
        &self.entries
    }

    /// Returns the summed planned bytes.
    #[must_use]
    pub const fn total_bytes(&self) -> u64 {
        self.total_bytes
    }
}

/// The memory report of a built context.
///
/// The report mirrors the plan exactly (planned lengths and bytes). `capacity_bytes` is the allocator
/// diagnostic and is always `≥ total_bytes` for the arrays the context has materialised.
#[derive(Clone, Debug, PartialEq, Eq)]
pub struct MemoryReport {
    entries: Vec<MemoryEntry>,
    total_bytes: u64,
    capacity_bytes: u64,
}

impl MemoryReport {
    /// Returns the planned entries in the plan's order.
    #[must_use]
    pub fn entries(&self) -> &[MemoryEntry] {
        &self.entries
    }

    /// Returns the summed planned bytes.
    #[must_use]
    pub const fn total_bytes(&self) -> u64 {
        self.total_bytes
    }

    /// Returns the summed allocated capacity of the materialised arrays.
    #[must_use]
    pub const fn capacity_bytes(&self) -> u64 {
        self.capacity_bytes
    }

    /// Builds the report from the admitting plan and the observed capacity.
    #[must_use]
    pub(crate) fn from_plan(plan: &MemoryPlan, capacity_bytes: u64) -> Self {
        Self {
            entries: plan.entries.clone(),
            total_bytes: plan.total_bytes,
            capacity_bytes,
        }
    }
}

fn entry(
    what: &'static str,
    elements: u64,
    element_bytes: u64,
    limit: u64,
) -> Result<MemoryEntry, ContextError> {
    Ok(MemoryEntry {
        what,
        elements,
        bytes: checked_mul(elements, element_bytes, what, limit)?,
    })
}

pub(crate) fn checked_mul(
    left: u64,
    right: u64,
    what: &'static str,
    limit: u64,
) -> Result<u64, ContextError> {
    left.checked_mul(right).ok_or(ContextError::LimitExceeded {
        what,
        value: u64::MAX,
        limit,
    })
}

pub(crate) fn checked_add(
    left: u64,
    right: u64,
    what: &'static str,
    limit: u64,
) -> Result<u64, ContextError> {
    left.checked_add(right).ok_or(ContextError::LimitExceeded {
        what,
        value: u64::MAX,
        limit,
    })
}

#[cfg(test)]
mod tests {
    use core::mem::size_of;

    use super::{ContextLimits, MemoryPlan, MemoryReport};
    use crate::context::ContextError;
    use crate::context::dimensions::Dimensions;

    fn ceria() -> Dimensions {
        Dimensions::new([6, 6, 6], 4, 5).expect("valid ceria dimensions")
    }

    #[test]
    fn the_default_limits_are_the_documented_budgets() {
        let limits = ContextLimits::default();
        assert_eq!(limits.max_positions, 500_000);
        assert_eq!(limits.max_particles, 64);
        assert_eq!(limits.max_pair_links, 16_000_000);
        assert_eq!(limits.max_links_per_position, 65_536);
        assert_eq!(limits.max_cluster_tables, 1_000_000);
        assert_eq!(limits.max_cluster_instances, 100_000);
        assert_eq!(limits.max_pools, 65_536);
        assert_eq!(limits.max_selectable_jump_count, u64::from(u32::MAX));
        assert_eq!(limits.max_path_nodes, 10);
        assert_eq!(limits.max_energy_vector_bytes, 512 * 1024 * 1024);
        assert_eq!(limits.max_link_bytes, 768 * 1024 * 1024);
        assert_eq!(limits.max_cluster_state_bytes, 256 * 1024 * 1024);
        assert_eq!(limits.max_cluster_energy_bytes, 256 * 1024 * 1024);
        assert_eq!(limits.max_pool_bytes, 256 * 1024 * 1024);
        assert_eq!(limits.max_status_count, 4_000_000);
        assert_eq!(limits.max_jump_links, 8_000_000);
        assert_eq!(limits.max_jump_status_bytes, 256 * 1024 * 1024);
        assert_eq!(limits.max_context_bytes, 1024 * 1024 * 1024);
    }

    #[test]
    fn the_plan_sizes_the_lattice_and_counter_arrays() {
        let plan = MemoryPlan::for_dimensions(&ceria(), &ContextLimits::default())
            .expect("the ceria plan fits");

        let entries = plan.entries();
        assert_eq!(entries.len(), 5);
        assert_eq!(entries[0].what, "lattice.particles");
        assert_eq!(entries[0].elements, 864);
        assert_eq!(entries[0].bytes, 864);
        assert_eq!(entries[1].what, "lattice.flags");
        assert_eq!(entries[1].bytes, 864);
        assert_eq!(entries[2].what, "lattice.mobile_tracker");
        assert_eq!(entries[2].bytes, 864 * 4);
        assert_eq!(entries[3].what, "counters.per_particle");
        assert_eq!(entries[3].elements, 5);
        assert_eq!(
            entries[3].bytes,
            5 * size_of::<crate::context::goals::ParticleCounters>() as u64
        );
        assert_eq!(entries[4].what, "counters.scalars");
        assert_eq!(entries[4].bytes, 16);
        assert_eq!(plan.total_bytes(), 864 + 864 + 3456 + 240 + 16);
    }

    #[test]
    fn every_entry_is_a_whole_number_of_elements() {
        let plan = MemoryPlan::for_dimensions(&ceria(), &ContextLimits::default())
            .expect("the ceria plan fits");

        for planned in plan.entries() {
            assert_ne!(planned.elements, 0, "{}", planned.what);
            assert_eq!(planned.bytes % planned.elements, 0, "{}", planned.what);
        }
    }

    #[test]
    fn the_report_equals_the_plan_on_lengths_and_bytes() {
        let plan = MemoryPlan::for_dimensions(&ceria(), &ContextLimits::default())
            .expect("the ceria plan fits");
        let report = MemoryReport::from_plan(&plan, plan.total_bytes() + 512);

        assert_eq!(report.entries(), plan.entries());
        assert_eq!(report.total_bytes(), plan.total_bytes());
        assert!(report.capacity_bytes() >= report.total_bytes());
    }

    #[test]
    fn the_position_cap_rejects_before_allocation() {
        let limits = ContextLimits {
            max_positions: 863,
            ..ContextLimits::default()
        };
        let error = MemoryPlan::for_dimensions(&ceria(), &limits).expect_err("864 > 863");
        assert!(matches!(
            error,
            ContextError::LimitExceeded {
                what: "max_positions",
                value: 864,
                limit: 863,
            }
        ));
    }

    #[test]
    fn a_tightened_particle_cap_rejects() {
        let limits = ContextLimits {
            max_particles: 4,
            ..ContextLimits::default()
        };
        let error = MemoryPlan::for_dimensions(&ceria(), &limits).expect_err("5 > 4");
        assert!(matches!(
            error,
            ContextError::LimitExceeded {
                what: "max_particles",
                value: 5,
                limit: 4,
            }
        ));
    }

    #[test]
    fn the_total_cap_is_the_binding_limit() {
        for cap in [0, 100, 5439] {
            let limits = ContextLimits {
                max_context_bytes: cap,
                ..ContextLimits::default()
            };
            let error = MemoryPlan::for_dimensions(&ceria(), &limits)
                .expect_err("the total exceeds the cap");
            assert!(matches!(
                error,
                ContextError::LimitExceeded {
                    what: "max_context_bytes",
                    value: 5440,
                    limit,
                } if limit == cap
            ));
        }

        let limits = ContextLimits {
            max_context_bytes: 5440,
            ..ContextLimits::default()
        };
        assert!(MemoryPlan::for_dimensions(&ceria(), &limits).is_ok());
    }
}

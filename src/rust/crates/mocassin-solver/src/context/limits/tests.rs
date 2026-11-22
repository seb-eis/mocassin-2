// SPDX-License-Identifier: MIT

//! Unit tests for the context limits and the phase-0 memory plan.

use std::path::Path;

use core::mem::size_of;

use mocassin_msl::Package;
use mocassin_types::{JobPackage, ReaderLimits};

use super::{ContextLimits, MemoryPlan, MemoryReport};
use crate::context::ContextError;
use crate::context::dimensions::Dimensions;

fn ceria() -> Dimensions {
    Dimensions::new([6, 6, 6], 4, 5).expect("valid ceria dimensions")
}

fn ceria_job() -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl");
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the msl-v1 fixture opens")
        .load_job(1)
        .expect("the fixture job maps")
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
    let plan = MemoryPlan::for_core_dimensions(&ceria(), &ContextLimits::default())
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
    let plan = MemoryPlan::for_core_dimensions(&ceria(), &ContextLimits::default())
        .expect("the ceria plan fits");

    for planned in plan.entries() {
        assert_ne!(planned.elements, 0, "{}", planned.what);
        assert_eq!(planned.bytes % planned.elements, 0, "{}", planned.what);
    }
}

#[test]
fn the_report_equals_the_plan_on_lengths_and_bytes() {
    let plan = MemoryPlan::for_core_dimensions(&ceria(), &ContextLimits::default())
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
    let error = MemoryPlan::for_core_dimensions(&ceria(), &limits).expect_err("864 > 863");
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
    let error = MemoryPlan::for_core_dimensions(&ceria(), &limits).expect_err("5 > 4");
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
        let error = MemoryPlan::for_core_dimensions(&ceria(), &limits)
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
    assert!(MemoryPlan::for_core_dimensions(&ceria(), &limits).is_ok());
}

#[test]
fn a_raised_path_node_cap_is_rejected_before_allocation() {
    let limits = ContextLimits {
        max_path_nodes: 11,
        ..ContextLimits::default()
    };
    let error = MemoryPlan::plan(&ceria_job(), &limits)
        .expect_err("the fixed jump-status scratch holds ten nodes");

    assert!(
        matches!(
            error,
            ContextError::InvalidInput {
                what: "max_path_nodes",
                ..
            }
        ),
        "unexpected error {error}"
    );
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn the_path_node_cap_may_be_the_fixed_scratch_bound() {
    let limits = ContextLimits {
        max_path_nodes: 10,
        ..ContextLimits::default()
    };
    assert!(MemoryPlan::plan(&ceria_job(), &limits).is_ok());

    let lowered = ContextLimits {
        max_path_nodes: 9,
        ..ContextLimits::default()
    };
    assert!(MemoryPlan::plan(&ceria_job(), &lowered).is_ok());
}

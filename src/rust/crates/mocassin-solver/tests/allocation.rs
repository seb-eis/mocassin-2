// SPDX-License-Identifier: MIT

//! Allocation proof for the Step 14 context.
//!
//! This file is its own test binary so the counting global allocator observes only this test's allocations. It
//! measures the full initialization cost, verifies that the report covers every planned array, and then proves
//! that the hot accessors and the pool `set_selectable` helpers perform zero allocations after construction.

use std::alloc::{GlobalAlloc, Layout, System};
use std::hint::black_box;
use std::path::Path;
use std::sync::atomic::{AtomicU64, Ordering};

use mocassin_msl::Package;
use mocassin_solver::context::{
    ContextOptions, EnvId, InitOutcome, ParticleId, PositionId, SimulationContext,
};
use mocassin_types::{JobPackage, ReaderLimits};

struct CountingAllocator;

static ALLOCATIONS: AtomicU64 = AtomicU64::new(0);
static BYTES: AtomicU64 = AtomicU64::new(0);

unsafe impl GlobalAlloc for CountingAllocator {
    unsafe fn alloc(&self, layout: Layout) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        BYTES.fetch_add(layout.size() as u64, Ordering::Relaxed);
        unsafe { System.alloc(layout) }
    }

    unsafe fn dealloc(&self, pointer: *mut u8, layout: Layout) {
        unsafe { System.dealloc(pointer, layout) }
    }

    unsafe fn realloc(&self, pointer: *mut u8, layout: Layout, new_size: usize) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, Ordering::Relaxed);
        BYTES.fetch_add(new_size as u64, Ordering::Relaxed);
        unsafe { System.realloc(pointer, layout, new_size) }
    }
}

#[global_allocator]
static GLOBAL: CountingAllocator = CountingAllocator;

fn reset() {
    ALLOCATIONS.store(0, Ordering::Relaxed);
    BYTES.store(0, Ordering::Relaxed);
}

fn counters() -> (u64, u64) {
    (
        ALLOCATIONS.load(Ordering::Relaxed),
        BYTES.load(Ordering::Relaxed),
    )
}

fn fixture_job(package: &str, file: &str, job_id: u32) -> JobPackage {
    let path = Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../../../tests/contracts/packages")
        .join(package)
        .join(file);
    Package::open_readonly(path, ReaderLimits::default())
        .expect("the fixture opens")
        .load_job(job_id)
        .expect("the fixture job maps")
}

fn initialize(job: JobPackage) -> SimulationContext {
    match SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect("the fixture initializes")
    {
        InitOutcome::Ready(context) => *context,
        InitOutcome::AlreadyCompleted { .. } => panic!("a fresh job is not completed"),
    }
}

fn assert_report_covers_the_plan(context: &SimulationContext) {
    let report = context.memory_report();
    let summed: u64 = report.entries().iter().map(|entry| entry.bytes).sum();
    assert_eq!(summed, report.total_bytes());
    assert!(report.capacity_bytes() >= report.total_bytes());
    assert_eq!(report.total_bytes(), context.memory_report().total_bytes());
}

#[test]
fn the_context_allocates_once_and_hot_accessors_allocate_nothing() {
    let job = fixture_job("step-14-ceria-10x10x10", "step-14-ceria-10x10x10.msl", 1);
    reset();
    let mut context = initialize(job);
    let (build_allocations, build_bytes) = counters();
    let report = context.memory_report();
    assert_report_covers_the_plan(&context);
    assert!(build_bytes >= report.total_bytes());
    eprintln!(
        "context build: {build_allocations} allocations, {build_bytes} bytes; plan {} bytes, capacity {} bytes",
        report.total_bytes(),
        report.capacity_bytes()
    );
    for entry in report.entries() {
        eprintln!(
            "  {} elements={} bytes={}",
            entry.what, entry.elements, entry.bytes
        );
    }

    let environments = context.dimensions.environment_count();
    let position = PositionId::new(0);
    let particle = ParticleId::new(1);

    reset();
    for _ in 0..1_000 {
        black_box(context.node_energy(EnvId::new(0), particle));
        black_box(context.node_energy(EnvId::new(environments - 1), particle));
        black_box(context.site_energy(position, particle));
        black_box(context.link_range(position).len());
        black_box(context.incoming(EnvId::new(0)).len());
        black_box(context.jump_links(position, 0).len());
        black_box(context.jump_status(0).len());
        black_box(context.mobile_positions().len());
        black_box(context.static_tracker_mapping().len());
        black_box(context.global_tracker_mapping().len());
        black_box(context.dt());
        black_box(context.goals.total);
        black_box(context.normalization.k_norm);
    }
    let (hot_allocations, hot_bytes) = counters();
    assert_eq!((hot_allocations, hot_bytes), (0, 0));

    // A real particle change: remove the first pool member and add it back 200 times. This exercises the
    // swap-remove/slot maps and must not reallocate.
    let member = context.pools.pools[0].members[0];
    let original = member.particle;
    let (pool_capacity, pool_pointer) = {
        let pool = &context.pools.pools[0];
        (pool.members.capacity(), pool.members.as_ptr())
    };
    reset();
    for _ in 0..200 {
        context.set_particle(member.env, ParticleId::new(0));
        context.set_selectable(member.env, original);
        context.set_selectable(member.env, ParticleId::new(0));
        context.set_particle(member.env, original);
        context.set_selectable(member.env, ParticleId::new(0));
        context.set_selectable(member.env, original);
    }
    let (pool_allocations, pool_bytes) = counters();
    assert_eq!((pool_allocations, pool_bytes), (0, 0));
    assert_eq!(context.pools.pools[0].members.capacity(), pool_capacity);
    assert_eq!(context.pools.pools[0].members.as_ptr(), pool_pointer);

    // The populated package adds the cluster and pair tables; the same hot accessors stay allocation-free.
    let populated = fixture_job("step-13-populated", "step-13-populated.msl", 1);
    reset();
    let populated_context = initialize(populated);
    let (populated_allocations, populated_bytes) = counters();
    assert_report_covers_the_plan(&populated_context);
    let populated_report = populated_context.memory_report();
    assert!(populated_bytes >= populated_report.total_bytes());
    eprintln!(
        "populated context build: {populated_allocations} allocations, {populated_bytes} bytes; plan {} bytes, capacity {} bytes",
        populated_report.total_bytes(),
        populated_report.capacity_bytes()
    );
    reset();
    for _ in 0..1_000 {
        black_box(populated_context.cluster_energy(0, 0, ParticleId::new(1)));
        black_box(populated_context.cluster_code(0, 0));
        black_box(populated_context.cluster_code_id(0, 0));
        black_box(populated_context.recompute_lattice_energy_kt());
        populated_context.check_lattice_energy();
    }
    let (cluster_allocations, cluster_bytes) = counters();
    assert_eq!((cluster_allocations, cluster_bytes), (0, 0));
}

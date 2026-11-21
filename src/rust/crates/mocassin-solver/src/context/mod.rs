// SPDX-License-Identifier: MIT

//! The simulation context: deterministic, allocation-bounded runtime state built once per job.
//!
//! [`SimulationContext::initialize`] is a pure function of a validated [`JobPackage`], the [`ContextOptions`],
//! and an optional cancellation flag. It performs no I/O, opens no package, and writes nothing. Construction is
//! phased (Step 14 design §1/§5.1) and every phase is validated before the next; a failed phase returns one
//! [`ContextError`] naming the field and the dense index, and no partial context escapes.
//!
//! Step 14 builds the context in six features. Feature 1 provides:
//!
//! - the [`Dimensions`]/[`EnvId`] contract and the checked index/wrap helpers;
//! - [`ContextLimits`], the phase-0 [`MemoryPlan`], and the [`MemoryReport`];
//! - the [`LatticeState`] particle/flag/mobile-tracker arenas and the zeroed [`Counters`];
//! - the empty [`LinkArena`], [`ClusterArena`], [`SelectionPools`], and [`JumpRuntime`] containers that features
//!   2–4 fill;
//! - the phase pipeline, cancellation, the exit-code mapping, and the invariant sweep;
//! - the `Send`/`!Sync` guarantee: the context is movable across threads but is deliberately not shareable,
//!   because one trajectory is single-threaded.
//!
//! Features 2–5 add the links/clusters, energy vectors, pools/jump status, and goals/normalization/resume. They
//! extend this struct and the plan; they do not change the dimension, error, phase, or memory-report contract.
//!
//! ```compile_fail
//! // A context must not be shared: one trajectory is single-threaded.
//! fn assert_sync<T: Sync>() {}
//! assert_sync::<mocassin_solver::context::SimulationContext>();
//! ```
//!
//! ```
//! // Moving a context to another thread is allowed; sharing it is not.
//! fn assert_send<T: Send>() {}
//! assert_send::<mocassin_solver::context::SimulationContext>();
//! ```

use core::marker::PhantomData;
use core::mem::size_of;
use core::sync::atomic::{AtomicBool, Ordering};

use mocassin_types::{JobPackage, SimulationKind};

mod clusters;
mod dimensions;
mod error;
mod goals;
mod jumps;
mod lattice;
mod limits;
mod links;
mod pools;

pub use clusters::{ClusterArena, ClusterInstance};
pub use dimensions::{Dimensions, EnvId};
pub use error::ContextError;
pub use goals::{Counters, ParticleCounters};
pub use jumps::JumpRuntime;
pub use lattice::{HAS_MOBILE_TRACKER, LatticeState, MOBILE, NO_MOBILE_TRACKER, STABLE};
pub use limits::{ContextLimits, MemoryEntry, MemoryPlan, MemoryReport};
pub use links::{ClusterAssoc, IncomingLink, Link, LinkArena};
pub use pools::{PoolMember, SelectionPool, SelectionPools};

/// The cancellation check stride: initialization tests the caller's flag every 4096 environments.
pub(crate) const CANCEL_STRIDE: usize = 4096;

/// One ordered phase of context construction.
///
/// The discriminants are the Step 14 design's phase numbers and must not be renumbered. Feature 1 implements
/// `MemoryPlan`, `ValidateInputs`, `Lattice`, and `InvariantSweep`; the remaining phases are filled by features
/// 2–5 and are part of the enum now so the ordering contract is stable.
#[derive(Clone, Copy, Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
#[repr(u8)]
pub enum InitPhase {
    /// Phase 0: plan every array with checked arithmetic and reject before allocation.
    MemoryPlan = 0,
    /// Phase 1: validate the dimensions, arrays, particle range, and jump tables the context reads.
    ValidateInputs = 1,
    /// Phase 2: build the lattice/environment states and the mobile tracker id space.
    Lattice = 2,
    /// Phase 3: build the owner-major link CSR and the inverse routing index (feature 2).
    Links = 3,
    /// Phase 4: validate cluster geometry and build the instances (feature 2).
    Clusters = 4,
    /// Phase 5: build the site table and the per-environment site+pair vectors (feature 3).
    SiteVectors = 5,
    /// Phase 6: build the per-instance cluster codes and energy vectors (feature 3).
    ClusterVectors = 6,
    /// Phase 7: compute the exact initial lattice energy (feature 3).
    InitialEnergy = 7,
    /// Phase 8: build and optimize the selection pools (feature 4).
    Pools = 8,
    /// Phase 9: build the receiver-major KMC jump status (feature 4).
    JumpStatus = 9,
    /// Phase 10: counters, goals, normalization, `nu_max`, and `dt` (feature 5).
    GoalsAndCounters = 10,
    /// Phase 11: RNG from the effective seed and the fresh/restored time (feature 5).
    RngAndTime = 11,
    /// Phase 12: invariant sweep before publishing the context.
    InvariantSweep = 12,
}

impl InitPhase {
    /// Every phase in execution order.
    pub const ALL: [Self; 13] = [
        Self::MemoryPlan,
        Self::ValidateInputs,
        Self::Lattice,
        Self::Links,
        Self::Clusters,
        Self::SiteVectors,
        Self::ClusterVectors,
        Self::InitialEnergy,
        Self::Pools,
        Self::JumpStatus,
        Self::GoalsAndCounters,
        Self::RngAndTime,
        Self::InvariantSweep,
    ];

    /// Returns the stable diagnostic name of the phase.
    #[must_use]
    pub const fn as_str(self) -> &'static str {
        match self {
            Self::MemoryPlan => "memory plan",
            Self::ValidateInputs => "input validation",
            Self::Lattice => "lattice",
            Self::Links => "links",
            Self::Clusters => "clusters",
            Self::SiteVectors => "site vectors",
            Self::ClusterVectors => "cluster vectors",
            Self::InitialEnergy => "initial energy",
            Self::Pools => "selection pools",
            Self::JumpStatus => "jump status",
            Self::GoalsAndCounters => "goals and counters",
            Self::RngAndTime => "rng and time",
            Self::InvariantSweep => "invariant sweep",
        }
    }
}

/// The result of a context initialization.
#[derive(Debug)]
pub enum InitOutcome {
    /// A fresh (or resumed) context, ready for the first Monte Carlo cycle.
    Ready(Box<SimulationContext>),
    /// The resume state is already complete. The run orchestration maps this to the distinct "already
    /// completed" outcome; the context never resumes it silently.
    AlreadyCompleted {
        /// The completed state's Monte Carlo step count.
        mcs: u64,
        /// The job's total goal at completion.
        total_goal: u64,
    },
}

/// The options of one context initialization.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct ContextOptions {
    /// The overridable library limits and the memory-plan caps.
    pub limits: ContextLimits,
}

/// The initialized runtime state of one job.
///
/// Every stored field is `Send + Sync`, but the context is deliberately **not `Sync`**: the `_not_sync`
/// marker makes the single-trajectory rule a compile-time guarantee instead of a comment. Moving the context to
/// a worker thread is fine; two threads cannot share one trajectory.
#[derive(Debug)]
pub struct SimulationContext {
    /// The validated job this context was built from; owns the shared `Arc<ParameterizedModel>`.
    pub job: JobPackage,
    /// The job kind.
    pub kind: SimulationKind,
    /// The validated supercell dimensions.
    pub dimensions: Dimensions,
    /// The lattice and environment states.
    pub lattice: LatticeState,
    /// The owner-major link CSR and the inverse routing index (empty until feature 2).
    pub links: LinkArena,
    /// The cluster instances and their derived state (empty until feature 2).
    pub clusters: ClusterArena,
    /// The KMC selection pools (empty until feature 4).
    pub pools: SelectionPools,
    /// The KMC jump status (empty until feature 4).
    pub jumps: JumpRuntime,
    /// The zeroed counters (feature 5 adds the goal/normalization arithmetic).
    pub counters: Counters,
    /// The limits the context was admitted against.
    pub limits: ContextLimits,
    /// The admitting plan, retained so [`SimulationContext::memory_report`] is the plan by construction.
    plan: MemoryPlan,
    /// Explicit `!Sync` marker; `Cell<()>` is `Send` and `!Sync`.
    _not_sync: PhantomData<core::cell::Cell<()>>,
}

impl SimulationContext {
    /// Builds the context for one validated job.
    ///
    /// Construction is single-threaded, deterministic, and allocation-bounded: the phase-0 plan runs before the
    /// first allocation, and no arena grows after construction.
    ///
    /// # Errors
    ///
    /// Returns a [`ContextError`] for an unrepresentable package (exit 12), a numerical/internal failure
    /// (exit 13), or a set cancellation flag (exit 15). See [`ContextError::exit_code`].
    pub fn initialize(
        job: JobPackage,
        options: &ContextOptions,
        cancel: Option<&AtomicBool>,
    ) -> Result<InitOutcome, ContextError> {
        initialize_with(job, options, cancel, &mut ())
    }

    /// Returns the memory report of this context.
    ///
    /// The report mirrors the admitting plan (planned lengths and bytes); `capacity_bytes` is the allocator
    /// diagnostic of the arrays materialised so far.
    #[must_use]
    pub fn memory_report(&self) -> MemoryReport {
        MemoryReport::from_plan(&self.plan, self.materialised_capacity_bytes())
    }

    fn materialised_capacity_bytes(&self) -> u64 {
        let particles =
            self.counters.per_particle.capacity() as u64 * size_of::<ParticleCounters>() as u64;
        self.lattice.capacity_bytes() + particles + 2 * size_of::<u64>() as u64
    }

    fn check_invariants(&self) -> Result<(), ContextError> {
        if !self.lattice.has_valid_shape(&self.dimensions) {
            return Err(ContextError::Invariant {
                what: "lattice arena length",
            });
        }

        if !self.lattice.has_valid_trackers() {
            return Err(ContextError::Invariant {
                what: "mobile tracker ids",
            });
        }

        if self.counters.per_particle.len() != self.dimensions.pc() as usize {
            return Err(ContextError::Invariant {
                what: "counter length",
            });
        }

        if !self.links.has_valid_shape(self.dimensions.p()) {
            return Err(ContextError::Invariant { what: "link CSR" });
        }

        if !self.clusters.has_valid_shape(&self.dimensions) {
            return Err(ContextError::Invariant {
                what: "cluster arena",
            });
        }

        if !self.pools.has_valid_shape(&self.dimensions) {
            return Err(ContextError::Invariant {
                what: "selection pools",
            });
        }

        let statuses =
            u64::from(self.dimensions.p()) * u64::from(self.job.model.counts.direction_count);
        if !self.jumps.has_valid_shape(statuses) {
            return Err(ContextError::Invariant {
                what: "jump status CSR",
            });
        }

        Ok(())
    }
}

/// Receives each phase as it starts; used by the phase-order test and available for future profiling hooks.
pub(crate) trait PhaseObserver {
    /// Called once per phase, in execution order, before the phase runs.
    fn observe(&mut self, phase: InitPhase);
}

impl PhaseObserver for () {
    fn observe(&mut self, _phase: InitPhase) {}
}

pub(crate) fn initialize_with(
    job: JobPackage,
    options: &ContextOptions,
    cancel: Option<&AtomicBool>,
    observer: &mut impl PhaseObserver,
) -> Result<InitOutcome, ContextError> {
    let probe = CancelProbe::new(cancel);

    probe.check()?;

    observer.observe(InitPhase::MemoryPlan);
    let plan = MemoryPlan::plan(&job, &options.limits)?;
    tracing::info!(
        phase = InitPhase::MemoryPlan.as_str(),
        bytes = plan.total_bytes(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::ValidateInputs);
    let dimensions = Dimensions::from_job(&job)?;
    validate_inputs(&job, &dimensions)?;
    tracing::info!(
        phase = InitPhase::ValidateInputs.as_str(),
        environments = dimensions.environment_count(),
        particles = dimensions.pc(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::Lattice);
    let lattice = LatticeState::build(&job, &dimensions, &probe)?;
    tracing::info!(
        phase = InitPhase::Lattice.as_str(),
        mobile = lattice.mobile_count(),
        "context phase completed"
    );

    let kind = job.kind;
    let counters = Counters::zeroed(dimensions.pc());
    let context = SimulationContext {
        job,
        kind,
        dimensions,
        lattice,
        links: LinkArena::empty(),
        clusters: ClusterArena::empty(),
        pools: SelectionPools::empty(),
        jumps: JumpRuntime::empty(),
        counters,
        limits: options.limits,
        plan,
        _not_sync: PhantomData,
    };

    probe.check()?;
    observer.observe(InitPhase::InvariantSweep);
    context.check_invariants()?;
    tracing::info!(
        phase = InitPhase::InvariantSweep.as_str(),
        "context phase completed"
    );

    Ok(InitOutcome::Ready(Box::new(context)))
}

/// Validates the package fields the context reads.
fn validate_inputs(job: &JobPackage, dimensions: &Dimensions) -> Result<(), ContextError> {
    let environments = dimensions.environment_count() as usize;
    let positions = dimensions.p() as usize;
    let particles = dimensions.pc() as usize;

    if job.lattice.particles.len() != environments {
        return Err(ContextError::InvalidInput {
            what: "lattice length",
            detail: format!(
                "the lattice has {} particles but A·B·C·P is {environments}",
                job.lattice.particles.len()
            ),
        });
    }

    if job.lattice.lattice_size != dimensions.lattice_size() {
        return Err(ContextError::InvalidInput {
            what: "lattice size",
            detail: format!(
                "the lattice declares {:?} but the model counts imply {:?}",
                job.lattice.lattice_size,
                dimensions.lattice_size()
            ),
        });
    }

    if job.lattice.position_count != dimensions.p() {
        return Err(ContextError::InvalidInput {
            what: "lattice position count",
            detail: format!(
                "the lattice declares {} P1 positions but the model counts imply {}",
                job.lattice.position_count,
                dimensions.p()
            ),
        });
    }

    if job.model.counts.position_count != dimensions.p()
        || job.model.structure.positions.len() != positions
        || job.model.structure.environments.len() != positions
    {
        return Err(ContextError::InvalidInput {
            what: "position count",
            detail: format!(
                "the model counts imply {} positions, the structure has {} positions and {} environments",
                dimensions.p(),
                job.model.structure.positions.len(),
                job.model.structure.environments.len()
            ),
        });
    }

    if job.model.counts.particle_count != dimensions.pc()
        || job.model.particle.charges.len() != particles
        || job.model.particle.symbols.len() != particles
        || job.model.particle.is_vacancy.len() != particles
    {
        return Err(ContextError::InvalidInput {
            what: "particle table",
            detail: format!(
                "the model counts imply {} particles, the table has {} charges, {} symbols, and {} vacancy flags",
                dimensions.pc(),
                job.model.particle.charges.len(),
                job.model.particle.symbols.len(),
                job.model.particle.is_vacancy.len()
            ),
        });
    }

    for (env, particle) in job.lattice.particles.iter().enumerate() {
        if usize::from(particle.get()) >= particles {
            return Err(ContextError::InvalidInput {
                what: "lattice particle",
                detail: format!(
                    "environment {env} holds particle {} but the table has {particles} entries",
                    particle.get()
                ),
            });
        }
    }

    let transitions = &job.model.transitions;
    let key_count = positions * particles;

    if transitions.jump_count.len() < key_count {
        return Err(ContextError::InvalidInput {
            what: "jump count table",
            detail: format!(
                "the table has {} entries but P·pc is {key_count}",
                transitions.jump_count.len()
            ),
        });
    }

    if transitions.jump_offsets.len() != transitions.jump_count.len() + 1 {
        return Err(ContextError::InvalidInput {
            what: "jump offsets",
            detail: format!(
                "{} offsets for {} jump counts",
                transitions.jump_offsets.len(),
                transitions.jump_count.len()
            ),
        });
    }

    let mapping_len = transitions.jump_offsets.last().copied().unwrap_or_default();
    if transitions.jump_mapping.len() != mapping_len as usize {
        return Err(ContextError::InvalidInput {
            what: "jump mapping",
            detail: format!(
                "{} mapping entries for a prefix sum of {mapping_len}",
                transitions.jump_mapping.len()
            ),
        });
    }

    Ok(())
}

/// The caller's cancellation flag, polled at phase boundaries and every [`CANCEL_STRIDE`] environments.
pub(crate) struct CancelProbe<'a> {
    flag: Option<&'a AtomicBool>,
}

impl<'a> CancelProbe<'a> {
    pub(crate) const fn new(flag: Option<&'a AtomicBool>) -> Self {
        Self { flag }
    }

    pub(crate) fn check(&self) -> Result<(), ContextError> {
        match self.flag {
            Some(flag) if flag.load(Ordering::Relaxed) => Err(ContextError::Cancelled),
            _ => Ok(()),
        }
    }
}

#[cfg(test)]
mod tests {
    use std::path::Path;
    use std::sync::atomic::AtomicBool;

    use mocassin_msl::Package;
    use mocassin_types::{JobPackage, ReaderLimits};

    use super::{
        CANCEL_STRIDE, CancelProbe, ContextError, ContextOptions, InitOutcome, InitPhase,
        PhaseObserver, SimulationContext, initialize_with,
    };

    fn fixture_job(job_id: u32) -> JobPackage {
        let path = Path::new(env!("CARGO_MANIFEST_DIR"))
            .join("../../../../tests/contracts/packages/msl-v1/ceria-temperature-sweep.msl");
        Package::open_readonly(path, ReaderLimits::default())
            .expect("the msl-v1 fixture opens")
            .load_job(job_id)
            .expect("the fixture job maps")
    }

    struct Recorder(Vec<InitPhase>);

    impl PhaseObserver for Recorder {
        fn observe(&mut self, phase: InitPhase) {
            self.0.push(phase);
        }
    }

    #[test]
    fn the_phase_enum_is_the_documented_ascending_sequence() {
        assert_eq!(InitPhase::ALL.len(), 13);
        assert!(InitPhase::ALL.windows(2).all(|pair| pair[0] < pair[1]));
        assert_eq!(InitPhase::MemoryPlan as u8, 0);
        assert_eq!(InitPhase::InvariantSweep as u8, 12);
        assert_eq!(
            InitPhase::ALL[InitPhase::Lattice as usize],
            InitPhase::Lattice
        );
    }

    #[test]
    fn initialization_visits_its_phases_in_order() {
        let mut recorder = Recorder(Vec::new());
        let outcome = initialize_with(
            fixture_job(1),
            &ContextOptions::default(),
            None,
            &mut recorder,
        )
        .expect("the msl-v1 job initializes");

        assert!(matches!(outcome, InitOutcome::Ready(_)));
        assert_eq!(
            recorder.0,
            [
                InitPhase::MemoryPlan,
                InitPhase::ValidateInputs,
                InitPhase::Lattice,
                InitPhase::InvariantSweep,
            ]
        );
        assert!(recorder.0.windows(2).all(|pair| pair[0] < pair[1]));
    }

    #[test]
    fn a_preset_cancel_flag_stops_before_the_plan() {
        let flag = AtomicBool::new(true);
        let mut recorder = Recorder(Vec::new());
        let error = initialize_with(
            fixture_job(1),
            &ContextOptions::default(),
            Some(&flag),
            &mut recorder,
        )
        .expect_err("a set flag cancels initialization");

        assert!(matches!(error, ContextError::Cancelled));
        assert_eq!(error.exit_code().code(), 15);
        assert!(recorder.0.is_empty());
    }

    #[test]
    fn the_cancel_probe_handles_a_missing_and_a_set_flag() {
        assert!(CancelProbe::new(None).check().is_ok());

        let clear = AtomicBool::new(false);
        assert!(CancelProbe::new(Some(&clear)).check().is_ok());

        let set = AtomicBool::new(true);
        assert!(matches!(
            CancelProbe::new(Some(&set)).check(),
            Err(ContextError::Cancelled)
        ));
    }

    #[test]
    fn the_cancellation_stride_is_four_thousand_ninety_six_environments() {
        assert_eq!(CANCEL_STRIDE, 4096);
    }

    #[test]
    fn the_context_is_send() {
        fn assert_send<T: Send>() {}
        assert_send::<SimulationContext>();
    }
}

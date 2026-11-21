// SPDX-License-Identifier: MIT

//! The simulation context: deterministic, allocation-bounded runtime state built once per job.
//!
//! [`SimulationContext::initialize`] is a pure function of a validated [`JobPackage`], the [`ContextOptions`],
//! and an optional cancellation flag. It performs no I/O, opens no package, and writes nothing. Construction is
//! phased (Step 14 design §1/§5.1) and every phase is validated before the next; a failed phase returns one
//! [`ContextError`] naming the field and the dense index, and no partial context escapes.
//!
//! The context owns:
//!
//! - the [`Dimensions`]/[`EnvId`] contract and the checked index/wrap helpers;
//! - [`ContextLimits`], the phase-0 [`MemoryPlan`], and the [`MemoryReport`];
//! - the [`LatticeState`] particle/flag/mobile-tracker arenas;
//! - the owner-major [`LinkArena`] and its inverse index, the [`ClusterArena`] codes/energies, the
//!   [`EnergyRuntime`] site/vector tables and initial total, the [`SelectionPools`], and the [`JumpRuntime`];
//! - the [`Counters`], [`TrackerIndex`], [`Goals`], [`Normalization`], RNG stream, time, and resume origin;
//! - the phase pipeline, cancellation, the exit-code mapping, and the invariant sweep;
//! - the `Send`/`!Sync` guarantee: the context is movable across threads but is deliberately not shareable,
//!   because one trajectory is single-threaded.
//!
//! Step 14's features 1–5 implement the runtime; feature 6 adds fixtures, benchmarks, documentation, and
//! closeout without adding runtime fields.
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

use mocassin_rng::Rng;
use mocassin_types::{ClusterTable, JobPackage, SimulationKind};

mod clusters;
mod dimensions;
mod energy;
mod error;
mod goals;
mod jumps;
mod lattice;
mod limits;
mod links;
mod pools;
mod resume;

pub use clusters::{ClusterArena, ClusterInstance, MAX_CLUSTER_SLOTS, NO_CLUSTER_CODE};
pub use dimensions::{Dimensions, EnvId};
pub use energy::{EnergyRuntime, FIELD_SCALE_PER_VM, K_B_EV};
pub use error::ContextError;
pub use goals::{Counters, Goals, Normalization, NormalizationState, ParticleCounters};
pub use jumps::JumpRuntime;
pub use lattice::{
    HAS_MOBILE_TRACKER, LatticeState, MOBILE, NO_MOBILE_TRACKER, STABLE, TrackerIndex,
};
pub use limits::{ContextLimits, MemoryEntry, MemoryPlan, MemoryReport};
pub use links::{ClusterAssoc, IncomingLink, Link, LinkArena};
pub use mocassin_types::{ParticleId, PositionId};
pub use pools::{NO_SLOT, PoolMember, SelectionPool, SelectionPools};
pub use resume::{ResumePhase, ResumeState};

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
    /// Phase 10: tracker index, counters, goals, normalization, `nu_max`, and `dt`.
    GoalsAndCounters = 10,
    /// Phase 11: RNG from the effective seed and the fresh/restored time.
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

/// Whether a context was built fresh or restored from a resume document.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ContextOrigin {
    /// A fresh context from the job's initial lattice.
    Fresh,
    /// A context restored from a validated resume state.
    Resumed,
}

/// The options of one context initialization.
#[derive(Clone, Debug, PartialEq)]
pub struct ContextOptions {
    /// The overridable library limits and the memory-plan caps.
    pub limits: ContextLimits,
    /// Build the KMC jump status. Defaults to true; an MMC context never builds it.
    pub build_jump_status: bool,
    /// Apply the job's selection optimizers after pool construction. Defaults to true.
    pub apply_optimizers: bool,
    /// The optional resume state from Step 18/19; `None` builds a fresh context.
    pub resume: Option<ResumeState>,
}

impl Default for ContextOptions {
    fn default() -> Self {
        Self {
            limits: ContextLimits::default(),
            build_jump_status: true,
            apply_optimizers: true,
            resume: None,
        }
    }
}

/// The initialized runtime state of one job.
///
/// Every stored field is `Send + Sync`, but the context is deliberately **not `Sync`**: the `_not_sync`
/// marker makes the single-trajectory rule a compile-time guarantee instead of a comment. Moving the context to
/// a worker thread is fine; two threads cannot share one trajectory.
#[derive(Debug)]
pub struct SimulationContext {
    /// The validated job this context was built from; owns the shared `Arc<ParameterizedModel>`.
    ///
    /// Its `lattice` is the package's initial lattice; the live state after a resume is `self.lattice`.
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
    /// The energy factor, site table, and site+pair vectors.
    pub energy: EnergyRuntime,
    /// The exact initial lattice energy in electronvolts (§7.4).
    pub lattice_energy_ev: f64,
    /// The KMC selection pools (empty until feature 4).
    pub pools: SelectionPools,
    /// The KMC jump status (empty until feature 4).
    pub jumps: JumpRuntime,
    /// The per-particle counters (zeroed fresh, restored on resume).
    pub counters: Counters,
    /// The tracker index space and mobile-position mapping.
    pub trackers: TrackerIndex,
    /// The KMC goals and block size (zeroed for MMC).
    pub goals: Goals,
    /// The normalization factors, `nu_max`, `dof`, and `dt`.
    pub normalization: Normalization,
    /// The ChaCha12 stream (fresh seed or restored state).
    pub rng: Rng,
    /// The simulated time in seconds; 0 fresh and for MMC.
    pub time_s: f64,
    /// Fresh or resumed.
    pub origin: ContextOrigin,
    /// The opaque solver state flags (0 fresh).
    pub state_flags: u64,
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

    /// Sets the selectability of one `(environment, particle)` after a state change.
    ///
    /// Callers update the lattice and then call this for the departed and the arrived particle. The pool has
    /// reserved capacity for every selectable combination, so the call never reallocates.
    pub fn set_selectable(&mut self, env: EnvId, particle: ParticleId) {
        let current = self.lattice.particle(env);
        self.pools
            .set_selectable(&self.dimensions, env, particle, current);
    }

    /// Sets the particle of one environment.
    ///
    /// This updates only the particle array. Callers (Step 15's state application) recompute the environment
    /// flags, the tracker id, and the pool membership afterwards.
    pub fn set_particle(&mut self, env: EnvId, particle: ParticleId) {
        self.lattice.set_particle(env, particle);
    }

    /// Returns the mobile-position mapping (`mobile_positions[id] = environment`).
    #[must_use]
    pub fn mobile_positions(&self) -> &[u32] {
        &self.trackers.mobile_positions
    }

    /// Returns the static tracker mapping `[position][particle] -> offset` (`-1` = none).
    #[must_use]
    pub fn static_tracker_mapping(&self) -> &[i32] {
        &self.job.model.transitions.static_tracker_mapping
    }

    /// Returns the global tracker mapping `[collection][particle] -> index` (`-1` = none).
    #[must_use]
    pub fn global_tracker_mapping(&self) -> &[i32] {
        &self.job.model.transitions.global_tracker_mapping
    }

    /// Returns the static tracker id of a non-void environment particle.
    #[must_use]
    pub fn static_tracker_id(&self, env: EnvId, particle: ParticleId) -> Option<u32> {
        self.trackers
            .static_tracker_id(&self.dimensions, env, particle)
    }

    /// Returns the simulated time in seconds (0 fresh and for MMC).
    #[must_use]
    pub const fn time_s(&self) -> f64 {
        self.time_s
    }

    /// Returns the time step per jump attempt in seconds (0 for MMC).
    #[must_use]
    pub const fn dt(&self) -> f64 {
        self.normalization.dt
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
        self.lattice.capacity_bytes()
            + self.links.capacity_bytes()
            + self.clusters.capacity_bytes()
            + self.energy.site.capacity() as u64 * size_of::<f64>() as u64
            + self.energy.vectors.capacity() as u64 * size_of::<f64>() as u64
            + self.pools.capacity_bytes()
            + self.jumps.capacity_bytes()
            + self.trackers.capacity_bytes()
            + particles
            + 2 * size_of::<u64>() as u64
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

        self.check_planned_length("lattice.particles", self.lattice.particles().len())?;
        self.check_planned_length("lattice.flags", self.lattice.flags().len())?;
        self.check_planned_length(
            "lattice.mobile_tracker",
            self.lattice.mobile_tracker().len(),
        )?;
        self.check_planned_length("counters.per_particle", self.counters.per_particle.len())?;
        self.check_planned_length("links.owner_offsets", self.links.owner_offsets.len())?;
        self.check_planned_length("links.links", self.links.links.len())?;
        self.check_planned_length("links.inverse_offsets", self.links.inverse_offsets.len())?;
        self.check_planned_length("links.inverse", self.links.inverse.len())?;
        self.check_planned_length("links.cluster_assoc", self.links.cluster_assoc.len())?;
        self.check_planned_length("clusters.instances", self.clusters.instances.len())?;
        self.check_planned_length(
            "clusters.centre_offsets",
            self.clusters.centre_offsets.len(),
        )?;
        self.check_planned_length("clusters.centres", self.clusters.centres.len())?;
        self.check_planned_length("clusters.slot_start", self.clusters.slot_start.len())?;
        self.check_planned_length(
            "clusters.slot_positions",
            self.clusters.slot_positions.len(),
        )?;
        self.check_planned_length("clusters.slot_cells", self.clusters.slot_cells.len())?;
        self.check_planned_length("clusters.codes", self.clusters.codes.len())?;
        self.check_planned_length("clusters.code_ids", self.clusters.code_ids.len())?;
        self.check_planned_length("clusters.energy_start", self.clusters.energy_start.len())?;
        self.check_planned_length("clusters.energy", self.clusters.energy.len())?;
        self.check_planned_length("energy.site", self.energy.site.len())?;
        self.check_planned_length("energy.vectors", self.energy.vectors.len())?;
        self.check_planned_length("pools.headers", self.pools.pools.len())?;
        self.check_planned_length("pools.slot", self.pools.slot.len())?;
        self.check_planned_length("pools.pool_of", self.pools.pool_of.len())?;
        self.check_planned_length("jumps.offsets", self.jumps.offsets.len())?;
        self.check_planned_length("jumps.links", self.jumps.links.len())?;
        self.check_planned_length(
            "trackers.mobile_positions",
            self.trackers.mobile_positions.len(),
        )?;

        if self.job.kind == SimulationKind::Kmc {
            if self.trackers.mobile_positions.len() != self.lattice.mobile_count() as usize {
                return Err(ContextError::Invariant {
                    what: "mobile position count",
                });
            }
            for (index, environment) in self.trackers.mobile_positions.iter().enumerate() {
                let env = EnvId::new(*environment);
                if self.lattice.mobile_tracker_id(env) != Some(index as u32) {
                    return Err(ContextError::Invariant {
                        what: "mobile position mapping",
                    });
                }
            }
            let expected_goals = Goals::for_job(&self.job, self.lattice.mobile_count())
                .map_err(|_| ContextError::Invariant { what: "goals" })?;
            if expected_goals != self.goals {
                return Err(ContextError::Invariant { what: "goals" });
            }
            if !self.normalization.k_norm.is_finite()
                || self.normalization.k_norm <= 0.0
                || !self.normalization.nu_max.is_finite()
                || self.normalization.nu_max <= 0.0
                || !self.normalization.dt.is_finite()
                || self.normalization.dt <= 0.0
            {
                return Err(ContextError::Invariant {
                    what: "normalization",
                });
            }
        } else if !self.trackers.mobile_positions.is_empty()
            || self.goals != Goals::zeroed()
            || self.normalization.dt != 0.0
            || self.time_s != 0.0
        {
            return Err(ContextError::Invariant {
                what: "MMC runtime state",
            });
        }
        if !self.time_s.is_finite() || self.time_s < 0.0 {
            return Err(ContextError::Invariant {
                what: "simulated time",
            });
        }

        let planned_members = self
            .plan
            .planned_elements("pools.members")
            .unwrap_or_default();
        let pool_capacity: u64 = self
            .pools
            .pools
            .iter()
            .map(|pool| pool.members.capacity() as u64)
            .sum();
        if pool_capacity < planned_members {
            return Err(ContextError::Invariant {
                what: "pool member capacity",
            });
        }
        if !self.lattice_energy_ev.is_finite() {
            return Err(ContextError::Invariant {
                what: "lattice energy",
            });
        }

        let cells = self.dimensions.cell_count() as usize;
        let mut rows = 0_u32;

        for (index, instance) in self.clusters.instances.iter().enumerate() {
            let Some(table) = self
                .job
                .model
                .energy
                .cluster_tables
                .get(instance.table as usize)
            else {
                return Err(ContextError::Invariant {
                    what: "cluster table index",
                });
            };
            if self.clusters.energy_start[index] != rows {
                return Err(ContextError::Invariant {
                    what: "cluster energy rows",
                });
            }

            for cell in 0..cells {
                let id = self.clusters.code_ids[index * cells + cell];
                if id != NO_CLUSTER_CODE && id as usize >= table.occupation_codes.len() {
                    return Err(ContextError::Invariant {
                        what: "cluster code id",
                    });
                }
            }

            rows = rows
                .checked_add(table.particle_table_mapping.len() as u32)
                .ok_or(ContextError::Invariant {
                    what: "cluster energy rows",
                })?;
        }

        if self.clusters.energy_start[self.clusters.instances.len()] != rows
            || (self.clusters.energy.len() as u64)
                != u64::from(self.dimensions.cell_count()) * u64::from(rows)
        {
            return Err(ContextError::Invariant {
                what: "cluster energy rows",
            });
        }
        let mut owner = 0_usize;
        for (link_id, link) in self.links.links.iter().enumerate() {
            while owner + 1 < self.links.owner_offsets.len()
                && link_id >= self.links.owner_offsets[owner + 1] as usize
            {
                owner += 1;
            }

            let end = link.cluster_start as usize + usize::from(link.cluster_len);
            for assoc_index in link.cluster_start as usize..end {
                let assoc = self.links.cluster_assoc[assoc_index];
                let Some(instance) = self.clusters.instances.get(assoc.instance as usize) else {
                    return Err(ContextError::Invariant {
                        what: "cluster association instance",
                    });
                };
                if instance.centre_position as usize != owner
                    || assoc.slot == 0
                    || assoc.slot >= instance.slot_count
                {
                    return Err(ContextError::Invariant {
                        what: "cluster association slot",
                    });
                }

                let slot = instance.slot_start as usize + usize::from(assoc.slot);
                if self.clusters.slot_positions[slot] != link.partner_position
                    || self.clusters.slot_cells[slot] != link.offset
                {
                    return Err(ContextError::Invariant {
                        what: "cluster association geometry",
                    });
                }
            }
        }

        if !self.pools.has_valid_shape(&self.dimensions) {
            return Err(ContextError::Invariant {
                what: "selection pools",
            });
        }
        if !self.pools.is_consistent(&self.dimensions, &self.lattice) {
            return Err(ContextError::Invariant {
                what: "selection pool consistency",
            });
        }

        let statuses =
            u64::from(self.dimensions.p()) * u64::from(self.job.model.counts.direction_count);
        if !self.jumps.has_valid_shape(statuses) {
            return Err(ContextError::Invariant {
                what: "jump status CSR",
            });
        }
        self.check_jump_links(statuses)?;

        Ok(())
    }

    fn check_jump_links(&self, statuses: u64) -> Result<(), ContextError> {
        let positions = self.dimensions.p() as usize;
        for status in 0..statuses as usize {
            let direction_index = status / positions;
            let position = (status % positions) as u32;
            let direction = &self.job.model.transitions.directions[direction_index];
            let selected = self.dimensions.cell_env(0, position);
            let links = self.jumps.status_links(status);

            if !self.direction_is_active(direction_index as u32) {
                if !links.is_empty() {
                    return Err(ContextError::Invariant {
                        what: "inactive jump status is not empty",
                    });
                }
                continue;
            }

            for jump_link in links {
                if jump_link.sender_path_id == jump_link.receiver_path_id {
                    return Err(ContextError::Invariant {
                        what: "jump link sender equals receiver",
                    });
                }
                let Some(sender) = self.resolve_node(selected, direction, jump_link.sender_path_id)
                else {
                    return Err(ContextError::Invariant {
                        what: "jump link sender node",
                    });
                };
                let Some(receiver) =
                    self.resolve_node(selected, direction, jump_link.receiver_path_id)
                else {
                    return Err(ContextError::Invariant {
                        what: "jump link receiver node",
                    });
                };
                let sender_position = self.dimensions.position_of(sender) as usize;
                let receiver_position = self.dimensions.position_of(receiver) as usize;
                if !self.job.model.structure.positions[sender_position].stable
                    || !self.job.model.structure.positions[receiver_position].stable
                {
                    return Err(ContextError::Invariant {
                        what: "jump link structural filter",
                    });
                }
                let receiver_links = self.link_range(PositionId::new(receiver_position as u32));
                let Some(link) = receiver_links.get(jump_link.link_id as usize) else {
                    return Err(ContextError::Invariant {
                        what: "jump link receiver link",
                    });
                };
                if self.partner_of(receiver, link) != sender {
                    return Err(ContextError::Invariant {
                        what: "jump link partner",
                    });
                }
            }
        }

        Ok(())
    }

    fn check_planned_length(&self, what: &'static str, actual: usize) -> Result<(), ContextError> {
        if self.plan.planned_elements(what) == Some(actual as u64) {
            Ok(())
        } else {
            Err(ContextError::Invariant { what })
        }
    }

    /// Returns the forward links owned by one P1 position.
    #[must_use]
    pub fn link_range(&self, position: PositionId) -> &[Link] {
        self.links.owner_links(position)
    }

    /// Returns the inverse routing entries of an absolute environment's P1 position.
    #[must_use]
    pub fn incoming(&self, env: EnvId) -> &[IncomingLink] {
        self.links
            .incoming_links(PositionId::new(self.dimensions.position_of(env)))
    }

    /// Resolves the partner environment of `owner` through `link`.
    #[must_use]
    pub fn partner_of(&self, owner: EnvId, link: &Link) -> EnvId {
        self.links.partner_of(&self.dimensions, owner, link)
    }

    /// Resolves the owner environment of `partner` through an inverse entry.
    #[must_use]
    pub fn incoming_owner(&self, partner: EnvId, incoming: IncomingLink) -> Option<EnvId> {
        self.links.owner_of(&self.dimensions, partner, incoming)
    }

    /// Returns the owner's link and its resolved partner environment.
    #[must_use]
    pub fn owner_link(&self, owner: EnvId, link_id: u32) -> Option<(&Link, EnvId)> {
        let position = PositionId::new(self.dimensions.position_of(owner));
        let link = self.links.owner_link(position, link_id)?;
        Some((link, self.partner_of(owner, link)))
    }

    /// Returns `true` when the payload has a pair interaction between `a` and `b` in either direction.
    ///
    /// This is Step 17's local-update membership test. A missing reverse link means the payload has no
    /// interaction in that direction and is not an error; no reverse link is synthesised.
    #[must_use]
    pub fn linked(&self, a: EnvId, b: EnvId) -> bool {
        let partner_position = self.dimensions.position_of(b);
        for link in self.link_range(PositionId::new(self.dimensions.position_of(a))) {
            if link.partner_position == partner_position && self.partner_of(a, link) == b {
                return true;
            }
        }

        for incoming in self.incoming(a) {
            if incoming.owner_position == self.dimensions.position_of(b)
                && self.incoming_owner(a, *incoming) == Some(b)
            {
                return true;
            }
        }

        false
    }

    /// Returns the cluster instances centred on one P1 position.
    #[must_use]
    pub fn centres(&self, position: PositionId) -> &[u32] {
        self.clusters.centre_instances(position)
    }

    /// Resolves a raw cluster slot tuple through the instance's table expansion map.
    ///
    /// The slot-0 particle of `raw_code` must be in the table's `particle_table_mapping`; a particle outside
    /// the mapping has no cluster row (data contract §3.3) and is an internal contract violation here. Callers
    /// that may see an unmapped centre particle (for example void) use [`SimulationContext::cluster_row`] or
    /// [`SimulationContext::cluster_code_id`] instead.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::MissingOccupationCode`] for a tuple absent from a present map,
    /// [`ContextError::ExpansionMapAbsent`] for a non-canonical tuple in a pre-map package, and
    /// [`ContextError::Invariant`] for an unknown instance or an unmapped centre particle.
    pub fn cluster_lookup(&self, instance: u32, raw_code: u64) -> Result<u32, ContextError> {
        let instance_data = self
            .clusters
            .instance(instance)
            .ok_or(ContextError::Invariant {
                what: "cluster instance index",
            })?;
        let table = self.cluster_table(instance_data.table)?;
        if self
            .cluster_row(instance, ParticleId::new((raw_code & 0x3F) as u8))
            .is_none()
        {
            return Err(ContextError::Invariant {
                what: "cluster lookup with an unmapped centre particle",
            });
        }
        clusters::lookup_table(table, raw_code, instance_data.table)
    }

    /// Returns the row of a centre particle in the instance's table, or `None` when it has no row.
    ///
    /// A particle absent from `particle_table_mapping` has no cluster contribution (data contract §3.3).
    #[must_use]
    pub fn cluster_row(&self, instance: u32, particle: ParticleId) -> Option<u32> {
        let instance_data = self.clusters.instance(instance)?;
        let table = self.cluster_table(instance_data.table).ok()?;
        table
            .particle_table_mapping
            .iter()
            .position(|mapped| *mapped == particle.get())
            .map(|row| row as u32)
    }

    /// Returns the stored raw occupation code of one instance and cell.
    #[must_use]
    pub fn cluster_code(&self, instance: u32, cell: u32) -> u64 {
        self.clusters
            .code(instance, cell, self.dimensions.cell_count())
    }

    /// Returns the stored canonical code id, or `None` for an unmapped centre particle.
    #[must_use]
    pub fn cluster_code_id(&self, instance: u32, cell: u32) -> Option<u32> {
        self.clusters
            .code_id(instance, cell, self.dimensions.cell_count())
    }

    /// Returns the raw occupation code of one instance and centre cell with optional slot overrides.
    ///
    /// The first override for a slot wins. This is allocation-free and is Step 15's S2 code builder.
    ///
    /// # Panics
    ///
    /// Panics when `instance` is not a valid instance id (an internal caller contract, never package data).
    #[must_use]
    pub fn occupation_code(
        &self,
        instance: u32,
        centre_cell: u32,
        overrides: &[(u8, ParticleId)],
    ) -> u64 {
        self.clusters
            .occupation_code(
                &self.dimensions,
                self.lattice.particles(),
                instance,
                centre_cell,
                overrides,
            )
            .expect("the cluster instance exists")
    }

    /// Returns the site+pair energy of one absolute environment and particle in kT.
    #[must_use]
    pub fn node_energy(&self, env: EnvId, particle: ParticleId) -> f64 {
        self.energy.node_energy(&self.dimensions, env, particle)
    }

    /// Returns the site background of one P1 position and particle in kT.
    #[must_use]
    pub fn site_energy(&self, position: PositionId, particle: ParticleId) -> f64 {
        self.energy
            .site_energy(&self.dimensions, position, particle)
    }

    /// Returns the cluster energy of one instance, cell, and centre particle in kT.
    ///
    /// A particle outside the table's `particle_table_mapping` has no cluster row and contributes zero.
    #[must_use]
    pub fn cluster_energy(&self, instance: u32, cell: u32, particle: ParticleId) -> f64 {
        energy::cluster_energy_at(&self.job, &self.clusters, instance, cell, particle)
    }

    /// Returns the view factor `|E| · 1e-10 / kT_eV` used by Step 15's field term.
    #[must_use]
    pub const fn field_scale(&self) -> f64 {
        self.energy.field_scale
    }

    /// Returns the normalized electric-field direction.
    #[must_use]
    pub const fn field_direction(&self) -> [f64; 3] {
        self.energy.field_direction
    }

    /// Returns the Cartesian `a`, `b`, and `c` cell vectors in ångström.
    #[must_use]
    pub fn cell_vectors(&self) -> [[f64; 3]; 3] {
        self.job.model.structure.cell_vectors_ang
    }

    /// Returns the eV→kT factor of this job.
    #[must_use]
    pub const fn ev_to_kt(&self) -> f64 {
        self.energy.ev_to_kt
    }

    /// Returns the thermal energy `k_B · T` in electronvolts.
    #[must_use]
    pub const fn kt_ev(&self) -> f64 {
        self.energy.kt_ev
    }

    /// Recomputes the full lattice energy in kT by an independent direct pair sum.
    ///
    /// This is the debug/consistency path: it sums the site table once, `0.5 ·` the forward pair links, and the
    /// cluster term per instance and cell. It must agree with [`SimulationContext::lattice_energy_ev`] divided
    /// by `kT_eV`.
    #[must_use]
    pub fn recompute_lattice_energy_kt(&self) -> f64 {
        let particles = self.dimensions.pc() as usize;
        let mut total = 0.0_f64;

        for environment in 0..self.dimensions.environment_count() {
            let env = EnvId::new(environment);
            if !self.lattice.is_stable(env) {
                continue;
            }

            let position = self.dimensions.position_of(env);
            let particle = self.lattice.particle(env).get() as usize;
            total += self.energy.site[position as usize * particles + particle];

            let cell = self.dimensions.cell_of(env);
            for link in self.link_range(PositionId::new(position)) {
                let partner = self
                    .dimensions
                    .wrapped_env(cell, link.offset, link.partner_position);
                let partner_particle = self.lattice.particle(partner).get() as usize;
                let table = &self.job.model.energy.pair_tables[link.pair_id as usize];
                total += 0.5
                    * table.energies[particle * particles + partner_particle]
                    * self.energy.ev_to_kt;
            }
        }

        for (instance, instance_data) in self.clusters.instances.iter().enumerate() {
            for cell in 0..self.dimensions.cell_count() {
                let centre = self
                    .dimensions
                    .cell_env(cell, instance_data.centre_position);
                let particle = self.lattice.particle(centre);
                total += energy::cluster_energy_at(
                    &self.job,
                    &self.clusters,
                    instance as u32,
                    cell,
                    particle,
                );
            }
        }

        total
    }

    /// Asserts in debug builds that the stored total equals the independent recomputation.
    pub fn check_lattice_energy(&self) {
        #[cfg(debug_assertions)]
        {
            let expected = self.recompute_lattice_energy_kt() * self.energy.kt_ev;
            let tolerance = 1e-9 * (1.0 + self.lattice_energy_ev.abs());
            debug_assert!(
                (self.lattice_energy_ev - expected).abs() <= tolerance,
                "lattice energy {} != recomputed {expected}",
                self.lattice_energy_ev
            );
        }
    }

    fn cluster_table(&self, table: u32) -> Result<&ClusterTable, ContextError> {
        self.job
            .model
            .energy
            .cluster_tables
            .get(table as usize)
            .ok_or(ContextError::Invariant {
                what: "cluster table index",
            })
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
    let mut plan = MemoryPlan::plan(&job, &options.limits)?;
    tracing::info!(
        phase = InitPhase::MemoryPlan.as_str(),
        bytes = plan.total_bytes(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::ValidateInputs);
    let dimensions = Dimensions::from_job(&job)?;
    validate_inputs(&job, &dimensions)?;
    if let Some(resume) = &options.resume {
        resume::validate(&job, &dimensions, resume)?;
        if resume.phase == ResumePhase::Completed {
            let mobile_count = resume::mobile_count(&resume.particles);
            let goals = Goals::for_job(&job, mobile_count)?;
            tracing::info!(
                phase = InitPhase::ValidateInputs.as_str(),
                result = "already completed",
                "context phase completed"
            );
            return Ok(InitOutcome::AlreadyCompleted {
                mcs: resume.mcs,
                total_goal: goals.total,
            });
        }
    }
    let particles: &[ParticleId] = options
        .resume
        .as_ref()
        .map_or(job.lattice.particles.as_slice(), |resume| {
            resume.particles.as_slice()
        });
    tracing::info!(
        phase = InitPhase::ValidateInputs.as_str(),
        environments = dimensions.environment_count(),
        particles = dimensions.pc(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::Lattice);
    let lattice = LatticeState::build(&job, &dimensions, particles, &probe)?;
    tracing::info!(
        phase = InitPhase::Lattice.as_str(),
        mobile = lattice.mobile_count(),
        "context phase completed"
    );
    if job.kind == SimulationKind::Kmc && lattice.mobile_count() == 0 {
        return Err(ContextError::NoMobileParticles);
    }

    probe.check()?;
    observer.observe(InitPhase::Links);
    let mut links = LinkArena::build(&job, &dimensions, &options.limits, &probe)?;
    tracing::info!(
        phase = InitPhase::Links.as_str(),
        links = links.links.len(),
        inverse = links.inverse.len(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::Clusters);
    let mut clusters = ClusterArena::build(&job, &dimensions, &mut links, particles, &probe)?;
    tracing::info!(
        phase = InitPhase::Clusters.as_str(),
        instances = clusters.instances.len(),
        slots = clusters.slot_positions.len(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::SiteVectors);
    let energy = EnergyRuntime::build(&job, &dimensions, &lattice, &links, &probe)?;
    tracing::info!(
        phase = InitPhase::SiteVectors.as_str(),
        site = energy.site.len(),
        vectors = energy.vectors.len(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::ClusterVectors);
    energy::fill_cluster_energies(&job, &dimensions, &mut clusters, energy.ev_to_kt, &probe)?;
    tracing::info!(
        phase = InitPhase::ClusterVectors.as_str(),
        rows = clusters.energy.len(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::InitialEnergy);
    let lattice_energy_kt =
        energy::initial_lattice_energy_kt(&job, &dimensions, &lattice, &energy, &clusters, &probe)?;
    let lattice_energy_ev = lattice_energy_kt * energy.kt_ev;
    if !lattice_energy_ev.is_finite() {
        return Err(ContextError::NonFiniteFactor {
            what: "lattice energy",
        });
    }
    tracing::info!(
        phase = InitPhase::InitialEnergy.as_str(),
        energy_ev = lattice_energy_ev,
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::Pools);
    let pools = if job.kind == SimulationKind::Kmc {
        SelectionPools::build(
            &job,
            &dimensions,
            &lattice,
            options.apply_optimizers,
            &options.limits,
            &probe,
        )?
    } else {
        SelectionPools::empty()
    };
    tracing::info!(
        phase = InitPhase::Pools.as_str(),
        pools = pools.pools.len(),
        members = pools.member_count(),
        selectable = pools.selectable_jump_count,
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::JumpStatus);
    let jumps = if job.kind == SimulationKind::Kmc && options.build_jump_status {
        JumpRuntime::build(
            &job,
            &dimensions,
            &links,
            &mut plan,
            &options.limits,
            &probe,
        )?
    } else {
        JumpRuntime::empty()
    };
    tracing::info!(
        phase = InitPhase::JumpStatus.as_str(),
        statuses = jumps.status_count(),
        links = jumps.links.len(),
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::GoalsAndCounters);
    let trackers = TrackerIndex::build(
        &job,
        &dimensions,
        &lattice,
        &mut plan,
        &options.limits,
        &probe,
    )?;
    let goals = Goals::for_job(&job, lattice.mobile_count())?;
    let counters = match &options.resume {
        Some(resume) => Counters {
            per_particle: resume.counters.clone(),
            cycles: resume.cycles,
            mcs: resume.mcs,
        },
        None => Counters::zeroed(dimensions.pc()),
    };
    if let Some(stored) = options
        .resume
        .as_ref()
        .and_then(|resume| resume.mobile_positions.as_ref())
        && *stored != trackers.mobile_positions
    {
        return Err(ContextError::ResumeMismatch {
            what: "mobile mapping",
        });
    }
    let normalization = match (&options.resume, job.kind) {
        (Some(resume), SimulationKind::Kmc) => {
            Normalization::resumed(&job, pools.selectable_jump_count, &resume.normalization)?
        }
        (_, SimulationKind::Kmc) => {
            Normalization::fresh(&job, energy.kt_ev, pools.selectable_jump_count)?
        }
        (_, SimulationKind::Mmc) => {
            Normalization::fresh(&job, energy.kt_ev, pools.selectable_jump_count)?
        }
    };
    tracing::info!(
        phase = InitPhase::GoalsAndCounters.as_str(),
        mobile = lattice.mobile_count(),
        total = goals.total,
        dt = normalization.dt,
        "context phase completed"
    );

    probe.check()?;
    observer.observe(InitPhase::RngAndTime);
    let (rng, time_s, origin) = match &options.resume {
        Some(resume) => (
            Rng::from_state(&resume.rng_state).map_err(ContextError::RngState)?,
            if job.kind == SimulationKind::Mmc {
                0.0
            } else {
                resume.simulated_time_s
            },
            ContextOrigin::Resumed,
        ),
        None => (Rng::from_seed(job.seed), 0.0, ContextOrigin::Fresh),
    };
    let state_flags = options
        .resume
        .as_ref()
        .map_or(0, |resume| resume.state_flags);
    tracing::info!(
        phase = InitPhase::RngAndTime.as_str(),
        time_s = time_s,
        "context phase completed"
    );

    let kind = job.kind;
    let context = SimulationContext {
        job,
        kind,
        dimensions,
        lattice,
        links,
        clusters,
        energy,
        lattice_energy_ev,
        pools,
        jumps,
        counters,
        trackers,
        goals,
        normalization,
        rng,
        time_s,
        origin,
        state_flags,
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

    let Some(simulation) = job
        .model
        .simulations
        .get(job.settings.simulation_index as usize)
    else {
        return Err(ContextError::InvalidInput {
            what: "simulation index",
            detail: format!(
                "job declares simulation {} but the model has {}",
                job.settings.simulation_index,
                job.model.simulations.len()
            ),
        });
    };
    if simulation.kind != job.kind {
        return Err(ContextError::InvalidInput {
            what: "simulation kind",
            detail: "the selected simulation kind does not match the job".to_owned(),
        });
    }

    if job.model.counts.pair_table_count as usize != job.model.energy.pair_tables.len()
        || job.model.counts.cluster_table_count as usize != job.model.energy.cluster_tables.len()
    {
        return Err(ContextError::InvalidInput {
            what: "energy table count",
            detail: format!(
                "the model counts declare {} pair and {} cluster tables, the vectors hold {} and {}",
                job.model.counts.pair_table_count,
                job.model.counts.cluster_table_count,
                job.model.energy.pair_tables.len(),
                job.model.energy.cluster_tables.len()
            ),
        });
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
                InitPhase::Links,
                InitPhase::Clusters,
                InitPhase::SiteVectors,
                InitPhase::ClusterVectors,
                InitPhase::InitialEnergy,
                InitPhase::Pools,
                InitPhase::JumpStatus,
                InitPhase::GoalsAndCounters,
                InitPhase::RngAndTime,
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

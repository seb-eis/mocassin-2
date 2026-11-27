// SPDX-License-Identifier: MIT

//! The phased construction pipeline of the simulation context.
//!
//! [`initialize_with`] runs the Step 14 phases in their fixed order, polls the caller's cancellation flag at the
//! phase boundaries, and hands each built container to the next phase. [`validate_inputs`] checks every package
//! field the context indexes before the state is built; the invariant sweep that runs after construction lives
//! in [`super::invariants`].

use core::marker::PhantomData;
use core::sync::atomic::AtomicBool;

use mocassin_rng::Rng;
use mocassin_types::{JobPackage, SimulationKind};

use super::energy;
use super::invariants;
use super::resume;
use super::{
    CancelProbe, ClusterArena, ContextError, ContextOptions, ContextOrigin, Counters, Dimensions,
    EnergyRuntime, Goals, InitOutcome, InitPhase, JumpRuntime, LatticeState, LinkArena, MemoryPlan,
    Normalization, PhaseObserver, ResumePhase, SelectionPools, SimulationContext, TrackerIndex,
};

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
    let particles: &[mocassin_types::ParticleId] = options
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
    invariants::check(&context)?;
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

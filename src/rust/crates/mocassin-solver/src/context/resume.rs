// SPDX-License-Identifier: MIT

//! The plain resume-state contract.
//!
//! Step 14 performs no file I/O and does not depend on `mocassin-state`: Step 18/19 decode a `.mcs` document
//! into [`ResumeState`] and pass it through `ContextOptions::resume`. The state is validated against the
//! package/job (parameterized model signature, seed hash, job id, kind, dimensions, particle table) and the
//! mobile mapping is re-derived from the resumed lattice and compared when the document carries one.
//!
//! The `.mcs` stores one counter entry per particle (data contract §4.4), so `counters` is a per-particle vector;
//! the context's `cycles`/`mcs` accumulators are separate fields.

use mocassin_rng::{RngStateV1, seed_hash};
use mocassin_types::{JobPackage, ParticleId, SimulationKind};

use super::dimensions::Dimensions;
use super::error::ContextError;
use super::goals::{NormalizationState, ParticleCounters};

/// The phase a resumed state is in.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ResumePhase {
    /// The state is inside the KMC pre-run.
    Prerun,
    /// The state is inside the main run.
    Run,
    /// The state is complete; Step 14 returns the distinct already-completed outcome.
    Completed,
}

/// The plain validated resume input built from a `.mcs` document by Step 18/19.
#[derive(Clone, Debug, PartialEq)]
pub struct ResumeState {
    /// The phase at the point of writing.
    pub phase: ResumePhase,
    /// The `.mcs` `metadata.modelSignature` (the parameterized model signature).
    pub model_signature: String,
    /// The `.mcs` `metadata.seedHash` (SHA-256 of the job seed).
    pub seed_hash: String,
    /// The job id.
    pub job_id: u32,
    /// The job kind.
    pub kind: SimulationKind,
    /// The supercell size.
    pub lattice_size: [u32; 3],
    /// The number of canonical P1 positions.
    pub position_count: u32,
    /// The compiled particle count.
    pub particle_count: u32,
    /// The particle index of every environment, length `A·B·C·P`.
    pub particles: Vec<ParticleId>,
    /// The per-particle counters, length `pc`.
    pub counters: Vec<ParticleCounters>,
    /// The total attempt count.
    pub cycles: u64,
    /// The accepted state-advance count.
    pub mcs: u64,
    /// The simulated time in seconds.
    pub simulated_time_s: f64,
    /// The K_fix/K_auto/RawMax/K_norm state.
    pub normalization: NormalizationState,
    /// The exact 64-byte ChaCha12 stream state.
    pub rng_state: RngStateV1,
    /// The opaque solver state flags.
    pub state_flags: u64,
    /// The optional mobile mapping cross-check (`mobileMapping` in `.mcs`).
    pub mobile_positions: Option<Vec<u32>>,
}

/// Validates a resume state against the package, job, and dimensions.
///
/// # Errors
///
/// Returns [`ContextError::ResumeMismatch`] (exit 12) for every binding or value mismatch. A malformed RNG blob
/// cannot reach this function because [`RngStateV1`] validates at construction; restoring the stream can still
/// fail and is mapped to [`ContextError::RngState`] (exit 13) by the RNG phase.
pub(crate) fn validate(
    job: &JobPackage,
    dimensions: &Dimensions,
    resume: &ResumeState,
) -> Result<(), ContextError> {
    let mismatch = |what: &'static str| ContextError::ResumeMismatch { what };

    if resume.model_signature != job.model.signature {
        return Err(mismatch("model signature"));
    }
    if resume.seed_hash != seed_hash(&job.seed) {
        return Err(mismatch("seed hash"));
    }
    if resume.job_id != job.job_id {
        return Err(mismatch("job id"));
    }
    if resume.kind != job.kind {
        return Err(mismatch("kind"));
    }
    if resume.lattice_size != job.lattice.lattice_size {
        return Err(mismatch("lattice size"));
    }
    if resume.position_count != job.lattice.position_count {
        return Err(mismatch("position count"));
    }
    if resume.particle_count != dimensions.pc() {
        return Err(mismatch("particle count"));
    }
    if resume.particles.len() != dimensions.environment_count() as usize {
        return Err(mismatch("particle array length"));
    }
    if resume
        .particles
        .iter()
        .any(|particle| usize::from(particle.get()) >= dimensions.pc() as usize)
    {
        return Err(mismatch("particle index"));
    }
    if !resume.simulated_time_s.is_finite() || resume.simulated_time_s < 0.0 {
        return Err(mismatch("simulated time"));
    }
    if resume.counters.len() != dimensions.pc() as usize {
        return Err(mismatch("counter array length"));
    }
    for (what, value) in [
        ("K_fix", resume.normalization.k_fix),
        ("K_auto", resume.normalization.k_auto),
        ("K_norm", resume.normalization.k_norm),
    ] {
        if !value.is_finite() || value <= 0.0 {
            return Err(mismatch(what));
        }
    }
    if !resume.normalization.raw_max.is_finite() || resume.normalization.raw_max < 0.0 {
        return Err(mismatch("RawMax"));
    }

    if let Some(stored) = &resume.mobile_positions {
        let expected = expected_mobile_positions(dimensions, &resume.particles);
        if *stored != expected {
            return Err(mismatch("mobile mapping"));
        }
    }

    Ok(())
}

/// Returns the mobile mapping for a particle array: every non-void environment in ascending order.
#[must_use]
pub(crate) fn expected_mobile_positions(
    dimensions: &Dimensions,
    particles: &[ParticleId],
) -> Vec<u32> {
    let mut positions = Vec::new();
    for environment in 0..dimensions.environment_count() {
        if particles[environment as usize].get() != 0 {
            positions.push(environment);
        }
    }
    positions
}

/// Returns the number of non-void (mobile) environments.
#[must_use]
pub(crate) fn mobile_count(particles: &[ParticleId]) -> u32 {
    particles
        .iter()
        .filter(|particle| particle.get() != 0)
        .count() as u32
}

// SPDX-License-Identifier: MIT

//! Real-fixture builders for the Step 15 energy-engine acceptance tests.

use mocassin_types::{JobPackage, ParameterizedModel, ParticleId, StateCode};

use super::{initialize, populated_kmc, with_model};

/// Uses one supercell cell with the supplied per-P1 occupations and no electric field.
pub fn one_cell(mut job: JobPackage, particles: &[u8]) -> JobPackage {
    job.lattice.lattice_size = [1, 1, 1];
    job.lattice.position_count = job.model.structure.positions.len() as u32;
    job.lattice.particles = particles.iter().copied().map(ParticleId::new).collect();
    job.settings.electric_field_vm = 0.0;
    job
}

/// Clears every applied model energy while preserving the real fixture geometry.
pub fn clear_energies(model: &mut ParameterizedModel) {
    for table in &mut model.energy.pair_tables {
        table.energies.fill(0.0);
    }
    for table in &mut model.energy.cluster_tables {
        table.energies.fill(0.0);
    }
    model.energy.defect_background.fill(0.0);
}

/// Removes pair and cluster interactions, leaving the real three-site migration rule and site table.
pub fn site_only_job(particles: &[u8]) -> JobPackage {
    let job = one_cell(populated_kmc(), particles);
    with_model(job, |model| {
        clear_energies(model);
        for environment in &mut model.structure.environments {
            environment.pairs.clear();
            environment.clusters.clear();
        }
        model.energy.cluster_tables.clear();
        model.counts.cluster_table_count = 0;
    })
}

/// Removes cluster interactions but retains the real pair-link geometry and tables.
pub fn pair_only_job(particles: &[u8]) -> JobPackage {
    let job = one_cell(populated_kmc(), particles);
    with_model(job, |model| {
        clear_energies(model);
        for environment in &mut model.structure.environments {
            environment.pairs.clear();
            environment.clusters.clear();
        }
        model.energy.cluster_tables.clear();
        model.counts.cluster_table_count = 0;
    })
}

/// Retains the real cluster geometry and pair links while zeroing pair and site energies.
pub fn cluster_only_job(particles: &[u8]) -> JobPackage {
    let job = one_cell(populated_kmc(), particles);
    with_model(job, |model| {
        for table in &mut model.energy.pair_tables {
            table.energies.fill(0.0);
        }
        model.energy.defect_background.fill(0.0);
    })
}

/// Initializes a real job after the caller's in-process model mutation.
pub fn context(job: JobPackage) -> mocassin_solver::context::SimulationContext {
    initialize(job)
}

/// Packs path-node particles into the six-bit state-code contract.
#[must_use]
pub fn state_code(nodes: &[u8]) -> StateCode {
    let mut raw = 0_u64;
    for (node, particle) in nodes.iter().copied().enumerate() {
        raw |= u64::from(particle) << (6 * node as u32);
    }
    StateCode::new(raw)
}

/// Asserts equality in the solver's thermal-energy units.
pub fn assert_close(actual: f64, expected: f64) {
    let tolerance = 1e-9 * (1.0 + expected.abs());
    assert!(
        (actual - expected).abs() <= tolerance,
        "expected {expected}, got {actual}"
    );
}

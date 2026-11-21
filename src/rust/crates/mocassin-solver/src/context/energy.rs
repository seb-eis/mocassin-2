// SPDX-License-Identifier: MIT

//! The energy runtime: the eV→kT factor, the site table, the site+pair vectors, and the initial total.
//!
//! Model and parameter tables are authored in electronvolts; the solver stores thermal energy units (kT) and
//! converts back only at output boundaries (Step 14 design §7.1). The factor is computed once per job:
//!
//! - `kT_eV = k_B_eV · T` and `ev_to_kt = 1 / kT_eV`;
//! - `field_scale = |E| · 1e-10 / kT_eV` in the units Step 15 multiplies by the rule's `charge_transport_vector`;
//! - `field_direction` is the job's normalized field direction.
//!
//! The site table is a unit-cell table `[position][particle]` in kT. The per-environment vector is the site
//! value plus the owner's own forward pair links, with the owner's particle as the table row (Step 14 design
//! §7.2). The cluster term is **not** folded into the vector: it is a separate per-centre array counted once,
//! so the exact initial total is
//!
//! `U = Σ_stable site + 0.5 · Σ_stable (vector − site) + Σ_instances Σ_cells cluster(instance, cell, centre)`.
//!
//! The table copies are not materialised: the model keeps its eV tables and Step 15 scales a delta by
//! [`EnergyRuntime::ev_to_kt`]. Everything precomputed here (site, vectors, cluster energies, total) is kT.

use mocassin_types::{JobPackage, ParticleId, PositionId};

use super::CancelProbe;
use super::clusters::{ClusterArena, lookup_table};
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;
use super::links::LinkArena;

/// The Boltzmann constant in electronvolts per kelvin.
pub const K_B_EV: f64 = 8.617_333_262e-5;

/// The unit-chain factor from volts per metre to electronvolts per ångström: `1e-10 m/Å`.
pub const FIELD_SCALE_PER_VM: f64 = 1e-10;

/// Six bits per cluster occupation slot.
const PARTICLE_MASK: u64 = 0x3F;

/// The per-job energy runtime of one context.
#[derive(Clone, Debug, PartialEq)]
pub struct EnergyRuntime {
    /// The eV→kT factor `1 / (k_B_eV · T)`.
    pub ev_to_kt: f64,
    /// The thermal energy `k_B_eV · T` in electronvolts.
    pub kt_ev: f64,
    /// `|E| · 1e-10 / kT_eV`; Step 15 multiplies it by the rule's charge transport vector.
    pub field_scale: f64,
    /// The normalized electric-field direction from the job settings.
    pub field_direction: [f64; 3],
    /// The unit-cell `[position][particle]` site background in kT, length `P · pc`.
    pub site: Vec<f64>,
    /// The `[environment][particle]` site+pair vector in kT, length `A·B·C·P · pc`.
    pub vectors: Vec<f64>,
}

impl EnergyRuntime {
    /// Builds the factor, the site table, and the site+pair vectors.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::NonFiniteFactor`] (exit 13) for a non-positive/non-finite temperature, `kT`, or
    /// field factor, [`ContextError::InvalidInput`] (exit 12) for a table length mismatch or a non-finite used
    /// value, and [`ContextError::Cancelled`] when the caller's flag is set.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        lattice: &super::lattice::LatticeState,
        links: &LinkArena,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let positions = dimensions.p() as usize;
        let particles = dimensions.pc() as usize;
        let environments = dimensions.environment_count() as usize;

        let temperature = job.settings.temperature_k;
        if !temperature.is_finite() || temperature <= 0.0 {
            return Err(ContextError::NonFiniteFactor {
                what: "temperature",
            });
        }

        let kt_ev = K_B_EV * temperature;
        if !kt_ev.is_finite() || kt_ev <= 0.0 {
            return Err(ContextError::NonFiniteFactor { what: "kT" });
        }

        let ev_to_kt = 1.0 / kt_ev;
        if !ev_to_kt.is_finite() || ev_to_kt <= 0.0 {
            return Err(ContextError::NonFiniteFactor { what: "ev_to_kt" });
        }

        let field_modulus = job.settings.electric_field_vm;
        if !field_modulus.is_finite() || field_modulus < 0.0 {
            return Err(ContextError::NonFiniteFactor {
                what: "electric field",
            });
        }

        let field_scale = field_modulus * FIELD_SCALE_PER_VM / kt_ev;
        if !field_scale.is_finite() {
            return Err(ContextError::NonFiniteFactor {
                what: "field_scale",
            });
        }

        let field_direction = job.settings.electric_field_direction;
        if !field_direction
            .iter()
            .all(|component| component.is_finite())
        {
            return Err(ContextError::NonFiniteFactor {
                what: "field direction",
            });
        }

        let site_len = positions
            .checked_mul(particles)
            .ok_or(ContextError::InvalidInput {
                what: "site table",
                detail: "P·pc overflows".to_owned(),
            })?;
        if job.model.energy.defect_background.len() != site_len {
            return Err(ContextError::InvalidInput {
                what: "defect background",
                detail: format!(
                    "the table has {} entries but P·pc is {site_len}",
                    job.model.energy.defect_background.len()
                ),
            });
        }

        for (table, pair) in job.model.energy.pair_tables.iter().enumerate() {
            if pair.energies.len() != particles * particles {
                return Err(ContextError::InvalidInput {
                    what: "pair energy table",
                    detail: format!(
                        "table {table} has {} entries but pc² is {}",
                        pair.energies.len(),
                        particles * particles
                    ),
                });
            }
        }

        for (table, cluster) in job.model.energy.cluster_tables.iter().enumerate() {
            if cluster.energies.len() != cluster.occupation_codes.len() {
                return Err(ContextError::InvalidInput {
                    what: "cluster energy table",
                    detail: format!(
                        "table {table} has {} energies for {} occupation codes",
                        cluster.energies.len(),
                        cluster.occupation_codes.len()
                    ),
                });
            }
            if cluster
                .particle_table_mapping
                .iter()
                .any(|particle| usize::from(*particle) >= particles)
            {
                return Err(ContextError::InvalidInput {
                    what: "cluster particle mapping",
                    detail: format!("a table {table} row is outside the particle table"),
                });
            }
        }

        let mut site = Vec::with_capacity(site_len);
        for value in &job.model.energy.defect_background {
            let converted = value * ev_to_kt;
            if !converted.is_finite() {
                return Err(ContextError::InvalidInput {
                    what: "site energy",
                    detail: "a site value is not finite after conversion".to_owned(),
                });
            }
            site.push(converted);
        }

        let vector_len = environments
            .checked_mul(particles)
            .ok_or(ContextError::InvalidInput {
                what: "energy vectors",
                detail: "A·B·C·P·pc overflows".to_owned(),
            })?;
        let mut vectors = vec![0.0_f64; vector_len];

        for environment in 0..environments {
            if environment % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let env = EnvId::new(environment as u32);
            let position = dimensions.position_of(env);
            let cell = dimensions.cell_of(env);
            let owner_links = links.owner_links(PositionId::new(position));

            for particle in 0..particles {
                let mut value = site[position as usize * particles + particle];
                for link in owner_links {
                    let partner = dimensions.wrapped_env(cell, link.offset, link.partner_position);
                    let partner_particle = lattice.particle(partner).get() as usize;
                    let table = &job.model.energy.pair_tables[link.pair_id as usize];
                    value += table.energies[particle * particles + partner_particle] * ev_to_kt;
                }

                if !value.is_finite() {
                    return Err(ContextError::InvalidInput {
                        what: "energy vector",
                        detail: format!(
                            "environment {environment} particle {particle} is not finite"
                        ),
                    });
                }
                vectors[environment * particles + particle] = value;
            }
        }

        Ok(Self {
            ev_to_kt,
            kt_ev,
            field_scale,
            field_direction,
            site,
            vectors,
        })
    }

    /// Returns the site energy of one P1 position and particle in kT.
    #[must_use]
    pub fn site_energy(
        &self,
        dimensions: &Dimensions,
        position: PositionId,
        particle: ParticleId,
    ) -> f64 {
        let index = position.get() as usize * dimensions.pc() as usize + particle.get() as usize;
        self.site[index]
    }

    /// Returns the site+pair energy of one absolute environment and particle in kT.
    #[must_use]
    pub fn node_energy(&self, dimensions: &Dimensions, env: EnvId, particle: ParticleId) -> f64 {
        let index = env.index() * dimensions.pc() as usize + particle.get() as usize;
        self.vectors[index]
    }
}

/// Fills the per-instance cluster energy array in kT.
///
/// # Errors
///
/// Returns the cluster-code errors for a missing/non-canonical raw tuple, an invalid canonical index, a
/// non-finite energy, or cancellation.
pub(crate) fn fill_cluster_energies(
    job: &JobPackage,
    dimensions: &Dimensions,
    clusters: &mut ClusterArena,
    ev_to_kt: f64,
    cancel: &CancelProbe<'_>,
) -> Result<(), ContextError> {
    let cells = dimensions.cell_count();
    let total_rows = clusters.energy_start.last().copied().unwrap_or(0) as usize;
    let instances = clusters.instances.len();

    for instance in 0..instances {
        if instance % super::CANCEL_STRIDE == 0 {
            cancel.check()?;
        }

        let instance_data = clusters.instances[instance];
        let table = &job.model.energy.cluster_tables[instance_data.table as usize];
        let start = clusters.energy_start[instance] as usize;

        for cell in 0..cells {
            let code = clusters.code(instance as u32, cell, cells);
            for (row, particle) in table.particle_table_mapping.iter().enumerate() {
                let raw = (code & !PARTICLE_MASK) | u64::from(*particle);
                let id = lookup_table(table, raw, instance_data.table)?;
                let Some(energy) = table.energies.get(id as usize) else {
                    return Err(ContextError::InvalidInput {
                        what: "cluster energy",
                        detail: format!(
                            "table {} has no energy for canonical index {id}",
                            instance_data.table
                        ),
                    });
                };
                let value = energy * ev_to_kt;
                if !value.is_finite() {
                    return Err(ContextError::InvalidInput {
                        what: "cluster energy",
                        detail: format!("instance {instance} cell {cell} row {row} is not finite"),
                    });
                }
                clusters.energy[cell as usize * total_rows + start + row] = value;
            }
        }
    }

    Ok(())
}

/// Returns the cluster energy of one instance, cell, and centre particle in kT.
///
/// A particle outside the table's `particle_table_mapping` has no cluster row and contributes zero.
#[must_use]
pub(crate) fn cluster_energy_at(
    job: &JobPackage,
    clusters: &ClusterArena,
    instance: u32,
    cell: u32,
    particle: ParticleId,
) -> f64 {
    let Some(instance_data) = clusters.instance(instance) else {
        return 0.0;
    };
    let table = &job.model.energy.cluster_tables[instance_data.table as usize];
    let Some(row) = table
        .particle_table_mapping
        .iter()
        .position(|mapped| *mapped == particle.get())
    else {
        return 0.0;
    };

    let total_rows = clusters.energy_start.last().copied().unwrap_or(0) as usize;
    let index =
        cell as usize * total_rows + clusters.energy_start[instance as usize] as usize + row;
    clusters.energy[index]
}

/// Computes the exact initial lattice energy in kT (Step 14 design §7.4).
///
/// # Errors
///
/// Returns [`ContextError::NonFiniteFactor`] when the total is not finite and [`ContextError::Cancelled`]
/// when the caller's flag is set.
pub(crate) fn initial_lattice_energy_kt(
    job: &JobPackage,
    dimensions: &Dimensions,
    lattice: &super::lattice::LatticeState,
    energy: &EnergyRuntime,
    clusters: &ClusterArena,
    cancel: &CancelProbe<'_>,
) -> Result<f64, ContextError> {
    let particles = dimensions.pc() as usize;
    let mut total = 0.0_f64;

    for environment in 0..dimensions.environment_count() as usize {
        if environment % super::CANCEL_STRIDE == 0 {
            cancel.check()?;
        }

        let env = EnvId::new(environment as u32);
        if !lattice.is_stable(env) {
            continue;
        }

        let position = dimensions.position_of(env) as usize;
        let particle = lattice.particle(env).get() as usize;
        let site = energy.site[position * particles + particle];
        let vector = energy.vectors[environment * particles + particle];
        total += site + 0.5 * (vector - site);
    }

    for (instance, instance_data) in clusters.instances.iter().enumerate() {
        for cell in 0..dimensions.cell_count() {
            let centre = dimensions.cell_env(cell, instance_data.centre_position);
            let particle = lattice.particle(centre);
            total += cluster_energy_at(job, clusters, instance as u32, cell, particle);
        }
    }

    if !total.is_finite() {
        return Err(ContextError::NonFiniteFactor {
            what: "lattice energy",
        });
    }

    Ok(total)
}

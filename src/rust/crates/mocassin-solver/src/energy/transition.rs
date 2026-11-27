// SPDX-License-Identifier: MIT

//! Read-only KMC transition-energy evaluation.

use mocassin_types::{JumpDirectionId, JumpRule, ParticleId, PositionId, StateCode};

use crate::context::{ContextError, EnvId, SimulationContext};

const MAX_PATH_NODES: usize = 10;

/// The transition-energy view over one simulation context.
///
/// The engine owns no allocation or scratch arena. Callers construct it per operation with
/// `EnergyEngine::new(&mut *context)` so the context has exactly one mutable borrower.
#[derive(Debug)]
pub struct EnergyEngine<'a> {
    ctx: &'a mut SimulationContext,
}

impl<'a> EnergyEngine<'a> {
    /// Borrows the context mutably for evaluation and, in later features, commit operations.
    #[must_use]
    pub const fn new(ctx: &'a mut SimulationContext) -> Self {
        Self { ctx }
    }

    /// Evaluates one KMC rule application without mutating the context.
    ///
    /// All returned energies and barriers are in kT. The caller resolves the rule from the current start
    /// occupation; this method evaluates the supplied state codes and does not draw, accept, or apply a state.
    ///
    /// # Errors
    ///
    /// Returns exit 12 for an invalid direction/rule pairing or a present v1 `static_correction`, the Step 14
    /// cluster-code errors when they are reached, and exit 13 for a non-finite field term or computed energy.
    pub fn transition_energies(
        &self,
        selected: EnvId,
        direction: JumpDirectionId,
        rule: &JumpRule,
    ) -> Result<TransitionEnergies, ContextError> {
        if rule.static_correction.is_some() {
            return Err(ContextError::InvalidInput {
                what: "static S2 correction",
                detail: "static S2 correction is not defined for this package version; rebuild the package"
                    .to_owned(),
            });
        }
        if selected.index() >= self.ctx.dimensions.environment_count() as usize {
            return Err(ContextError::InvalidInput {
                what: "selected environment",
                detail: format!("environment {} is outside the supercell", selected.index()),
            });
        }

        let direction_data = self
            .ctx
            .job
            .model
            .transitions
            .directions
            .get(direction.get() as usize)
            .ok_or_else(|| ContextError::InvalidInput {
                what: "jump direction",
                detail: format!("direction {} is outside the model", direction.get()),
            })?;
        if direction_data.collection_id != rule.collection_id {
            return Err(ContextError::InvalidInput {
                what: "transition collection",
                detail: "the direction and rule belong to different collections".to_owned(),
            });
        }

        let length = direction_data.path.len();
        if length == 0 || length > MAX_PATH_NODES {
            return Err(ContextError::InvalidInput {
                what: "jump path",
                detail: format!("the path has {length} nodes; the supported range is 1..=10"),
            });
        }

        let mut path = [EnvId::new(0); MAX_PATH_NODES];
        if self.ctx.resolve_path(selected, direction_data, &mut path) != length {
            return Err(ContextError::InvalidInput {
                what: "jump path",
                detail: "the path could not be resolved at the selected environment".to_owned(),
            });
        }

        let mut s0_particles = [0_u8; MAX_PATH_NODES];
        let mut s1_particles = [0_u8; MAX_PATH_NODES];
        let mut s2_particles = [0_u8; MAX_PATH_NODES];
        decode_state(
            rule.state0,
            length,
            self.ctx.dimensions.pc(),
            &mut s0_particles,
        )?;
        decode_state(
            rule.state1,
            length,
            self.ctx.dimensions.pc(),
            &mut s1_particles,
        )?;
        decode_state(
            rule.state2,
            length,
            self.ctx.dimensions.pc(),
            &mut s2_particles,
        )?;

        let mut s0 = 0.0_f64;
        let mut s0_clusters = 0.0_f64;
        for node in 0..length {
            let particle = ParticleId::new(s0_particles[node]);
            s0 += self.ctx.node_energy(path[node], particle);
            s0_clusters += self.cluster_s0(path[node], particle);
        }
        s0 += s0_clusters;

        let mut raw_s1 = 0.0_f64;
        for node in 1..length {
            raw_s1 += self
                .ctx
                .node_energy(path[node], ParticleId::new(s1_particles[node]));
        }

        let mut s2 = 0.0_f64;
        let mut s2_clusters = 0.0_f64;
        for node in 0..length {
            let receiver = path[node];
            let position = PositionId::new(self.ctx.dimensions.position_of(receiver));
            let mut value = self
                .ctx
                .node_energy(receiver, ParticleId::new(s2_particles[node]));
            for link in self.ctx.link_range(position) {
                let partner = self.ctx.partner_of(receiver, link);
                let Some(partner_node) = path[..length]
                    .iter()
                    .position(|candidate| *candidate == partner)
                else {
                    continue;
                };
                if s0_particles[partner_node] == s2_particles[partner_node] {
                    continue;
                }
                let table = self
                    .ctx
                    .job
                    .model
                    .energy
                    .pair_tables
                    .get(link.pair_id as usize)
                    .ok_or(ContextError::Invariant {
                        what: "pair table index",
                    })?;
                let particles = self.ctx.dimensions.pc() as usize;
                let row = usize::from(s2_particles[node]);
                let old = usize::from(s0_particles[partner_node]);
                let new = usize::from(s2_particles[partner_node]);
                value += (table.energies[row * particles + new]
                    - table.energies[row * particles + old])
                    * self.ctx.ev_to_kt();
            }
            s2_clusters += self.cluster_s2(path[node], &path[..length], &s2_particles[..length])?;
            s2 += value;
        }
        s2 += s2_clusters;

        let field_term = self.field_term(rule)?;
        let conf = s2 - s0;
        let s1 = s0 + raw_s1 + 0.5 * conf;
        let barrier_forward = (s1 - s0) + field_term;
        let barrier_backward = (s1 - s2) - field_term;

        for (what, value) in [
            ("s0", s0),
            ("raw s1", raw_s1),
            ("s2", s2),
            ("s1", s1),
            ("barrier_forward", barrier_forward),
            ("barrier_backward", barrier_backward),
        ] {
            if !value.is_finite() {
                return Err(ContextError::Invariant { what });
            }
        }

        Ok(TransitionEnergies {
            s0,
            s1,
            s2,
            barrier_forward,
            barrier_backward,
            field_term,
        })
    }

    fn cluster_s0(&self, centre: EnvId, particle: ParticleId) -> f64 {
        let position = PositionId::new(self.ctx.dimensions.position_of(centre));
        let cell = self.ctx.dimensions.cell_of(centre);
        let mut total = 0.0_f64;
        for &instance in self.ctx.centres(position) {
            total += self.ctx.cluster_energy(instance, cell, particle);
        }
        total
    }

    fn cluster_s2(
        &self,
        centre: EnvId,
        path: &[EnvId],
        s2_particles: &[u8],
    ) -> Result<f64, ContextError> {
        let position = PositionId::new(self.ctx.dimensions.position_of(centre));
        let cell = self.ctx.dimensions.cell_of(centre);
        let mut total = 0.0_f64;

        for &instance in self.ctx.centres(position) {
            let instance_data =
                self.ctx
                    .clusters
                    .instance(instance)
                    .ok_or(ContextError::Invariant {
                        what: "cluster instance index",
                    })?;
            let (start, _) =
                self.ctx
                    .clusters
                    .slot_range(instance)
                    .ok_or(ContextError::Invariant {
                        what: "cluster slot range",
                    })?;
            let mut overrides = [(0_u8, ParticleId::new(0)); MAX_PATH_NODES];
            let mut override_count = 0_usize;

            for slot in 0..instance_data.slot_count as usize {
                let member = if slot == 0 {
                    centre
                } else {
                    self.ctx.dimensions.wrapped_env(
                        cell,
                        self.ctx.clusters.slot_cells[start + slot],
                        self.ctx.clusters.slot_positions[start + slot],
                    )
                };
                if let Some(path_node) = path.iter().position(|candidate| *candidate == member) {
                    overrides[override_count] =
                        (slot as u8, ParticleId::new(s2_particles[path_node]));
                    override_count += 1;
                }
            }

            let raw = self
                .ctx
                .occupation_code(instance, cell, &overrides[..override_count]);
            let slot_count = instance_data.slot_count;
            if (0..slot_count).any(|slot| (raw >> (u32::from(slot) * 6)) & 0x3F == 0) {
                continue;
            }
            let centre_particle = ParticleId::new((raw & 0x3F) as u8);
            if self.ctx.cluster_row(instance, centre_particle).is_none() {
                continue;
            }

            let canonical = self.ctx.cluster_lookup(instance, raw)?;
            let table = self
                .ctx
                .job
                .model
                .energy
                .cluster_tables
                .get(instance_data.table as usize)
                .ok_or(ContextError::Invariant {
                    what: "cluster table index",
                })?;
            let energy = table
                .energies
                .get(canonical as usize)
                .ok_or(ContextError::Invariant {
                    what: "cluster energy index",
                })?
                * self.ctx.ev_to_kt();
            if !energy.is_finite() {
                return Err(ContextError::Invariant {
                    what: "cluster energy",
                });
            }
            total += energy;
        }

        Ok(total)
    }

    fn field_term(&self, rule: &JumpRule) -> Result<f64, ContextError> {
        let scale = self.ctx.field_scale();
        if scale == 0.0 {
            return Ok(0.0);
        }
        let direction = self.ctx.field_direction();
        let transport = rule.charge_transport_vector;
        let term = -0.5
            * scale
            * (transport[0] * direction[0]
                + transport[1] * direction[1]
                + transport[2] * direction[2]);
        if !term.is_finite() {
            return Err(ContextError::NonFiniteFactor { what: "field term" });
        }
        Ok(term)
    }
}

/// The KMC transition energies of one resolved rule application, in kT.
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct TransitionEnergies {
    /// The current-state path energy, including separate cluster terms at path centres.
    pub s0: f64,
    /// The full midpoint state energy `s0 + raw_s1 + 0.5 * (s2 - s0)`.
    pub s1: f64,
    /// The final-state path energy after every path node's owner-major corrections.
    pub s2: f64,
    /// `b01 = (s1 - s0) + field_term`.
    pub barrier_forward: f64,
    /// `b10 = (s1 - s2) - field_term`.
    pub barrier_backward: f64,
    /// `-0.5 * field_scale * (charge_transport_vector · field_direction)`.
    pub field_term: f64,
}

impl TransitionEnergies {
    /// Returns the no-field forward barrier used by start-unstable classification.
    #[must_use]
    pub const fn barrier_forward_no_field(self) -> f64 {
        self.s1 - self.s0
    }

    /// Returns the no-field backward barrier used by end-unstable classification.
    #[must_use]
    pub const fn barrier_backward_no_field(self) -> f64 {
        self.s1 - self.s2
    }
}

fn decode_state(
    state: StateCode,
    length: usize,
    particle_count: u32,
    output: &mut [u8; MAX_PATH_NODES],
) -> Result<(), ContextError> {
    if !state.has_clear_bits_above(length) {
        return Err(ContextError::InvalidInput {
            what: "transition state code",
            detail: "the state carries occupation bits above the path length".to_owned(),
        });
    }
    for (node, slot) in output.iter_mut().enumerate().take(length) {
        let particle = state.node(node);
        if u32::from(particle) >= particle_count {
            return Err(ContextError::InvalidInput {
                what: "transition state particle",
                detail: format!(
                    "node {node} names particle {particle}, outside the particle table"
                ),
            });
        }
        *slot = particle;
    }
    Ok(())
}

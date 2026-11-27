// SPDX-License-Identifier: MIT

//! Shared KMC rule-path resolution and state-code decoding.
//!
//! The read-only evaluator and the committing state application resolve the same `(selected, direction, rule)`
//! tuple through this module, so the two entry points cannot disagree about the wrapping rule, the length limit,
//! or the six-bit state-code validation. The context's `resolve_path` remains the single implementation of the
//! wrapping itself (Step 14 §9.1).

use mocassin_types::{JumpDirectionId, JumpRule, StateCode};

use crate::context::{ContextError, EnvId, SimulationContext};

/// The fixed maximum path length (also the state-code bound).
pub(crate) const MAX_PATH_NODES: usize = 10;

/// One resolved rule application: the absolute path nodes and the three decoded state codes.
pub(crate) struct RulePath {
    nodes: [EnvId; MAX_PATH_NODES],
    state0: [u8; MAX_PATH_NODES],
    state1: [u8; MAX_PATH_NODES],
    state2: [u8; MAX_PATH_NODES],
    length: usize,
}

impl RulePath {
    /// Returns the resolved path nodes.
    pub(crate) fn nodes(&self) -> &[EnvId] {
        &self.nodes[..self.length]
    }

    /// Returns the decoded `S0` particles in path order.
    pub(crate) fn state0(&self) -> &[u8] {
        &self.state0[..self.length]
    }

    /// Returns the decoded `S1` particles in path order.
    pub(crate) fn state1(&self) -> &[u8] {
        &self.state1[..self.length]
    }

    /// Returns the decoded `S2` particles in path order.
    pub(crate) fn state2(&self) -> &[u8] {
        &self.state2[..self.length]
    }

    /// Returns the number of path nodes.
    pub(crate) fn len(&self) -> usize {
        self.length
    }
}

/// Resolves one rule application at the selected environment and decodes all three state codes.
///
/// # Errors
///
/// Returns exit 12 for a present v1 `static_correction`, an out-of-supercell selection, a direction/rule
/// collection mismatch, an unsupported path length, an unresolvable path, or an invalid state code.
pub(crate) fn resolve_rule_path(
    ctx: &SimulationContext,
    selected: EnvId,
    direction: JumpDirectionId,
    rule: &JumpRule,
) -> Result<RulePath, ContextError> {
    if rule.static_correction.is_some() {
        return Err(ContextError::InvalidInput {
            what: "static S2 correction",
            detail:
                "static S2 correction is not defined for this package version; rebuild the package"
                    .to_owned(),
        });
    }
    if selected.index() >= ctx.dimensions.environment_count() as usize {
        return Err(ContextError::InvalidInput {
            what: "selected environment",
            detail: format!("environment {} is outside the supercell", selected.index()),
        });
    }

    let direction_data = ctx
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

    let mut nodes = [EnvId::new(0); MAX_PATH_NODES];
    if ctx.resolve_path(selected, direction_data, &mut nodes) != length {
        return Err(ContextError::InvalidInput {
            what: "jump path",
            detail: "the path could not be resolved at the selected environment".to_owned(),
        });
    }

    let mut state0 = [0_u8; MAX_PATH_NODES];
    let mut state1 = [0_u8; MAX_PATH_NODES];
    let mut state2 = [0_u8; MAX_PATH_NODES];
    let particle_count = ctx.dimensions.pc();
    decode_state(rule.state0, length, particle_count, &mut state0)?;
    decode_state(rule.state1, length, particle_count, &mut state1)?;
    decode_state(rule.state2, length, particle_count, &mut state2)?;

    Ok(RulePath {
        nodes,
        state0,
        state1,
        state2,
        length,
    })
}

/// Decodes one six-bit-per-node state code with bounds checks.
fn decode_state(
    state: StateCode,
    length: usize,
    particle_count: u32,
    output: &mut [u8; MAX_PATH_NODES],
) -> Result<(), ContextError> {
    if !state.has_clear_bits_above(length) {
        return Err(ContextError::InvalidInput {
            what: "transition state code",
            detail: "the state carries bits above the path length".to_owned(),
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

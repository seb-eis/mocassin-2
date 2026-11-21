// SPDX-License-Identifier: MIT

//! The KMC jump-status CSR.
//!
//! One status is one `(P1 position, direction)` pair, indexed `status = direction · P + position`. The status is
//! translation-compact: the sender/receiver geometry is a property of the path shape and is resolved to absolute
//! environments at use. Each link names the sender and receiver path nodes explicitly and indexes the
//! **receiver's** owner-major forward link list; the receiver's cluster associations travel with that link, so
//! the energy engine never reverse-searches the path (Step 14 design §9).
//!
//! Only the directions of the job simulation's active collections are built; the `.msl` direction tables are a
//! union across every collection (see [`super::pools::active_direction_mask`]). Inactive statuses remain empty,
//! but the CSR keeps the global `direction · P + position` index space so Step 16 can address them by the
//! selected global direction id.

use mocassin_types::{
    JobPackage, JumpCollectionId, JumpDirection, JumpDirectionId, JumpLink, JumpRule, ParticleId,
    PositionId, RuleId,
};

use super::CancelProbe;
use super::dimensions::{Dimensions, EnvId};
use super::error::ContextError;
use super::limits::{ContextLimits, MemoryEntry, MemoryPlan};
use super::links::LinkArena;

/// The jump-status offsets and links grouped by ascending status index.
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct JumpRuntime {
    /// Status offsets, length `S + 1`, with `S = P · D`.
    pub offsets: Vec<u32>,
    /// The receiver-major jump links grouped by status.
    pub links: Vec<JumpLink>,
}

impl JumpRuntime {
    /// Returns the empty jump status of a context that does not build it (MMC or a disabled option).
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no offset or link is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.offsets.is_empty() && self.links.is_empty()
    }

    /// Returns the status count `S = P · D` of a built table.
    #[must_use]
    pub fn status_count(&self) -> usize {
        self.offsets.len().saturating_sub(1)
    }

    /// Returns the links of one status; an out-of-range status is empty.
    #[must_use]
    pub fn status_links(&self, status: usize) -> &[JumpLink] {
        if status + 1 >= self.offsets.len() {
            return &[];
        }
        let start = self.offsets[status] as usize;
        let end = self.offsets[status + 1] as usize;
        &self.links[start..end]
    }

    /// Returns the links of `(position, direction)`.
    #[must_use]
    pub fn links_at(&self, position: PositionId, direction: u32, positions: u32) -> &[JumpLink] {
        if positions == 0 {
            return &[];
        }
        let status = direction as usize * positions as usize + position.get() as usize;
        self.status_links(status)
    }

    /// Returns the summed allocator capacity of the arena arrays.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        self.offsets.capacity() as u64 * 4
            + self.links.capacity() as u64 * core::mem::size_of::<JumpLink>() as u64
    }

    /// Builds the receiver-major jump status for the job's active directions.
    ///
    /// The counting pass runs before the `links` vector is allocated; the caps and the plan are checked first.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::LimitExceeded`] for `max_status_count`, `max_jump_links`, or
    /// `max_jump_status_bytes`, [`ContextError::InvalidInput`] for an invalid path, and cancellation.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        links: &LinkArena,
        plan: &mut MemoryPlan,
        limits: &ContextLimits,
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let active = super::pools::active_direction_mask(job)?;
        let positions = dimensions.p() as usize;
        let directions = job.model.transitions.directions.len();
        if directions != job.model.counts.direction_count as usize {
            return Err(ContextError::InvalidInput {
                what: "direction count",
                detail: "the model count and the direction vector disagree".to_owned(),
            });
        }
        let statuses = positions
            .checked_mul(directions)
            .ok_or(ContextError::LimitExceeded {
                what: "max_status_count",
                value: u64::MAX,
                limit: limits.max_status_count,
            })?;

        if statuses as u64 > limits.max_status_count {
            return Err(ContextError::LimitExceeded {
                what: "max_status_count",
                value: statuses as u64,
                limit: limits.max_status_count,
            });
        }

        for direction in &job.model.transitions.directions {
            if direction.path.is_empty()
                || direction.path.len() > usize::from(limits.max_path_nodes)
            {
                return Err(ContextError::InvalidInput {
                    what: "jump path",
                    detail: format!(
                        "direction {} has {} path nodes; the limit is {}",
                        direction.object_id,
                        direction.path.len(),
                        limits.max_path_nodes
                    ),
                });
            }
            if direction
                .path
                .iter()
                .any(|node| node[3] < 0 || node[3] as usize >= positions)
            {
                return Err(ContextError::InvalidInput {
                    what: "jump path position",
                    detail: format!(
                        "direction {} has a node position outside the unit cell",
                        direction.object_id
                    ),
                });
            }
        }

        let mut total_links = 0_u64;
        for (direction_index, direction) in job.model.transitions.directions.iter().enumerate() {
            if !active[direction_index] {
                continue;
            }
            for position in 0..positions {
                if (direction_index * positions + position).is_multiple_of(super::CANCEL_STRIDE) {
                    cancel.check()?;
                }
                let count = count_status_links(dimensions, links, job, direction, position as u32);
                let bound = direction.path.len() * direction.path.len().saturating_sub(1);
                if count > bound {
                    return Err(ContextError::Invariant {
                        what: "jump status bound",
                    });
                }
                total_links += count as u64;
            }
        }

        if total_links > limits.max_jump_links {
            return Err(ContextError::LimitExceeded {
                what: "max_jump_links",
                value: total_links,
                limit: limits.max_jump_links,
            });
        }

        let status_bytes = (statuses as u64 + 1)
            .checked_mul(4)
            .and_then(|bytes| {
                total_links
                    .checked_mul(core::mem::size_of::<JumpLink>() as u64)
                    .and_then(|links| bytes.checked_add(links))
            })
            .ok_or(ContextError::LimitExceeded {
                what: "max_jump_status_bytes",
                value: u64::MAX,
                limit: limits.max_jump_status_bytes,
            })?;
        if status_bytes > limits.max_jump_status_bytes {
            return Err(ContextError::LimitExceeded {
                what: "max_jump_status_bytes",
                value: status_bytes,
                limit: limits.max_jump_status_bytes,
            });
        }

        plan.set_entries(
            [
                MemoryEntry {
                    what: "jumps.offsets",
                    elements: statuses as u64 + 1,
                    bytes: (statuses as u64 + 1) * 4,
                },
                MemoryEntry {
                    what: "jumps.links",
                    elements: total_links,
                    bytes: total_links * core::mem::size_of::<JumpLink>() as u64,
                },
            ],
            limits,
        )?;

        let mut offsets = vec![0_u32; statuses + 1];
        for status in 0..statuses {
            let direction_index = status / positions;
            let position = (status % positions) as u32;
            let count = if active[direction_index] {
                count_status_links(
                    dimensions,
                    links,
                    job,
                    &job.model.transitions.directions[direction_index],
                    position,
                )
            } else {
                0
            };
            offsets[status + 1] =
                offsets[status]
                    .checked_add(count as u32)
                    .ok_or(ContextError::Invariant {
                        what: "jump status offset",
                    })?;
        }

        let mut runtime = Self {
            offsets,
            links: Vec::with_capacity(total_links as usize),
        };
        for status in 0..statuses {
            if status % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }
            let direction_index = status / positions;
            if !active[direction_index] {
                continue;
            }
            let position = (status % positions) as u32;
            fill_status_links(
                dimensions,
                links,
                job,
                &job.model.transitions.directions[direction_index],
                position,
                &mut runtime.links,
            );
        }

        Ok(runtime)
    }

    /// Returns `true` when the CSR has a consistent shape for `statuses` statuses.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, statuses: u64) -> bool {
        if self.offsets.is_empty() {
            return self.links.is_empty();
        }

        self.offsets.len() as u64 == statuses + 1
            && self.offsets.first() == Some(&0)
            && self.offsets.last().copied() == Some(self.links.len() as u32)
            && self.offsets.windows(2).all(|pair| pair[0] <= pair[1])
    }
}

/// Resolves the path node of a direction at the selected start environment.
pub(crate) fn resolve_node(
    dimensions: &Dimensions,
    selected: EnvId,
    direction: &JumpDirection,
    node: u8,
) -> Option<EnvId> {
    let index = usize::from(node);
    if index >= direction.path.len() {
        return None;
    }
    if index == 0 {
        return Some(selected);
    }
    let encoding = direction.path[index];
    let position = u32::try_from(encoding[3])
        .ok()
        .filter(|position| (*position as usize) < dimensions.p() as usize)?;
    Some(dimensions.wrapped_env(
        dimensions.cell_of(selected),
        [encoding[0], encoding[1], encoding[2]],
        position,
    ))
}

fn path_nodes(
    dimensions: &Dimensions,
    selected: EnvId,
    direction: &JumpDirection,
) -> [Option<EnvId>; 10] {
    let mut nodes = [None; 10];
    for (node, _) in direction.path.iter().enumerate().take(10) {
        nodes[node] = resolve_node(dimensions, selected, direction, node as u8);
    }
    nodes
}

fn count_status_links(
    dimensions: &Dimensions,
    links: &LinkArena,
    job: &JobPackage,
    direction: &JumpDirection,
    position: u32,
) -> usize {
    let selected = dimensions.cell_env(0, position);
    let nodes = path_nodes(dimensions, selected, direction);
    let length = direction.path.len();
    let mut count = 0_usize;

    for sender in 0..length {
        let Some(sender_env) = nodes[sender] else {
            continue;
        };
        if !is_stable(job, dimensions, sender_env) {
            continue;
        }
        for (receiver, receiver_env) in nodes.iter().enumerate().take(length) {
            if sender == receiver {
                continue;
            }
            let Some(receiver_env) = *receiver_env else {
                continue;
            };
            if !is_stable(job, dimensions, receiver_env) {
                continue;
            }
            if receiver_link(dimensions, links, receiver_env, sender_env).is_some() {
                count += 1;
            }
        }
    }

    count
}

fn fill_status_links(
    dimensions: &Dimensions,
    links: &LinkArena,
    job: &JobPackage,
    direction: &JumpDirection,
    position: u32,
    output: &mut Vec<JumpLink>,
) {
    let selected = dimensions.cell_env(0, position);
    let nodes = path_nodes(dimensions, selected, direction);
    let length = direction.path.len();

    for sender in 0..length {
        let Some(sender_env) = nodes[sender] else {
            continue;
        };
        if !is_stable(job, dimensions, sender_env) {
            continue;
        }
        for (receiver, receiver_env) in nodes.iter().enumerate().take(length) {
            if sender == receiver {
                continue;
            }
            let Some(receiver_env) = *receiver_env else {
                continue;
            };
            if !is_stable(job, dimensions, receiver_env) {
                continue;
            }
            if let Some(link_id) = receiver_link(dimensions, links, receiver_env, sender_env) {
                output.push(JumpLink {
                    sender_path_id: sender as u8,
                    receiver_path_id: receiver as u8,
                    link_id,
                });
            }
        }
    }
}

fn receiver_link(
    dimensions: &Dimensions,
    links: &LinkArena,
    receiver: EnvId,
    sender: EnvId,
) -> Option<u32> {
    let position = PositionId::new(dimensions.position_of(receiver));
    for (index, link) in links.owner_links(position).iter().enumerate() {
        if links.partner_of(dimensions, receiver, link) == sender {
            return Some(index as u32);
        }
    }
    None
}

fn is_stable(job: &JobPackage, dimensions: &Dimensions, env: EnvId) -> bool {
    job.model.structure.positions[dimensions.position_of(env) as usize].stable
}

/// Returns the state-code path length of a direction.
#[must_use]
pub fn path_len(direction: &JumpDirection) -> u8 {
    u8::try_from(direction.path.len()).unwrap_or(u8::MAX)
}

/// Step 14's Step 16 helpers on the context.
impl super::SimulationContext {
    /// Returns one compiled rule.
    #[must_use]
    pub fn rule(&self, rule_id: RuleId) -> Option<&JumpRule> {
        self.job.model.transitions.rules.get(rule_id.get() as usize)
    }

    /// Returns the rule ids of one collection.
    #[must_use]
    pub fn collection_rules(&self, collection_id: JumpCollectionId) -> Option<&[RuleId]> {
        self.job
            .model
            .transitions
            .collections
            .get(collection_id.get() as usize)
            .map(|collection| collection.rule_ids.as_slice())
    }

    /// Returns the position's allowed-particle mask (void is always allowed and is not in the mask).
    #[must_use]
    pub fn allowed_mask_of(&self, env: EnvId) -> u64 {
        self.job.model.structure.positions[self.dimensions.position_of(env) as usize].allowed_mask
    }

    /// Returns the relative-th active direction of `(position, particle)`.
    #[must_use]
    pub fn direction_for(
        &self,
        position: PositionId,
        particle: ParticleId,
        relative: u32,
    ) -> Option<JumpDirectionId> {
        let particles = self.dimensions.pc() as usize;
        let key = position.get() as usize * particles + particle.get() as usize;
        let count = *self.job.model.transitions.jump_count.get(key)?;
        let offset = *self.job.model.transitions.jump_offsets.get(key)? as usize;
        let mut seen = 0_u32;
        for direction in &self.job.model.transitions.jump_mapping[offset..offset + count as usize] {
            if !self.direction_is_active(*direction) {
                continue;
            }
            if seen == relative {
                return Some(JumpDirectionId::new(*direction));
            }
            seen += 1;
        }
        None
    }

    /// Returns `true` when a direction belongs to the job simulation's active collections.
    #[must_use]
    pub fn direction_is_active(&self, direction: u32) -> bool {
        let Some(direction) = self
            .job
            .model
            .transitions
            .directions
            .get(direction as usize)
        else {
            return false;
        };
        let Some(simulation) = self
            .job
            .model
            .simulations
            .get(self.job.settings.simulation_index as usize)
        else {
            return false;
        };
        simulation
            .jump_collection_ids
            .contains(&direction.collection_id.get())
    }

    /// Builds the absolute path of a direction from a selected start environment.
    ///
    /// Returns the number of nodes written; a slice shorter than the path writes nothing and returns 0.
    pub fn resolve_path(
        &self,
        selected: EnvId,
        direction: &JumpDirection,
        output: &mut [EnvId],
    ) -> usize {
        let length = direction.path.len();
        if output.len() < length {
            return 0;
        }
        for (node, slot) in output.iter_mut().enumerate().take(length) {
            let Some(env) = resolve_node(&self.dimensions, selected, direction, node as u8) else {
                return 0;
            };
            *slot = env;
        }
        length
    }

    /// Resolves one absolute path node of a direction.
    #[must_use]
    pub fn resolve_node(
        &self,
        selected: EnvId,
        direction: &JumpDirection,
        node: u8,
    ) -> Option<EnvId> {
        resolve_node(&self.dimensions, selected, direction, node)
    }

    /// Returns the state-code path length of a direction.
    #[must_use]
    pub fn path_len(&self, direction: &JumpDirection) -> u8 {
        path_len(direction)
    }

    /// Returns the links of one `(position, direction)` jump status.
    #[must_use]
    pub fn jump_links(&self, position: PositionId, direction: u32) -> &[JumpLink] {
        self.jumps
            .links_at(position, direction, self.dimensions.p())
    }

    /// Returns the links of one raw status index.
    #[must_use]
    pub fn jump_status(&self, status: usize) -> &[JumpLink] {
        self.jumps.status_links(status)
    }
}

#[cfg(test)]
mod tests {
    use super::{JumpRuntime, path_len, resolve_node};
    use crate::context::Dimensions;
    use mocassin_types::JumpDirection;

    #[test]
    fn an_empty_status_is_valid_for_any_status_count() {
        let statuses = JumpRuntime::empty();
        assert!(statuses.is_empty());
        assert!(statuses.has_valid_shape(0));
        assert_eq!(statuses.status_count(), 0);
    }

    #[test]
    fn an_inconsistent_csr_is_rejected() {
        let statuses = JumpRuntime {
            offsets: vec![0, 1],
            links: Vec::new(),
        };
        assert!(!statuses.has_valid_shape(1));
    }

    #[test]
    fn path_resolution_replaces_node_zero_and_wraps_relative_cells() {
        let dimensions = Dimensions::new([2, 2, 2], 2, 2).expect("dimensions");
        let direction = JumpDirection {
            object_id: "test".to_owned(),
            position_id: 0,
            collection_id: mocassin_types::JumpCollectionId::new(0),
            path: vec![[0, 0, 0, 0], [1, 0, 0, 1], [0, -1, 0, 0]],
            movement: vec![[0.0; 3]; 3],
            path_vector: [0.0; 3],
            length: 0.0,
            is_inverse: false,
        };
        let selected = dimensions.cell_env(0, 1);
        assert_eq!(
            resolve_node(&dimensions, selected, &direction, 0),
            Some(selected)
        );
        assert_eq!(
            resolve_node(&dimensions, selected, &direction, 1),
            Some(dimensions.cell_env(4, 1))
        );
        assert_eq!(
            resolve_node(&dimensions, selected, &direction, 2),
            Some(dimensions.cell_env(2, 0))
        );
        assert_eq!(resolve_node(&dimensions, selected, &direction, 3), None);
        assert_eq!(path_len(&direction), 3);
    }
}

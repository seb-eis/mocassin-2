// SPDX-License-Identifier: MIT

//! The cluster runtime: instances, geometry, occupation codes, and per-centre energies.
//!
//! A cluster table can have more than one centre: the writer emits slot `0` for every P1 position of the centre
//! site, so the runtime keeps **one instance per slot-0 link** (Step 14 design §6.2/§6.3). Geometry is a flat
//! CSR (`slot_start` + `slot_positions`/`slot_cells`), never a `Vec` per instance; the centre index is a CSR
//! over P1 positions so Step 15 can read every cluster term of a path node without a supercell scan.
//!
//! **Slot geometry (OD-1).** A payload cluster link persists only the table id and the environment's own slot;
//! the member's relative cell offset is not in `.msl` v1. Slot `0` is the centre by definition (`[0,0,0]`).
//! Each slot `1..=n` is resolved, per instance, from the centre position's forward links:
//!
//! 1. links whose partner position is one of the payload positions carrying that slot; exactly one wins;
//! 2. only for a multi-centre table (more than one slot-0 link): links whose partner position belongs to the
//!    slot's member **site**; exactly one wins (the accepted rotated member-site image);
//! 3. otherwise the geometry is absent or ambiguous and initialization fails with
//!    [`ContextError::ClusterGeometry`].
//!
//! **Codes.** `codes[instance, cell]` is the raw slot tuple with the centre particle in slot `0` and the
//! member particles in slots `1..=n`, six bits per slot (slot 0 least significant). A present expansion map is
//! authoritative: the tuple is resolved by the context with
//! [`mocassin_types::ClusterTable::lookup`], and the only success is
//! [`mocassin_types::ClusterCodeLookup::Canonical`]. No solver-side symmetry, tuple sorting, or direct
//! `occupation_codes` fallback is permitted (F-29). A particle absent from `particle_table_mapping` has no
//! cluster row by contract, so its `code_id` is [`NO_CLUSTER_CODE`] and no lookup is performed for it.

use std::collections::BTreeMap;

use mocassin_types::{ClusterTable, JobPackage, ParticleId, PositionId};

use super::CancelProbe;
use super::dimensions::Dimensions;
use super::error::ContextError;
use super::links::{ClusterAssoc, LinkArena};

/// The `code_ids` value of a centre particle without a cluster row.
pub const NO_CLUSTER_CODE: u32 = u32::MAX;

/// The maximum number of slots of one cluster (the writer's `ClusterReduction.MaxClusterSites`).
pub const MAX_CLUSTER_SLOTS: u8 = 9;

/// Six bits per occupation slot.
const PARTICLE_BITS: u32 = 6;
/// The particle mask of one occupation slot.
const PARTICLE_MASK: u64 = (1 << PARTICLE_BITS) - 1;

/// The cluster instances and their derived state.
#[derive(Clone, Debug, Default, PartialEq)]
pub struct ClusterArena {
    /// One instance per slot-0 link.
    pub instances: Vec<ClusterInstance>,
    /// Centre offsets, length `P + 1`; the instances centred on a position are
    /// `centres[centre_offsets[position]..centre_offsets[position + 1]]`.
    pub centre_offsets: Vec<u32>,
    /// Instance ids grouped by ascending centre P1 position.
    pub centres: Vec<u32>,
    /// Slot offsets, length `K + 1`; the instance's slots are
    /// `slot_start[instance]..slot_start[instance + 1]`.
    pub slot_start: Vec<u32>,
    /// P1 position of each instance slot; slot 0 is the centre.
    pub slot_positions: Vec<u32>,
    /// Relative cell offset of each instance slot; slot 0 is `[0, 0, 0]`.
    pub slot_cells: Vec<[i32; 3]>,
    /// Raw occupation code per `instance * A·B·C + cell`.
    pub codes: Vec<u64>,
    /// Canonical occupation index, or [`NO_CLUSTER_CODE`] for an unmapped centre particle.
    pub code_ids: Vec<u32>,
    /// Energy-row prefix per instance, length `K + 1`.
    pub energy_start: Vec<u32>,
    /// Per-instance cluster energy in kT, indexed `cell * Σ mapping_len + energy_start[i] + row`.
    ///
    /// Feature 2 allocates and zeroes the array; feature 3 fills the values.
    pub energy: Vec<f64>,
}

impl ClusterArena {
    /// Returns the empty arena of a context whose clusters are not built yet.
    #[must_use]
    pub fn empty() -> Self {
        Self::default()
    }

    /// Returns `true` when no cluster instance is stored.
    #[must_use]
    pub fn is_empty(&self) -> bool {
        self.instances.is_empty()
    }

    /// Returns the number of instances.
    #[must_use]
    pub fn len(&self) -> usize {
        self.instances.len()
    }

    /// Returns one instance.
    #[must_use]
    pub fn instance(&self, instance: u32) -> Option<&ClusterInstance> {
        self.instances.get(instance as usize)
    }

    /// Returns the instances centred on one P1 position.
    #[must_use]
    pub fn centre_instances(&self, position: PositionId) -> &[u32] {
        let index = position.get() as usize;
        if self.centre_offsets.is_empty() {
            return &[];
        }
        let start = self.centre_offsets[index] as usize;
        let end = self.centre_offsets[index + 1] as usize;
        &self.centres[start..end]
    }

    /// Returns the instance's slot range.
    #[must_use]
    pub fn slot_range(&self, instance: u32) -> Option<(usize, usize)> {
        let index = instance as usize;
        let start = *self.slot_start.get(index)? as usize;
        let end = *self.slot_start.get(index + 1)? as usize;
        Some((start, end))
    }

    /// Returns the stored raw code of one instance and cell.
    #[must_use]
    pub fn code(&self, instance: u32, cell: u32, cell_count: u32) -> u64 {
        self.codes[instance as usize * cell_count as usize + cell as usize]
    }

    /// Returns the stored canonical code id, or `None` for an unmapped centre particle.
    #[must_use]
    pub fn code_id(&self, instance: u32, cell: u32, cell_count: u32) -> Option<u32> {
        let id = self.code_ids[instance as usize * cell_count as usize + cell as usize];
        (id != NO_CLUSTER_CODE).then_some(id)
    }

    /// Builds the instances, their geometry, and the initial codes.
    ///
    /// # Errors
    ///
    /// Returns [`ContextError::ClusterGeometry`] for an absent or ambiguous member image, a non-contiguous slot
    /// set, a duplicated slot link, or a slot count above [`MAX_CLUSTER_SLOTS`]; the cluster-code errors for a
    /// tuple absent from a present map or a non-canonical tuple in a pre-map package; and
    /// [`ContextError::Cancelled`] when the caller's flag is set.
    pub(crate) fn build(
        job: &JobPackage,
        dimensions: &Dimensions,
        links: &mut LinkArena,
        particles: &[ParticleId],
        cancel: &CancelProbe<'_>,
    ) -> Result<Self, ContextError> {
        let positions = dimensions.p() as usize;
        let cells = dimensions.cell_count() as usize;
        let table_count = job.model.energy.cluster_tables.len();
        let shapes = collect_shapes(job, dimensions, table_count)?;

        // One instance per slot-0 link, ordered by centre position and then table index.
        let mut pending: Vec<(u32, u32)> = Vec::new();
        for shape in shapes.values() {
            for centre in &shape.slot0_positions {
                pending.push((*centre, shape.table));
            }
        }
        pending.sort_unstable();

        let mut centre_offsets = vec![0_u32; positions + 1];
        for (centre, _) in &pending {
            centre_offsets[*centre as usize + 1] += 1;
        }
        for position in 0..positions {
            centre_offsets[position + 1] += centre_offsets[position];
        }

        let mut instances = Vec::with_capacity(pending.len());
        let mut slot_start = Vec::with_capacity(pending.len() + 1);
        let mut slot_positions = Vec::new();
        let mut slot_cells = Vec::new();
        let mut assocs: Vec<(usize, ClusterAssoc)> = Vec::new();
        slot_start.push(0);

        for (instance, (centre, table)) in pending.iter().copied().enumerate() {
            if instance % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let shape = shapes.get(&table).ok_or(ContextError::Invariant {
                what: "cluster table shape",
            })?;
            let owner = PositionId::new(centre);
            let owner_start = links.owner_offsets[centre as usize] as usize;
            let owner_links = links.owner_links(owner);
            let mut instance_slots = 0_u8;
            let mut resolved = [0_usize; MAX_CLUSTER_SLOTS as usize];
            let mut fallback_site: Option<u32> = None;
            let instance_id = u32::try_from(instance).map_err(|_| ContextError::Invariant {
                what: "cluster instance count",
            })?;

            slot_positions.push(centre);
            slot_cells.push([0, 0, 0]);
            instance_slots += 1;

            for (resolved_count, slot) in (1..shape.slot_count).enumerate() {
                let candidates = &shape.slot_candidates[slot as usize];
                let mut direct: Option<(usize, u32)> = None;
                let mut direct_count = 0_usize;

                for (local, link) in owner_links.iter().enumerate() {
                    if candidates.binary_search(&link.partner_position).is_ok() {
                        direct_count += 1;
                        direct = Some((local, link.partner_position));
                    }
                }

                let (local, partner_position, used_fallback) = match (direct_count, direct) {
                    (1, Some((local, partner))) => (local, partner, false),
                    (0, _) if shape.slot0_positions.len() > 1 => {
                        let member_site = shape.slot_sites[slot as usize];
                        let mut fallback: Option<(usize, u32)> = None;
                        let mut fallback_count = 0_usize;
                        for (local, link) in owner_links.iter().enumerate() {
                            if job.model.structure.positions[link.partner_position as usize]
                                .site_index
                                == member_site
                            {
                                fallback_count += 1;
                                fallback = Some((local, link.partner_position));
                            }
                        }

                        match (fallback_count, fallback) {
                            (1, Some((local, partner))) => (local, partner, true),
                            _ => {
                                return Err(ContextError::ClusterGeometry {
                                    cluster_table: table,
                                    instance: centre,
                                    slot,
                                    detail: format!(
                                        "member site {member_site} has {fallback_count} candidate images"
                                    ),
                                });
                            }
                        }
                    }
                    _ => {
                        return Err(ContextError::ClusterGeometry {
                            cluster_table: table,
                            instance: centre,
                            slot,
                            detail: format!(
                                "the centre has {direct_count} links to the slot's candidate positions"
                            ),
                        });
                    }
                };

                if used_fallback {
                    if fallback_site == Some(shape.slot_sites[slot as usize]) {
                        return Err(ContextError::ClusterGeometry {
                            cluster_table: table,
                            instance: centre,
                            slot,
                            detail: "two slots use the same member-site fallback".to_owned(),
                        });
                    }
                    fallback_site = Some(shape.slot_sites[slot as usize]);
                }

                let absolute = owner_start + local;
                if resolved[..resolved_count].contains(&absolute) {
                    return Err(ContextError::ClusterGeometry {
                        cluster_table: table,
                        instance: centre,
                        slot,
                        detail: "two slots resolve to the same forward link".to_owned(),
                    });
                }
                resolved[resolved_count] = absolute;
                assocs.push((
                    absolute,
                    ClusterAssoc {
                        instance: instance_id,
                        slot,
                    },
                ));

                let link = &owner_links[local];
                slot_positions.push(partner_position);
                slot_cells.push(link.offset);
                instance_slots += 1;
            }

            let instance_slot_start = *slot_start.last().ok_or(ContextError::Invariant {
                what: "cluster slot start",
            })?;
            instances.push(ClusterInstance {
                table,
                centre_position: centre,
                slot_start: instance_slot_start,
                slot_count: instance_slots,
            });
            slot_start.push(u32::try_from(slot_positions.len()).map_err(|_| {
                ContextError::LimitExceeded {
                    what: "cluster slots",
                    value: slot_positions.len() as u64,
                    limit: u64::from(u32::MAX),
                }
            })?);
        }

        // Group the associations by forward link so `cluster_start`/`cluster_len` describe one contiguous
        // range; the stable sort keeps the instance/slot order inside each link.
        assocs.sort_by_key(|(link_id, _)| *link_id);
        for (link_id, assoc) in assocs {
            links.attach_assoc(link_id, assoc)?;
        }

        let centres: Vec<u32> = (0..instances.len()).map(|index| index as u32).collect();
        let codes_len = instances
            .len()
            .checked_mul(cells)
            .ok_or(ContextError::LimitExceeded {
                what: "cluster codes",
                value: u64::MAX,
                limit: u64::from(u32::MAX),
            })?;
        let mut codes = vec![0_u64; codes_len];
        let mut code_ids = vec![NO_CLUSTER_CODE; codes_len];
        let mut energy_start = Vec::with_capacity(instances.len() + 1);
        energy_start.push(0_u32);
        let mut energy_rows = 0_u32;

        for instance in &instances {
            let table = cluster_table(job, instance.table)?;
            let mapping_len = u32::try_from(table.particle_table_mapping.len()).map_err(|_| {
                ContextError::Invariant {
                    what: "cluster mapping length",
                }
            })?;
            energy_rows =
                energy_rows
                    .checked_add(mapping_len)
                    .ok_or(ContextError::LimitExceeded {
                        what: "cluster energy rows",
                        value: u64::MAX,
                        limit: u64::from(u32::MAX),
                    })?;
            energy_start.push(energy_rows);
        }

        let energy_len =
            cells
                .checked_mul(energy_rows as usize)
                .ok_or(ContextError::LimitExceeded {
                    what: "cluster energy",
                    value: u64::MAX,
                    limit: u64::from(u32::MAX),
                })?;
        let energy = vec![0.0_f64; energy_len];

        for (instance, instance_data) in instances.iter().enumerate() {
            if instance % super::CANCEL_STRIDE == 0 {
                cancel.check()?;
            }

            let table = cluster_table(job, instance_data.table)?;
            let base = instance * cells;
            for cell in 0..dimensions.cell_count() {
                let raw = build_code(
                    dimensions,
                    particles,
                    &slot_positions,
                    &slot_cells,
                    instance_data,
                    cell,
                    &[],
                );
                codes[base + cell as usize] = raw;
                if has_centre_row(table, raw) && !raw_has_void_member(raw, instance_data.slot_count)
                {
                    code_ids[base + cell as usize] = lookup_table(table, raw, instance_data.table)?;
                }
            }

            // Resolve every allowed centre particle for the energy rows of this instance and cell. A centre
            // particle outside the mapping (for example void) has no row and therefore no lookup.
            for centre in &table.particle_table_mapping {
                let overrides = [(0_u8, ParticleId::new(*centre))];
                for cell in 0..dimensions.cell_count() {
                    let raw = build_code(
                        dimensions,
                        particles,
                        &slot_positions,
                        &slot_cells,
                        instance_data,
                        cell,
                        &overrides,
                    );
                    if !raw_has_void_member(raw, instance_data.slot_count) {
                        let _ = lookup_table(table, raw, instance_data.table)?;
                    }
                }
            }
        }

        Ok(Self {
            instances,
            centre_offsets,
            centres,
            slot_start,
            slot_positions,
            slot_cells,
            codes,
            code_ids,
            energy_start,
            energy,
        })
    }

    /// Returns the summed allocator capacity of the arena arrays.
    #[must_use]
    pub(crate) fn capacity_bytes(&self) -> u64 {
        self.instances.capacity() as u64 * core::mem::size_of::<ClusterInstance>() as u64
            + self.centre_offsets.capacity() as u64 * 4
            + self.centres.capacity() as u64 * 4
            + self.slot_start.capacity() as u64 * 4
            + self.slot_positions.capacity() as u64 * 4
            + self.slot_cells.capacity() as u64 * 12
            + self.codes.capacity() as u64 * 8
            + self.code_ids.capacity() as u64 * 4
            + self.energy_start.capacity() as u64 * 4
            + self.energy.capacity() as u64 * 8
    }

    /// Returns `true` when the flat cluster arrays have a consistent shape.
    #[must_use]
    pub(crate) fn has_valid_shape(&self, dimensions: &Dimensions) -> bool {
        let positions = dimensions.p() as usize;
        let cells = u64::from(dimensions.cell_count());

        if self.centre_offsets.is_empty() {
            return self.is_empty()
                && self.centres.is_empty()
                && self.slot_start.is_empty()
                && self.slot_positions.is_empty()
                && self.slot_cells.is_empty()
                && self.codes.is_empty()
                && self.code_ids.is_empty()
                && self.energy_start.is_empty()
                && self.energy.is_empty();
        }

        if self.centre_offsets.len() != positions + 1
            || self.centre_offsets.first() != Some(&0)
            || self.centre_offsets.last().copied() != Some(self.centres.len() as u32)
            || !self
                .centre_offsets
                .windows(2)
                .all(|pair| pair[0] <= pair[1])
        {
            return false;
        }

        if self.instances.is_empty() {
            return self.centres.is_empty()
                && self.slot_start == [0]
                && self.slot_positions.is_empty()
                && self.slot_cells.is_empty()
                && self.codes.is_empty()
                && self.code_ids.is_empty()
                && self.energy_start == [0]
                && self.energy.is_empty();
        }

        let instances = self.instances.len();
        let slots = self.slot_positions.len();
        let energy_rows = u64::from(self.energy_start.last().copied().unwrap_or(0));

        self.centres.len() == instances
            && self.centres.iter().all(|id| (*id as usize) < instances)
            && self.slot_start.len() == instances + 1
            && self.slot_start.first() == Some(&0)
            && self.slot_start.last().copied() == Some(slots as u32)
            && self.slot_start.windows(2).all(|pair| pair[0] <= pair[1])
            && self.slot_cells.len() == slots
            && self.codes.len() as u64 == instances as u64 * cells
            && self.code_ids.len() == self.codes.len()
            && self.energy_start.len() == instances + 1
            && self.energy_start.first() == Some(&0)
            && self.energy_start.windows(2).all(|pair| pair[0] <= pair[1])
            && cells.checked_mul(energy_rows) == Some(self.energy.len() as u64)
            && self.instances.iter().enumerate().all(|(index, instance)| {
                let start = self.slot_start[index] as u64;
                let end = self.slot_start[index + 1] as u64;
                u64::from(instance.centre_position) < u64::from(dimensions.p())
                    && instance.slot_count != 0
                    && instance.slot_count <= MAX_CLUSTER_SLOTS
                    && start < slots as u64
                    && end <= slots as u64
                    && end - start == u64::from(instance.slot_count)
                    && u64::from(instance.slot_start) == start
                    && self.slot_positions[start as usize] == instance.centre_position
                    && self.slot_cells[start as usize] == [0, 0, 0]
            })
            && self
                .slot_positions
                .iter()
                .all(|position| *position < dimensions.p())
    }

    /// Builds the raw occupation code of one instance and centre cell with optional slot overrides.
    ///
    /// Overrides are `(slot, particle)` pairs; the first entry for a slot wins. No allocation is performed.
    #[must_use]
    pub fn occupation_code(
        &self,
        dimensions: &Dimensions,
        particles: &[ParticleId],
        instance: u32,
        centre_cell: u32,
        overrides: &[(u8, ParticleId)],
    ) -> Option<u64> {
        let instance_data = self.instance(instance)?;
        Some(build_code(
            dimensions,
            particles,
            &self.slot_positions,
            &self.slot_cells,
            instance_data,
            centre_cell,
            overrides,
        ))
    }
}

/// One validated cluster-table geometry.
struct TableShape {
    table: u32,
    slot0_positions: Vec<u32>,
    slot_candidates: Vec<Vec<u32>>,
    slot_sites: Vec<u32>,
    slot_count: u8,
}

fn collect_shapes(
    job: &JobPackage,
    dimensions: &Dimensions,
    table_count: usize,
) -> Result<BTreeMap<u32, TableShape>, ContextError> {
    let positions = dimensions.p() as usize;
    let mut builders: BTreeMap<u32, TableShape> = BTreeMap::new();

    for position in 0..positions {
        for cluster in &job.model.structure.environments[position].clusters {
            let table = cluster.cluster_id;
            if table as usize >= table_count {
                return Err(ContextError::InvalidInput {
                    what: "cluster link table",
                    detail: format!(
                        "P{position} references cluster table {table} but the model has {table_count}"
                    ),
                });
            }

            let slot = cluster.byte_position;
            let shape = builders.entry(table).or_insert_with(|| TableShape {
                table,
                slot0_positions: Vec::new(),
                slot_candidates: Vec::new(),
                slot_sites: Vec::new(),
                slot_count: 0,
            });
            if shape.slot_candidates.len() <= usize::from(slot) {
                shape
                    .slot_candidates
                    .resize_with(usize::from(slot) + 1, Vec::new);
            }
            shape.slot_candidates[usize::from(slot)].push(position as u32);
        }
    }

    for shape in builders.values_mut() {
        if shape.slot_candidates.first().is_none_or(Vec::is_empty) {
            return Err(ContextError::ClusterGeometry {
                cluster_table: shape.table,
                instance: 0,
                slot: 0,
                detail: "the table has no slot-0 link".to_owned(),
            });
        }

        let slot_count = u8::try_from(shape.slot_candidates.len()).map_err(|_| {
            ContextError::ClusterGeometry {
                cluster_table: shape.table,
                instance: 0,
                slot: 0,
                detail: "the slot count does not fit u8".to_owned(),
            }
        })?;
        if slot_count > MAX_CLUSTER_SLOTS {
            return Err(ContextError::ClusterGeometry {
                cluster_table: shape.table,
                instance: 0,
                slot: slot_count - 1,
                detail: format!(
                    "the table has {slot_count} slots, above the limit {MAX_CLUSTER_SLOTS}"
                ),
            });
        }
        shape.slot_count = slot_count;

        for (slot, candidates) in shape.slot_candidates.iter().enumerate() {
            if candidates.is_empty() {
                return Err(ContextError::ClusterGeometry {
                    cluster_table: shape.table,
                    instance: 0,
                    slot: slot as u8,
                    detail: "the slot set is not contiguous".to_owned(),
                });
            }
            if !candidates.windows(2).all(|pair| pair[0] < pair[1]) {
                return Err(ContextError::ClusterGeometry {
                    cluster_table: shape.table,
                    instance: 0,
                    slot: slot as u8,
                    detail: "a position carries the same slot twice".to_owned(),
                });
            }
        }

        let centre_site =
            job.model.structure.positions[shape.slot_candidates[0][0] as usize].site_index;
        if shape.slot_candidates[0].iter().any(|position| {
            job.model.structure.positions[*position as usize].site_index != centre_site
        }) {
            return Err(ContextError::ClusterGeometry {
                cluster_table: shape.table,
                instance: 0,
                slot: 0,
                detail: "slot-0 links span more than one site".to_owned(),
            });
        }
        shape.slot0_positions = shape.slot_candidates[0].clone();

        shape.slot_sites = vec![centre_site; shape.slot_candidates.len()];
        for slot in 1..shape.slot_candidates.len() {
            let first = shape.slot_candidates[slot][0];
            let site = job.model.structure.positions[first as usize].site_index;
            if shape.slot_candidates[slot].iter().any(|position| {
                job.model.structure.positions[*position as usize].site_index != site
            }) {
                return Err(ContextError::ClusterGeometry {
                    cluster_table: shape.table,
                    instance: 0,
                    slot: slot as u8,
                    detail: "the slot's positions span more than one site".to_owned(),
                });
            }
            shape.slot_sites[slot] = site;
        }
    }

    Ok(builders)
}

/// Builds the raw six-bit occupation code of one instance and centre cell.
fn build_code(
    dimensions: &Dimensions,
    particles: &[ParticleId],
    slot_positions: &[u32],
    slot_cells: &[[i32; 3]],
    instance: &ClusterInstance,
    centre_cell: u32,
    overrides: &[(u8, ParticleId)],
) -> u64 {
    let start = instance.slot_start as usize;
    let mut code = 0_u64;

    for slot in 0..instance.slot_count {
        let particle = overrides
            .iter()
            .find(|(override_slot, _)| *override_slot == slot)
            .map(|(_, particle)| *particle)
            .unwrap_or_else(|| {
                let environment = if slot == 0 {
                    dimensions.cell_env(centre_cell, instance.centre_position)
                } else {
                    let index = start + usize::from(slot);
                    dimensions.wrapped_env(centre_cell, slot_cells[index], slot_positions[index])
                };
                particles[environment.index()]
            });
        code |= u64::from(particle.get()) << (u32::from(slot) * PARTICLE_BITS);
    }

    code
}

fn cluster_table(job: &JobPackage, table: u32) -> Result<&ClusterTable, ContextError> {
    job.model
        .energy
        .cluster_tables
        .get(table as usize)
        .ok_or(ContextError::Invariant {
            what: "cluster table index",
        })
}

fn has_centre_row(table: &ClusterTable, raw_code: u64) -> bool {
    table
        .particle_table_mapping
        .contains(&((raw_code & PARTICLE_MASK) as u8))
}

/// Returns `true` when a member slot is void.
///
/// The `.msl` occupation table enumerates only the site's allowed (non-void) particles; a void member has no
/// cluster row and contributes zero. The centre slot is handled by `particle_table_mapping`.
pub(crate) fn raw_has_void_member(raw_code: u64, slot_count: u8) -> bool {
    (1..slot_count).any(|slot| (raw_code >> (u32::from(slot) * PARTICLE_BITS)) & PARTICLE_MASK == 0)
}

pub(crate) fn lookup_table(
    table: &ClusterTable,
    raw_code: u64,
    cluster_table: u32,
) -> Result<u32, ContextError> {
    match table.lookup(raw_code) {
        mocassin_types::ClusterCodeLookup::Canonical(index) => Ok(index),
        mocassin_types::ClusterCodeLookup::MissingFromMap(code) => {
            Err(ContextError::MissingOccupationCode {
                cluster_table,
                code,
            })
        }
        mocassin_types::ClusterCodeLookup::ExpansionMapAbsent(code) => {
            Err(ContextError::ExpansionMapAbsent {
                cluster_table,
                code,
            })
        }
    }
}

/// One cluster instance, one per slot-0 link.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ClusterInstance {
    /// The shared cluster-table index.
    pub table: u32,
    /// The centre P1 position.
    pub centre_position: u32,
    /// The first slot of this instance in the flat slot arrays.
    pub slot_start: u32,
    /// The number of slots, including the centre slot 0.
    pub slot_count: u8,
}

#[cfg(test)]
mod tests {
    use super::{ClusterArena, ClusterInstance, MAX_CLUSTER_SLOTS, NO_CLUSTER_CODE};
    use crate::context::Dimensions;

    fn dimensions() -> Dimensions {
        Dimensions::new([2, 2, 2], 4, 5).expect("valid test dimensions")
    }

    #[test]
    fn an_instance_is_sixteen_bytes() {
        assert_eq!(core::mem::size_of::<ClusterInstance>(), 16);
    }

    #[test]
    fn an_empty_arena_is_valid() {
        let arena = ClusterArena::empty();
        assert!(arena.is_empty());
        assert!(arena.has_valid_shape(&dimensions()));
        assert_eq!(MAX_CLUSTER_SLOTS, 9);
        assert_eq!(NO_CLUSTER_CODE, u32::MAX);
    }

    #[test]
    fn a_half_built_arena_is_rejected() {
        let arena = ClusterArena {
            instances: vec![ClusterInstance {
                table: 0,
                centre_position: 0,
                slot_start: 0,
                slot_count: 2,
            }],
            ..ClusterArena::empty()
        };
        assert!(!arena.has_valid_shape(&dimensions()));
    }
}

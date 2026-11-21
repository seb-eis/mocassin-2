// SPDX-License-Identifier: MIT

//! Facts recorded by the bounded scan and the cross-checks that must agree.

use crate::error::PackageError;

/// One cluster table's stored lengths.
#[derive(Debug, Clone, Copy, Default)]
pub(crate) struct ClusterFacts {
    /// The number of occupation codes.
    pub codes: u64,
    /// The number of energies.
    pub energies: u64,
    /// The number of particle-table mapping rows.
    pub mapping: u64,
    /// The `(expanded_codes, canonical_indices)` lengths when a map is present.
    pub expansions: Option<(u64, u64)>,
}

/// Structural facts recorded while scanning a model payload.
#[derive(Debug, Default)]
pub(crate) struct ScanFacts {
    pub format_version: Option<u64>,
    pub particle_count: Option<u64>,
    pub particle_lengths: [Option<u64>; 3],
    pub position_count: Option<u64>,
    pub site_count: Option<u64>,
    pub positions_len: Option<u64>,
    pub environments_len: Option<u64>,
    pub pair_links_total: u64,
    pub pair_table_count: Option<u64>,
    pub pair_table_energy_lengths: Vec<u64>,
    pub cluster_table_count: Option<u64>,
    pub cluster_table_lengths: Vec<ClusterFacts>,
    pub cluster_expansion_bytes: u64,
    pub defect_background_len: Option<u64>,
    pub collection_count: Option<u64>,
    pub collection_kinds: Vec<Option<&'static str>>,
    pub direction_count: Option<u64>,
    pub direction_collections: Vec<u32>,
    pub direction_path_lengths: Vec<u64>,
    pub direction_movement_lengths: Vec<u64>,
    pub rule_count: Option<u64>,
    pub rule_collections: Vec<u32>,
    pub rule_tracker_lengths: Vec<u64>,
    pub jump_count_len: Option<u64>,
    pub jump_mapping_len: Option<u64>,
    pub static_tracker_mapping_len: Option<u64>,
    pub global_tracker_mapping_len: Option<u64>,
    pub simulation_count: Option<u64>,
    pub simulation_kinds: Vec<Option<&'static str>>,
    pub simulation_collections: Vec<Vec<u32>>,
}

/// Structural facts recorded while scanning a job payload.
#[derive(Debug, Default)]
pub(crate) struct JobFacts {
    pub format_version: Option<u64>,
    pub kind: Option<&'static str>,
    pub flags: Option<u64>,
    pub optimizer_count: Option<u64>,
    pub extension_count: Option<u64>,
}

/// Structural facts recorded while scanning a lattice payload.
#[derive(Debug, Default)]
pub(crate) struct LatticeFacts {
    pub format_version: Option<u64>,
    pub lattice_size: [u32; 3],
    pub position_count: Option<u64>,
    pub lattice_len: Option<u64>,
}

fn absent(section: &'static str, name: &str) -> PackageError {
    PackageError::Scan {
        section,
        detail: format!("required fact \"{name}\" is absent"),
    }
}

fn overflow(section: &'static str, what: &str) -> PackageError {
    PackageError::Scan {
        section,
        detail: format!("{what} overflows 64-bit arithmetic"),
    }
}

fn mismatch(section: &'static str, what: &str, expected: u64, found: u64) -> PackageError {
    PackageError::Scan {
        section,
        detail: format!("{what} has length {found}, expected {expected}"),
    }
}

/// Cross-checks the model facts that must agree (Step 13 design §3.4).
pub(crate) fn validate_model(facts: &ScanFacts) -> Result<(), PackageError> {
    const SECTION: &str = "model";

    let particles = facts
        .particle_count
        .ok_or_else(|| absent(SECTION, "particle.count"))?;
    let positions = facts
        .position_count
        .ok_or_else(|| absent(SECTION, "structure.position_count"))?;
    let collections = facts
        .collection_count
        .ok_or_else(|| absent(SECTION, "transitions.jump_collections"))?;

    for (index, name) in ["charges", "symbols", "is_vacancy"].into_iter().enumerate() {
        let length = facts.particle_lengths[index]
            .ok_or_else(|| absent(SECTION, "particle array length"))?;
        if length != particles {
            return Err(mismatch(
                SECTION,
                &format!("particle.{name}"),
                particles,
                length,
            ));
        }
    }

    if facts.positions_len != Some(positions) {
        return Err(mismatch(
            SECTION,
            "structure.positions",
            positions,
            facts.positions_len.unwrap_or(0),
        ));
    }

    if facts.environments_len != Some(positions) {
        return Err(mismatch(
            SECTION,
            "structure.environments",
            positions,
            facts.environments_len.unwrap_or(0),
        ));
    }

    let particle_pairs = particles
        .checked_mul(particles)
        .ok_or_else(|| overflow(SECTION, "particle_count²"))?;
    for (index, length) in facts.pair_table_energy_lengths.iter().enumerate() {
        if *length != particle_pairs {
            return Err(mismatch(
                SECTION,
                &format!("energy.pair_tables[{index}].energies"),
                particle_pairs,
                *length,
            ));
        }
    }

    let site_background = positions
        .checked_mul(particles)
        .ok_or_else(|| overflow(SECTION, "position_count × particle_count"))?;

    for (name, length) in [
        ("energy.defect_background", facts.defect_background_len),
        ("transitions.jump_count", facts.jump_count_len),
        (
            "transitions.static_tracker_mapping",
            facts.static_tracker_mapping_len,
        ),
    ] {
        if length != Some(site_background) {
            return Err(mismatch(
                SECTION,
                name,
                site_background,
                length.unwrap_or(0),
            ));
        }
    }

    let collection_particles = collections
        .checked_mul(particles)
        .ok_or_else(|| overflow(SECTION, "collection_count × particle_count"))?;
    if facts.global_tracker_mapping_len != Some(collection_particles) {
        return Err(mismatch(
            SECTION,
            "transitions.global_tracker_mapping",
            collection_particles,
            facts.global_tracker_mapping_len.unwrap_or(0),
        ));
    }

    for (index, (path, movement)) in facts
        .direction_path_lengths
        .iter()
        .zip(&facts.direction_movement_lengths)
        .enumerate()
    {
        if path != movement {
            return Err(PackageError::Scan {
                section: SECTION,
                detail: format!(
                    "transitions.jump_directions[{index}].path has {path} nodes but movement has {movement}"
                ),
            });
        }
    }

    // The first direction of a collection fixes its common path length. Directions in one collection must agree;
    // that semantic check belongs to the typed validation and is not repeated here.
    let mut collection_path_lengths = vec![None; usize::try_from(collections).unwrap_or(0)];
    for (collection, path) in facts
        .direction_collections
        .iter()
        .zip(&facts.direction_path_lengths)
    {
        if let Some(slot) = collection_path_lengths.get_mut(*collection as usize)
            && slot.is_none()
        {
            *slot = Some(*path);
        }
    }

    for (index, (collection, tracker_length)) in facts
        .rule_collections
        .iter()
        .zip(&facts.rule_tracker_lengths)
        .enumerate()
    {
        if let Some(Some(path_length)) = collection_path_lengths.get(*collection as usize)
            && tracker_length != path_length
        {
            return Err(PackageError::Scan {
                section: SECTION,
                detail: format!(
                    "transitions.jump_rules[{index}].tracker_order has {tracker_length} entries but its collection path has {path_length}"
                ),
            });
        }
    }

    for (index, (kind, used)) in facts
        .simulation_kinds
        .iter()
        .zip(&facts.simulation_collections)
        .enumerate()
    {
        let Some(kind) = kind else {
            continue;
        };
        for collection in used {
            if let Some(Some(collection_kind)) = facts.collection_kinds.get(*collection as usize)
                && collection_kind != kind
            {
                return Err(PackageError::Scan {
                    section: SECTION,
                    detail: format!(
                        "simulations[{index}] is {kind} but collection {collection} is {collection_kind}"
                    ),
                });
            }
        }
    }

    Ok(())
}

/// Cross-checks the lattice facts (guards and derived length).
pub(crate) fn validate_lattice(
    facts: &LatticeFacts,
    max_lattice_payload_bytes: u64,
    max_positions: u64,
    max_position_count: u64,
) -> Result<(), PackageError> {
    const SECTION: &str = "lattice";

    let [a, b, c] = facts.lattice_size.map(u64::from);
    let position_count = facts
        .position_count
        .ok_or_else(|| absent(SECTION, "position_count"))?;

    if position_count > max_position_count {
        return Err(PackageError::Scan {
            section: SECTION,
            detail: format!(
                "position_count {position_count} exceeds the reader limit {max_position_count}"
            ),
        });
    }

    let expected = a
        .checked_mul(b)
        .and_then(|value| value.checked_mul(c))
        .and_then(|value| value.checked_mul(position_count))
        .ok_or_else(|| overflow(SECTION, "a × b × c × position_count"))?;

    if expected > max_positions {
        return Err(PackageError::Scan {
            section: SECTION,
            detail: format!("lattice length {expected} exceeds the reader limit {max_positions}"),
        });
    }

    if expected > i32::MAX as u64 {
        return Err(PackageError::Scan {
            section: SECTION,
            detail: format!("lattice length {expected} exceeds the signed 32-bit index limit"),
        });
    }

    let guarded_bytes = expected
        .checked_mul(4)
        .ok_or_else(|| overflow(SECTION, "4 × a × b × c × position_count"))?;
    if guarded_bytes > max_lattice_payload_bytes {
        return Err(PackageError::PayloadTooLarge {
            section: SECTION,
            declared: guarded_bytes,
            limit: max_lattice_payload_bytes,
        });
    }

    let lattice_len = facts
        .lattice_len
        .ok_or_else(|| absent(SECTION, "lattice"))?;
    if lattice_len != expected {
        return Err(mismatch(
            SECTION,
            "lattice particle array",
            expected,
            lattice_len,
        ));
    }

    Ok(())
}

// SPDX-License-Identifier: MIT

//! Acceptance tests over the real-writer C-centered package with equivalent starts and a mask difference.

mod support;

use std::fs;

use mocassin_msl::{Package, decode_model_payload};
use mocassin_types::{ReaderLimits, SimulationKind};

use support::fixture_dir;

fn package_path() -> std::path::PathBuf {
    fixture_dir("step-13-centered").join("step-13-centered.msl")
}

#[test]
fn the_centered_package_maps_and_exposes_equivalent_starts() {
    let package = Package::open_readonly(package_path(), ReaderLimits::default())
        .expect("the centered fixture opens");
    assert_eq!(package.job_ids().expect("ids"), vec![1]);

    let job = package.load_job(1).expect("the centered job maps");
    assert_eq!(job.kind, SimulationKind::Mmc);
    assert_eq!(job.model.counts.particle_count, 4);
    assert_eq!(job.model.counts.position_count, 8);
    assert_eq!(job.model.counts.site_count, 2);
    assert_eq!(job.model.counts.pair_table_count, 4);
    assert_eq!(job.model.counts.cluster_table_count, 0);
    assert_eq!(job.model.counts.collection_count, 1);
    assert_eq!(job.model.counts.direction_count, 4);
    assert_eq!(job.model.counts.rule_count, 8);
    assert_eq!(
        job.model.signature,
        "0740daff9ba08ff94148ce9ec4e908e0ccfe724da654d5499d27a1d56cc39e60"
    );
    assert_eq!(
        job.model.model_signature,
        "d160d1d37d4071e0109bae71fc9a2ff4e0e03a4e48c8e39fdabf2bada5ac4e49"
    );
    assert_eq!(
        job.lattice.signature,
        "9be91a8e78e64ad257c54243a290ff846426ec86afe68f3c4500545f56146dfb"
    );
    assert_eq!(job.lattice.lattice_size, [2, 2, 2]);
    assert_eq!(job.lattice.len(), 64);

    let particle_count = job.model.counts.particle_count;
    let mut cross_buckets = Vec::new();
    for position in 0..job.model.counts.position_count {
        for particle in 0..particle_count {
            let bucket = position * particle_count + particle;
            let offset = job.model.transitions.jump_offsets[bucket as usize] as usize;
            let end = job.model.transitions.jump_offsets[bucket as usize + 1] as usize;
            for direction in &job.model.transitions.jump_mapping[offset..end] {
                let direction = &job.model.transitions.directions[*direction as usize];
                if direction.position_id != position {
                    cross_buckets.push((position, direction.position_id));
                }
            }
        }
    }

    assert!(cross_buckets.contains(&(2, 6)), "{cross_buckets:?}");
    assert!(cross_buckets.contains(&(3, 7)), "{cross_buckets:?}");
    assert!(cross_buckets.contains(&(4, 0)), "{cross_buckets:?}");
    assert!(cross_buckets.contains(&(5, 1)), "{cross_buckets:?}");
}

#[test]
fn the_centered_package_has_a_per_node_mask_difference() {
    let package = Package::open_readonly(package_path(), ReaderLimits::default())
        .expect("the centered fixture opens");
    let job = package.load_job(1).expect("the centered job maps");
    let model = &job.model;
    let mut differing = Vec::new();

    for collection in &model.transitions.collections {
        let node_count = collection
            .direction_ids
            .iter()
            .map(|direction| {
                model.transitions.directions[direction.get() as usize]
                    .path
                    .len()
            })
            .max()
            .unwrap_or(0);
        for node in 0..node_count {
            let mut masks = Vec::new();
            for direction in &collection.direction_ids {
                let path = &model.transitions.directions[direction.get() as usize].path;
                if let Some(encoding) = path.get(node) {
                    masks.push(model.structure.positions[encoding[3] as usize].allowed_mask);
                }
            }
            if masks.windows(2).any(|pair| pair[0] != pair[1]) {
                differing.push((collection.kind, node, masks));
            }
        }
    }

    assert!(
        !differing.is_empty(),
        "the centered real-writer fixture must carry a per-node mask difference"
    );
}

#[test]
fn the_centered_raw_model_payload_decodes() {
    let payload = fs::read(
        fixture_dir("step-13-centered")
            .join("payloads")
            .join("model-1.bin"),
    )
    .expect("read the centered model payload");
    let model = decode_model_payload(&payload, &ReaderLimits::default())
        .expect("the centered model payload decodes");
    assert_eq!(model.particle.count, 4);
    assert_eq!(model.structure.position_count, 8);
    assert_eq!(model.structure.site_count, 2);
    assert_eq!(model.energy.pair_tables.len(), 4);
    assert_eq!(model.energy.cluster_tables.len(), 0);
    assert_eq!(model.transitions.jump_collections.len(), 1);
    assert_eq!(model.transitions.jump_directions.len(), 4);
    assert_eq!(model.transitions.jump_rules.len(), 8);
}

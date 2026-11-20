// SPDX-License-Identifier: MIT

//! Acceptance tests over the real-writer populated pair/cluster + MMC package.

mod support;

use std::fs;
use std::sync::Arc;

use mocassin_msl::{Package, decode_job_payload, decode_lattice_payload, decode_model_payload};
use mocassin_types::{ReaderLimits, SimulationKind};

use support::fixture_dir;

fn package_path() -> std::path::PathBuf {
    fixture_dir("step-13-populated").join("step-13-populated.msl")
}

fn raw_payload(name: &str) -> std::path::PathBuf {
    fixture_dir("step-13-populated").join("payloads").join(name)
}

#[test]
fn the_populated_package_maps_both_simulation_kinds() {
    let package = Package::open_readonly(package_path(), ReaderLimits::default())
        .expect("the populated fixture opens");
    assert_eq!(package.job_ids().expect("ids"), vec![1, 2]);

    let kmc = package.load_job(1).expect("the KMC job maps");
    assert_eq!(kmc.kind, SimulationKind::Kmc);
    assert_eq!(kmc.model.counts.particle_count, 4);
    assert_eq!(kmc.model.counts.position_count, 3);
    assert_eq!(kmc.model.counts.pair_table_count, 6);
    assert_eq!(kmc.model.counts.cluster_table_count, 1);
    assert_eq!(kmc.model.counts.collection_count, 2);
    assert_eq!(kmc.model.counts.direction_count, 4);
    assert_eq!(kmc.model.counts.rule_count, 8);
    assert_eq!(
        kmc.model.signature,
        "abbd7062c53dd66a42009c52ffd7169e75645d78dac3c4cf7d5f94f7bddb2e79"
    );
    assert_eq!(
        kmc.model.model_signature,
        "f0dd41980d90e803a00f726e588f2895852d15f9fa7f658965b5c91495b94ba0"
    );
    assert_eq!(
        kmc.lattice.signature,
        "eee85b1aa92041b00cba7362acf930627a75becf077eb8dc9c4d90a7f3993475"
    );
    assert_eq!(kmc.lattice.lattice_size, [2, 2, 2]);
    assert_eq!(kmc.lattice.len(), 24);
    assert_eq!(kmc.settings.target_mcsp, 100);
    assert_eq!(kmc.settings.temperature_k, 773.0);

    assert_eq!(kmc.model.energy.pair_tables.len(), 6);
    assert!(
        kmc.model
            .energy
            .pair_tables
            .iter()
            .all(|table| table.energies.len() == 16)
    );
    assert_eq!(kmc.model.energy.cluster_tables.len(), 1);
    assert!(
        !kmc.model.energy.cluster_tables[0]
            .occupation_codes
            .is_empty()
    );
    assert_eq!(kmc.model.energy.defect_background.len(), 12);
    assert_eq!(kmc.model.transitions.jump_offsets.len(), 13);

    let mmc = package.load_job(2).expect("the MMC job maps");
    assert_eq!(mmc.kind, SimulationKind::Mmc);
    assert_eq!(mmc.settings.temperature_k, 1200.0);
    assert_eq!(mmc.settings.pre_run_mcsp, 0);
    assert!(mmc.settings.break_tolerance.is_some());
    assert_eq!(
        mmc.settings.break_sample_length_mcsp,
        Some(100),
        "the MMC break sample length is mapped"
    );
    assert!(Arc::ptr_eq(&kmc.model, &mmc.model));

    let simulation = &mmc.model.simulations[mmc.settings.simulation_index as usize];
    assert_eq!(simulation.kind, SimulationKind::Mmc);
    assert!(simulation.tracker_models.global_tracker_indices.is_empty());
    assert!(simulation.tracker_models.static_tracker_indices.is_empty());

    for collection in &mmc.model.transitions.collections {
        if collection.kind == SimulationKind::Mmc {
            assert!(!collection.direction_ids.is_empty());
            let direction =
                &mmc.model.transitions.directions[collection.direction_ids[0].get() as usize];
            for rule_id in &collection.rule_ids {
                let rule = &mmc.model.transitions.rules[rule_id.get() as usize];
                assert_eq!(rule.state1.get(), 0, "MMC state1 must be zero");
                assert_eq!(rule.tracker_order.len(), direction.path.len());
            }
        }
    }
}

#[test]
fn the_populated_fixture_has_a_per_node_mask_difference() {
    let package = Package::open_readonly(package_path(), ReaderLimits::default())
        .expect("the populated fixture opens");
    let job = package.load_job(1).expect("the KMC job maps");
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
                    let position = encoding[3] as usize;
                    masks.push(model.structure.positions[position].allowed_mask);
                }
            }
            if masks.windows(2).any(|pair| pair[0] != pair[1]) {
                differing.push((collection.kind, node, masks));
            }
        }
    }

    assert!(
        !differing.is_empty(),
        "the real-writer fixture must carry at least one per-node mask difference"
    );
}

#[test]
fn the_populated_raw_payloads_decode() {
    let limits = ReaderLimits::default();

    let model = decode_model_payload(
        &fs::read(raw_payload("model-1.bin")).expect("read the model payload"),
        &limits,
    )
    .expect("the populated model payload decodes");
    assert_eq!(model.particle.count, 4);
    assert_eq!(model.structure.position_count, 3);
    assert_eq!(model.energy.pair_tables.len(), 6);
    assert_eq!(model.energy.cluster_tables.len(), 1);
    assert!(
        model
            .energy
            .pair_tables
            .iter()
            .all(|table| table.energies.len() == 16)
    );
    assert_eq!(model.transitions.jump_collections.len(), 2);
    assert_eq!(model.transitions.jump_directions.len(), 4);
    assert_eq!(model.transitions.jump_rules.len(), 8);
    assert_eq!(model.simulations.len(), 2);
    assert_eq!(model.particle.symbols, ["∅", "O", "Vc", "H"]);

    let kmc = decode_job_payload(
        &fs::read(raw_payload("job-1.bin")).expect("read the KMC payload"),
        &limits,
    )
    .expect("the KMC payload decodes");
    assert_eq!(kmc.kind, "kmc");
    assert_eq!(kmc.target_mcsp, 100);
    assert_eq!(kmc.temperature_k, 773.0);

    let mmc = decode_job_payload(
        &fs::read(raw_payload("job-2.bin")).expect("read the MMC payload"),
        &limits,
    )
    .expect("the MMC payload decodes");
    assert_eq!(mmc.kind, "mmc");
    assert_eq!(mmc.target_mcsp, 100);
    assert_eq!(mmc.temperature_k, 1200.0);
    assert_eq!(mmc.pre_run_mcsp, 0);
    assert_eq!(mmc.break_sample_length_mcsp, Some(100));

    let lattice = decode_lattice_payload(
        &fs::read(raw_payload("lattice-1.bin")).expect("read the lattice payload"),
        &limits,
    )
    .expect("the populated lattice payload decodes");
    assert_eq!(lattice.lattice_size, [2, 2, 2]);
    assert_eq!(lattice.position_count, 3);
    assert_eq!(lattice.lattice.len(), 24);
}

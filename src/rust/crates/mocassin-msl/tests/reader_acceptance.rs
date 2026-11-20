// SPDX-License-Identifier: MIT

//! Acceptance tests over the real .NET-written `.msl` packages.

mod support;

use std::fs;
use std::sync::Arc;

use mocassin_msl::{Package, PackageError};
use mocassin_types::{ErrorClass, ExitCode, ReaderLimits, SimulationKind};

use support::{TempDir, copy_fixture, fixture_dir, fixture_file};

fn open_msl_v1() -> Package {
    Package::open_readonly(
        fixture_file("msl-v1", "ceria-temperature-sweep.msl"),
        ReaderLimits::default(),
    )
    .expect("the msl-v1 fixture opens read-only")
}

#[test]
fn the_msl_v1_package_opens_with_the_documented_schema() {
    let package = open_msl_v1();
    let schema = package.schema();

    assert_eq!(schema.format_version, 1);
    assert_eq!(schema.project_id, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c00");
    assert_eq!(schema.job_selection, "all");
    assert_eq!(schema.symmetry_contract_version, "symmetry-1");
    assert_eq!(schema.space_group_data_version, "spglib-2.8.0/catalog-1");
    assert_eq!(
        schema.space_group_data_sha256,
        "1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395"
    );
    assert!(!schema.app_version.is_empty());
    assert!(!schema.created_utc.is_empty());
    assert!(!schema.generator.is_empty());

    assert_eq!(package.limits(), &ReaderLimits::default());
    assert_eq!(package.job_ids().expect("ids"), vec![1, 2, 3, 4, 5, 6]);
}

#[test]
fn every_msl_v1_job_maps_and_verifies_both_signatures() {
    let package = open_msl_v1();
    let expected = [
        (1_u32, 150_u64, 6_u32, 773.0_f64),
        (2, 150, 6, 773.0),
        (3, 200, 8, 773.0),
        (4, 200, 8, 773.0),
        (5, 200, 6, 973.0),
        (6, 200, 8, 973.0),
    ];

    for (job_id, target_mcsp, cell, temperature_k) in expected {
        let job = package.load_job(job_id).expect("the job maps");

        assert_eq!(job.job_id, job_id);
        assert_eq!(job.kind, SimulationKind::Kmc);
        assert_eq!(
            job.model.signature,
            "1845b70e82320d75f768b33d1a9a46a8ae6170c33ad96013317f89fe9189a74b"
        );
        assert_eq!(
            job.model.model_signature,
            "92cf375eb1cc9bf6efaac33430fa3d44660ad8a7b355d3976441ce7c1d683e60"
        );
        assert_eq!(job.model.format_version, 1);
        assert_eq!(job.model.particle.charges.len(), 5);
        assert_eq!(job.model.particle.symbols.len(), 5);
        assert_eq!(job.model.particle.is_vacancy.len(), 5);
        assert_eq!(job.model.counts.particle_count, 5);
        assert_eq!(job.model.counts.position_count, 4);
        assert_eq!(job.model.counts.site_count, 4);
        assert_eq!(job.model.counts.environment_count, 4);
        assert_eq!(job.model.counts.pair_table_count, 0);
        assert_eq!(job.model.counts.cluster_table_count, 0);
        assert_eq!(job.model.counts.collection_count, 1);
        assert_eq!(job.model.counts.direction_count, 2);
        assert_eq!(job.model.counts.rule_count, 4);
        assert_eq!(job.model.counts.simulation_count, 1);
        assert_eq!(job.model.counts.global_tracker_count, 2);
        assert_eq!(job.model.counts.static_trackers_per_cell, 16);
        assert_eq!(job.model.structure.positions.len(), 4);
        assert_eq!(job.model.structure.environments.len(), 4);
        assert_eq!(job.model.structure.positions[0].allowed_mask, 0b1_1000);
        assert_eq!(job.model.structure.positions[1].allowed_mask, 0b110);
        assert_eq!(job.model.transitions.jump_offsets.len(), 21);
        assert_eq!(job.model.simulations[0].kind, SimulationKind::Kmc);

        assert_eq!(job.settings.simulation_index, 0);
        assert_eq!(job.settings.target_mcsp, target_mcsp);
        assert_eq!(job.settings.temperature_k, temperature_k);
        assert_eq!(job.settings.electric_field_vm, 10_000_000.0);
        assert_eq!(job.settings.flags.bits(), 0x72);
        assert!(!job.doping_info.is_empty());
        assert_eq!(job.seed.len(), 32);
        assert_eq!(job.optimizers.len(), usize::from(job_id <= 4));
        if let Some(optimizer) = job.optimizers.first() {
            assert_eq!(optimizer.position_id.get(), 0);
            assert_eq!(optimizer.particle_id.get(), 3);
        }
        assert!(job.extensions.is_empty());
        assert!(job.instruction.is_none());

        assert_eq!(job.lattice.lattice_size, [cell, cell, cell]);
        assert_eq!(job.lattice.position_count, 4);
        assert_eq!(
            u64::try_from(job.lattice.len()).expect("length"),
            u64::from(cell) * u64::from(cell) * u64::from(cell) * 4
        );
        assert!(
            job.lattice
                .particles
                .iter()
                .all(|particle| particle.get() < 5)
        );
    }
}

#[test]
fn the_lattice_signature_matches_the_stored_row() {
    let package = open_msl_v1();
    let job = package.load_job(1).expect("job 1 maps");
    assert_eq!(
        job.lattice.signature,
        "880b6eac79e9e920b6ea448c15f21b06be772f4bad774f12ea45c07420d25845"
    );

    let job_three = package.load_job(3).expect("job 3 maps");
    assert_eq!(
        job_three.lattice.signature,
        "ed688990f40a444649ec6883cdb7ad0510e62beaa3a93b1d378bb4f12ab20784"
    );
}

#[test]
fn the_amended_net10_package_maps_and_yields_the_same_signatures() {
    let package = Package::open_readonly(
        fixture_file("amend-1-net10", "ceria-temperature-sweep.msl"),
        ReaderLimits::default(),
    )
    .expect("the pre-amendment fixture opens read-only");

    assert_eq!(package.schema().format_version, 1);
    assert_eq!(package.job_ids().expect("ids"), vec![1, 2, 3, 4, 5, 6]);

    let job = package.load_job(1).expect("job 1 maps");
    assert_eq!(
        job.model.signature,
        "1845b70e82320d75f768b33d1a9a46a8ae6170c33ad96013317f89fe9189a74b"
    );
    assert_eq!(
        job.model.model_signature,
        "92cf375eb1cc9bf6efaac33430fa3d44660ad8a7b355d3976441ce7c1d683e60"
    );
    assert_eq!(
        job.lattice.signature,
        "880b6eac79e9e920b6ea448c15f21b06be772f4bad774f12ea45c07420d25845"
    );
    assert_eq!(job.settings.target_mcsp, 150);
    assert_eq!(job.lattice.lattice_size, [6, 6, 6]);
    assert_eq!(job.lattice.len(), 864);

    let job_three = package.load_job(3).expect("job 3 maps");
    assert_eq!(
        job_three.lattice.signature,
        "ed688990f40a444649ec6883cdb7ad0510e62beaa3a93b1d378bb4f12ab20784"
    );
    assert_eq!(job_three.lattice.lattice_size, [8, 8, 8]);
    assert_eq!(job_three.lattice.len(), 2048);
}

#[test]
fn the_model_is_mapped_once_and_shared_through_the_cache() {
    let package = open_msl_v1();

    let first = package.load_job(1).expect("job 1");
    let second = package.load_job(2).expect("job 2");
    assert!(Arc::ptr_eq(&first.model, &second.model));

    let direct = package.load_model(1).expect("model 1");
    assert!(Arc::ptr_eq(&first.model, &direct));

    let missing = package.load_model(99).expect_err("model 99 does not exist");
    assert_eq!(missing.kind(), ErrorClass::Model);
}

#[test]
fn an_unknown_job_is_an_argument_error() {
    let package = open_msl_v1();
    let error = package.load_job(99).expect_err("job 99 does not exist");
    assert_eq!(error.kind(), ErrorClass::InvalidArguments);
    assert_eq!(error.exit_code(), ExitCode::InvalidArguments);
    assert_eq!(
        error.source.to_string(),
        "job 99 is not present in the package"
    );
}

#[test]
fn a_0444_package_opens_read_only_without_sidecars() {
    let temp = TempDir::new("readonly");
    let path = copy_fixture("msl-v1", "ceria-temperature-sweep.msl", &temp);

    let mut permissions = fs::metadata(&path).expect("metadata").permissions();
    permissions.set_readonly(true);
    fs::set_permissions(&path, permissions).expect("set the read-only mode");

    let package = Package::open_readonly(&path, ReaderLimits::default()).expect("open 0444");
    let loaded = package.load_job(1).expect("load from 0444");
    assert_eq!(loaded.settings.target_mcsp, 150);

    let wal = format!("{}-wal", path.display());
    let shm = format!("{}-shm", path.display());
    assert!(!std::path::Path::new(&wal).exists(), "no -wal sidecar");
    assert!(!std::path::Path::new(&shm).exists(), "no -shm sidecar");
}

#[test]
fn a_missing_path_or_a_directory_is_an_io_error() {
    let temp = TempDir::new("io-errors");
    let limits = ReaderLimits::default();

    let missing = match Package::open_readonly(temp.join("missing.msl"), limits) {
        Ok(_) => panic!("a missing path must not open"),
        Err(error) => error,
    };
    assert!(matches!(missing, PackageError::Io(_)));
    assert_eq!(missing.kind(), ErrorClass::Io);
    assert_eq!(missing.exit_code(), ExitCode::IoError);

    let directory = match Package::open_readonly(temp.path(), limits) {
        Ok(_) => panic!("a directory must not open"),
        Err(error) => error,
    };
    assert!(matches!(directory, PackageError::Io(_)));
}

#[test]
fn the_fixture_directories_are_present() {
    assert!(
        fixture_dir("msl-v1")
            .join("ceria-temperature-sweep.msl")
            .is_file()
    );
    assert!(
        fixture_dir("amend-1-net10")
            .join("ceria-temperature-sweep.msl")
            .is_file()
    );
}

#[test]
fn the_seed_override_is_the_effective_job_seed() {
    let package = open_msl_v1();
    let package_seed = package.load_job(1).expect("the job maps").seed;
    let override_seed = [0x5A_u8; 32];

    let job = package
        .load_job_with_seed(1, Some(override_seed))
        .expect("the job maps with an override");
    assert_eq!(job.seed, override_seed);
    assert_ne!(job.seed, package_seed);

    let default_job = package
        .load_job(1)
        .expect("the job maps without an override");
    assert_eq!(default_job.seed, package_seed);
}

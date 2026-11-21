// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-2 link CSR and inverse routing index.

mod support;

use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, MemoryPlan, PositionId, SimulationContext,
};
use mocassin_types::{JobPackage, ParticleId};

use support::{
    amend2, centered, ceria, initialize, populated_kmc, populated_mmc, pre_map, with_model,
};

fn payload_links(job: &JobPackage) -> Vec<(u32, u32, [i32; 3], u32)> {
    let mut links = Vec::new();
    for (owner, environment) in job.model.structure.environments.iter().enumerate() {
        for pair in &environment.pairs {
            links.push((
                owner as u32,
                pair.target_position.get(),
                pair.offset,
                pair.pair_id,
            ));
        }
    }
    links
}

fn sorted_payload_links(job: &JobPackage) -> Vec<(u32, u32, [i32; 3], u32)> {
    let mut links = payload_links(job);
    links.sort_by(|left, right| {
        (left.0, left.3, left.1, left.2).cmp(&(right.0, right.3, right.1, right.2))
    });
    links
}

fn context_links(context: &SimulationContext) -> Vec<(u32, u32, [i32; 3], u32)> {
    let mut links = Vec::new();
    for position in 0..context.dimensions.p() {
        for link in context.link_range(PositionId::new(position)) {
            links.push((position, link.partner_position, link.offset, link.pair_id));
        }
    }
    links
}

fn payload_forward(
    job: &JobPackage,
    a: EnvId,
    b: EnvId,
    dimensions: &mocassin_solver::context::Dimensions,
) -> bool {
    let owner = dimensions.position_of(a);
    for pair in &job.model.structure.environments[owner as usize].pairs {
        let target = dimensions.wrapped_env(
            dimensions.cell_of(a),
            pair.offset,
            pair.target_position.get(),
        );
        if target == b {
            return true;
        }
    }
    false
}

#[test]
fn the_owner_major_csr_matches_a_brute_force_payload_expansion() {
    for job in [populated_kmc(), centered()] {
        let expected = sorted_payload_links(&job);
        let context = initialize(job);
        assert_eq!(context_links(&context), expected);
    }
}

#[test]
fn the_inverse_index_is_exact_for_every_owner_and_cell() {
    let context = initialize(populated_kmc());

    for owner in 0..context.dimensions.p() {
        for (local, link) in context
            .link_range(PositionId::new(owner))
            .iter()
            .enumerate()
        {
            for cell in 0..context.dimensions.cell_count() {
                let owner_env = context.dimensions.cell_env(cell, owner);
                let partner_env = context.partner_of(owner_env, link);
                let matches: Vec<_> = context
                    .incoming(partner_env)
                    .iter()
                    .filter(|entry| entry.owner_position == owner && entry.link_id == local as u32)
                    .copied()
                    .collect();
                assert_eq!(matches.len(), 1, "owner P{owner} link {local} cell {cell}");
                assert_eq!(
                    context.incoming_owner(partner_env, matches[0]),
                    Some(owner_env),
                    "owner P{owner} link {local} cell {cell}"
                );
            }
        }
    }

    assert_eq!(context.links.links.len(), context.links.inverse.len());
}

#[test]
fn partner_resolution_matches_the_wrapped_payload_image_for_every_cell() {
    let context = initialize(populated_kmc());

    for cell in 0..context.dimensions.cell_count() {
        for owner in 0..context.dimensions.p() {
            let owner_env = context.dimensions.cell_env(cell, owner);
            for link in context.link_range(PositionId::new(owner)) {
                let expected =
                    context
                        .dimensions
                        .wrapped_env(cell, link.offset, link.partner_position);
                assert_eq!(context.partner_of(owner_env, link), expected);
            }
        }
    }
}

#[test]
fn duplicate_images_are_kept_and_sorted_by_table_then_partner_then_offset() {
    let context = initialize(populated_kmc());

    let p2 = context.link_range(PositionId::new(2));
    assert_eq!(p2.len(), 2);
    assert_eq!(p2[0].pair_id, 2);
    assert_eq!(p2[0].offset, [1, 0, 0]);
    assert_eq!(p2[1].pair_id, 3);
    assert_eq!(p2[1].offset, [0, 0, 0]);

    let p0 = context.link_range(PositionId::new(0));
    assert_eq!(p0.len(), 2);
    assert_eq!(p0[0].pair_id, 4);
    assert_eq!(p0[1].pair_id, 5);

    assert_eq!(
        context.incoming(context.dimensions.cell_env(0, 0)).len(),
        3,
        "P0 is the partner of P1's link and both P2 links"
    );
}

#[test]
fn linked_matches_the_brute_force_relation_for_every_environment_pair() {
    let job = populated_kmc();
    let context = initialize(job.clone());

    for a in 0..context.dimensions.environment_count() {
        for b in 0..context.dimensions.environment_count() {
            let a = EnvId::new(a);
            let b = EnvId::new(b);
            let expected = payload_forward(&job, a, b, &context.dimensions)
                || payload_forward(&job, b, a, &context.dimensions);
            assert_eq!(context.linked(a, b), expected, "{a} <-> {b}");
        }
    }
}

#[test]
fn a_missing_reverse_link_is_not_synthesised() {
    let one_direction = with_model(centered(), |model| {
        model.structure.environments[1]
            .pairs
            .retain(|pair| pair.target_position.get() != 0);
    });
    let context = initialize(one_direction);
    let a = context.dimensions.cell_env(0, 0);
    let b = context.dimensions.cell_env(0, 1);

    assert!(context.linked(a, b), "P0 still has a forward link to P1");
    assert!(
        context
            .link_range(PositionId::new(1))
            .iter()
            .all(|link| link.partner_position != 0),
        "no reverse link is synthesised"
    );
    assert_eq!(context.links.links.len(), 7);
    assert_eq!(context.links.inverse.len(), 7);

    let both_directions = with_model(centered(), |model| {
        model.structure.environments[0]
            .pairs
            .retain(|pair| pair.target_position.get() != 1);
        model.structure.environments[1]
            .pairs
            .retain(|pair| pair.target_position.get() != 0);
    });
    let context = initialize(both_directions);
    let a = context.dimensions.cell_env(0, 0);
    let b = context.dimensions.cell_env(0, 1);
    assert!(!context.linked(a, b));
    assert!(!context.linked(b, a));
    assert_eq!(context.links.links.len(), 6);
}

#[test]
fn hostile_offsets_wrap_without_a_panic() {
    let offset = [i32::MAX, i32::MIN, i32::MAX];
    let job = with_model(populated_kmc(), |model| {
        let link = model.structure.environments[0]
            .pairs
            .iter_mut()
            .find(|pair| pair.pair_id == 5)
            .expect("P0 has a table-5 link");
        link.offset = offset;
    });
    let context = initialize(job);

    let local = context
        .link_range(PositionId::new(0))
        .iter()
        .position(|link| link.pair_id == 5)
        .expect("the hostile link is built");
    let link = &context.link_range(PositionId::new(0))[local];
    assert_eq!(link.offset, offset);

    for cell in 0..context.dimensions.cell_count() {
        let owner = context.dimensions.cell_env(cell, 0);
        let expected = context
            .dimensions
            .wrapped_env(cell, offset, link.partner_position);
        assert_eq!(context.partner_of(owner, link), expected);
        assert!(context.linked(owner, expected));

        let partner_position = PositionId::new(link.partner_position);
        let incoming = context
            .links
            .incoming_links(partner_position)
            .iter()
            .find(|entry| entry.owner_position == 0 && entry.link_id == local as u32)
            .copied()
            .expect("the hostile link has an inverse entry");
        assert_eq!(
            context.incoming_owner(expected, incoming),
            Some(owner),
            "cell {cell}"
        );
    }
}

#[test]
fn the_link_orientation_uses_the_owner_table_and_never_synthesises_the_reverse() {
    let job = with_model(populated_kmc(), |model| {
        let count = model.counts.particle_count as usize;
        model.energy.pair_tables[0].energies =
            (0..count * count).map(|value| value as f64).collect();
    });
    let context = initialize(job);

    let p1 = context.link_range(PositionId::new(1));
    let to_p0: Vec<_> = p1
        .iter()
        .filter(|link| link.partner_position == 0)
        .collect();
    assert_eq!(to_p0.len(), 1);
    assert_eq!(to_p0[0].pair_id, 0);
    assert!(
        context
            .link_range(PositionId::new(0))
            .iter()
            .all(|link| link.partner_position != 1),
        "P0 has no forward link to P1"
    );

    let p1_env = context.dimensions.cell_env(0, 1);
    let (link, partner) = context.owner_link(p1_env, 0).expect("P1's first link");
    assert_eq!(link.pair_id, 0);
    assert_eq!(partner, context.dimensions.cell_env(0, 0));
    assert!(context.linked(p1_env, partner));
}

#[test]
fn initialization_is_deterministic() {
    let first = initialize(amend2());
    let second = initialize(amend2());

    assert_eq!(first.links, second.links);
    assert_eq!(first.clusters, second.clusters);
    assert_eq!(
        first.clusters.slot_positions,
        second.clusters.slot_positions
    );
    assert_eq!(first.clusters.slot_cells, second.clusters.slot_cells);
}

#[test]
fn the_memory_plan_accounts_for_the_links_and_clusters() {
    let cases = [
        (populated_kmc(), 940_u64),
        (populated_mmc(), 940),
        (amend2(), 1204),
        (pre_map(), 1204),
        (centered(), 996),
        (ceria(), 5508),
    ];

    for (job, total) in cases {
        let context = initialize(job);
        let plan =
            MemoryPlan::plan(&context.job, &context.limits).expect("the plan admits the job");
        let report = context.memory_report();

        assert_eq!(report.entries(), plan.entries());
        assert_eq!(report.total_bytes(), plan.total_bytes());
        assert_eq!(report.total_bytes(), total);
        assert!(report.capacity_bytes() >= report.total_bytes());
    }

    let context = initialize(populated_kmc());
    let plan = MemoryPlan::plan(&context.job, &context.limits).expect("plan");
    assert_eq!(plan.entry("links.links").expect("entry").elements, 6);
    assert_eq!(
        plan.entry("links.cluster_assoc").expect("entry").elements,
        2
    );
    assert_eq!(plan.entry("clusters.instances").expect("entry").elements, 1);
    assert_eq!(plan.entry("clusters.codes").expect("entry").elements, 8);
    assert_eq!(plan.entry("clusters.energy").expect("entry").elements, 16);
}

fn expect_limit(
    job: &JobPackage,
    mutate: impl FnOnce(&mut ContextOptions),
    what: &'static str,
    value: u64,
    limit: u64,
) {
    let mut options = ContextOptions::default();
    mutate(&mut options);
    let error = SimulationContext::initialize(job.clone(), &options, None)
        .expect_err("the tightened cap rejects the job");
    match error {
        ContextError::LimitExceeded {
            what: actual_what,
            value: actual_value,
            limit: actual_limit,
        } => {
            assert_eq!(actual_what, what);
            assert_eq!(actual_value, value);
            assert_eq!(actual_limit, limit);
        }
        other => panic!("unexpected error {other}"),
    }
}

#[test]
fn every_link_and_cluster_cap_rejects_before_allocation() {
    let job = populated_kmc();

    expect_limit(
        &job,
        |options| options.limits.max_pair_links = 5,
        "max_pair_links",
        6,
        5,
    );
    expect_limit(
        &job,
        |options| options.limits.max_links_per_position = 1,
        "max_links_per_position",
        2,
        1,
    );
    expect_limit(
        &job,
        |options| options.limits.max_cluster_instances = 0,
        "max_cluster_instances",
        1,
        0,
    );
    expect_limit(
        &job,
        |options| options.limits.max_cluster_tables = 0,
        "max_cluster_tables",
        1,
        0,
    );
    expect_limit(
        &job,
        |options| options.limits.max_link_bytes = 263,
        "max_link_bytes",
        264,
        263,
    );
    expect_limit(
        &job,
        |options| options.limits.max_cluster_state_bytes = 195,
        "max_cluster_state_bytes",
        196,
        195,
    );
    expect_limit(
        &job,
        |options| options.limits.max_cluster_energy_bytes = 127,
        "max_cluster_energy_bytes",
        128,
        127,
    );
    expect_limit(
        &job,
        |options| options.limits.max_context_bytes = 939,
        "max_context_bytes",
        940,
        939,
    );
}

#[test]
fn a_tightened_particle_range_is_rejected_by_the_input_phase() {
    let job = with_model(populated_kmc(), |model| {
        model.counts.particle_count = 3;
    });
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("the particle table disagrees with the count");
    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "particle table",
            ..
        }
    ));
}

#[test]
fn a_particle_value_outside_the_table_is_rejected() {
    let mut job = populated_kmc();
    job.lattice.particles[0] = ParticleId::new(63);
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("particle 63 is outside the table");
    assert!(matches!(
        error,
        ContextError::InvalidInput {
            what: "lattice particle",
            ..
        }
    ));
}

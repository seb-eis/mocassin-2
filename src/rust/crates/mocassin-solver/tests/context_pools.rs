// SPDX-License-Identifier: MIT

//! Acceptance tests for the Step 14 feature-4 selection pools.

mod support;

use mocassin_solver::context::{
    ContextError, ContextOptions, EnvId, NO_SLOT, PositionId, SimulationContext,
};
use mocassin_types::{ParticleId, SelectionOptimizer};

use support::{amend2, centered, initialize, populated_kmc, populated_mmc, with_model};

fn active_mask(job: &mocassin_types::JobPackage) -> Vec<bool> {
    let simulation = &job.model.simulations[job.settings.simulation_index as usize];
    let mut active_collections = vec![false; job.model.transitions.collections.len()];
    for id in &simulation.jump_collection_ids {
        active_collections[*id as usize] = true;
    }
    job.model
        .transitions
        .directions
        .iter()
        .map(|direction| active_collections[direction.collection_id.get() as usize])
        .collect()
}

fn filtered_count(
    job: &mocassin_types::JobPackage,
    active: &[bool],
    position: u32,
    particle: ParticleId,
) -> u32 {
    let pc = job.model.counts.particle_count as usize;
    let key = position as usize * pc + particle.get() as usize;
    let count = job.model.transitions.jump_count[key] as usize;
    let offset = job.model.transitions.jump_offsets[key] as usize;
    job.model.transitions.jump_mapping[offset..offset + count]
        .iter()
        .filter(|direction| active[**direction as usize])
        .count() as u32
}

fn expected_selectable(job: &mocassin_types::JobPackage, context: &SimulationContext) -> u64 {
    if context.kind == mocassin_types::SimulationKind::Mmc {
        return 0;
    }
    let active = active_mask(job);
    let mut total = 0_u64;

    for environment in 0..context.dimensions.environment_count() {
        let env = EnvId::new(environment);
        let position = context.dimensions.position_of(env);
        let particle = context.lattice.particle(env);
        let count = filtered_count(job, &active, position, particle);
        if count == 0 {
            continue;
        }
        let mask = job.model.structure.environments[position as usize].selection_particle_mask;
        if mask & (1_u64 << particle.get()) == 0 {
            continue;
        }
        if job.optimizers.iter().any(|optimizer| {
            optimizer.position_id == PositionId::new(position) && optimizer.particle_id == particle
        }) {
            continue;
        }
        total += u64::from(count);
    }
    total
}

#[test]
fn the_populated_kmc_pool_uses_only_the_active_directions() {
    let job = populated_kmc();
    let active = active_mask(&job);
    assert!(active[0] && active[1] && !active[2] && !active[3]);
    let context = initialize(job.clone());

    assert_eq!(context.pools.pools.len(), 1);
    assert_eq!(context.pools.pools[0].direction_count, 1);
    assert_eq!(context.pools.member_count(), 16);
    assert_eq!(context.pools.selectable_jump_count, 16);
    assert_eq!(
        expected_selectable(&job, &context),
        context.pools.selectable_jump_count
    );

    let p0_particle1 = PositionId::new(0);
    assert_eq!(
        context.direction_for(p0_particle1, ParticleId::new(1), 0),
        Some(mocassin_types::JumpDirectionId::new(0))
    );
    assert_eq!(
        context.direction_for(p0_particle1, ParticleId::new(1), 1),
        None
    );
    assert!(context.direction_is_active(0));
    assert!(!context.direction_is_active(2));

    let pc = context.dimensions.pc() as usize;
    assert_eq!(context.pools.pool_of[1], 0);
    assert_eq!(context.pools.pool_of[2], 0);
    assert_eq!(context.pools.pool_of[3], NO_SLOT);
    assert_eq!(context.pools.pool_of[pc + 1], NO_SLOT);

    for (index, member) in context.pools.pools[0].members.iter().enumerate() {
        assert_eq!(
            context.pools.slot[member.env.index() * pc + member.particle.get() as usize],
            index as u32
        );
        assert_eq!(context.lattice.particle(member.env), member.particle);
    }
}

#[test]
fn the_amend_2_pool_has_two_active_directions_per_member() {
    let job = amend2();
    let context = initialize(job.clone());

    assert_eq!(context.pools.pools.len(), 1);
    assert_eq!(context.pools.pools[0].direction_count, 2);
    assert_eq!(context.pools.member_count(), 16);
    assert_eq!(context.pools.selectable_jump_count, 32);
    assert_eq!(
        expected_selectable(&job, &context),
        context.pools.selectable_jump_count
    );

    assert_eq!(
        context.direction_for(PositionId::new(1), ParticleId::new(2), 0),
        Some(mocassin_types::JumpDirectionId::new(0))
    );
    assert_eq!(
        context.direction_for(PositionId::new(1), ParticleId::new(2), 1),
        Some(mocassin_types::JumpDirectionId::new(2))
    );
    assert_eq!(
        context.direction_for(PositionId::new(1), ParticleId::new(2), 2),
        None
    );
}

#[test]
fn an_mmc_context_has_no_pools() {
    for job in [populated_mmc(), centered()] {
        let context = initialize(job);
        assert!(context.pools.is_empty());
        assert_eq!(context.pools.selectable_jump_count, 0);
        assert_eq!(context.pools.member_count(), 0);
    }
}

#[test]
fn the_selection_mask_controls_selectability() {
    let job = with_model(populated_kmc(), |model| {
        model.structure.environments[0].selection_particle_mask = 0x4;
    });
    let context = initialize(job.clone());

    assert_eq!(context.pools.pool_of[1], NO_SLOT);
    assert_eq!(context.pools.pool_of[2], 0);
    assert_eq!(context.pools.member_count(), 8);
    assert_eq!(context.pools.selectable_jump_count, 8);
    assert_eq!(
        expected_selectable(&job, &context),
        context.pools.selectable_jump_count
    );
    assert!(
        context
            .pools
            .pools
            .iter()
            .flat_map(|pool| pool.members.iter())
            .all(|member| !(context.dimensions.position_of(member.env) == 0
                && member.particle.get() == 1))
    );
}

#[test]
fn optimizers_remove_and_set_selectable_re_adds() {
    let mut job = populated_kmc();
    job.optimizers.push(SelectionOptimizer {
        position_id: PositionId::new(0),
        particle_id: ParticleId::new(1),
    });
    let mut context = initialize(job.clone());

    assert_eq!(context.pools.member_count(), 8);
    assert_eq!(context.pools.selectable_jump_count, 8);
    assert_eq!(context.pools.pool_of[1], 0);
    assert!(
        context
            .pools
            .pools
            .iter()
            .flat_map(|pool| pool.members.iter())
            .all(|member| !(context.dimensions.position_of(member.env) == 0
                && member.particle.get() == 1))
    );

    for environment in 0..context.dimensions.environment_count() {
        let env = EnvId::new(environment);
        if context.dimensions.position_of(env) == 0
            && context.lattice.particle(env) == ParticleId::new(1)
        {
            context.set_selectable(env, ParticleId::new(1));
        }
    }

    assert_eq!(context.pools.member_count(), 16);
    assert_eq!(context.pools.selectable_jump_count, 16);
}

#[test]
fn set_selectable_swaps_without_reallocating() {
    let mut context = initialize(populated_kmc());
    let pc = context.dimensions.pc() as usize;
    let capacities: Vec<usize> = context
        .pools
        .pools
        .iter()
        .map(|pool| pool.members.capacity())
        .collect();
    let pointer = context.pools.pools[0].members.as_ptr();
    let members = context.pools.pools[0].members.clone();

    let first = members[0];
    let original = first.particle;
    let void = ParticleId::new(0);

    context.set_particle(first.env, void);
    context.set_selectable(first.env, original);
    context.set_selectable(first.env, void);
    assert_eq!(context.pools.member_count(), 15);
    assert_eq!(context.pools.selectable_jump_count, 15);

    let last = members[members.len() - 1];
    let moved_index = context.pools.pools[0]
        .members
        .iter()
        .position(|member| *member == last)
        .expect("the last member moved into the freed slot");
    assert_eq!(moved_index, 0);
    assert_eq!(
        context.pools.slot[last.env.index() * pc + last.particle.get() as usize],
        0
    );

    context.set_particle(first.env, original);
    context.set_selectable(first.env, void);
    context.set_selectable(first.env, original);
    assert_eq!(context.pools.member_count(), 16);
    assert_eq!(context.pools.selectable_jump_count, 16);

    for _ in 0..200 {
        context.set_particle(first.env, void);
        context.set_selectable(first.env, original);
        context.set_selectable(first.env, void);
        context.set_particle(first.env, original);
        context.set_selectable(first.env, void);
        context.set_selectable(first.env, original);
    }
    assert_eq!(context.pools.member_count(), 16);
    assert_eq!(
        capacities,
        context
            .pools
            .pools
            .iter()
            .map(|pool| pool.members.capacity())
            .collect::<Vec<_>>()
    );
    assert_eq!(pointer, context.pools.pools[0].members.as_ptr());
}

#[test]
fn a_pool_cap_rejects_and_selectable_count_is_checked() {
    let job = populated_kmc();

    let mut options = ContextOptions::default();
    options.limits.max_pools = 0;
    let error = SimulationContext::initialize(job.clone(), &options, None)
        .expect_err("one pool exceeds the cap");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_pools",
            value: 1,
            limit: 0,
        }
    ));

    let mut options = ContextOptions::default();
    options.limits.max_pool_bytes = 719;
    let error = SimulationContext::initialize(job.clone(), &options, None)
        .expect_err("the pool arrays need 720 bytes");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_pool_bytes",
            value: 720,
            limit: 719,
        }
    ));

    let mut options = ContextOptions::default();
    options.limits.max_selectable_jump_count = 15;
    let error = SimulationContext::initialize(job, &options, None)
        .expect_err("16 selectable jumps exceed the cap");
    assert!(matches!(
        error,
        ContextError::LimitExceeded {
            what: "max_selectable_jump_count",
            value: 16,
            limit: 15,
        }
    ));
}

#[test]
fn a_job_without_selectable_jumps_is_rejected() {
    let job = with_model(populated_kmc(), |model| {
        model.structure.environments[0].selection_particle_mask = 0;
        model.structure.environments[2].selection_particle_mask = 0;
    });
    let error = SimulationContext::initialize(job, &ContextOptions::default(), None)
        .expect_err("no environment is selectable");
    assert!(matches!(error, ContextError::NoSelectableJumps));
    assert_eq!(error.exit_code().code(), 12);
}

#[test]
fn the_pool_plan_is_the_reserved_capacity() {
    let context = initialize(populated_kmc());
    let report = context.memory_report();
    let entry = |what: &str| {
        report
            .entries()
            .iter()
            .find(|entry| entry.what == what)
            .unwrap_or_else(|| panic!("{what} is missing"))
    };

    assert_eq!(entry("pools.headers").elements, 1);
    assert_eq!(entry("pools.members").elements, 32);
    assert_eq!(entry("pools.slot").elements, 96);
    assert_eq!(entry("pools.pool_of").elements, 12);
    let capacity: usize = context
        .pools
        .pools
        .iter()
        .map(|pool| pool.members.capacity())
        .sum();
    assert!(capacity >= entry("pools.members").elements as usize);
    assert_eq!(context.pools.member_count(), 16);
}

#[test]
fn the_pools_are_deterministic() {
    let first = initialize(populated_kmc());
    let second = initialize(populated_kmc());
    assert_eq!(first.pools, second.pools);
}

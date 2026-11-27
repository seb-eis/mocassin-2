// SPDX-License-Identifier: MIT

//! PR-fast error and field-term tests for Step 15 feature 1.

mod support;

use mocassin_solver::context::{ContextError, EnvId};
use mocassin_solver::energy::EnergyEngine;
use mocassin_types::{JumpDirectionId, PairLink, PositionId};

use support::energy::{assert_close, context, pair_only_job, site_only_job, state_code};

#[test]
fn a_positive_charge_displacement_along_the_field_lowers_the_forward_barrier() {
    let mut job = site_only_job(&[2, 0, 1]);
    job.settings.electric_field_vm = 1.0e7;
    job.settings.electric_field_direction = [1.0, 0.0, 0.0];
    let model = std::sync::Arc::make_mut(&mut job.model);
    model.particle.charges[1] = 1.0;
    let mut ctx = context(job);
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    rule.charge_transport_vector = [5.0, 0.0, 0.0];
    let expected_field = -0.5 * ctx.field_scale() * 5.0;

    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("the finite field term evaluates");

    assert_close(energies.field_term, expected_field);
    assert_close(
        energies.barrier_forward,
        energies.barrier_forward_no_field() + expected_field,
    );
    assert_close(
        energies.barrier_backward,
        energies.barrier_backward_no_field() - expected_field,
    );
    assert!(energies.barrier_forward < energies.barrier_forward_no_field());
}

#[test]
fn a_non_finite_field_term_is_a_numerical_failure() {
    let mut job = site_only_job(&[2, 0, 1]);
    job.settings.electric_field_vm = 1.0e7;
    let mut ctx = context(job);
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    rule.charge_transport_vector = [f64::NAN, 0.0, 0.0];

    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect_err("a NaN transport dot product is not finite");
    assert!(matches!(
        error,
        ContextError::NonFiniteFactor { what: "field term" }
    ));
    assert_eq!(error.exit_code().code(), 13);
}

#[test]
fn zero_field_has_a_zero_term_even_for_a_hand_mutated_transport_vector() {
    let mut ctx = context(site_only_job(&[2, 0, 1]));
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    rule.charge_transport_vector = [f64::NAN, 0.0, 0.0];
    let energies = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect("zero field contributes no field term");
    assert_eq!(energies.field_term, 0.0);
}

#[test]
fn a_present_static_correction_fails_closed_instead_of_guessing_s2() {
    let mut ctx = context(site_only_job(&[2, 0, 1]));
    let mut rule = ctx.job.model.transitions.rules[0].clone();
    rule.static_correction = Some([1.0, 2.0, 3.0]);
    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect_err("v1 static S2 semantics are undefined");
    assert!(matches!(error, ContextError::InvalidInput { .. }));
    assert_eq!(error.exit_code().code(), 12);
    assert!(error.to_string().contains("rebuild the package"));
}

#[test]
fn a_computed_non_finite_pair_delta_is_an_invariant_failure() {
    let mut job = pair_only_job(&[2, 1, 1]);
    let model = std::sync::Arc::make_mut(&mut job.model);
    model.structure.environments[1].pairs = vec![PairLink {
        target_position: PositionId::new(0),
        offset: [0, 0, 0],
        pair_id: 0,
    }];
    model.transitions.rules[0].state0 = state_code(&[2, 1, 1]);
    model.transitions.rules[0].state1 = state_code(&[0, 0, 0]);
    model.transitions.rules[0].state2 = state_code(&[1, 1, 2]);

    let mut ctx = context(job);
    // Failure-inject the model table after the finite context vectors were built.
    let model = std::sync::Arc::make_mut(&mut ctx.job.model);
    model.energy.pair_tables[0].energies[4 + 2] = f64::MAX;
    model.energy.pair_tables[0].energies[4 + 1] = -f64::MAX;
    let rule = ctx.job.model.transitions.rules[0].clone();
    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(0), JumpDirectionId::new(0), &rule)
        .expect_err("the finite table entries overflow in their computed difference");
    assert!(matches!(error, ContextError::Invariant { .. }));
    assert_eq!(error.exit_code().code(), 13);
}

#[test]
fn an_out_of_range_selected_environment_is_a_data_error_not_a_panic() {
    let mut ctx = context(site_only_job(&[2, 0, 1]));
    let rule = ctx.job.model.transitions.rules[0].clone();
    let error = EnergyEngine::new(&mut ctx)
        .transition_energies(EnvId::new(u32::MAX), JumpDirectionId::new(0), &rule)
        .expect_err("the selected environment is outside the context");
    assert!(matches!(error, ContextError::InvalidInput { .. }));
    assert_eq!(error.exit_code().code(), 12);
}

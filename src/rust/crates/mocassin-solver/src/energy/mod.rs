// SPDX-License-Identifier: MIT

//! Transition-energy evaluation over one [`SimulationContext`](crate::context::SimulationContext).
//!
//! Step 15 feature 1 adds the read-only KMC evaluation and the acceptance-exponential kernel. State
//! application and MMC exchange are separate features of the same concept module.

mod exponential;
mod transition;

pub use exponential::acceptance_exponential;
pub use transition::{EnergyEngine, TransitionEnergies};

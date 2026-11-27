// SPDX-License-Identifier: MIT

//! Transition-energy evaluation over one [`SimulationContext`](crate::context::SimulationContext).
//!
//! The read-only KMC evaluation, fixed-order state application, and acceptance-exponential kernel live
//! here. MMC exchange is a separate feature of the same concept module.

mod commit;
mod exponential;
mod path;
mod transition;

pub use exponential::acceptance_exponential;
pub use transition::{EnergyEngine, TransitionEnergies};

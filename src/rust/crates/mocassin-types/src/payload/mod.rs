// SPDX-License-Identifier: MIT

//! Serde mirrors of the `.msl` v1 payload maps.
//!
//! The field set is owned by `Mocassin.Contracts.Packages` and
//! `contracts/generated/schema/package-payloads-v1.schema.json`; Rust must not invent a second schema. Each type
//! decodes a MessagePack map with snake_case string keys. Readers ignore unknown keys, a missing required field
//! is a decode error, and nullable fields are `Option<T>`.
//!
//! Payloads are untrusted input: the `mocassin-msl` reader scans the MessagePack document against
//! [`crate::ReaderLimits`] before it decodes into these types, then validates indices, lengths, and values. These
//! structs only reproduce the stored shape; they perform no validation.

mod energy;
mod job;
mod lattice;
mod model;
mod simulation;
mod transition;

pub use energy::{ClusterTablePayload, EnergyPayload, PairTablePayload};
pub use job::{ExtensionPayload, JobPayload, SelectionOptimizerPayload};
pub use lattice::LatticePayload;
pub use model::{
    ClusterLinkPayload, EnvironmentPayload, ModelPayload, PairLinkPayload, ParticlePayload,
    PositionPayload, StructurePayload,
};
pub use simulation::{SimulationPayload, SimulationSettingsPayload, TrackerModelPayload};
pub use transition::{
    JumpCollectionPayload, JumpDirectionPayload, JumpRulePayload, TransitionPayload,
};

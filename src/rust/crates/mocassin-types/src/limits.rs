// SPDX-License-Identifier: MIT

//! Bounds applied by the `.msl` reader before it allocates or trusts a payload.
//!
//! The defaults mirror the .NET writer/verifier guards wherever those exist, and add solver allocation caps
//! where the writer has no equivalent (Step 13 design §3.3). A package that is valid but larger than one of the
//! solver-only caps is rejected with a structured error and needs a raised [`ReaderLimits`]; the reader does not
//! claim that every package the writer can produce is readable.
//!
//! The limits are a library parameter. Step 13 does not expose them on the command line.

/// Byte, element, depth, and per-field count limits for one package read.
///
/// A caller overrides only what it needs:
///
/// ```
/// use mocassin_types::ReaderLimits;
///
/// let limits = ReaderLimits {
///     max_rules: 2_000_000,
///     ..ReaderLimits::DEFAULT
/// };
/// assert_eq!(limits.max_rules, 2_000_000);
/// ```
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct ReaderLimits {
    /// Maximum decompressed model payload size in bytes. Default 512 MiB, matching the .NET verifier guard.
    pub max_model_payload_bytes: u64,

    /// Maximum decompressed job payload size in bytes. Default 8 MiB, matching the .NET verifier guard.
    pub max_job_payload_bytes: u64,

    /// Maximum decompressed lattice payload size in bytes. Default 512 MiB, matching the .NET lattice cap.
    pub max_lattice_payload_bytes: u64,

    /// MessagePack element budget for a model payload. Default 512,000,000.
    ///
    /// This covers a writer-valid 512 MiB payload with headroom; the reader still applies the per-field caps to
    /// the typed decode.
    pub max_model_elements: u64,

    /// MessagePack element budget for a job payload. Default 1,000,000.
    ///
    /// The job payload is a small fixed schema.
    pub max_job_elements: u64,

    /// Maximum MessagePack nesting depth. Default 8.
    ///
    /// The model nests map → structure → positions → position → `particle_ids` → scalar, at least five
    /// container levels; 8 leaves headroom without permitting a stack-exhaustion document.
    pub max_messagepack_depth: u32,

    /// Maximum number of P1 positions. Default 500,000, matching `SupercellGenerator.MaximumPositions`.
    ///
    /// The total lattice linear index must also fit the signed 32-bit limit; the reader checks that separately.
    pub max_positions: u32,

    /// Maximum number of positions in one unit cell (P). Default 10,000, matching
    /// `UnitCellVectorEncoder.MaxP1Positions`.
    pub max_position_count: u32,

    /// Maximum number of particles including void. Default 64, matching the six-bit state code and the
    /// 63-custom-particle limit.
    pub max_particles: u32,

    /// Maximum number of transition rules. Default 1,000,000, matching the **default**
    /// `CompilationOptions.MaxRuleCount`.
    ///
    /// That option is `init` and a caller can raise it; a package built with a raised limit needs a raised
    /// [`ReaderLimits::max_rules`].
    pub max_rules: u32,

    /// Maximum number of jump directions. Default 1,000,000. Solver allocation cap; the writer has no
    /// equivalent.
    pub max_directions: u32,

    /// Maximum number of jump collections. Default 4,096. Solver allocation cap; the writer has no equivalent.
    pub max_collections: u32,

    /// Maximum number of pair-interaction tables. Default 1,000,000. Solver allocation cap.
    pub max_pair_tables: u32,

    /// Maximum number of cluster-interaction tables. Default 1,000,000. Solver allocation cap.
    pub max_cluster_tables: u32,

    /// Cumulative typed-bytes budget for the cluster `code_expansions` arrays, `Σ(8E + 4I)`. Default 128 MiB.
    ///
    /// The stored integers use MessagePack's smallest-width encoding, so the model payload byte guard does not
    /// bound the typed `u64`+`u32` arrays. The scan checks this budget before the arrays are allocated; a
    /// writer-valid package above it needs a raised limit. For a valid map (`E == I = N`) the derived entry cap
    /// is 11,184,810.
    pub max_cluster_expansion_bytes: u64,

    /// Maximum total number of environment pair links. Default 16,000,000. Solver allocation cap.
    pub max_pair_links: u32,

    /// Maximum number of pair links on one position. Default 65,536. Solver allocation cap.
    pub max_pair_links_per_position: u32,

    /// Maximum number of cluster links on one position. Default 4,096. Solver allocation cap.
    pub max_cluster_links_per_position: u32,

    /// Maximum number of nodes in one jump path. Default 10, matching the six-bit node slots of a `u64` state
    /// code.
    pub max_path_nodes: u32,

    /// Maximum number of extension configuration entries in a job payload. Default 256.
    pub max_extensions: u32,

    /// Maximum number of simulations in a model payload. Default 4,096.
    pub max_simulations: u32,
}

impl ReaderLimits {
    /// The documented defaults (Step 13 design §3.3).
    pub const DEFAULT: Self = Self {
        max_model_payload_bytes: 512 * 1024 * 1024,
        max_job_payload_bytes: 8 * 1024 * 1024,
        max_lattice_payload_bytes: 512 * 1024 * 1024,
        max_model_elements: 512_000_000,
        max_job_elements: 1_000_000,
        max_messagepack_depth: 8,
        max_positions: 500_000,
        max_position_count: 10_000,
        max_particles: 64,
        max_rules: 1_000_000,
        max_directions: 1_000_000,
        max_collections: 4_096,
        max_pair_tables: 1_000_000,
        max_cluster_tables: 1_000_000,
        max_cluster_expansion_bytes: 128 * 1024 * 1024,
        max_pair_links: 16_000_000,
        max_pair_links_per_position: 65_536,
        max_cluster_links_per_position: 4_096,
        max_path_nodes: 10,
        max_extensions: 256,
        max_simulations: 4_096,
    };
}

impl Default for ReaderLimits {
    fn default() -> Self {
        Self::DEFAULT
    }
}

#[cfg(test)]
mod tests {
    use super::ReaderLimits;

    #[test]
    fn defaults_match_the_documented_table() {
        let limits = ReaderLimits::DEFAULT;
        assert_eq!(limits.max_model_payload_bytes, 536_870_912);
        assert_eq!(limits.max_job_payload_bytes, 8_388_608);
        assert_eq!(limits.max_lattice_payload_bytes, 536_870_912);
        assert_eq!(limits.max_model_elements, 512_000_000);
        assert_eq!(limits.max_job_elements, 1_000_000);
        assert_eq!(limits.max_messagepack_depth, 8);
        assert_eq!(limits.max_positions, 500_000);
        assert_eq!(limits.max_position_count, 10_000);
        assert_eq!(limits.max_particles, 64);
        assert_eq!(limits.max_rules, 1_000_000);
        assert_eq!(limits.max_directions, 1_000_000);
        assert_eq!(limits.max_collections, 4_096);
        assert_eq!(limits.max_pair_tables, 1_000_000);
        assert_eq!(limits.max_cluster_tables, 1_000_000);
        assert_eq!(limits.max_cluster_expansion_bytes, 134_217_728);
        assert_eq!(limits.max_pair_links, 16_000_000);
        assert_eq!(limits.max_pair_links_per_position, 65_536);
        assert_eq!(limits.max_cluster_links_per_position, 4_096);
        assert_eq!(limits.max_path_nodes, 10);
        assert_eq!(limits.max_extensions, 256);
        assert_eq!(limits.max_simulations, 4_096);
    }

    #[test]
    fn default_trait_returns_the_documented_defaults() {
        assert_eq!(ReaderLimits::default(), ReaderLimits::DEFAULT);
    }

    #[test]
    fn a_caller_can_raise_one_limit_without_touching_the_rest() {
        let limits = ReaderLimits {
            max_rules: 2_000_000,
            ..ReaderLimits::DEFAULT
        };
        assert_eq!(limits.max_rules, 2_000_000);
        assert_eq!(limits.max_positions, 500_000);
        assert_eq!(limits.max_model_payload_bytes, 536_870_912);
    }
}

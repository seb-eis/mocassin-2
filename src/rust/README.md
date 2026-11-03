# Rust workspace

The workspace is rooted at [`Cargo.toml`](Cargo.toml). Crates live under [`crates/`](crates/):

| Crate | Responsibility |
| --- | --- |
| `mocassin-types` | `serde` types for compiled package and state metadata; no I/O |
| `mocassin-msl` | Read `.msl` SQLite, verify version/signature, decode payloads |
| `mocassin-rng` | Deterministic, platform-independent RNG streams |
| `mocassin-solver` | Simulation context, energy engine, KMC, MMC, trackers, statistics |
| `mocassin-state` | MessagePack `.mcs` schema, writer, and reader |
| `mocassin-extension` | Extension trait and C ABI for optional custom routines |
| `mocassin-cli` | `mocassin-sim` argument parsing, logging, signals, exit codes |

The solver never reads project JSON and never writes `.msl`/`.msr`. See
[`docs/planning/01-target-architecture.md`](../../docs/planning/01-target-architecture.md) §4.

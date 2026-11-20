# `mocassin-msl` fuzz targets

This is the Step 13 `cargo-fuzz` workspace for the package reader. It is **outside** the main Rust workspace and
is not built by `cargo test`; it uses the standard `libfuzzer-sys` harness.

| Target | Path | What it exercises |
| --- | --- | --- |
| `payload_scan` | `fuzz_targets/payload_scan.rs` | The bounded model-payload scan (reached through `decode_model_payload`) |
| `payload_decode` | `fuzz_targets/payload_decode.rs` | The job and lattice typed decode paths |
| `package_open` | `fuzz_targets/package_open.rs` | Read-only SQLite open, schema checks, and one-job load over the fuzz bytes |

The corpus starts from the real `msl-v1` model/job/lattice payloads and the real `.msl` package. A target run must
never panic or allocate past the documented `ReaderLimits` envelope.

## Running

```sh
cargo +nightly fuzz build --fuzz-dir src/rust/crates/mocassin-msl/fuzz
cargo +nightly fuzz run --fuzz-dir src/rust/crates/mocassin-msl/fuzz payload_scan -- -max_total_time=10
cargo +nightly fuzz run --fuzz-dir src/rust/crates/mocassin-msl/fuzz payload_decode -- -max_total_time=10
cargo +nightly fuzz run --fuzz-dir src/rust/crates/mocassin-msl/fuzz package_open -- -max_total_time=10
```

Step 13 runs a short time-boxed smoke run only. Continuous/nightly fuzzing, corpus growth, and crash triage are
Step 29 work; do not add a scheduled CI job for it.

The committed corpus contains only the real-fixture seeds. A run writes newly discovered units next to them;
remove those before committing unless a unit is deliberately promoted to a regression seed.

## Step 13 run (2026-11-20, x86_64-unknown-linux-gnu, nightly, 5 s per target)

| Target | Runs | Average exec/s | Crashes |
| --- | ---: | ---: | --- |
| `payload_scan` | 186,312 | 31,052 | 0 |
| `payload_decode` | 158,648 | 26,441 | 0 |
| `package_open` | 6,535 | 1,089 | 0 |

The run used `-max_total_time=5 -print_final_stats=1`; peak RSS stayed below 420 MiB.

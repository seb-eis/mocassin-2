# Tests

| Directory | Contents |
| --- | --- |
| [`contracts/`](contracts/README.md) | Cross-language `.mocprj`/`.msl`/`.msr`/`.mcs`, job-folder, and event fixtures |
| [`reference/`](reference/README.md) | Reference builds, observables, and statistical parity assets |

Unit, property, and integration tests live with their projects (`src/dotnet/*.Tests`, Rust crate tests,
`src/python/tests`). Fixtures here are shared across languages and must be produced by the real writers.

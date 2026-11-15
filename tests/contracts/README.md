# Contract fixtures

Versioned cross-language fixtures for the formats defined in
[`docs/planning/02-data-contracts.md`](../../docs/planning/02-data-contracts.md):

- `project-*.mocprj` — canonical project JSON written by the real project writer
  (`ProjectJson.SerializeToUtf8Bytes`); includes the Step 01 envelope fixtures plus the Step 03–07
  particle/structure, symmetry-metadata, transition, energy, lattice/simulation, and validation-report
  fixtures; mirrored by the `samples/blank` and `samples/ceria` files;
- `model-*.msgpack.zst` — compiled model payloads written by .NET and read by Rust;
- `package-*.msl`, `package-*.msr` — package/results stores written by the real writers;
- `state-*.mcs` — state documents written by Rust and read by .NET/Python;
- `job-folder/` — a real solver job folder;
- `events-*.jsonl` — machine-readable event streams;
- `signature-vectors.json` — canonical projection bytes and SHA-256 digests;
- `api-baseline/net11.0/` — checked-in assemblies for the `ApiCompat` gate.
- `packages/msl-v1/` — the checked-in `.msl` v1 fixture written by the amended .NET 11 writer, its
  `package-summary-v1` golden, the extracted payloads, and the env-gated regeneration test.
- `packages/amend-1-net10/` — a real `.msl` written by the unamended `net10.0` writer, its
  `package-summary-v1` golden, and provenance; a compatibility fixture that is never regenerated.
- `packages/amend-1/` — the pre-amendment codec goldens: raw payloads (`payloads/*.bin`), `ZstdSharp.Port`
  frames at levels 3 and 9 (`golden/*.zst`), and `goldens.json`. Bytes are an observation and may be
  re-captured with a recorded note; the level → `Quality` mapping does not change.

The `.gitignore` contains explicit exceptions so `*.mcs`, `*.msl`, `*.msr`, `*.log`, and `Job*/` fixtures are
tracked. Do not hand-author binary fixtures; regenerate them with the real writer and record the command and
format version. Refresh the API baseline only with `tools/update-api-baseline.sh` after an intentional public
API change.

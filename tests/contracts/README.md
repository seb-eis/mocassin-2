# Contract fixtures

Versioned cross-language fixtures for the formats defined in
[`docs/planning/02-data-contracts.md`](../../docs/planning/02-data-contracts.md):

- `project-*.mocprj` — canonical project JSON;
- `model-*.msgpack.zst` — compiled model payloads written by .NET and read by Rust;
- `package-*.msl`, `package-*.msr` — package/results stores written by the real writers;
- `state-*.mcs` — state documents written by Rust and read by .NET/Python;
- `job-folder/` — a real solver job folder;
- `events-*.jsonl` — machine-readable event streams;
- `signature-vectors.json` — canonical projection bytes and SHA-256 digests.

The `.gitignore` contains explicit exceptions so `*.mcs`, `*.msl`, `*.msr`, `*.log`, and `Job*/` fixtures are
tracked. Do not hand-author binary fixtures; regenerate them with the real writer and record the command and
format version.

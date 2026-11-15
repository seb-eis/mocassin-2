# Pre-amendment codec golden frames (amendment 1)

These are the **`ZstdSharp.Port 0.8.8` frames** for the real `.msl` payloads, captured before the codec swap
(`step-11-amend-1` §9.4). They make the quality-mapping equivalence test possible after the third-party package is
removed: the in-box .NET 11 encoder must reproduce these frames at the same level.

The frames are a byte-level observation, **not** a format contract. ADR 0010 and the data contracts exclude
compressed bytes from canonical signatures and from `package-summary-v1`, so a later runtime/Zstd patch may change
the bytes without changing the format or the readable content. If that happens, re-capture `golden/**` and
`goldens.json` with a recorded note; the level → `Quality` mapping does not change.

## Layout

- `payloads/<id>.bin` — the raw MessagePack payload, extracted from a package built by the unamended writer and
  decompressed with `zstd` 1.5.7.
- `golden/<id>-l<level>.zst` — the `ZstdSharp` frame at level 3 or 9 for that raw payload.
- `goldens.json` — the machine-readable manifest: payload id, kind, origin, raw size and SHA-256, frame level,
  size and SHA-256, the capture commit, and the `sample-model-1` seed vector.

Payload ids and kinds:

| id | kind | origin | raw bytes |
| --- | --- | --- | ---: |
| `sample-model-1` | model | authored sample | 3,475 |
| `sample-lattice-3`, `sample-lattice-4`, `sample-lattice-6` | lattice | authored sample | 2,109 |
| `sample-lattice-1`, `sample-lattice-2`, `sample-lattice-5` | lattice | authored sample | 925 |
| `sample-job-2`, `sample-job-4`, `sample-job-5`, `sample-job-6` | job | authored sample | 509–544 |
| `supplementary-lattice-1` | lattice | supplementary (40×40×40, real pipeline) | 256,063 |

The supplementary payload is real pipeline output from the same compiled model and generator, expanded to a
40×40×40 supercell so the corpus contains one package-scale payload. It is not part of the authored sample.

## Capture provenance

- Commit `aa481143aff254e0a7faf2828e7dc150c2d82833`, SDK `10.0.403` / runtime `10.0.14`,
  `ZstdSharp.Port 0.8.8`, `zstd` CLI `1.5.7`.
- The measurement report and raw JSON are throwaway evidence under `/tmp/opencode/part-a/results/`; the durable
  record is `docs/planning/amendments/step-11-amend-1-dotnet-11-native-zstd.md` §9.1/§9.4.
- On the measured runtime the .NET 11 native encoder produced **byte-identical** frames at the same level for all
  12 payloads (24/24). Do not turn that observation into a hard format promise.

## Test

The codec-swap feature adds a PR-fast test that reads every `payloads/*.bin`, compresses it with the package
encoder at levels 3 and 9, and compares the frame to `golden/<id>-l<level>.zst`. The `sample-model-1` seed vector
is 3,475 B → 1,415 B at level 3 and 1,387 B at level 9.

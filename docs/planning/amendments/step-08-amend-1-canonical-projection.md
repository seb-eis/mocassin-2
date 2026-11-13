# Step 08 Amendment 1 — Canonical Compiled-Model Projection

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-08-amend-1` |
| Parent step | 08 — Model Compilation Pipeline |
| Status | Planned |
| Branch | `feature/step-08-model-compilation-pipeline/amend-1-canonical-projection` |
| Design review | `docs/planning/reviews/step-08-amend-1.md` |
| Parent audit | `docs/planning/audits/step-08.md` |
| Integration commit | — |
| Combined audit commit | — |
| Tag | `step-08` |
| Date | 2026-11-13 |
| Owner | Human owner |

Parent step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

## Rationale

The Step 08 audit finding **A2** is a Blocker. The frozen design says the model signature is a canonical
MessagePack projection (ADR 0010) whose field order and types come from `contracts/manifest.json`. The
manifest only declared an opaque `formatVersion` plus four `bin` sections, and the implementation wrote a
custom `BinaryWriter` layout. That layout is not reproducible from the manifest, uses non-canonical integer and
string encodings, and cannot be shared with Step 11 or the Rust solver.

This is a definition gap, not an ordinary bug: the nested projection schema was never written down. The
amendment defines it and replaces the temporary encoder. The parent audit is paused at `Fixes required`; the
amendment is integrated before the combined freeze and re-audit.

## Scope

- Define the complete canonical `signatures.model` projection layout in `contracts/manifest.json`.
- Generate a machine-readable projection descriptor from the manifest.
- Replace the temporary custom encoder with a canonical MessagePack encoder built on the existing
  `MessagePack` package.
- Add an independent Python re-encoding vector so C# and a second implementation agree.
- Regenerate the model signature golden vectors and `compiled-inspection-v1` JSON hashes.

## Non-goals

- No change to the `ParameterizedModel` payload (Step 09), `.msl` SQLite schema (Step 11), or solver
  behaviour.
- No change to the compiled geometry, rules, conservation, tracking, or simulation semantics.
- No new dependency outside the centrally pinned `MessagePack` 3.1.12 package.

## Design

### Manifest projection

`signatures.model.fields` becomes an explicit, ordered projection. `object` fields are MessagePack arrays in
manifest field order; `array` fields are MessagePack arrays; only `map` fields use MessagePack maps. The
top-level projection is:

| # | Field | Type |
| ---: | --- | --- |
| 0 | `formatVersion` | `uint64` |
| 1 | `symmetryContractVersion` | `string` |
| 2 | `spaceGroupDataVersion` | `string` |
| 3 | `spaceGroupDataSha256` | `string` |
| 4 | `particles` | `array` of `{index uint64, id string, symbol string, charge float64, isVacancy bool}` |
| 5 | `structure` | `{cellVectors array<array<float64>>, volume float64, sites array<...>, environments array<...>, positions array<...>, interactionRange array<int64>}` |
| 6 | `energy` | `{pairGeometries array<...>, pairTables array<...>, clusterTables array<...>}` |
| 7 | `transitions` | `{collections array<...>, directions array<...>, rules array<...>, globalTrackers array<...>, staticTrackers array<...>, jumpCounts array<int64>, jumpOffsets array<int64>, jumpMappings array<int64>}` |
| 8 | `simulations` | `array` of `{id string, kind int64, activeCollections array<uint64>, globalTrackers array<uint64>, staticTrackers array<uint64>}` |

The implementation adds the exact nested field lists to the manifest. The current compiled-model record
fields define them:

- `CompiledStructureModel`: cell vectors, volume, sites, environments, positions, interaction range.
- `CompiledEnergyModel`: directed pair geometry IDs/energy IDs/indices/offsets/buckets/chirality, pair table
  IDs/counts, cluster energy IDs/geometry/occupation codes/tuples/expansions/particle mapping.
- `CompiledTransitionModel`: collection identity/kind/mobile mask/direction and rule indices, direction
  stable ID/collection/start/path/movement/path vector/inverse/equivalent starts, rule stable ID/collection/
  directions/state codes/movement/charge vector/tracker order/inverse, tracker definitions, jump tables.
- `CompiledSimulation`: ID, kind, active collections, tracker indices. Runtime settings and energy/frequency
  values are excluded.

### Canonical encoding

- Add `MessagePack` 3.1.12 to `Mocassin.Compilation`. The package is MIT-compatible, centrally pinned, and
  already used by the solution; this is not a `Mocassin.Contracts` dependency.
- Implement `CanonicalProjectionWriter` using `MessagePackWriter` for array/string/bin framing and
  fixed-width raw integers for canonical `uint64`/`int64` values.
- Encode every projection as a MessagePack array in manifest order; encode optional values as `nil`; encode
  `float64` values with `-0.0` normalized to `+0.0`; reject non-finite required values.
- Keep the SHA-256 domain separation `mocassin/model-signature/v1 || 0x00 || projection`.
- Remove the temporary `BinaryWriter` projection. There is no compatibility shim; no compiled model or
  `.msl` package has been released.

### Cross-language evidence

- Extend `tools/contracts/generate.py` to emit the model projection descriptor from the manifest.
- Add a Python canonical encoder/checker (`msgpack` for framing plus fixed-width integer headers) that reads
  the generated descriptor and compares a real ceria projection digest to the C# signature vector.
- Keep the C# golden signature and JSON inspection hash in `tests/contracts/signature-vectors.json`.

### Versioning

Keep `signatures.model` / `ModelSignatureV1` / `model-v1`: this is the first definition of the compiled-model
projection and no `.msl` package has been released. The generated descriptor and golden vectors are updated;
Step 11 reuses the same projection. Any later released-format change requires a version bump and an ADR.

## API and contract impact

- `contracts/manifest.json` and generated `contracts/generated/signatures/model-v1.json` change.
- `tests/contracts/signature-vectors.json` changes.
- `ModelSignatureProjection` remains the public signing entry point; its bytes and signature values change.
- No PublicAPI change; no ApiCompat baseline change. The `MessagePack` package reference is a new project
  dependency and is covered by the licence/vulnerability audit.
- `docs/reference/contracts.md`/`docs/development/compilation-pipeline.md` describe the projection and the
  independent vector.

## Temporary implementation and migration

The current `BinaryWriter` projection is temporary and fails the audit. The amendment replaces it. No
compatibility shim is required because the compiled-model JSON inspection is not a persisted format and no
`.msl` package exists yet. Any scratch caches are per-run or caller-owned and must be invalidated by the new
projection.

## Acceptance criteria

- [ ] The manifest defines every canonical projection field with name, type, order, and nested section layout.
- [ ] The generator emits the projection descriptor and `--check` is current.
- [ ] The C# encoder uses the standard MessagePack package with fixed-width canonical integers.
- [ ] An independent Python implementation reproduces the ceria signature vector.
- [ ] Golden signatures and inspection JSON hashes are regenerated and deterministic.
- [ ] Energy values, runtime settings, timings, diagnostics, and file names remain excluded.
- [ ] All local gates pass, including `api` and `contracts`.

## Tests

| Test | Assertion |
| --- | --- |
| Manifest schema validation | Every projection field has a supported type and order; generator check passes |
| C# canonical encoding | Format version is uint64; strings/bin/arrays use MessagePack; -0.0 normalized |
| Independent Python vector | Python re-encoding of the ceria projection matches the C# signature |
| Determinism | Two compiles produce identical bytes/signature/JSON |
| Scope exclusion | Runtime/temperature and energy-value edits do not change the signature |
| Golden refresh | Ceria/MMC/vehicle/interstitialcy signatures are pinned |
| Cache integrity | A projection-keyed cache entry with the wrong shape is rejected with CMP012 |

## Documentation

- [ ] `docs/development/compilation-pipeline.md` describes the manifest-driven projection and encoder.
- [ ] `docs/planning/02-data-contracts.md` §1.3/§3.3 references the explicit projection.
- [ ] `docs/planning/adr/0010-canonical-messagepack-signatures.md` notes the shared encoder and descriptor.
- [ ] Step page, amendment index, status, parity, audit request, and review record updated.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Manifest schema becomes too large to maintain | Generate the descriptor and validate nested fields in `contracts` tests |
| C# and Python encoders diverge | Independent vector is part of `contracts` and fails on mismatch |
| Signature values change again | Pre-release, no persisted `.msl`; golden vectors regenerated once |
| MessagePack package adds a dependency | MIT-compatible, already pinned and audited; no Contracts dependency |
| Nested arrays are misordered | Manifest order is the only order; no maps for structs; golden vectors pin it |

## Dependencies

- Parent step remains active and paused at `Fixes required`.
- The parent audit must be re-run at the combined commit after integration.
- Steps 09/11 consumers must use the same `model-v1` projection; Step 11 reuses the manifest descriptor.

## Integration

- Branch: `feature/step-08-model-compilation-pipeline/amend-1-canonical-projection`
- Parent step branch: `step/08-model-compilation-pipeline`
- Approval: pending
- Squash commit: pending
- Parent audit request updated: pending
- Combined frozen commit: pending

## Audit

- Parent audit record: `docs/planning/audits/step-08.md`
- Audit verdict: Fixes required (A2)
- Merge commit: pending
- Tag: pending
- Post-merge fallback required: no

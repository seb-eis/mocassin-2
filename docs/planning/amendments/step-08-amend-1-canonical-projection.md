# Step 08 Amendment 1 — Canonical Compiled-Model Projection

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-08-amend-1` |
| Parent step | 08 — Model Compilation Pipeline |
| Status | Ready for implementation (round 9, no findings) |
| Branch | `feature/step-08-model-compilation-pipeline/amend-1-canonical-projection` |
| Design review | `docs/planning/reviews/step-08-amend-1.md` (rounds 1–9; round 9 Ready for implementation, no findings) |
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
- Add an independent Python conformance checker so the canonical bytes/types can be verified by a second implementation.
- Regenerate the model signature golden vectors and `compiled-inspection-v1` JSON hashes.

## Non-goals

- No change to the `ParameterizedModel` payload (Step 09), `.msl` SQLite schema (Step 11), or solver
  behaviour.
- No change to the compiled geometry, rules, conservation, tracking, or simulation semantics.
- No new .NET dependency outside the centrally pinned `MessagePack` 3.1.12 package; the Python checker adds a
  new dev-only `msgpack==1.2.4` requirement in `tools/contracts/requirements.txt`.

## Design

### Manifest projection

`signatures.model.fields` becomes an explicit, ordered projection. `object` fields are MessagePack arrays in
manifest field order; `array` fields are MessagePack arrays; `map` fields are the only MessagePack maps and
none are used in v1. Every nested object is an array with the field order below.

Top-level projection (MessagePack array of 9 values):

| # | Field | Type |
| ---: | --- | --- |
| 0 | `formatVersion` | `uint64` |
| 1 | `symmetryContractVersion` | `string` |
| 2 | `spaceGroupDataVersion` | `string` |
| 3 | `spaceGroupDataSha256` | `string` |
| 4 | `particles` | `array<particle>` |
| 5 | `structure` | `structure` |
| 6 | `energy` | `energy` |
| 7 | `transitions` | `transitions` |
| 8 | `simulations` | `array<simulation>` |

#### Particles

`particle` = `[index uint64, id string, symbol string, charge float64, isVacancy bool]`.

#### Structure

`structure` = `[cellVectors array<array<float64>>, volume float64, sites array<site>,
environments array<environment>, positions array<position>, interactionRange array<uint64>]`.

- `cellVectors`: exactly three arrays of three `float64` values.
- `interactionRange`: exactly four `uint64` values.
- `site` = `[index uint64, id string, fractional array<float64> (3), stability int64,
  particleSetId string, environmentIndex uint64, positionIndices array<uint64>]`.
- `environment` = `[index uint64, siteIndex int64 (sentinel -1 for the shared stable environment),
  stability int64, allowedParticleMask uint64,
  updateParticleIndices array<uint64>]`.
- `position` is an 11-element array: `[p uint64, encoding encoding, fractional array<float64> (3), siteIndex uint64,
  siteId string, stability int64, allowedParticleMask uint64, environmentIndex uint64, orbitIndex uint64,
  pairLinks array<pairLink>, clusterLinks array<clusterLink>]`.
- `encoding` = `[cellA int64, cellB int64, cellC int64, p uint64]`. The position encoding is retained as the
  canonical base-cell `(0,0,0,p)`; the writer asserts `Encoding.P == P` and zero cell offsets.
- `pairLink` = `[pairTableIndex uint64, targetP uint64, offset encoding, distanceBucketFm uint64]`.
- `clusterLink` = `[clusterTableIndex uint64, siteSlot uint64]`.

#### Energy

`energy` = `[pairGeometries array<pairGeometry>, pairTables array<pairTable>,
clusterTables array<clusterTable>]`.

- `pairGeometry` = `[geometryId string, energyId string, pairTableIndex uint64, centerSiteIndex uint64,
  centerP uint64, partnerSiteIndex uint64, partnerP uint64, offset encoding, distanceBucketFm uint64,
  isChiral bool, chiralPartnerGeometryId string|nil]`.
- `pairTable` = `[index uint64, energyId string, particleCount uint64]`. Energy values are excluded.
- `clusterTable` = `[clusterEnergyId string, clusterId string, centerSiteIndex uint64, centerP uint64,
  sitePIndices array<uint64>, geometryEncodings array<encoding>, occupations array<occupation>,
  particleTableMapping array<uint64>]`. Cluster energy values are excluded.
- `occupation` = `[canonicalCode uint64, canonicalTuple array<uint64>,
  expansions array<[occupationCode uint64, permutationIndex uint64]>]`.

#### Transitions

`transitions` = `[collections array<collection>, directions array<direction>, rules array<rule>,
globalTrackers array<globalTracker>, staticTrackers array<staticTracker>, jumpCounts array<uint64>,
jumpOffsets array<uint64>, jumpMappings array<uint64>]`.

- `collection` = `[index uint64, transitionKind int64, transitionId string, kind int64,
  mobileParticleMask uint64, directionIndices array<uint64>]`.
- `movement` has one three-component `float64` vector per path node. The outer length equals `path.Length`.
- `direction` = `[objectId string, collectionIndex uint64, positionId uint64, path array<encoding>,
  movement array<array<float64>>, isInverse bool, equivalentStartPositions array<uint64>]`.
- `rule` `movement` has one three-component `float64` vector per path node; its outer length equals the
  representative direction path length.
- `rule` = `[ruleGeometryId string, collectionIndex uint64, directionIndices array<uint64>,
  state0 uint64, state1 uint64, state2 uint64, movement array<array<float64>>,
  chargeTransportVector array<float64> (3), trackerOrder array<uint64>, isInverse bool]`.
  The default attempt frequency is a constant and is excluded.
- `globalTracker` = `[index uint64, collectionIndex uint64, particleIndex uint64]`.
- `staticTracker` = `[index uint64, positionIndex uint64, particleIndex uint64]`.

Dropped/derived fields and rationale:

| Field | Treatment | Rationale |
| --- | --- | --- |
| `collection.ruleIndices` | excluded | Derived from `rule.collectionIndex`; rule array order is canonical. |
| `rule.directionIndex` | excluded | `directionIndices` carries every merged direction; the dense representative index is not a stable identity. |
| `direction.index`, `rule.index` | excluded | Array position is the dense index; `ObjectId`/`RuleGeometryId` are the stable identities. |
| `clusterTable.index` | excluded | Array position is the dense index; `ClusterEnergyId` is the stable identity. |
| pair `CenterSiteId`/`PartnerSiteId` | excluded | The structure site table and site indices carry the identity; duplicate GUIDs would add no information. |
| `simulation.intent` | excluded | `UseDualDofCorrection`/`UsePreRun` are derived from kind/MCSP and runtime settings; Step 10/11 own the job flags. |
| `direction.pathVector`, `pathLength` | excluded | Display/tracking only and derivable from `movement` plus the cell vectors; the signed `movement` and rule `chargeTransportVector` carry the physics. |
| `StaticCorrection` | excluded in v1 | Null in v1; a versioned field when the S2 correction lands. |

#### Simulations

`simulation` = `[id string, kind int64, activeCollections array<uint64>,
globalTrackers array<uint64>, staticTrackers array<uint64>]`. Runtime settings, temperatures, MCSP, field,
pre-run, break settings, job counts, seeds, and instructions are excluded.

### Canonical encoding

The writer is `Mocassin.Compilation.Signatures.CanonicalProjectionWriter` (internal). It uses
`MessagePackWriter` over an `IBufferWriter<byte>` for buffer management, the typed `WriteUInt64`/`WriteInt64`
and `Write(double)` writers for the fixed-width scalars, and `WriteRaw` for the forced
`0xdd`/`0xdb`/`0xc6`/`0xc3`/`0xc2`/`0xc0` headers.
It must not call the compact `WriteArrayHeader`/`WriteString` convenience methods or the minimal-width
`Write(ulong)`/`Write(long)` overloads; those would break the required header bytes. Fixed-width scalars use
the typed writers; all forced headers use `WriteRaw`/`Advance`. The byte rules are:

| Value | Encoding |
| --- | --- |
| array | `0xdd` + 4-byte big-endian count |
| map | not used in v1 |
| string | `0xdb` + 4-byte big-endian UTF-8 byte length + UTF-8 bytes |
| binary | `0xc6` + 4-byte big-endian length + bytes |
| `uint64` | `0xcf` + 8-byte big-endian value |
| `int64` | `0xd3` + 8-byte big-endian two's-complement value |
| `float64` | `0xcb` + 8-byte big-endian IEEE-754 bits |
| `bool` | `0xc3` (true) / `0xc2` (false) |
| nil | `0xc0` |

- The per-field table above is authoritative. As a summary: non-negative counts, dense indices, particle
  masks, state codes, tracker indices, jump counts/offsets/mappings, distance buckets, cluster canonical
  tuples/mappings, and interaction-range cell counts are `uint64`. Signed `int64` is used only for spatial
  cell offsets, stability/kind enum values, and the -1 sentinel in `environment.siteIndex` only.
  Pair/cluster site indices are non-negative and therefore `uint64`.
- All projected floating-point values are `float64`. The language-neutral quantization operation is:
  `scaled = value * 1e12`; if `scaled >= 0`, `rounded = floor(scaled + 0.5)`, else
  `rounded = ceil(scaled - 0.5)`; `result = rounded / 1e12`; `result == 0` is normalized to `+0.0`.
  This is round-half-away-from-zero on the binary64 product, not the language `round` function. NaN and Inf
  are rejected. Pin `5e-13 → 1e-12`, `-4e-13 → 0.0`, `0.30000000000000004 → 0.3`, and `-0.0 → 0.0` in both
  C# and Python. This amendment updates parent Step 08 §11.4 and `compilation-pipeline.md` from
  “charge transport vectors are quantized” to “all projected float64 values are quantized”.
- Every field is present. Optional strings use `nil`; no field is omitted.
- The projection is the top-level array above. The signature is
  `SHA-256("mocassin/model-signature/v1" || 0x00 || projection)`.
- `StaticCorrection` is absent from `model-v1`: v1 stores `null` and Step 15 uses the dynamic S2 path. If a
  static correction lands, a new projection version adds a `staticCorrection` field.
- The distance bucket is unsigned; only signed `movement` and the signed rule charge vector carry physics.
- No compression, timestamps, diagnostics, timings, cache state, file names, or display-only distances
  (`DistanceAngstrom`, `pathLength`) are encoded; the raw display distance and path length are
  inspection-only.

### Cross-language projection fixtures

`CompiledModelJson.Write` remains the human inspection projection and does not carry raw projection input: JSON
cannot preserve the `uint64`/`int64`/`float64` distinction (`2.0` may round-trip as `2`, `-0.0` as `-0`).

Instead, a golden fixture generator writes, for each sample, `tests/contracts/projections/<id>.bin` (the exact
canonical MessagePack projection bytes) and `tests/contracts/projections/<id>.json`:

```json
{
  "fixtureFormat": "canonical-projection-v1",
  "id": "model-ceria-kmc",
  "descriptor": "model-v1",
  "signatureHex": "…",
  "projectionSha256": "…"
}
```

`<id>` is the vector id (`model-ceria-kmc`, `model-ceria-mmc`, `model-vehicle5`, `model-interstitialcy5`,
`model-barium-zirconate`); the descriptor is always `model-v1`. The fixture is generated by
`ProjectionFixtureTests.WriteProjectionFixtures` (a C# test using the real
compiler); regenerate with:

```sh
MOCASSIN_UPDATE_GOLDENS=1 dotnet test src/dotnet/Mocassin.Compilation.Tests/Mocassin.Compilation.Tests.csproj -c Release \
  --filter FullyQualifiedName~ProjectionFixtureTests.WriteProjectionFixtures
```

`ProjectionFixtureTests` writes only when `MOCASSIN_UPDATE_GOLDENS=1`; in a normal test run it compares the
checked-in `.bin`/`.json` files with a fresh compile and fails on drift. `projectionSha256` is the lowercase
SHA-256 of the raw `.bin` bytes. The descriptor is the generated
`contracts/generated/signatures/model-v1.json`; the
`ProjectionDescriptorTests` reads that same `model-v1.json`, not a second descriptor, and compares its recursive
field names/order/types with the internal `ProjectionLayout` table used by the encoder. The files are tracked; they are not a package format.

`tools/contracts/generate.py` emits the complete `model-v1` projection descriptor (nested field names, order,
and scalar types) in the existing `contracts/generated/signatures/model-v1.json`; there is no separate
projection file. The Python checker
`tools/contracts/check_model_projection.py`:

1. reads `contracts/generated/signatures/model-v1.json` and each `.bin` fixture;
2. decodes the MessagePack tree with `msgpack` 1.2.4 (declared in `tools/contracts/requirements.txt`);
3. walks the descriptor to recover scalar types (uint64/int64/float64) and re-encodes the tree using fixed-
   width `struct.pack` headers and the byte rules above;
4. compares the re-encoded bytes with the fixture, recomputes the domain-separated signature and
   `projectionSha256`, and compares both, plus `fixtureFormat`, with the `.json` metadata and
   `signature-vectors.json`.

`tools/contracts/requirements.txt` is added with `msgpack==1.2.4`. `tools/verify.sh run_contracts` installs it
with `python -m pip install -r tools/contracts/requirements.txt` before running
`python tools/contracts/check_model_projection.py --check`. `tools/contracts/README.md` documents the checker
and fixture regeneration; `docs/planning/06-toolchain-requirements.md` moves the `msgpack` row to Step 08
(dev-only, also used by Step 22).

The checker runs in `./tools/verify.sh contracts`, so a descriptor/encoder drift fails the normal gate. The
fixtures cover ceria KMC, ceria MMC, vehicle5, interstitialcy5, and barium zirconate. Step 11 remains the owner
of the production Rust encoder and vector; Python is an independent pre-Step-11 check.

GUID-valued IDs (`ParticleId`, `ParticleSetId`, `CellSiteId`, `ClusterId`, transition IDs, `SimulationId`) are
encoded as lowercase `Guid.ToString("D", CultureInfo.InvariantCulture)` text (`8-4-4-4-12`, hyphens,
lowercase hex). Content-hash IDs (`GeometryId`, `EnergyId`, `ClusterEnergyId`, `ObjectId`,
`RuleGeometryId`) are the lowercase 64-character SHA-256 strings exactly as produced; they are never parsed as
GUIDs. The fixture `fixtureFormat` is distinct from the compiled-model `formatVersion` and the inspection
`formatVersion`.

### Versioning

Keep `signatures.model` / `ModelSignatureV1` / `model-v1`: this is the first definition of the compiled-model
projection and no `.msl` package has been released (`02-data-contracts.md` §3.3 records the same pre-release
precedent for `model_signature`). The generated descriptor and golden vectors are updated;
Step 11 reuses the same projection. Any later released-format change requires a version bump and an ADR.

## API and contract impact

- `contracts/manifest.json` and generated `contracts/generated/signatures/model-v1.json` change.
- `tests/contracts/signature-vectors.json` changes.
- `ModelSignatureProjection` remains the public signing entry point; its bytes and signature values change.
- No public API change; `CanonicalProjectionWriter` is internal and no ApiCompat baseline changes. The
  `MessagePack` package reference is a new project dependency and is covered by the licence/vulnerability
  audit.
- `contracts/generated/README.md` and `docs/development/compilation-pipeline.md` describe the projection and
  the conformance fixtures.

## Temporary implementation and migration

The current `BinaryWriter` projection is temporary and fails the audit. The amendment replaces it. No
compatibility shim is required because the compiled-model JSON inspection is not a persisted format and no
`.msl` package exists yet. Any scratch caches are per-run or caller-owned and must be invalidated by the new
projection.

## Acceptance criteria

- [ ] The manifest defines every canonical projection field with name, type, order, and nested section layout.
- [ ] The generator emits the complete projection descriptor and `--check` is current; the Python checker is in `verify.sh contracts`.
- [ ] The C# encoder uses the standard MessagePack package with fixed-width canonical integers.
- [ ] An independent Python conformance checker reproduces all five projection fixtures and signatures; it is
  a byte/type verifier, not the production model encoder.
- [ ] Golden signatures and inspection JSON hashes are regenerated and deterministic; the manifest, `signature-vectors.json`, `.bin`/`.json` fixtures, and C# goldens all contain the same five samples.
- [ ] Energy values, runtime settings, timings, diagnostics, and file names remain excluded.
- [ ] All local gates pass, including `api` and `contracts`.

## Tests

| Test | Assertion |
| --- | --- |
| Manifest schema validation | Every projection field has a supported type and order; generator check passes |
| Descriptor binding | The generated descriptor’s recursive field order/types match the encoder’s projection tree |
| C# canonical encoding | Format version is uint64; strings/bin/arrays use fixed MessagePack headers; bool/nil are `0xc3`/`0xc2`/`0xc0`; -0.0 normalized |
| Optional values | A chiral pair with a partner and one without encode string vs nil reproducibly |
| Fixed-width integers | Python decodes/encodes uint64/int64/float64 per the descriptor; 2.0 stays float64 |
| Quantization midpoint vector | `5e-13→1e-12`, `-4e-13→0.0`, `0.30000000000000004→0.3`, `-0.0→0.0` match in C# and Python |
| Independent Python vectors | Python re-encoding matches the `.bin` fixtures for ceria, MMC, vehicle, interstitialcy, and BaZrO₃ |
| Determinism | Two compiles produce identical bytes/signature/JSON |
| Display-only exclusion | Changing a display distance without changing its bucket does not change the signature |
| Scope exclusion | Runtime/temperature and energy-value edits do not change the signature |
| Golden refresh | Ceria/MMC/vehicle/interstitialcy/BaZrO₃ signatures are pinned and fixture freshness is checked |
| Cache integrity | A projection-keyed cache entry with the wrong shape is rejected with CMP012 |

## Documentation

- [ ] `docs/development/compilation-pipeline.md` describes the manifest-driven projection and encoder.
- [ ] `docs/planning/02-data-contracts.md` §1.3/§3.3 references the explicit projection.
- [ ] `docs/planning/adr/0010-canonical-messagepack-signatures.md` notes the shared encoder and descriptor.
- [ ] `tools/contracts/README.md` documents the `msgpack` requirements, checker, and fixture regeneration.
- [ ] The generated `contracts/generated/README.md` describes the projection and fixtures.
- [ ] `docs/planning/06-toolchain-requirements.md` records the dev-only `msgpack` row and Step 08 use.
- [ ] `./tools/verify.sh contracts` installs `tools/contracts/requirements.txt` and runs the projection checker.
- [ ] Step page, amendment index, status, parity, audit request, and review record updated.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Manifest schema becomes too large to maintain | Generate the descriptor and validate nested fields in `contracts` tests |
| C# and Python encoders diverge | Independent conformance checker is part of `contracts` and fails on mismatch |
| Signature values change again | Pre-release, no persisted `.msl`; golden vectors regenerated once |
| MessagePack package adds a dependency | MIT-compatible, centrally pinned and approved; no Contracts dependency |
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
- Parent audit request updated: after integration; refresh `docs/planning/closeout/step-08.json`, run
  `./tools/verify.sh all --require`, dispatch one replacement closeout CI run, and apply the merge-tree rule if
  `main` moves
- Combined frozen commit: pending; re-audit scope is A1, A3–A9 and minors m1–m7 in addition to A2

## Audit

- Parent audit record: `docs/planning/audits/step-08.md`
- Audit verdict: Fixes required (A1–A9)
- Merge commit: pending
- Tag: pending
- Post-merge fallback required: no

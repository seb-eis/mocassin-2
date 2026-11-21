# Step 11 Amendment 2 — Cluster Occupation Code Expansion Map

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-11-amend-2` |
| Parent step | 11 — Simulation Package and `.msl` Writer |
| Status | Planned — design review requested 2026-11-21 |
| Branch | `docs/step-11-amend-2` (design); implementation branch created after approval |
| Design review | `docs/planning/reviews/step-11-amend-2.md` |
| Parent audit | Step 11 audit is historical (`docs/planning/audits/step-11.md`); the post-merge amendment audit is `docs/planning/audits/step-11-amend-2.md` (to be created) |
| Integration commit | — (planned: `--no-ff` merge into `main`) |
| Combined audit commit | — |
| Tag | `step-11.1` (proposed; `step-11` is never moved) |
| Date | 2026-11-21 |
| Owner | Human owner |

Parent step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

## Rationale

Step 14's design review round 3 raised **F-29 (Major)**: the frozen `.msl` cluster table stores one
`CanonicalCode` per symmetry-reduced occupation orbit, but the solver builds the raw slot tuple of the current
lattice and needs the orbit's stored **energy** for it. The payload carries neither the orbit expansion map nor
the induced permutation group, so a raw tuple that is not the canonical representative has no entry and fails
closed as `MissingOccupationCode`. A perfectly valid package is therefore diagnosed as corrupt data.

The defect is real and reproducible with the primary sample. `samples/ceria/ceria.mocprj` has a two-member
cluster (centre at the unstable oxygen transition site, offsets `[-0.25, 0, 0]` and `[+0.25, 0, 0]`) in space
group 225. All three sites admit `{O, Vc}`. The centre mirror swaps the two member slots, so the eight raw
tuples reduce to six canonical orbits: four tuples are invariant under the swap (two per centre particle) and
the other four form one non-trivial pair per centre particle. Compiling the sample yields `OccupationCount == 6`
and non-empty `Expansions` (`CeriaClusterCompilesToSortedCanonicalOccupations`). With compiled particle indices
`1 = Ce`, `2 = O`, `3 = Vc`, the eight raw codes are:

| Centre particle | Member tuple | Raw code | Canonical code | Stored today |
| ---: | --- | ---: | ---: | --- |
| 2 | `(2, 2)` | 8322 | 8322 | yes |
| 2 | `(2, 3)` | 12418 | 12418 | yes |
| 2 | `(3, 2)` | 8386 | 12418 | **no** |
| 2 | `(3, 3)` | 12482 | 12482 | yes |
| 3 | `(2, 2)` | 8323 | 8323 | yes |
| 3 | `(2, 3)` | 12419 | 12419 | yes |
| 3 | `(3, 2)` | 8387 | 12419 | **no** |
| 3 | `(3, 3)` | 12483 | 12483 | yes |

The table is a "member tuple = `(slot 1, slot 2)`" view of the full code `c + 64·p1 + 4096·p2`. The two failing
codes, `8386` and `8387`, are **numerically smaller** than their canonical codes, `12418` and `12419`, because
the reduction's canonical order is lexicographic in **site order starting at the centre**, while the 6-bit code
puts slot 0 in the least significant bits. A solver cannot recover the canonical code by sorting the slot
particles or by taking the minimum code in the orbit; it would have to re-run the point-group reduction.

The original solver avoided the problem by storing every equivalent code: `CreateAllGroupLookupCodesOnModel`
builds `GroupLookupCodes` from all `OccupationStates`, and the solver looks up the exact runtime code. The
canonical-only `.msl` table is a regression for exactly the non-P1 multi-member case. The checked fixtures hid
it: `step-13-populated` is P1 (every orbit is a singleton), and the Step 14 multi-centre fixture has one member.

The owner has decided that `.msl` must let a reader resolve **any valid raw cluster slot tuple** (one whose
particles are allowed by the table's site masks) to its stored canonical occupation code, by adding an
**additive expansion map** to the model payload. The alternative of recomputing symmetry in the solver is
excluded: the Rust solver has no space-group catalog, and a second reduction implementation would be a parity
risk. A limitation-only resolution is also excluded: the primary ceria cluster would stay un-runnable for two
of eight assignments.

## Scope

**In scope**

- One optional, additive model-payload field per cluster table: `code_expansions`, mapping every raw slot-tuple
  code the writer enumerated to its canonical occupation index.
- The writer path that produces it from the existing `CompiledClusterOccupation.Expansions` (already computed
  by `ClusterReduction` and reached through `ClusterCompilationStage`), with duplicate and coverage control.
- The `.NET` payload DTO, the write-time byte estimate, the read-only verifier check (`PKG010`), and the
  `package-summary-v1`/contract boundary (unchanged).
- The Rust payload DTO, reader validation, and runtime lookup accessor in `mocassin-types`/`mocassin-msl`.
- The format and version policy: optional field, `format_version = 1`, absent-field behaviour, the ADR 0013
  addendum, and the fixture policy.
- Fixtures: a new non-P1 two-member cluster package built from the primary ceria geometry, the regenerated
  `step-13-populated` package, a byte-fixed pre-map package for the absent-field path, and the assertion that
  packages without cluster tables are byte-identical.
- The Step 14 consumption contract (raw tuple to canonical index, fail-closed behaviour) and the cross-step
  benchmark requirement.
- The post-merge `step-11.1` process: maintenance branch, focused design review, separate audit, tag, and the
  document updates in this page and the parent step page.

**Non-goals**

- No runtime symmetry computation in Rust; no space-group catalog in the solver; no second reduction kernel.
- No new template identity or parameterization semantics. Templates still key on the canonical occupation
  tuple; the map is a resolution table, not an authored value.
- No change to `signatures.model`, `signatures.parameterization`, or `signatures.lattice`; no new signed field.
- No `format_version` bump while `.msl` v1 is unreleased (see §7). If a `.msl` has been released before
  implementation, this amendment stops and the owner decides a major-version change.
- No change to `occupation_codes`, `energies`, `particle_table_mapping`, the SQLite schema, the job or lattice
  payloads, `.msr`, `.mcs`, the job-folder contract, or the extension ABI.
- No new `PKG` diagnostic code; `PKG010` ("A package payload value is out of bounds") covers the structural
  violations.
- No new `ReaderLimits` field. The map is bounded by the model payload byte guard, the element budget, and the
  MessagePack declared-length check (see §6.3).
- No change to the Step 11 audit record or `docs/planning/closeout/step-11.json`; both are historical.
- No Step 14 implementation code. Step 14 consumes the field; its design re-review closes F-29 by reference.

## Design

### 1. Decision

For every cluster table in the model payload, the writer emits an optional expansion map:

```text
code_expansions = nil
               | { "expanded_codes": [u64, ...], "canonical_indices": [u32, ...] }
```

`expanded_codes` is the strictly ascending set of every raw slot-tuple code the writer enumerated for the
table. `canonical_indices[j]` is the index `i` such that `expanded_codes[j]` belongs to the orbit of
`occupation_codes[i]`; `energies[i]` is that orbit's applied energy. The map is nested in each
`cluster_tables[i]` object, so every table has its own code space.

`nil` means "every orbit is a singleton, so `occupation_codes` is already complete". The writer emits `nil` iff
the total expansion count equals the canonical occupation count; otherwise it emits the map. A pre-amendment
v1 package has no `code_expansions` key at all; a reader treats absent and `nil` identically.

Stored map key order is **not** a contract. The current writer places `code_expansions` after `occupation_codes`;
readers must accept any key order.

### 2. Why this shape

- **Exact and solver-simple.** One binary search over a sorted `u64` array plus one index into `energies`; no
  permutation application, no group data, no allocation in the loop. The hot path stays flat-array based.
- **Compact.** 12 bytes per raw tuple (`u64` code + `u32` index). The grouped alternative (per-canonical code
  lists plus `occupation_count + 1` offsets) is 8 bytes per tuple plus 8 bytes per canonical code, larger for
  the common case and requiring a second search. A nested array of per-canonical lists has the same size but no
  global ordering invariant; the single sorted array gives duplicate and coverage checks for free.
- **Self-validating.** Strict global ascending makes duplicates impossible at the format level. The canonical
  self-entry rule (`occupation_codes[i]` must appear with `canonical_indices[j] == i`) proves that no canonical
  code is missing from its own map, so every canonical code is resolvable.
- **Optional.** Singleton tables (all P1 and most one-member clusters) carry `nil`; the map cost is paid only
  where reduction actually created non-trivial orbits.
- **Already signed.** The Step 08 model signature projects `occupations[].expansions` with the full occupation
  code and permutation index (`ModelSignatureProjection` and
  `contracts/manifest.json` → `signatures.model...clusterTables[].occupations[].expansions`). The payload field
  is a serialization of data the model signature already binds; it is not a new signed concept.

Rejected alternatives:

| Option | Why rejected |
| --- | --- |
| Solver-side symmetry reduction | Duplicates the exact space-group kernel and catalog in Rust; parity and drift risk; violates the F-29 requirement not to re-introduce runtime symmetry computation. |
| Store the induced permutation group only | The reader would apply the group at load or runtime; that is the same symmetry computation in another place, and the writer/reader must agree on the operation order and canonical comparison. |
| Store one expansion code per canonical code without indices | Cannot express an orbit larger than one without a second index array; either loses exactness or needs the same index. |
| Record a limitation and reject non-canonical tuples | The primary ceria cluster would fail for two of eight assignments; a writer-valid package would be reported as corrupt. |
| Store `GroupLookupCodes` as a separate top-level table | Breaks the per-table code space and duplicates the canonical index; a nested map is the same data with a local key. |

### 3. Producer

The reduction already computes the answer:

- `ClusterReduction.Reduce` enumerates every allowed tuple, canonicalizes under the centre stabilizer's induced
  site permutations, and stores, per canonical cluster, the sorted, de-duplicated set of equivalent codes
  (`ClusterOccupation.Expansions`; `ClusterBuilder.Add` de-duplicates through a `HashSet<ulong>`).
- `ClusterCompilationStage` re-encodes each expansion with the centre particle at slot 0
  (`CompiledClusterOccupation.Expansions`) and sorts occupations by canonical code.

`EnergyPayloadMapper` builds the map from that data; it does not recompute symmetry:

```text
pairs = []
for i in 0..table.Occupations.Length:
    for expansion in table.Occupations[i].Expansions:
        pairs.add((expansion.OccupationCode, i))
sort pairs by code
assert every code is strictly ascending            // no duplicate raw tuple
assert every table.Occupations[i].CanonicalCode appears with canonical index i
if pairs.Count == table.Occupations.Length: map = nil
else: map = { expanded_codes = codes, canonical_indices = indices }
```

**Coverage proof (`GroupLookupCodes` equivalent).** Let $G_0$ be the centre stabilizer's induced permutation
group and $O(t)$ the orbit of tuple $t$ under $G_0$. `Reduce` visits every allowed tuple $t$, computes the
lexicographic minimum $c(t)$, and adds $O(c(t))$ to the canonical cluster for $c(t)$. Because $c(t) \in O(t)$
and $G_0$ is a group, $O(c(t)) = O(t)$; and because the identity is always in `InducedPermutations`
(`GetPointGroup` adds the identity operation, whose permutation is the identity), $c(t) \in O(c(t))$. The union
over all visited tuples is therefore exactly the set of enumerated tuples, each canonical cluster's expansion
list contains its own canonical code, and distinct canonical clusters have disjoint orbits. The map is the
union of those lists with the canonical index attached, which is the `GroupLookupCodes` coverage of the
original, restricted to the writer's allowed tuple set.

**Duplicate control.** A raw tuple belongs to exactly one orbit, so the union must be a function. The writer
sorts globally and rejects a non-strictly-ascending sequence or a code claimed by two different canonical
indices with `InvalidOperationException`: that is an internal compiler/reduction invariant, not a user error.
The same code with the same index is de-duplicated by the per-orbit set; the global sort also collapses it.

**Byte estimate.** `ClusterCompilationStage` currently estimates `8 × OccupationCount` bytes of cluster-table
data. It adds `12 × Σ|Expansions|` for the map so the existing `CompilationOptions.MaxEnergyTableBytes` check
covers the encoded field. The default (2 GiB) is not changed. A table at the cap can still exceed the solver's
512 MiB model-payload guard; that is the pre-existing energy-table/payload-guard relationship, not a new
failure mode, and the reader rejects the package with `PKG004`/the Rust byte guard rather than allocating past
it.

### 4. Encoding and validation rules

For one table, with `G = occupation_codes.Length`, `N = expanded_codes.Length`, and `particle_count` from the
model payload:

1. `G = energies.Length`; this amendment does not change the existing `occupation_codes`/`energies` equality.
2. `code_expansions` is either `nil` or a map with exactly the two keys `expanded_codes` and
   `canonical_indices`.
3. `N = canonical_indices.Length`.
4. `expanded_codes[0] < expanded_codes[1] < ... < expanded_codes[N-1]` strictly.
5. `canonical_indices[j] < G` for every `j`.
6. For every $j$, `expanded_codes[j] >> 60 == 0` and each of its ten 6-bit fields is `< particle_count`.
7. For every $i$ there is exactly one $j$ with `expanded_codes[j] == occupation_codes[i]` and
   `canonical_indices[j] == i` (canonical self-entry). Exactly one follows from strict ascending.
8. `N == G` iff every orbit is a singleton; the writer emits `nil` in that case and the map otherwise. A reader
   accepts a map with `N == G` too (it is structurally valid and means the identity map).
9. `nil` and absent mean "no map". For a singleton table, `occupation_codes` is complete. For a table with a
   non-singleton orbit, absence is a package built before this amendment or a writer defect; the reader must
   fail closed on a raw tuple that is not canonical rather than guess (see §6.2).

The encoding does not carry a slot count. It does not need one: the code is self-describing up to the ten
6-bit fields, high slots are zero (void), and the reader already validates `occupation_codes` with the same
ten-slot rule. The writer knows the table's slot count from `SitePIndices`.

**Stored encoding.** `code_expansions` is a MessagePack map with the two keys above; the values are MessagePack
arrays of integers. The .NET writer uses MessagePack's smallest-width integer encoding, as it does for the rest
of the stored payload; a reader accepts every integer width for the target type (`u64` for codes, `u32` for
indices). This is the stored map, not a canonical projection: ADR 0010's fixed-width rule applies only if a
future signed projection includes the field, which this amendment explicitly does not add.

### 5. `.NET` surface

- `Mocassin.Contracts.Packages` gains a payload DTO for the nested map (for example
  `CodeExpansionPayload(ImmutableArray<ulong> ExpandedCodes, ImmutableArray<uint> CanonicalIndices)` with
  `[MessagePackObject]` keys `expanded_codes` and `canonical_indices`).
- `ClusterTablePayload` gains a `[Key("code_expansions")]` member of that nullable type. The existing
  four-argument primary constructor is preserved as an overload (delegating with `null`) so ApiCompat does not
  report a removed constructor. Existing keys are not renamed or reordered. If MessagePack-CSharp needs an
  explicit `[SerializationConstructor]` with the overload present, add it and cover the chosen shape with a
  payload round-trip test.
- `EnergyPayloadMapper` always writes the key: `nil` for singleton tables, the map otherwise.
- `PackageVerificationChecks.ValidateModel` applies §4 rules 2–7 and 9 to every table and appends the existing
  `PKG010` error text. It does not recompute symmetry and does not check orbit equivalence; the map is
  writer-asserted like `occupation_codes`.
- `PackageSummaryBuilder` and `package-summary-v1` are unchanged: the summary has `clusterTableCount`, not codes
  or payload bytes.
- `PublicAPI.Unshipped.txt` gains the new type, its members, and the property. Adding a property and an
  overload is ApiCompat-compatible; removing the old constructor would not be, which is why the overload stays.
- XML documentation states the slot order, the canonical-index meaning, the `nil` semantics, and the absence
  rule.

### 6. Rust reader and Step 14 lookup

#### 6.1 Payload and runtime types

- `mocassin-types::payload::ClusterTablePayload` gains
  `#[serde(default)] pub code_expansions: Option<CodeExpansionPayload>`; serde ignores unknown fields, so a
  pre-amendment package deserializes with `None` and a pre-amendment reader ignores the new key.
- The runtime `ClusterTable` gains `expansions: Option<ClusterExpansions>`; `ClusterExpansions` holds the two
  arrays and exposes `canonical_index(raw_code) -> Option<u32>` (binary search).
- `ClusterTable::canonical_index(raw_code) -> Option<u32>` uses the map when present, otherwise binary-searches
  `occupation_codes`. That single accessor is what Step 14 and Step 15 call.
- Adding a public field to `ClusterTable` and a public type is a Rust API addition (exhaustive struct literals in
  the workspace tests are updated). `mocassin-types` is pre-1.0; record the change in the amendment and the
  Step 13 page's API note. No `.msl` format consequence.

#### 6.2 Lookup and fail-closed semantics

Step 14 builds the raw code exactly as it does today: slot 0 is the centre particle, slots `1..=n` are the
resolved member particles, packed with 6 bits per slot, slot 0 least significant. Then:

- map present and `canonical_index(raw)` returns `Some(i)` → `code_id = i`, energy = `energies[i]`;
- map present and `None` → a data error (`MissingOccupationCode`, exit 12), unchanged;
- map absent and `occupation_codes.binary_search(raw)` succeeds → that index. For a singleton table this is
  complete and correct;
- map absent and the raw code is not canonical → a **distinct** fail-closed error (proposed
  `ExpansionMapAbsent`/recompile-required, exit 12) naming the table and the raw code and telling the user the
  package predates the expansion map and must be rebuilt. Do not fall back to another code, do not sort the
  slot particles, do not take a minimum.

Step 14 owns the error enum, wording, and tests; this amendment fixes the semantics. The fail-closed rule is
the only safe absence behaviour because a reader cannot distinguish "old package" from "writer emitted a
partial map".

#### 6.3 Bounds

No new `ReaderLimits` field is needed:

- The MessagePack scanner's `check_declared(length, 1)` already rejects a declared array length that cannot fit
  in the remaining bytes, and `max_model_elements` bounds the element count.
- Every `expanded_codes` entry occupies at least 8 payload bytes, so `N <= model_payload_bytes / 8` per table
  and in total; the typed allocation is at most `12N ≤ 1.5 × model_payload_bytes`, inside the existing 512 MiB
  guard.
- The reader validates §4 in a single pass over the two ascending arrays; coverage uses a merge with
  `occupation_codes`, so it is $O(N + G)$ with no set allocation.
- A malformed map (length mismatch, non-ascending, duplicate, index out of range, high bits, particle out of
  range, missing canonical self-entry) is a `PackageError` naming `energy.cluster_tables.code_expansions` and
  the offending field; the solver exits 11 (package error), not 12.

### 7. Signature and version policy

**Signatures.** No signed projection changes:

- `signatures.model` already includes `canonicalCode`, `canonicalTuple`, and every `expansion.occupationCode`
  and `permutationIndex`. Adding the stored field does not change the compiled model, the projection bytes, or
  the digest. The amendment is a serialization of signed data, not a new signed concept.
- `signatures.parameterization` is the six-field `[formatVersion, modelSignature, pairTables, groupSets,
  siteEnergies, transitionRules]` projection. It contains applied values in canonical order and no cluster
  codes. The map is geometry-derived, not a value, so it is excluded.
- `signatures.lattice` is unchanged.
- `package-summary-v1` excludes payload byte hashes and has no code-level fields, so it is unchanged.
- `contracts/manifest.json` and `contracts/generated/**` are unchanged: the manifest's model `energy` member is
  an open object (`additionalProperties: true`), as it already is for `occupation_codes`, `energies`, and
  `particle_table_mapping`. The inner structure is specified in
  [`02-data-contracts.md` §3.3](../02-data-contracts.md#33-compiled-model-payload) and
  [`msl-format.md`](../../reference/msl-format.md#payload-contents). Tightening the manifest to enumerate
  payload-internal energy fields is a separate contract-tooling change and is recorded as a gated decision in
  [`05-open-decisions.md`](../05-open-decisions.md#3-gated-decisions), not done here.
- `./tools/verify.sh contracts` must stay green with **no** `contracts/**` diff. A diff is a contract change and
  a review finding, not an expected consequence.

**Version.** The field is additive and optional, and `.msl` v1 is **unreleased**:

- `formats.package.payloads.formatVersion` stays `1`; the model payload `format_version` stays `1`.
- This uses the same pre-release basis that ADR 0009 records for the package shape and ADR 0010 records for the
  signature layout: no released `.msl` exists, so an additive optional field may enter v1 without a major bump.
  The amendment adds an explicit **additive-within-a-major** rule to ADR 0013 (an addendum, not a rewrite) so
  the policy is not an unwritten exception.
- **Compatibility direction.** A new reader reads an old v1 package without the field (defined absence above). A
  pre-amendment v1 reader reads a new package and ignores the unknown key (the payload unknown-field policy is
  `ignore`; MessagePack-CSharp ignores unknown map keys and the Rust scanner skips them). The latter is proven
  once by a migration check (§9.4), not by a permanent second reader.
- **Fixtures.** This is a current-major change: regenerate the current-major fixture that contains a cluster
  table, keep a byte-fixed pre-map v1 package for the absence path, and add the new two-member fixture. No
  previous-major fixture is created because no previous major exists. At the first released `.msl`, v1 freezes;
  any later field change is a new major with a previous-major reader.
- **Stop condition.** If a `.msl` is published or declared released before implementation, do not ship this as
  an additive v1. Record the situation and ask the owner for a major-version decision. Until then, the
  pre-release delta is defensible and is recorded here.

**Old packages with a non-singleton cluster.** A package built between Step 11 and Step 11.1 that contains a
two-member cluster has no map and cannot resolve its non-canonical tuples. It is not silently mis-read: the
Step 14 lookup fails closed with the recompile-required error. The documented fix is to rebuild the package,
consistent with "`.msl` is immutable; rebuild for a new package". This is a pre-release limitation, not a
supported long-term compatibility mode.

### 8. Documentation and contract reconciliation

| Page | Change |
| --- | --- |
| [`02-data-contracts.md` §3.3](../02-data-contracts.md#33-compiled-model-payload) | Add `code_expansions` to the `energy`/`cluster_tables` description; add the slot order, coverage, `nil`/absence, signature, and bound rules; update the "codes are canonical" bullet to name the expansion map. |
| [`02-data-contracts.md` §3.4](../02-data-contracts.md#34-job-payload) | Solver read-bounds paragraph: the map is bounded by the model guard/element budget and needs no new limit. (The table lives under §3.3; the read-bounds list is in §3.4.) |
| [`01-target-architecture.md`](../01-target-architecture.md) | §3.1: the compiled-model payload carries the cluster occupation expansion map so the solver resolves raw tuples without symmetry; §7.6: additive optional payload fields within a major, with defined absence. |
| [`ADR 0013`](../adr/0013-format-compatibility-and-api-versioning.md) | Addendum recording additive-within-a-major payload fields, absence semantics, and the boundary at the first release. |
| [`05-open-decisions.md`](../05-open-decisions.md) | Resolved row for the expansion-map decision; gated row for enumerating payload-internal energy fields in the manifest. |
| Step 11 page | Amendment pointer and status; parent audit/closeout records stay historical. |
| Step 14 page | Cross-step note that F-29 is resolved by this amendment and that the Step 14 design must consume `canonical_index`; the authoritative design text is on the Step 14 design branch. |
| [`msl-format.md`](../../reference/msl-format.md) | Payload contents, the exact map shape, lookup, reader bounds, compatibility, and the signature/summary exclusion. |
| Fixture READMEs | Provenance, SHAs, the expected 8→6 expansion, and the pre-map compatibility copy. |
| Parity matrix | After implementation, the group/cluster row's evidence names the expansion map. |

### 9. Fixtures and tests

#### 9.1 New two-member fixture

- **Project:** a new committed sample, `samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj`,
  derived from `samples/ceria/ceria.mocprj` (the primary cluster is kept verbatim: centre
  `018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f07`, offsets `[-0.25,0,0]` and `[+0.25,0,0]`), with one small KMC job
  (2×2×2 or 1×1×1) so the package stays in the kilobyte range. A copy is used instead of the primary sample so
  the sample's tuning and the Step 13/14 fixture set do not move.
- **Location:** `tests/contracts/packages/step-11-amend-2/` with `step-11-amend-2.msl`, `.sha256`,
  `package-summary-v1.json`, `payloads/index.json`, `payloads/model-1.bin`, and `README.md` with provenance,
  the writer commit, SDK, `created_utc`, and the job count.
- **Expectation document:** `cluster-expansions-v1.json` (a test fixture, not a contract manifest artifact)
  records, for table 0: `occupation_codes` (the six canonical codes), `expanded_codes` (the eight raw codes),
  `canonical_indices`, and the two non-canonical raw codes `8386` and `8387` with their canonical index and
  canonical code (`12418` and `12419`). The test asserts the actual values produced by the compiled particle
  order; the table above is the expected result for `1 = Ce`, `2 = O`, `3 = Vc`.
- The package is generated once by the real `SimulationPackageWriter`, is deterministic for the fixed seed and
  timestamp, and is never hand-edited.

#### 9.2 Regenerated and preserved fixtures

| Fixture | Cluster tables | Action | Reason |
| --- | --- | --- | --- |
| `tests/contracts/packages/step-13-populated/` | 1 (P1, 12 singleton codes) | Regenerate `.msl`, `.sha256`, `payloads/model-1.bin`, `payloads/index.json`; keep `package-summary-v1.json` and assert it is unchanged | The cluster table gains `code_expansions: nil`; the logical summary must not move |
| `tests/contracts/packages/amend-2-pre-map/` (new) | 1 (P1, 12 singleton codes) | Byte-fixed copy of the pre-change `step-13-populated.msl`, its `.sha256`, payload index, and a README | Proves the absent-field path on a real cluster table for the life of v1; never regenerated |
| `tests/contracts/packages/msl-v1/` | 0 | No regeneration; assert byte identity on rebuild | No cluster table, so no new key |
| `tests/contracts/packages/step-13-centered/` | 0 | No regeneration; assert byte identity on rebuild | No cluster table |
| `tests/contracts/packages/amend-1-net10/` | 0 | Never regenerated | Pre-amendment compatibility fixture (amendment 1) |
| `tests/contracts/packages/amend-1/payloads/`, `golden/`, `goldens.json` | 0 | No change | Codec goldens are byte-fixed and the model payload has no cluster table |

If a `0`-cluster fixture's rebuilt bytes differ, the difference is investigated before any golden is updated;
it must not be accepted as "the writer changed".

#### 9.3 Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Writer map for the ceria cluster | PR-fast | Table 0 emits `N = 8`, `G = 6`; strict ascending; `8386` and `8387` resolve to the canonical indices of `12418` and `12419`; every canonical code self-maps |
| Writer singleton table | PR-fast | `step-13-populated`'s table emits `code_expansions = nil` |
| Writer duplicate/coverage guard | PR-fast | A synthetic table with a raw code claimed by two canonical indices, or a missing canonical self-entry, throws `InvalidOperationException` |
| Writer byte estimate | PR-fast | A small `MaxEnergyTableBytes` rejects a table whose map pushes it over the limit with `CMP` limit diagnostics; the estimate includes `12 × Σ\|Expansions\|` |
| `GroupLookupCodes` equivalence (property) | PR-fast | For random small clusters and random allowed sets, the emitted map is a total function from every tuple enumerated by the reduction oracle to exactly one canonical code, and no tuple is missing |
| .NET verifier | PR-fast | `PKG010` for length mismatch, non-ascending/duplicate, index out of range, high bits, particle out of range, missing canonical self-entry; a valid map verifies |
| .NET pre-map read | PR-fast | `amend-2-pre-map` and `amend-1-net10` verify and keep their recorded summaries; the DTO decodes `CodeExpansions == null` |
| Rust reader: new fixture | PR-fast | Loads; table 0 has 8 expanded codes and 6 indices; `canonical_index` returns the expected index for all eight raw codes, including the two previously failing ones |
| Rust reader: absent map | PR-fast | The pre-map fixture loads with `expansions == None`; all 12 canonical codes resolve by the direct path; a synthetic non-canonical code returns `None` |
| Rust reader: hostile map | PR-fast | Length mismatch, non-ascending, duplicate, index out of range, high bits, particle out of range, and missing canonical self-entry each return a `PackageError` naming `energy.cluster_tables.code_expansions` |
| Signature/summary invariance | PR-fast | The new fixture's `model_signature` and `signature` equal the values computed from the same project before the change; `signature-vectors.json` and the projection fixtures are unchanged; `verify.sh contracts` has no diff |
| Determinism | PR-fast | Two builds of the new fixture with the same request produce identical bytes and SHA-256 |
| Forward read (one-time) | PR-medium | The pre-amendment `.NET` verifier and Rust reader at the parent commit open the new fixture and ignore `code_expansions` (the .NET reader succeeds and the Rust scanner skips the unknown key); recorded in the feature log, not a recurring test |
| Step 14 lookup | Step 14 | The context resolves all eight assignments on the new fixture; an absent map on a non-canonical tuple returns the recompile-required error |

#### 9.4 Commands

The implementation gate is the normal local matrix: `./tools/verify.sh dotnet`, `rust`, `contracts`, `docs`,
`policy`, `structure`, `api`, then `./tools/verify.sh all --require` on the frozen amendment branch. The one
manual closeout CI dispatch runs the Ubuntu `CI` workflow at the freeze commit. No per-push CI is used. The
forward-read check builds the parent commit in a temporary worktree and runs its `mocassin`/reader test on the
new fixture; the result and the parent SHA are recorded in the feature log.

### 10. Post-merge path (`step-11.1`)

This is a post-merge amendment: Step 11 is merged and tagged `step-11`. `AGENTS.md` §3.11.8 and the
[amendment README](README.md#7-post-merge-fallback) apply.

1. **Design (this branch).** `docs/step-11-amend-2` from `main` at `bbc2d08`; this page, the review request, and
   the reconciled pages; `./tools/verify.sh docs --require` and `policy --require`; commits carry `[skip ci]`;
   no push, merge, or tag by the planner. The owner approves the design verdict and the start of implementation.
2. **Design merge.** Merge `docs/step-11-amend-2` into `main` with `--no-ff` only after owner approval (a
   documentation-only maintenance change). The merge commit is the design baseline.
3. **Implementation branch.** `fix/step-11.1-cluster-code-expansion` from the updated `main` (a short-lived
   maintenance branch under the post-merge fallback). One feature-sized delivery: writer + DTO + verifier +
   fixtures + Rust reader + docs. Commit with imperative messages; no step branch is created.
4. **Local gates and freeze.** Run `./tools/verify.sh all --require` on the branch head and record the exact
   commands/results. Freeze the branch head as the audit commit.
5. **Closeout CI.** Dispatch one manual `CI` workflow run (Ubuntu, path-filtered) on the frozen commit. Record
   the run ID/URL and the full `head_sha`. Platform CI is not required: no P/Invoke, native library, RID, path,
   GUI, or platform-conditional code is touched. Record "not run" with that rationale in the audit.
6. **Audit.** Create `docs/planning/audits/step-11-amend-2.md` from the
   [step audit template](../audits/step-audit-template.md), set `Ready for audit`, and ask the orchestrator to
   spawn a fresh auditor. The auditor reads this page, the diff, the fixtures, the data contracts, ADR 0010/0013,
   and the Step 14 F-29 finding, runs the gates, and writes only its record. A `Passed` (or `Passed with notes`
   accepted by the owner) verdict is required before merge.
7. **Merge and tag.** After owner approval, merge the maintenance branch into `main` with `--no-ff`, wait for the
   automatic `main` CI run to be green, then create the annotated tag `step-11.1` on the merge commit and push
   it. Never move or delete `step-11`. Delete the maintenance branch locally and remotely.
8. **Records.** Update this page (status `Merged`, audit commit, CI, merge commit, tag), the amendment index,
   the Step 11 page, and `status.md`. `docs/planning/closeout/step-11.json` and the Step 11 audit record are not
   rewritten.
9. **Step 14.** The Step 14 design re-review closes F-29 by reference to this amendment; Step 14's implementation
   consumes `ClusterTable::canonical_index`. No Step 14 code is part of `step-11.1`.

**Process gap to decide.** `tools/check_closeout_evidence.py` accepts only `step-NN` or `step-NN[a-z]`, so a
`step-11.1` closeout-evidence file would fail the checker. Until the owner decides otherwise, the post-merge
amendment's audited commit, closeout CI, `main` SHA, merge commit, and tag are recorded in the audit record and
this page only. Extending the checker/README to post-merge amendment IDs is a separate `docs/` process change,
not part of this amendment.

### 11. Sizing and complexity

**Class: Low.** One optional field in one payload object, one mapper function, one verifier branch, one Rust
DTO/validator/accessor, and a bounded set of fixtures. The difficult reasoning (orbit coverage) is already
performed by the Step 02/08 reduction and is proven by the property test. No step split is needed. The work is
one maintenance delivery plus its audit.

## Acceptance criteria

- [ ] `EnergyPayloadMapper` emits a complete, strictly ascending `code_expansions` map for the ceria two-member
      table; all eight raw tuples resolve; the two non-canonical codes map to their canonical indices and
      energies.
- [ ] Singleton tables emit `nil`; the writer never emits a partial, duplicate, or non-ascending map; a
      violation throws an internal invariant error.
- [ ] The property test proves `GroupLookupCodes`-equivalent coverage against the reduction oracle for random
      small clusters.
- [ ] The `.NET` verifier applies the structural rules and reports `PKG010` for every violation; a valid map
      verifies.
- [ ] The Rust reader validates the same rules, exposes `ClusterTable::canonical_index`, and rejects hostile
      maps with a field-specific `PackageError`.
- [ ] The new fixture is deterministic, carries the documented expectation, and its `package-summary-v1` and
      signatures are the logical values from before the change.
- [ ] The regenerated `step-13-populated` fixture's summary is unchanged; the byte-fixed pre-map fixture keeps
      its SHA; the no-cluster fixtures rebuild byte-identically or the difference is explained.
- [ ] The pre-amendment reader reads the new fixture in the one-time forward check.
- [ ] `signatures.model`, `signatures.parameterization`, `signatures.lattice`, `signature-vectors.json`,
      `tests/contracts/projections/**`, and `contracts/**` are unchanged; `verify.sh contracts` is green.
- [ ] `.NET` and Rust public API additions are recorded (`PublicAPI.Unshipped.txt`, ApiCompat, Rust API note);
      the old `ClusterTablePayload` constructor still exists.
- [ ] `./tools/verify.sh all --require` is green at the audit commit; the single closeout CI run is green; the
      main CI run is green before `step-11.1` is tagged.
- [ ] Documentation is updated (§8) and `./tools/verify.sh docs --require` passes.
- [ ] The Step 14 design re-review references this amendment and F-29 is closed by reference; the parity row's
      evidence is updated after implementation.

## Tests

The full matrix is in §9.3. The gate rows are: writer coverage/duplicate, `GroupLookupCodes` property,
verifier `PKG010`, Rust reader validation and lookup, signature/summary invariance, determinism, and the
one-time forward read.

## Documentation

- [ ] `02-data-contracts.md` §3.3/§3.4 updated with the field, coverage, absence, signature, and bounds rules.
- [ ] `01-target-architecture.md` §3.1/§7.6 updated.
- [ ] ADR 0013 addendum records additive-within-a-major payload fields.
- [ ] `05-open-decisions.md` resolved/gated rows updated.
- [ ] Step 11 page, amendment index, `status.md`, and Step 14 cross-step note updated.
- [ ] `msl-format.md` updated.
- [ ] Fixture READMEs and `tests/contracts/README.md` updated.
- [ ] Parity matrix evidence updated after implementation.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Payload growth for large cluster tables | `nil` for singleton tables; `12N` in the writer estimate; the existing 2 GiB energy-table cap; the reader byte guard and element budget; a large-table property test |
| Writer emits an incomplete or wrong map | Global sort + strict-ascending/coverage assertions; verifier `PKG010`; property test against the reduction oracle; fail-closed runtime lookup |
| Old package with a non-singleton cluster is unreadable | Fail closed with a recompile-required error, never a wrong energy; documented as a pre-release limitation; rebuild is the supported path |
| Signature or summary drift | Explicit no-change acceptance criteria and a fixture that compares the logical signatures/summary before and after; a diff is a blocking finding |
| API break in `Mocassin.Contracts` or `mocassin-types` | Preserve the old constructor; additive property/field/type; `PublicAPI.Unshipped.txt` and ApiCompat; Rust is pre-1.0 and the workspace literals are updated |
| Fixture churn hides a real change | Only the cluster-table fixture is regenerated; the no-cluster fixtures are asserted byte-identical; the pre-map package is byte-fixed |
| Process gap for `step-11.1` evidence | Recorded in the audit and this page; checker extension is a separate owner-decided process change |
| The amendment is implemented after a `.msl` release | Stop condition in §7: escalate for a major-version decision instead of shipping an additive v1 |

## Dependencies

- Step 11 `Done`, merged at `84ceb76`, tagged `step-11`; amendment 1 integrated. The parent audit is closed, so
  no audit is running and a post-merge amendment is the correct path.
- Step 13 `Done`, merged and tagged; the Rust reader exists and is bounded. No Step 13 re-open is needed for
  compatibility; its reader/runtime types gain the optional field.
- Step 14 design review round 3 record at `1ea8090` (reviewed design `87be9de`; branch
  `review/step-14-design-round-3`, not merged) records F-29. The Step 14 design must be corrected on its own
  branch to consume this map; this amendment supplies the format and reader contract, not the Step 14 context
  code.
- No released `.msl` (pre-release condition, §7).
- `contracts/manifest.json` is unchanged; `verify.sh contracts` must show no diff.

## Integration

- Branch: `docs/step-11-amend-2` (design, this branch); `fix/step-11.1-cluster-code-expansion` (implementation)
- Parent step branch: none (Step 11 is merged and deleted); maintenance branch from `main`
- Approval: human owner approves the design verdict, the implementation start, the merge, and the tag
- Squash/merge: implementation branch `--no-ff` into `main` after the audit
- Parent audit request updated: no parent audit; separate `docs/planning/audits/step-11-amend-2.md`
- Combined frozen commit: the implementation branch head at freeze

## Audit

- Parent audit record: `docs/planning/audits/step-11.md` (historical; not reopened)
- Amendment audit record: `docs/planning/audits/step-11-amend-2.md` (to be created)
- Audit verdict: pending
- Merge commit: pending
- Tag: `step-11.1` (proposed; `step-11` stays)
- Post-merge fallback required: yes

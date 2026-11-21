# Step 11 Amendment 2 — Cluster Occupation Code Expansion Map

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-11-amend-2` |
| Parent step | 11 — Simulation Package and `.msl` Writer |
| Status | Ready for audit — implementation branch frozen; design review round 4 `Ready for implementation` at `bf273ff`; A2-07 process change and the `step-11.1` path owner-approved 2026-11-21 |
| Branch | `fix/step-11.1-cluster-code-expansion` (implementation); `docs/step-11-amend-2` design merged into `main` as `2f3a6fe` |
| Design review | `docs/planning/reviews/step-11-amend-2.md` |
| Parent audit | Step 11 audit is historical (`docs/planning/audits/step-11.md`); the post-merge amendment audit is `docs/planning/audits/step-11-amend-2.md` |
| Integration commit | — (pending `--no-ff` merge into `main` after audit and owner approval) |
| Combined audit commit | — (the maintenance branch head at freeze) |
| Tag | `step-11.1` (proposed; `step-11` is never moved) |
| Date | 2026-11-21 |
| Owner | Human owner |

Parent step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

## Owner approvals (2026-11-21)

The human owner recorded these decisions on the Step 11 amendment gate:

- **A2-07 process change — approved.** A post-merge amendment's audit record
  (`docs/planning/audits/step-NN-amend-M.md`) is the single evidence source for the audited commit, closeout CI,
  `main` SHA, merge commit, merge-tree CI, platform CI, and tag; the parent `closeout/step-NN.json` stays
  historical and the closeout-evidence checker is unchanged. The audit/closeout pages and template are normative
  from this approval.
- **`step-11.1` implementation path — approved after the design gate.** Proceed on the maintenance branch from
  `main` with the separate amendment audit and the `step-11.1` tag only after a green `main` CI run; `step-11`
  is never moved.
- **Design corrections — directed.** Fix A2-R3-01 and A2-R3-02/A2-R3-03, refreeze, and run review round 4
  before implementation. This is not acceptance of the round-3 `Ready with notes` verdict.

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

- One optional, additive model-payload field per cluster table: `code_expansions`, covering every raw slot-tuple
  code the writer enumerated and mapping it to its canonical occupation index (the map is a superset; §3).
- The writer path that produces it from the existing `CompiledClusterOccupation.Expansions` (already computed
  by `ClusterReduction` and reached through `ClusterCompilationStage`), with duplicate and coverage control.
- The `.NET` payload DTO, the write-time byte estimate, a decode-time expansion budget, the read-only verifier
  check (`PKG010`), and the `package-summary-v1`/contract boundary (unchanged).
- The Rust payload DTO, an optional-key scan path, the `max_cluster_expansion_bytes` reader limit, validation,
  and the runtime lookup accessor in `mocassin-types`/`mocassin-msl`.
- The format and version policy: optional field, `format_version = 1`, fail-closed absent-field behaviour, the
  ADR 0013 addendum, and the fixture policy.
- Fixtures: a new non-P1 two-member cluster package built from the primary ceria geometry, the regenerated
  `step-13-populated` package, a byte-fixed **non-singleton** pre-map package for the real absent-field path,
  and the unchanged-payload/summary assertions for packages without cluster tables.
- The Step 14 consumption contract (raw tuple to canonical index, fail-closed behaviour) and the cross-step
  benchmark requirement; F-29's formal closure stays a Step 14 gate.
- The sample docs (`samples/README.md`, `samples/step-11-cluster-expansion/README.md`).
- The post-merge `step-11.1` process: maintenance branch, focused design review, separate audit, tag, the
  audit/closeout page amendment for evidence, and the document updates in this page and the parent step page.

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
- No change to `tools/check_closeout_evidence.py` in **this docs remediation**. The post-merge evidence gap is
  resolved by amending the audit/closeout pages and template (A2-07); extending the checker to `step-NN.M`
  remains a possible later process change, not part of this amendment.
- No change to the Step 11 audit record or `docs/planning/closeout/step-11.json`; both are historical.
- No Step 14 implementation code. Step 14 consumes the field; F-29's formal closure is a Step 14 gate.

## Design

### 1. Decision

For every cluster table in the model payload, the writer emits an optional expansion map:

```text
code_expansions = nil
               | { "expanded_codes": [u64, ...], "canonical_indices": [u32, ...] }
```

`expanded_codes` is the strictly ascending set covering every raw slot-tuple code the writer enumerated for the
table, and it may also contain additional orbit codes whose particles are not allowed at a member slot (§3).
`canonical_indices[j]` is the index `i` such that `expanded_codes[j]` belongs to the orbit of
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
over all visited tuples therefore **contains** every enumerated tuple, each canonical cluster's expansion list
contains its own canonical code, and distinct canonical clusters have disjoint orbits (a group orbit is an
equivalence class, so the union is still a function).

The coverage is a **superset**, not an exact set. `AddTuple` adds the full orbit of the canonical tuple without
filtering by the per-slot allowed particle sets. When two member sites are swapped by the group but have
different allowed sets (for example `{O,Vc}` and `{O,Vc,H}`), an expansion code can place `H` at a slot whose
site does not allow it. That code is never produced by a valid lattice and is harmless for exact-code lookup,
but it is present in `expanded_codes`. The producer cannot filter it from `Expansions` alone: the member allowed
sets are not carried on `CompiledClusterTable`. The designed semantics are therefore:

- **Completeness:** every tuple the writer enumerated is present with its canonical index. A runtime tuple is
  assembled from the instance slots whose particles are allowed by the table's site masks, so it is enumerated.
- **Superset tolerance:** the map may contain additional codes in the same group orbit. A reader must not treat
  their presence as an error; they are never built at runtime.
- **Duplicate control is unaffected:** group orbits partition the code space, so even the superset assigns each
  code to exactly one canonical index. The writer still rejects a non-strictly-ascending sequence.

The `GroupLookupCodes` equivalence is therefore "covers every enumerated code", not "equals the enumerated set";
the property test asserts completeness and superset tolerance, not set equality (A2-05).

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

For one table, with `G = occupation_codes.Length`, `E = expanded_codes.Length`,
`I = canonical_indices.Length`, `N = E = I` for a valid map, and `particle_count` from the model payload:

1. `G = energies.Length`; this amendment does not change the existing `occupation_codes`/`energies` equality.
2. `code_expansions` is either `nil` or a map whose **required** keys are `expanded_codes` and
   `canonical_indices`. Unknown keys inside the map are ignored under the payload `unknownFields: "ignore"`
   policy (ADR 0013 addendum); a verifier does not need a third key to be an error, and a future additive
   nested member stays possible (A2-04).
3. `E == I`; the scans reject an unequal pair (`E != I`) at scan time, before any typed array is allocated
   (A2-R2-01). A malformed pair is a package error, not a length-mismatch finding deferred to the verifier.
4. `expanded_codes[0] < expanded_codes[1] < ... < expanded_codes[E-1]` strictly.
5. `canonical_indices[j] < G` for every `j` in `0..I`.
6. For every $j$ in `0..E`, `expanded_codes[j] >> 60 == 0` and each of its ten 6-bit fields is
   `< particle_count`. Like `occupation_codes`, this accepts a nonzero field in a slot above the table's actual
   slot count. Such a code is unreachable at runtime and harmless; do not tighten one side only (A2-14).
7. For every $i$ there is exactly one $j$ with `expanded_codes[j] == occupation_codes[i]` and
   `canonical_indices[j] == i` (canonical self-entry). Exactly one follows from strict ascending.
8. `N == G` iff every orbit is a singleton; the writer emits `nil` in that case and the map otherwise. A reader
   accepts a map with `N == G` too (it is structurally valid and means the identity map).
9. `nil` and absent mean "no map". For a singleton table, `occupation_codes` is complete. For a table with a
   non-singleton orbit, absence is a package built before this amendment or a writer defect; the reader must
   fail closed on a raw tuple that is not canonical rather than guess (see §6.2).
10. The map contains **every enumerated raw tuple and may contain extra orbit codes** (§3). Extra codes are
    valid entries; a reader must not reject them. What is forbidden is a missing enumerated tuple or a code
    assigned to the wrong orbit.
11. The typed cost of one table is `8E + 4I` (`u64` codes plus `u32` indices), not `12 × N`; a malformed pair
    could otherwise hide a large `expanded_codes` behind a small `canonical_indices`. The cumulative
    `Σ(8E + 4I)` across all tables in one model payload is bounded by the read-time expansion budget
    (`ReaderLimits::max_cluster_expansion_bytes` / `PackageVerificationOptions.MaxClusterExpansionBytes`,
    default 128 MiB), **and** the scans reject `E != I` and duplicate known keys before allocating. The bound
    is enforced **before** the typed arrays are allocated; over-budget is a fail-closed package error, not a
    best-effort decode (§6.3, A2-01, A2-R2-01).

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
- `ClusterTablePayload` gains a `[Key("code_expansions")]` member of that nullable type. The primary
  constructor becomes five-argument (`objectId, occupationCodes, codeExpansions, energies,
  particleTableMapping`), while the existing four-argument constructor remains as a delegating overload
  (`codeExpansions: null`). The generated five-argument `Deconstruct` is new; add a **manual four-argument
  `Deconstruct`** (`out string`, `out ImmutableArray<ulong>`, `out ImmutableArray<double>`,
  `out ImmutableArray<byte>`) that discards `CodeExpansions`, so the existing public `Deconstruct` member is
  preserved (A2-03). Existing keys are not renamed or reordered. If MessagePack-CSharp needs an explicit
  `[SerializationConstructor]` with the overload set, add it and cover the chosen shape with a payload
  round-trip test.
- `EnergyPayloadMapper` always writes the key: `nil` for singleton tables, the map otherwise.
- `PackageVerificationChecks.ValidateModel` applies §4 rules 2–10 to every table and appends the existing
  `PKG010` error text. It does not recompute symmetry and does not check orbit equivalence; the map is
  writer-asserted like `occupation_codes`. `PKG010`'s current wording ("out of bounds") is broader in use than
  its text; a later diagnostics pass may broaden the help text to "structural or out of bounds" without
  changing the code or this design (A2-16).
- **Decode-time budget (A2-01, A2-R2-01).** `PackageVerificationOptions` gains
  `MaxClusterExpansionBytes` (init property, default 128 MiB). `PackageReader.ReadModelsAsync` calls a
  model-specific decode overload that, after Zstd decompression and **before** `MessagePackSerializer.
  Deserialize<ModelPayload>`, runs a bounded `ClusterExpansionScan` over the MessagePack bytes. The scan reads
  only the known path `energy.cluster_tables[*].code_expansions`, tracks duplicate known keys with a seen-mask
  (`code_expansions` twice, or `expanded_codes`/`canonical_indices` twice, is a decode error), reads the two
  array headers, and accumulates `8E + 4I` across tables. It **rejects `E != I` at scan time** and rejects a
  payload above the option **before** any typed array is allocated. A `nil` or absent `code_expansions`
  contributes zero entries; a present map is scanned for both array headers, and a missing array is a scan
  error rather than a deferred length mismatch. The scan skips every other value with the non-allocating
  `MessagePackReader.Skip`, enforces a small depth bound, and reports a decode error that
  `PackagePayloadDecoder` maps to the existing `PKG004`; it does not allocate the model. The writer's trusted
  round-trip self-check (`PayloadEncoder.Decode`) remains a structural decode and does not apply the solver's
  read budget.
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
- The runtime `ClusterTable` gains an `expansions: Option<ClusterExpansions>` member. It is
  **`#[doc(hidden)] pub`**, not private: `ClusterTable` has all-public fields and no constructor, and it is
  constructed by a struct literal in `mocassin-msl::map_energy` and in the `mocassin-msl` integration test.
  A private field would make that cross-crate construction impossible. The field carries a doc comment (so
  `missing_docs` stays satisfied), `#[doc(hidden)]` keeps it out of rustdoc, and it is **not** the supported
  semantic API: Step 14/15 use `lookup` only. If the crate later adopts a `ClusterTable::new`/builder, the
  field can become private; that is a separate public-API change (A2-R3-01).
- `ClusterTable::lookup(raw_code) -> ClusterCodeLookup` is the **single accessor** Step 14 and Step 15 call
  (A2-R2-02). It is a small public enum, not `Option<u32>`, so the two fail-closed causes stay distinguishable
  without exposing the map:
  - `Canonical(index)`: the raw code resolves (map hit, or a no-map singleton/canonical direct hit);
  - `MissingFromMap(raw)`: a map is present and the raw code is not in it → `MissingOccupationCode`;
  - `ExpansionMapAbsent(raw)`: no map and the raw code is not canonical → the recompile-required error.
  A convenience `canonical_index(raw) -> Option<u32>` may exist as `lookup(raw).index()`, but it is not the
  accessor the semantics rest on and must not be used to choose between the two errors.
- **Construction path (A2-R3-01).** The single construction path stays the existing public-field struct
  literal: `map_energy` builds `ClusterTable { object_id, occupation_codes, energies,
  particle_table_mapping, expansions }`, and `tests/projections.rs` does the same. `expansions` is additive and
  `#[doc(hidden)] pub`, so no constructor is introduced and no existing literal breaks; the one integration-test
  literal is extended with `expansions: None`. The documented public surface is `lookup` plus the existing
  fields; the hidden field is an API-visible implementation detail recorded as an additive public field.
  `mocassin-types` is pre-1.0. Record the addition in §5 and §6.1 of this page only; the Step 13 page has no
  API-note section and is not re-opened (A2-09). The rustdoc `missing_docs` denial (satisfied by the field's doc
  comment), `#[doc(hidden)]`, and the workspace API tests are the gate; no `.msl` format consequence.
- `ReaderLimits` gains `max_cluster_expansion_bytes: u64` with a **128 MiB** default (A2-01). Like
  `max_rules`, it is a solver-only cap: a writer-valid package above it is rejected with a structured error and
  needs a raised `ReaderLimits`. The budget is `Σ(8E + 4I)`; for a valid map (`E == I = N`) that is `12N`, and
  the derived cap is 11,184,810 entries. The encoded payload for that many entries is roughly 21–150 MiB
  depending on the integer widths, so the cap binds before the 512 MiB model guard.

#### 6.2 Lookup and fail-closed semantics

Step 14 builds the raw code exactly as it does today: slot 0 is the centre particle, slots `1..=n` are the
resolved member particles, packed with 6 bits per slot, slot 0 least significant. Then:

- `Canonical(i)` → `code_id = i`, energy = `energies[i]`;
- `MissingFromMap(_)` → a data error (`MissingOccupationCode`, exit 12), unchanged;
- `ExpansionMapAbsent(_)` → a **distinct** fail-closed error (proposed `ExpansionMapAbsent`/
  recompile-required, exit 12) naming the table and the raw code and telling the user the package predates the
  expansion map and must be rebuilt. A no-map table whose raw code is canonical still returns `Canonical`, so a
  singleton package keeps working. Do not fall back to another code, do not sort the slot particles, do not
  take a minimum.

Step 14 owns the error enum, wording, and tests; this amendment fixes the semantics. The fail-closed rule is
the only safe absence behaviour because a reader cannot distinguish "old package" from "writer emitted a
partial map". Step 15 matches `Canonical(i)` only; the other variants are impossible after a successful context
initialization.

#### 6.3 Bounds and the scan layer (A2-01, A2-02, A2-R2-01)

The earlier "8 bytes per entry / 1.5×" argument is **withdrawn**. The stored integers use MessagePack's
smallest-width encoding: a code is 1–9 bytes and an index is 1–5 bytes, so a 512 MiB payload can declare
roughly 38–268M entries and drive a typed `u64` + `u32` pair of roughly 0.45–3.2 GB. The payload byte guard
alone is therefore **not** a bound on the typed arrays, and the .NET typed decode has the same exposure before
the verifier runs.

The design adds an explicit budget on **both** read paths, checked before the arrays are allocated. The cost is
`8E + 4I`, **not** `12 × N` from one array: a malformed map can declare a large `expanded_codes` and a small
`canonical_indices`, so a single-array formula would let the large `Vec<u64>`/`ImmutableArray<ulong>` allocate
before the length mismatch is rejected. Both scans also reject `E != I` and duplicate known keys at scan time.

| Path | Knob | Default | Enforcement |
| --- | --- | --- | --- |
| Rust | `ReaderLimits::max_cluster_expansion_bytes` | 128 MiB | `scan_model` reads the two array headers per table, rejects `E != I` and a duplicate optional/known key via its seen-mask, and accumulates `8E + 4I`; over budget returns `PackageError::PayloadTooLarge { section: "model cluster expansion", declared, limit }` (package class, exit 11). `map/model.rs` re-checks the same cumulative budget, the length equality, and the values before cloning the typed arrays, as defence in depth. |
| .NET | `PackageVerificationOptions.MaxClusterExpansionBytes` | 128 MiB | `PackageReader.ReadModelsAsync` uses a model decode overload that runs a bounded `ClusterExpansionScan` over the decompressed MessagePack **before** `MessagePackSerializer.Deserialize<ModelPayload>`; the scan rejects `E != I`, duplicate `code_expansions`/`expanded_codes`/`canonical_indices` keys, and accumulates `8E + 4I`. Over budget or malformed returns the decode error that maps to `PKG004`. `PackageVerificationChecks.ValidateModel` then applies the structural rules and `PKG010`. |

The budget is cumulative across all cluster tables in the model payload, not per table, because many small
tables can still allocate the same total. For a valid map (`E == I = N`) the derived entry cap is
`floor(128 MiB / 12) = 11,184,810` entries per model read; the scan rejects `E != I` first, so a malformed
single array cannot use the other array's smaller length to pass. A caller that needs a larger writer-valid
package raises the limit; the error is explicit and actionable.

**Rust scan ownership (A2-02).** `scan_map` currently *requires* every key in its `known` list. Adding
`code_expansions` to `CLUSTER_TABLE_KEYS` would make it required and reject every pre-map package before serde's
`#[serde(default)]` applies. The design therefore adds an **optional-key** path:

- `scan_map` keeps its current signature and behaviour for the existing call sites (a wrapper with an empty
  optional list).
- A new `scan_map_optional(cursor, required, optional, handler)` (or a `KeySpec { name, required }` list) checks
  only the required mask after the loop and lets the handler observe an optional key.
- `CLUSTER_TABLE_KEYS` stays the four required keys; `CLUSTER_TABLE_OPTIONAL_KEYS = ["code_expansions"]`.
  The optional key participates in the same seen-mask, so a duplicate `code_expansions` is rejected at scan
  time (A2-R2-01).
- The handler for `code_expansions` reads a nil scalar as absent/`None`, or a map with the two required arrays;
  it records `E` and `I` separately in `ClusterFacts`, **rejects `E != I` immediately**, accumulates
  `8E + 4I` in `ScanFacts`, and skips the values without allocating. Unknown nested keys are skipped; a
  duplicate nested known key is rejected by the nested `scan_map` call.
- `map/model.rs` validates §4 after the typed decode (strict ascending, indices, high bits, particles,
  canonical self-entry), re-checks `E == I`, and re-checks the cumulative `8E + 4I` budget. The typed arrays
  cannot exceed the budget because the scan already rejected a mismatched pair and an over-budget total.
- A pre-map fixture test asserts the scan accepts absence; hostile tests assert an unequal pair, a duplicate
  `code_expansions`, and a duplicate nested key are rejected at the scan before the typed allocation, with the
  field path.

Other bounds:

- The scanner's `check_declared(length, 1)` still rejects a declared array length that cannot fit in the
  remaining bytes, and `max_model_elements` bounds the element count.
- The reader validates §4 in a single pass over the two ascending arrays; coverage uses a merge with
  `occupation_codes`, so it is $O(N + G)$ with no set allocation.
- A malformed map (length mismatch, non-ascending, duplicate, index out of range, high bits, particle out of
  range, missing canonical self-entry, or over budget) is a `PackageError` naming
  `energy.cluster_tables.code_expansions` and the offending field; the solver exits 11 (package error), not 12.

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
- **Compatibility direction.** A new reader reads an old v1 package without the field under the defined absence
  rule. That rule is **fail-closed, not fully readable**: a singleton table resolves from `occupation_codes`,
  and a non-singleton table with a non-canonical tuple returns a recompile-required error and never a wrong
  value. A pre-amendment v1 reader reads a new package and ignores the unknown key (the payload unknown-field
  policy is `ignore`; MessagePack-CSharp ignores unknown map keys and the Rust scanner skips them). The latter
  is proven once by a migration check (§9.4), not by a permanent second reader.
- **Fixtures.** This is a current-major change: regenerate the current-major fixture that contains a cluster
  table, add the new two-member fixture, and keep a **byte-fixed pre-map non-singleton package** built by the
  parent writer for the real absence case (A2-06). No previous-major fixture is created because no previous
  major exists. At the first released `.msl`, v1 freezes; any later field change is a new major with a
  previous-major reader.
- **Stop condition.** If a `.msl` is published or declared released before implementation, do not ship this as
  an additive v1. Record the situation and ask the owner for a major-version decision. Until then, the
  pre-release delta is defensible and is recorded here.

**Old packages with a non-singleton cluster.** A package built between Step 11 and Step 11.1 that contains a
two-member cluster has no map and cannot resolve its non-canonical tuples. It is not silently mis-read: the
Step 14 lookup fails closed with the recompile-required error. The documented fix is to rebuild the package,
consistent with "`.msl` is immutable; rebuild for a new package". This is a pre-release limitation, not a
supported long-term compatibility mode. The byte-fixed pre-map non-singleton fixture is the regression test for
the load-and-fail-closed behaviour (A2-06).

### 8. Documentation and contract reconciliation

| Page | Change |
| --- | --- |
| [`02-data-contracts.md` §3.3](../02-data-contracts.md#33-compiled-model-payload) | Add `code_expansions` to the `energy`/`cluster_tables` description; add the slot order, superset coverage, `nil`/absence, signature, and budget rules; update the "codes are canonical" bullet to name the expansion map. |
| [`02-data-contracts.md` §3.4](../02-data-contracts.md#34-job-payload) | Solver read-bounds table/paragraph: the expansion budget is an explicit `max_cluster_expansion_bytes` cap (default 128 MiB, `Σ(8E + 4I)`), not a consequence of the model guard. |
| [`01-target-architecture.md`](../01-target-architecture.md) | §3.1: the compiled-model payload carries the cluster occupation expansion map so the solver resolves raw tuples without symmetry; §7.6: additive optional payload fields within a major, with defined fail-closed absence. |
| [`ADR 0013`](../adr/0013-format-compatibility-and-api-versioning.md) | Addendum recording additive-within-a-major payload fields, **fail-closed** absence semantics (a rebuild may be required), and the boundary at the first release (A2-11). |
| [`05-open-decisions.md`](../05-open-decisions.md) | Resolved row for the expansion-map decision and the 128 MiB budget; gated row for enumerating payload-internal energy fields in the manifest; the `step-11.1` evidence resolution. |
| Step 11 page | Amendment pointer and status; parent audit/closeout records stay historical. |
| Step 14 page | Cross-step note naming the unmerged commits/record (`87be9de`, `1ea8090`) and stating that F-29's formal closure is a **Step 14 gate**, not a `step-11.1` acceptance criterion (A2-08). |
| [`msl-format.md`](../../reference/msl-format.md) | Payload contents, the exact map shape, superset lookup semantics, the explicit read budget, compatibility, and the signature/summary exclusion. |
| Fixture READMEs | Provenance, SHAs, the expected 8→6 expansion, the **non-singleton** pre-map compatibility package, and the refreshed `step-13-populated` size/SHA table (A2-06, A2-15). |
| `samples/README.md` and `samples/step-11-cluster-expansion/README.md` | Add the new sample row and a sample README with purpose, derivation, job, and build/regeneration hint (A2-10). |
| [`closeout/README.md`](../closeout/README.md), [`audits/README.md`](../audits/README.md), [`step-audit-template.md`](../audits/step-audit-template.md) | Document the post-merge amendment evidence rule: `closeout/step-NN.M.json` is the evidence file when the checker is extended, or the audit record is the evidence source when the pages are amended. The chosen resolution is the page amendment because this remediation is docs-only; the checker is not edited (A2-07). |
| [`diagnostics.md`](../../reference/diagnostics.md) | Optional later wording pass for `PKG010`: "structural or out of bounds". No code or descriptor change (A2-16). |
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
- **Sample docs:** add the row to `samples/README.md` and a
  `samples/step-11-cluster-expansion/README.md` (purpose, derivation from `samples/ceria`, the job, and the
  fixture build/regeneration hint), matching the `step-13-populated` sample pattern (A2-10).
- **Expectation document:** `cluster-expansions-v1.json` (a test fixture, not a contract manifest artifact)
  records, for table 0: `occupation_codes` (the six canonical codes), the **enumerated** `expanded_codes` (the
  eight raw codes for this symmetric geometry), `canonical_indices`, and the two non-canonical raw codes `8386`
  and `8387` with their canonical index and canonical code (`12418` and `12419`). The test asserts the actual
  values produced by the compiled particle order; the table above is the expected result for `1 = Ce`,
  `2 = O`, `3 = Vc`. The fixture's allowed sets are symmetric, so the map equals the enumerated set here; the
  superset rule is exercised by the asymmetric property case, not by this fixture (A2-05).
- The package is generated once by the real `SimulationPackageWriter`, is deterministic for the fixed seed and
  timestamp, and is never hand-edited.

#### 9.2 Regenerated and preserved fixtures

| Fixture | Cluster tables | Action | Reason |
| --- | --- | --- | --- |
| `tests/contracts/packages/step-13-populated/` | 1 (P1, 12 singleton codes) | Regenerate `.msl`, `.sha256`, `payloads/model-1.bin`, `payloads/index.json`, **and the README size/SHA table**; keep `package-summary-v1.json` and assert it is unchanged | The cluster table gains `code_expansions: nil`; the logical summary must not move; the README is already stale (A2-15) |
| `tests/contracts/packages/amend-2-pre-map/` (new) | 1 non-singleton table (2 members) | Build the **two-member ceria sample** with the **pre-amendment writer at `bbc2d08`** in a temporary worktree (copy the new sample in; it parses on the parent) and commit the byte-fixed `.msl`, `.sha256`, `package-summary-v1.json`, payload index, and README. This package has **no** `code_expansions` and 8 raw → 6 canonical codes | The real F-29 absence case: the reader must load it, resolve the six canonical codes, and return `ClusterCodeLookup::ExpansionMapAbsent` (or `canonical_index(...).is_none()` if the convenience is kept) for `8386`/`8387`; never regenerated (A2-06, A2-R3-02) |
| `tests/contracts/packages/msl-v1/` | 0 | No regeneration; compare the extracted model payload and `package-summary-v1`, and record the package bytes as a same-host observation | No cluster table, so no new key; `.msl` bytes include `app_version`/`generator` and are runtime metadata, not the golden (A2-12) |
| `tests/contracts/packages/step-13-centered/` | 0 | Same as `msl-v1`: payload/summary equality, package bytes as a same-host observation | No cluster table (A2-12) |
| `tests/contracts/packages/amend-1-net10/` | 0 | Never regenerated | Pre-amendment compatibility fixture (amendment 1) |
| `tests/contracts/packages/amend-1/payloads/`, `golden/`, `goldens.json` | 0 | No change | Codec goldens are byte-fixed and the model payload has no cluster table |

If a `0`-cluster fixture's extracted payload or summary differs, the difference is investigated before any
golden is updated; it must not be accepted as "the writer changed". Package-byte differences caused only by
`app_version`/`generator`/runtime metadata are not a regression.

#### 9.3 Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Writer map for the ceria cluster | PR-fast | Table 0 emits `N = 8`, `G = 6`; strict ascending; `8386` and `8387` resolve to the canonical indices of `12418` and `12419`; every canonical code self-maps |
| Writer singleton table | PR-fast | `step-13-populated`'s table emits `code_expansions = nil` |
| Writer duplicate/coverage guard | PR-fast | A synthetic table with a raw code claimed by two canonical indices, or a missing canonical self-entry, throws `InvalidOperationException` |
| Writer byte estimate | PR-fast | A small `MaxEnergyTableBytes` rejects a table whose map pushes it over the limit with `CMP` limit diagnostics; the estimate includes `12 × Σ\|Expansions\|` |
| `GroupLookupCodes` completeness (property) | PR-fast | For random small clusters and random allowed sets, (a) **every** tuple enumerated by the reduction oracle is present with exactly one canonical index, (b) every canonical code self-maps, and (c) **extra** orbit codes are accepted (superset). Do not assert set equality (A2-05) |
| Asymmetric allowed sets (property) | PR-fast | Two swapped member sites with `{O,Vc}` and `{O,Vc,H}`: the map is a superset; every enumerated tuple resolves; every extra code's six-bit fields remain `< particle_count` and are never produced by a valid lattice (A2-05) |
| .NET verifier | PR-fast | `PKG010` for length mismatch, non-ascending/duplicate, index out of range, high bits, particle out of range, missing canonical self-entry; extra superset codes are accepted; a valid map verifies |
| .NET API surface | PR-fast | Both the four-argument and five-argument `ClusterTablePayload` constructors and `Deconstruct` methods exist; the five-argument primary round-trips; `PublicAPI.Unshipped.txt` and ApiCompat are clean (A2-03) |
| .NET decode budget | PR-fast | A model payload whose expansion arrays declare `8E + 4I` above `PackageVerificationOptions.MaxClusterExpansionBytes` is rejected by the pre-scan with `PKG004` **before** a typed model is materialized; an unequal pair (`E != I`, large `expanded_codes` behind a small `canonical_indices`) and a duplicate `code_expansions`/`expanded_codes`/`canonical_indices` key are rejected at scan time before allocation; a payload just under the cap loads; **a raised `MaxClusterExpansionBytes` accepts the same package the default rejects**; a nested MessagePack bomb does not stack-overflow or over-allocate (A2-01, A2-R2-01, A2-R3-03) |
| .NET pre-map read | PR-fast | `amend-2-pre-map` verifies and keeps its recorded summary; the DTO decodes `CodeExpansions == null`; `amend-1-net10` still verifies |
| Rust reader: new fixture | PR-fast | Loads; table 0 has 8 expanded codes and 6 indices; `lookup` returns `Canonical` with the expected index for all eight raw codes, including the two previously failing ones |
| Rust scan: optional key | PR-fast | The pre-map fixture passes `scan_model` with `code_expansions` absent; a malformed present optional key is rejected with the field path; `CLUSTER_TABLE_KEYS` remains the four required keys (A2-02) |
| Rust reader: absent map | PR-fast | The non-singleton pre-map fixture loads; `lookup` returns `Canonical` for the six canonical codes and `ExpansionMapAbsent` for `8386`/`8387` (or `canonical_index(...).is_none()` for those two if the convenience is kept); Step 14 maps `ExpansionMapAbsent` to the recompile-required error. The test does not read the hidden `expansions` field (A2-06, A2-R3-02) |
| Rust reader: hostile map | PR-fast | Length mismatch, non-ascending, duplicate, index out of range, high bits, particle out of range, and missing canonical self-entry each return a `PackageError` naming `energy.cluster_tables.code_expansions` |
| Rust read budget | PR-fast | A synthetic payload whose expansion arrays declare `8E + 4I` above `ReaderLimits::max_cluster_expansion_bytes` returns `PayloadTooLarge` at the scan **before** the typed `Vec` allocation; an unequal pair (`E != I`) and a duplicate optional/nested key are rejected at scan time before allocation; a payload just under the cap loads; a raised limit accepts it (A2-01, A2-R2-01) |
| Signature/summary invariance | PR-fast | The new fixture's `model_signature` and `signature` equal the values computed from the same project before the change; `signature-vectors.json` and the projection fixtures are unchanged; `verify.sh contracts` has no diff |
| Determinism | PR-fast | Two builds of the new fixture with the same request produce identical bytes and SHA-256 |
| Forward read (one-time) | **Evidence (one-time manual)** | The pre-amendment `.NET` verifier and Rust reader at the parent commit open the new fixture and ignore `code_expansions` (the .NET reader succeeds and the Rust scanner skips the unknown key); recorded in the feature log, not a recurring test and not a PR tier (A2-13) |
| Step 14 lookup | Step 14 | The context resolves all eight assignments on the new fixture; an absent map on a non-canonical tuple returns the recompile-required error. **F-29's formal closure is a Step 14 gate**, not a `step-11.1` test (A2-08) |

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
   the Step 11 page, and `status.md`. `docs/planning/closeout/step-11.json` and the Step 11 audit record are
   historical and are not rewritten. The amendment's evidence lives in
   `docs/planning/audits/step-11-amend-2.md` under the post-merge rule below.
9. **Step 14.** The Step 14 design re-review closes F-29 by reference to this amendment; Step 14's implementation
   consumes `ClusterTable::lookup`. F-29's **formal closure is a Step 14 gate** owned by the Step 14
   planner/step agent, not a `step-11.1` acceptance criterion. The `main` Step 14 page names the unmerged design
   `87be9de` and the round-3 record `1ea8090` so a `main` reader can find them. No Step 14 code is part of
   `step-11.1`.

**Post-merge evidence (A2-07) — page amendment, not a checker change.** The earlier "record it in the audit
only" plan contradicted [`audits/README.md`](../audits/README.md) §3 and the
[audit template](../audits/step-audit-template.md), which require a closeout evidence file. Two resolutions were
possible: extend `tools/check_closeout_evidence.py` to accept `step-NN.M` and add
`closeout/step-11.1.json`, or amend the audit/closeout pages to define the audit record as the evidence source
for a post-merge amendment. **This remediation takes the second option**, because it is docs-only (the task
forbids code/tests/fixtures in this change), because `step-11`'s evidence file must stay historical, and because
a post-merge amendment is not a step and has no step-branch closeout ceremony to bind to. The change is:

- [`audits/README.md`](../audits/README.md) gains a **"Post-merge amendment audits"** subsection: the audit
  record is the single source for the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze,
  merge commit, and suffixed tag; the step's `closeout/step-NN.json` is historical and is not rewritten; the
  closeout-evidence checker is not run for the amendment.
- [`closeout/README.md`](../closeout/README.md) states that a post-merge amendment ID is outside the
  `step-NN`/`step-NN[a-z]` evidence-file form and uses the audit record instead; the template note is updated so
  the rule is discoverable.
- [`step-audit-template.md`](../audits/step-audit-template.md) changes the `Closeout evidence` row's guidance:
  `closeout/step-NN.json`, or "this record" for a post-merge amendment.
- `05-open-decisions.md` item 7 is resolved to this rule.

This is a process change under `AGENTS.md` §3.15; the owner approves it at the re-review/start gate. No page
claims `step-11.1` has a machine-checkable JSON evidence file.

### 11. Sizing and complexity

**Class: Low.** One optional field in one payload object, one mapper function, one verifier branch, one Rust
DTO/validator/accessor, and a bounded set of fixtures. The difficult reasoning (orbit coverage) is already
performed by the Step 02/08 reduction and is proven by the property test. No step split is needed. The work is
one maintenance delivery plus its audit.

## Acceptance criteria

- [x] `EnergyPayloadMapper` emits a complete, strictly ascending `code_expansions` map for the ceria two-member
      table; every enumerated raw tuple resolves; the two non-canonical codes map to their canonical indices and
      energies.
- [x] Singleton tables emit `nil`; the writer never emits a partial, duplicate, or non-ascending map; a
      violation throws an internal invariant error.
- [x] The property test proves `GroupLookupCodes` **completeness** against the reduction oracle for random small
      clusters, accepts superset orbit codes, and includes an asymmetric-allowed-set case (A2-05).
- [x] The `.NET` verifier applies the structural rules and reports `PKG010` for every violation; a valid map
      verifies.
- [x] The .NET decode pre-scan and the Rust scan enforce the cumulative `128 MiB` expansion budget before the
      typed arrays are allocated, with fail-closed errors and hostile-payload tests; the limit is raisable
      (A2-01).
- [x] The Rust scan supports `code_expansions` as an **optional** key; a pre-map package passes the scan, and a
      malformed present map is rejected with the field path (A2-02).
- [x] The Rust reader validates the same rules, exposes `ClusterTable::lookup` with the three-way
      `ClusterCodeLookup` result, and rejects hostile maps with a field-specific `PackageError` (A2-R2-02).
- [x] The `.NET` four-argument `ClusterTablePayload` constructor and `Deconstruct` are preserved alongside the
      five-argument forms; ApiCompat/`PublicAPI.Unshipped.txt` are clean (A2-03).
- [x] The new fixture is deterministic, carries the documented expectation, and its `package-summary-v1` and
      signatures are the logical values from before the change.
- [x] The regenerated `step-13-populated` fixture's summary is unchanged and its README size/SHA table is
      refreshed; the no-cluster fixtures' extracted payloads/summaries are unchanged (package bytes are a
      same-host observation) (A2-12/A2-15).
- [x] A byte-fixed **non-singleton pre-map** package built by the parent writer loads, resolves its six
      canonical codes, and returns the recompile-required result for the two non-canonical codes (A2-06).
- [x] The pre-amendment reader reads the new fixture in the one-time forward check (recorded as evidence, not a
      PR tier) (A2-13).
- [x] `signatures.model`, `signatures.parameterization`, `signatures.lattice`, `signature-vectors.json`,
      `tests/contracts/projections/**`, and `contracts/**` are unchanged; `verify.sh contracts` is green.
- [x] `.NET` and Rust public API additions are recorded (`PublicAPI.Unshipped.txt`, ApiCompat, §5/§6.1 of this
      page); the old `ClusterTablePayload` constructor and `Deconstruct` still exist (A2-09).
- [x] The audit/closeout pages and template define the post-merge amendment evidence rule; no page claims a
      `step-11.1` JSON evidence file (A2-07).
- [x] `samples/README.md` and `samples/step-11-cluster-expansion/README.md` are added (A2-10).
- [ ] `./tools/verify.sh all --require` is green at the audit commit; the single closeout CI run is green; the
      main CI run is green before `step-11.1` is tagged.
- [x] Documentation is updated (§8) and `./tools/verify.sh docs --require` passes.
- [x] The Step 14 pointer names `87be9de`/`1ea8090` and records F-29's formal closure as a Step 14 gate; the
      parity row's evidence is updated after implementation (A2-08).
## Tests

The full matrix is in §9.3. The gate rows are: writer coverage/duplicate, `GroupLookupCodes` property,
verifier `PKG010`, Rust reader validation and lookup, signature/summary invariance, determinism, and the
one-time forward read.

## Documentation

- [x] `02-data-contracts.md` §3.3/§3.4 updated with the field, superset coverage, absence, signature, and budget rules.
- [x] `01-target-architecture.md` §3.1/§7.6 updated.
- [x] ADR 0013 addendum records additive-within-a-major payload fields and **fail-closed** absence.
- [x] `05-open-decisions.md` resolved/gated rows and the post-merge evidence item updated.
- [x] Step 11 page, amendment index, `status.md`, and Step 14 cross-step note updated (commits `87be9de`/`1ea8090`, closure gate).
- [x] `msl-format.md` updated.
- [x] Fixture READMEs, `tests/contracts/README.md`, and the `step-13-populated` size/SHA table updated.
- [x] `samples/README.md` and `samples/step-11-cluster-expansion/README.md` added.
- [x] `audits/README.md` §3.1, `closeout/README.md`, and `step-audit-template.md` define the post-merge evidence rule.
- [x] `PKG010` help wording noted for a later diagnostics pass.
- [x] Parity matrix evidence updated after implementation.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Payload growth for large cluster tables | `nil` for singleton tables; `12N` in the writer estimate; the existing 2 GiB energy-table cap; the explicit `128 MiB` typed-expansion budget on both readers (raisable), enforced before allocation; a large-table hostile test |
| Typed-expansion amplification from smallest-width MessagePack | The explicit budget is based on `8E + 4I` per table, not on encoded bytes or a single array length; both readers reject `E != I` and duplicate keys at scan time and check the cumulative budget before allocating; the earlier 8-bytes/1.5× arithmetic is removed from all pages (A2-01, A2-R2-01) |
| Writer emits an incomplete or wrong map | Global sort + strict-ascending/completeness assertions; verifier `PKG010`; property test against the reduction oracle; fail-closed runtime lookup |
| Old package with a non-singleton cluster is unreadable | Fail closed with a recompile-required error, never a wrong energy; documented as a pre-release limitation; the byte-fixed non-singleton pre-map fixture is the regression test; rebuild is the supported path |
| Signature or summary drift | Explicit no-change acceptance criteria and a fixture that compares the logical signatures/summary before and after; a diff is a blocking finding |
| API break in `Mocassin.Contracts` or `mocassin-types` | Preserve the old constructor **and the four-argument `Deconstruct`**; additive property/field/type; `PublicAPI.Unshipped.txt` and ApiCompat; Rust is pre-1.0 and the workspace literals are updated (A2-03) |
| Fixture churn hides a real change | Only the cluster-table fixture is regenerated; the no-cluster fixtures are compared by extracted payload/summary; the pre-map package is byte-fixed; package bytes are a same-host observation (A2-12) |
| Process gap for `step-11.1` evidence | Resolved by the audit/closeout page amendment: the audit record is the evidence source for a post-merge amendment; `step-11.json` stays historical (A2-07) |
| The amendment is implemented after a `.msl` release | Stop condition in §7: escalate for a major-version decision instead of shipping an additive v1 |

## Dependencies

- Step 11 `Done`, merged at `84ceb76`, tagged `step-11`; amendment 1 integrated. The parent audit is closed, so
  no audit is running and a post-merge amendment is the correct path.
- Step 13 `Done`, merged and tagged; the Rust reader exists and is bounded. No Step 13 re-open is needed for
  compatibility; its reader/runtime types gain the optional field.
- Step 14 design review round 3 record at `1ea8090` (reviewed design `87be9de`; branch
  `review/step-14-design-round-3`, not merged) records F-29. The Step 14 design must be corrected on its own
  branch to consume this map; this amendment supplies the format and reader contract, not the Step 14 context
  code. **F-29's formal closure is a Step 14 gate**; the `step-11.1` audit treats the pointer as a follow-up,
  not a completed acceptance criterion (A2-08).
- No released `.msl` (pre-release condition, §7).
- `contracts/manifest.json` is unchanged; `verify.sh contracts` must show no diff.

## Implementation

- **Branch:** `fix/step-11.1-cluster-code-expansion`, from `main` at the design merge `2f3a6fe`.
- **`.NET`:** `CodeExpansionPayload` and the five-argument `ClusterTablePayload` (the four-argument
  constructor and `Deconstruct` stay); `EnergyPayloadMapper.MapCodeExpansions` flattens
  `CompiledClusterOccupation.Expansions` with a global sort, same-code/different-orbit rejection, a
  canonical self-entry assertion, and `nil` iff every orbit is a singleton; the cluster-stage byte estimate
  adds `12 × Σ|Expansions|`; `PackageVerificationOptions.MaxClusterExpansionBytes` (128 MiB) and the
  non-allocating `ClusterExpansionScan` run before `MessagePackSerializer.Deserialize<ModelPayload>`;
  `PackageVerificationChecks.ValidateModel` applies the structural rules under `PKG010`.
- **Rust:** `ClusterTablePayload.code_expansions` with `#[serde(default)]`; `ClusterExpansions` and the
  `#[doc(hidden)] pub` runtime field; `ClusterTable::lookup` and `ClusterCodeLookup`; `scan_map_optional` so
  `code_expansions` is optional but participates in the duplicate mask; the cumulative `Σ(8E + 4I)` budget
  (`ReaderLimits::max_cluster_expansion_bytes`, 128 MiB) and the `E != I`/duplicate checks reject before the
  typed arrays allocate; `map/model.rs` re-checks lengths, ascending order, indices, slots, particles, the
  canonical self-entry, and the cumulative budget.
- **Fixtures:** `tests/contracts/packages/step-11-amend-2/` (real writer; 8 raw → 6 canonical, `8386 → 12418`
  and `8387 → 12419`); `tests/contracts/packages/amend-2-pre-map/` (byte-fixed, non-singleton, no map);
  regenerated `step-13-populated` (singleton table → `code_expansions: nil`, summary and signatures unchanged,
  README size/SHA refreshed); `msl-v1`, `step-13-centered`, `amend-1-net10`, and the codec goldens are
  untouched. The new sample is `samples/step-11-cluster-expansion/`.
- **Sample deviation:** the fixture uses a **P-1 (space group 2) origin-shifted** form of the ceria primary
  cluster, with the centre at the inversion origin. The P-1 inversion fixes the centre and swaps the two member
  slots, so the eight raw tuples still reduce to the six canonical codes in the design table. The full
  space-group-225 cell generates multiple symmetry-equivalent KMC paths from one start and trips `CMP004`
  ambiguity; the translated P-1 representation keeps the cluster geometry, identities, offsets, and codes
  while making the KMC path unambiguous and the fixture compact. It is recorded in the sample and fixture
  READMEs.
- **Forward read (one-time evidence):** the pre-amendment `.NET` verifier and Rust reader at `bbc2d08`:
  `dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- verify <new fixture>` → all ten checks
  pass, exit 0; `cargo run -p mocassin-cli -- --db <new fixture> --job 1 --dry-run` → `validation: ok`. Both
  ignore the unknown `code_expansions` key.
- **Gates:** `./tools/verify.sh all --require` exit 0 (1,991 .NET tests including the reduction-oracle property
  test, format/ApiCompat/contracts/docs/AOT), Rust `fmt`/`clippy`/`test` plus release build, Python, and the
  single Ubuntu closeout CI [`44896189617`](https://github.com/seb-eis/mocassin-2/actions/runs/44896189617)
  green at `ef568beca62f6f51559460d01ba5627219e90345`.

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

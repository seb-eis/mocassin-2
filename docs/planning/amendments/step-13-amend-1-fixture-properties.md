# Step 13 Amendment 1 — Populated-fixture properties and the equivalent-start compiler defect

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-13-amend-1` |
| Parent step | 13 — Rust Solver Foundation and Package Reader |
| Status | Withdrawn — resolved by the Step 08 fix `1f5b473` |
| Branch | `feature/step-13-rust-solver-foundation-and-package-reader/fix-audit-round-1` (draft only; no amendment implementation branch yet) |
| Design review | — (withdrawn; no review requested) |
| Parent audit | [`docs/planning/audits/step-13.md`](../audits/step-13.md) |
| Integration commit | — |
| Combined audit commit | — |
| Tag | `step-13` |
| Date | 2026-11-20 |
| Owner | — (withdrawn; no acceptance required) |

Parent step page: [`Step 13 — Rust Solver Foundation and Package Reader`](../steps/step-13-rust-solver-foundation-and-package-reader.md)

## Withdrawal

**Withdrawn — resolved by the Step 08 fix `1f5b473`.** The Step 08 maintenance commit changes
`TransitionCompilationStage` so translation-equivalent starts are merged and `EquivalentStartPositions` can
contain a different P1 start; `EncodingCompilationStage.BuildMappings` then emits cross-bucket `jump_mapping`
entries. The real-writer [`step-13-centered`](../../../tests/contracts/packages/step-13-centered/README.md)
fixture derived from `samples/step-13-centered/step-13-centered.mocprj` (itself derived from the C-centered
sample) now carries **both** the translation-equivalent start mapping and the per-node mask difference. The
Rust and .NET fixture tests assert the cross-bucket mapping and the mask difference. This amendment no longer
previously requested owner acceptance for synthetic coverage; it is withdrawn and retained for history.

The Step 11 verifier observation (`byte_position >= site_count` is a cluster-slot-vs-site-index contract
mismatch) remains a separate deferred maintenance item and is not part of this withdrawal.

The sections below are the retained historical proposal. All "pending owner acceptance" language is
superseded by this withdrawal; no owner acceptance is required.

## Rationale

Step 13 audit round 1 found **F-1 (Major)**: the real-writer populated fixture did not carry the per-node mask
difference or the equivalent-start mapping that design §7, the Step 13 test plan, and design-review resolution
n-14 require, and the substitution was not recorded as an amendment. The owner asked for a bounded spike: try
once more to build the real fixture, and if a property is impossible, produce reproducible evidence and record
the limitation here rather than silently accepting it.

The spike outcome is split:

- **Per-node mask difference: delivered.** The fixture sample now gives the O2 site an extended
  `{O, Vc, H}` particle set while O1 keeps `{O, Vc}` and U1 keeps `{O, Vc}`. The real writer emits a model
  where node 0 of the KMC collection admits `H` only from the reverse (O2) direction, and nodes 0/1 of the MMC
  collection show the same difference. The regenerated package, summary, payloads, and both the Rust and .NET
  fixture tests assert the property. No design change is needed for this half.
- **Equivalent-start mapping: impossible with the current Step 08 compiler.** `TransitionCompilationStage.AddDraft`
  builds `DirectionKey` from the relative path, whose first component is `(0, 0, 0, start.P)`. Two drafts can
  merge only with the same start `P`; `EquivalentStartPositions` therefore only ever repeats the representative
  start, and `EncodingCompilationStage.BuildMappings` never places a direction in a different start bucket. The
  checked-in Fm-3m `msl-v1` package and a P-1 variant of this sample both show no cross-bucket mapping. The
  reader semantics are already covered by synthetic tests; the reviewed n-14 property and the shared Step 16
  exact-check fixture are what cannot be met.

The P-1 `PKG010` claim recorded in feature 6 is **not reproducible**: changing only
`model.structure.spaceGroup.number` to `2` builds, verifies, and publishes a package with six positions. The
verifier's `byte_position >= site_count` check is independently inconsistent with the payload contract
(`byte_position` is the cluster slot, not a site index), but no valid package tripped it in this spike.

This is a design/test-fixture deviation plus a compiler defect that is outside Step 13's write scope. It must be
recorded. The amendment is withdrawn; no acceptance is required.

## Scope

- Record the delivered per-node mask-difference property and its real-writer evidence.
- Historical request: synthetic coverage for the equivalent-start mapping in Step 13, with the
  real-writer property deferred to a Step 08 maintenance fix.
- Re-plan the Step 16 exact per-direction check to consume either a synthetic vector or the fixture built after
  the Step 08 fix.
- Record the Step 08 compiler defect and the Step 11 verifier check as follow-ups with named owners.
- Update the Step 13 page, fixture/sample READMEs, audit resolution, and evidence.

## Non-goals

- Do not change `Mocassin.Compilation`, `Mocassin.Packages`, or `Mocassin.Results` in this amendment. The
  compiler and verifier fixes are separate maintenance work.
- Do not change the `.msl` schema, payload fields, versions, or signature definitions.
- Do not add a hand-authored package or a fake mapping.
- Do not implement the Step 16 exact per-direction check here.

## Design

### Delivered fixture change

The sample `samples/step-13-populated/step-13-populated.mocprj` gains:

- particle `H` (`Proton`, charge `+1`, not a vacancy);
- particle set `Oxygen site extended` = `{O, Vc, H}` on the O2 site;
- a site-energy entry for `H` at O2.

The regenerated `tests/contracts/packages/step-13-populated/` package has 4 particles, 3 positions, 6 pair
tables, 1 cluster table, 2 collections, and 8 rules. The model is a valid writer output and verifies with the
ten-check verifier. The KMC collection has a per-node mask difference at node 0 and the MMC collection at nodes
0 and 1. The fixture README and the two fixture tests assert it.

### Accepted limitation: equivalent-start mapping

Step 13 accepts the synthetic tests
`map::tests::model::a_direction_may_be_mapped_into_another_start_bucket`,
`a_collection_union_admits_a_particle_only_one_direction_allows`,
`a_particle_outside_the_union_is_rejected_even_when_mass_and_charge_hold`, and
`void_nodes_are_exempt_from_the_union_check` as the Step 13 evidence for the reader contract. The real-writer
equivalent-start property is deferred until the Step 08 compiler can produce it.

### Step 08 compiler defect

`TransitionCompilationStage` should merge symmetry-equivalent starts even when the P1 start index differs, or
`DirectionKey` should exclude the start `P` from the equivalence test. The current code makes
`EquivalentStartPositions` dead for different starts. **Resolved** by the Step 08 maintenance fix
`1f5b473` (`fix/step-08-equivalent-start-mapping`), with Step 29 responsible for the statistical/parity
acceptance after the fix and Step 16 re-planning its exact-check fixture. The fix needs a transition-compiler regression test
that produces a direction under a different start bucket.

### Step 11 verifier observation

`PackageVerificationChecks.ValidateModel` rejects `cluster.byte_position >= site_count`. The payload contract and
`ClusterLinkPayload` define `byte_position` as the cluster slot (`0` is the centre), bounded by the cluster node
count, not by the reference-site count. The check should use the cluster table's node count (or the documented
`< 10` slot bound). No valid package in this repository currently trips it because duplicate-site clusters fail
earlier with `CLI007`, so this is a latent defect, not a reproduced failure. Deferred owner: a **Step 11
maintenance fix**, with Step 29 including it in the verifier parity sweep.

## Reproduction evidence

**Per-node mask difference.** The regenerated `step-13-populated` package is built by the real writer and its
extracted model payload shows `positions = [(0, [1,2]), (1, [1,2]), (2, [1,2,3])]`; the KMC collection's node 0
and the MMC collection's nodes 0/1 map different directions to masks `{O,Vc}` and `{O,Vc,H}`. The Rust
`the_populated_fixture_has_a_per_node_mask_difference` and .NET
`FixtureCarriesANonTrivialPerNodeMaskDifference` tests assert it.

**Equivalent-start mapping (blocked).** Inspect the source:

```text
src/dotnet/Mocassin.Compilation/Transitions/TransitionCompilationStage.cs: AddDraft
  relative[0] = (0, 0, 0, resolved[0].P)
  key = DirectionKey(collection, relative, IsInverse)
  if an existing draft has the key: existing.EquivalentStarts.Add(resolved[0].P)
src/dotnet/Mocassin.Compilation/Simulations/EncodingCompilationStage.cs: BuildMappings
  starts = direction.EquivalentStartPositions.IsDefaultOrEmpty
      ? [direction.PositionId] : direction.EquivalentStartPositions
```

Because the key contains the start `P`, a duplicate key has the same start `P`; the list can only repeat the
representative. Both checked-in transition packages confirm the consequence: the `msl-v1` Fm-3m model (two
directions) and the P-1 variant below have no bucket whose position differs from `direction.position_id`.

**P-1 `PKG010` is not reproducible.** The variant is derived from the checked-in sample and built with the real
CLI:

```sh
python3 - <<'PY'
import json
p = json.load(open("samples/step-13-populated/step-13-populated.mocprj"))
p["model"]["structure"]["spaceGroup"] = {"number": 2, "axis": "none", "origin": "none"}
json.dump(p, open("/tmp/step-13-populated-p1.mocprj", "w"), indent=2)
PY
dotnet run --project src/dotnet/Mocassin.Cli -c Release -- \
  build /tmp/step-13-populated-p1.mocprj -o /tmp/step-13-populated-p1.msl
# observed: 10/10 build stages, Verification: passed, 6 P1 positions, exit 0
```

No `PKG010` occurs. A 3-offset cluster variant that would make a slot index exceed the site count fails earlier
with `CLI007` (unexpected internal error, exit 10), so the verifier's `byte_position >= site_count` check is
recorded as a latent contract mismatch rather than a reproduced valid-package rejection.

## API and contract impact

- Public API additions: none in this amendment; the `DirectionPool`/`JumpStatus` containers and the effective
  `JobPackage.seed` are Step 13 remediation, not this amendment.
- Package/results/state formats: unchanged. The regenerated fixture changes model/lattice signatures because it
  is a different model; no field, schema, or version changes.
- XML documentation and ApiCompat: no .NET production API change in this amendment. The Rust fixture tests and
  READMEs are documentation/test changes.

## Temporary implementation and migration

The synthetic equivalent-start tests were temporary acceptance evidence until the compiler fix landed. The fix
is `1f5b473`, and the real-writer `step-13-centered` fixture now covers the property; the synthetic tests remain
as focused reader assertions. The Step 16 fixture plan is updated to name the compiler fix as a dependency; it
must not claim the real equivalent-start property before the fix.

## Acceptance criteria

- [ ] The real-writer populated fixture carries the non-trivial per-node mask difference, and the Rust and .NET
      fixture tests assert it. *(Delivered in the Step 13 remediation branch.)*
- [x] **Withdrawn** — no owner acceptance is required; the property is met by `1f5b473` and the centered fixture.
- [x] The Step 08 compiler defect is fixed by `1f5b473`, with a regression test on `main`.
      requirement.
- [x] The Step 11 verifier `byte_position` check is recorded as a deferred maintenance item.
- [ ] The Step 16 exact per-direction fixture plan is re-pointed at the compiler fix or a synthetic vector.
- [ ] No `.msl` schema, field, or version change is introduced.

## Tests

| Test | Assertion |
| --- | --- |
| Rust `the_populated_fixture_has_a_per_node_mask_difference` | At least one collection node maps directions to positions with different allowed masks. |
| .NET `FixtureCarriesANonTrivialPerNodeMaskDifference` | The same property in the decoded model payload; particle table is `∅, O, Vc, H`. |
| Rust `a_direction_may_be_mapped_into_another_start_bucket` | The reader accepts a bucket whose position differs from `direction.position_id`. |
| Step 08 regression (follow-up) | A symmetry-equivalent start with a different `P` is merged and appears in a different `jump_mapping` bucket. |

## Documentation

- [x] The fixture README and sample README record the delivered property and the limitation.
- [x] The Step 13 page and audit resolution link this amendment.
- [x] The Step 16 page records the centered fixture as its exact-check dependency; no owner acceptance is required.
- [x] Status, parity, and the Step 08/11 defect notes are updated; the Step 08 fix is on `main` and the Step 11 item is deferred.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Step 13 closes without a real equivalent-start package. | Resolved by the Step 08 fix `1f5b473` and the real-writer `step-13-centered` fixture; the synthetic reader tests remain focused assertions. |
| The `--seed`/mask fixture changes alter stored signatures. | The fixture is a test artifact; the summary, payload index, SHA-256, and both fixture tests are regenerated together and verified. No schema version changes. |
| The P-1 `PKG010` claim is repeated without evidence. | The step page and both fixture READMEs are corrected: a plain P-1 variant verifies; the residual verifier check is recorded as latent with its source path. |
| A later writer changes `is_vacancy[0]`. | The reader now rejects `is_vacancy[0] == true` (the writer's void particle is not an authored vacancy); the check is documented and tested. |

## Dependencies

- Parent step status: In audit / re-audit remediation. The parent step branch is not merged or tagged.
- Required features/contracts: Step 08 transition compilation, Step 11 package verifier, Step 16 KMC engine.
- Historical: the parent audit was running when this amendment was proposed; it is withdrawn before the merge.

## Integration

- Branch: `feature/step-13-rust-solver-foundation-and-package-reader/fix-audit-round-1`
- Parent step branch: `step/13-rust-solver-foundation-and-package-reader`
- Approval: withdrawn; resolved by `1f5b473`; no acceptance required
- Squash commit: —
- Parent audit request updated: —
- Combined frozen commit: —

## Audit

- Parent audit record: [`docs/planning/audits/step-13.md`](../audits/step-13.md)
- Audit verdict: **Fixes required** (round 1); re-audit required after the amendment decision
- Merge commit: —
- Tag: —
- Post-merge fallback required: no (the parent step is not merged or tagged)

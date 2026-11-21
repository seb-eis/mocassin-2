# Step 11 Amendment 2 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-11-amend-2.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer (amendment 2) |
| Request owner | Planner (design freeze); the implementor is assigned after the verdict |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — post-merge maintenance branch after approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `6ff913671351798cb64678b506986ea6649364a4` |
| Docs branch | `docs/step-11-amend-2` |
| Review date | 2026-11-21 |
| Reviewer | — |

Step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)
Amendment page: [`Step 11 Amendment 2 — Cluster Occupation Code Expansion Map`](../amendments/step-11-amend-2-cluster-code-expansion.md)

**Freeze note.** The design docs, including this request record, were committed as the design freeze
`6ff913671351798cb64678b506986ea6649364a4`; the `Reviewed commit` cell is filled with that SHA by this
documentation-only request follow-up. The only delta between the freeze commit and this request commit is this
cell and the freeze SHA in this sentence. The freeze includes the amendment page, the data-contract,
architecture, ADR, open-decision, reference, step-page, index, and status updates. No code, test, configuration,
contract, generated artifact, or fixture is part of the freeze.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and implementor update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation (post-merge amendment; Step 11 is `Done`, merged at `84ceb76`, tagged `step-11`) |
| Depends on | Step 11 (`Done`, tagged; amendment 1 integrated) and Step 13 (`Done`, tagged; Rust reader exists). Step 14 design-review round 3 (record `1ea8090`, reviewed design `87be9de`, branch `review/step-14-design-round-3`, not merged) records F-29. No released `.msl` exists. |
| Delivers | One optional per-cluster-table `code_expansions` member in the `.msl` model payload, mapping every raw slot-tuple code to its canonical occupation index and applied energy; writer and verifier support; Rust reader validation and lookup; a new two-member cluster fixture, a regenerated singleton fixture, and a byte-fixed pre-map fixture; the documentation, signature/version policy, and post-merge `step-11.1` audit/tag plan. No solver physics code. |

## Design decisions reviewed

- [ ] **F-29 problem and evidence.** The `.msl` cluster table stores only canonical orbit codes; the solver
      builds the raw slot tuple and cannot resolve non-canonical members. Verify the eight-to-six ceria
      reduction, the two failing raw codes, and that the original stored every equivalent code.
- [ ] **Additive map shape.** One optional `code_expansions` member per table with strictly ascending
      `expanded_codes:[u64]` and parallel `canonical_indices:[u32]`; `null`/absent means singleton orbits.
      Verify the shape is exact, compact, and resolves every raw tuple without a sort/minimum heuristic.
- [ ] **Producer coverage and duplicate control.** `EnergyPayloadMapper` builds the map from the existing
      `CompiledClusterOccupation.Expansions`; no second symmetry pass. Verify the `GroupLookupCodes`-equivalent
      coverage proof, the canonical self-entry rule, strict ordering, and the internal invariant on duplicates.
- [ ] **Signature and canonical-projection impact.** The map is not a new signature input; `signatures.model`
      already projects the expansions, the parameterization projection has no cluster codes, and the lattice
      projection is untouched. Verify no projection/vector/generated-artifact change and that
      `package-summary-v1` is unchanged.
- [ ] **Version policy.** Optional additive member at `format_version = 1` while `.msl` is unreleased, with the
      ADR 0013 addendum, the ADR 0009 pre-release basis, defined absence semantics, a stop condition at first
      release, and current-major rather than previous-major fixtures.
- [ ] **Reader bounds and Step 14 semantics.** No new `ReaderLimits` field; the map is bounded by the model
      payload guard, element budget, and declared-length check. The lookup uses the map when present, the direct
      canonical table when absent, and fails closed with a recompile-required error for a non-canonical tuple in
      an old package.
- [ ] **Fixtures and regeneration.** A new ceria-derived two-member package with an expectation document;
      `step-13-populated` regenerated with an unchanged summary; a byte-fixed pre-map package; byte-identity
      assertions for the packages without cluster tables; deterministic regeneration.
- [ ] **Post-merge `step-11.1` process.** Maintenance branch from `main`, focused design review, separate audit,
      one manual closeout CI run, `--no-ff` merge, tag only after green `main` CI, and no movement of `step-11`.
      Verify the closeout-evidence ID gap is recorded, not silently worked around.
- [ ] **Documentation, status, parity, and audit plan.** Data contracts §3.3/§3.4, architecture §3.1/§7.6, ADR
      0013 addendum, open decisions, Step 11 page, Step 14 cross-step note, `msl-format.md`, fixture READMEs,
      parity evidence after implementation.
- [ ] **Licensing and provenance.** No GPL-derived content; the new fixture derives from the MIT-licensed ceria
      sample; the original `GroupLookupCodes` behaviour is re-implemented from the documented design, not copied.
- [ ] **Complexity and split.** Class Low, one maintenance delivery, no split; the hard orbit reasoning stays in
      the existing Step 02/08 reduction and is proven by a property test.

## Dependency and interface checks

- [ ] Step 11 is `Done`, merged and tagged; amendment 1 is integrated; the parent audit is closed, so the
      post-merge fallback is the correct path and no parent audit is running.
- [ ] Step 13 is `Done` and tagged; the Rust reader and `ReaderLimits` exist; adding an optional payload field
      and a runtime accessor is a compatible evolution under the documented unknown-field policy.
- [ ] The Step 14 design's F-29 is addressed by this amendment without editing the unmerged Step 14 design; the
      cross-step note and the re-review path are explicit.
- [ ] Inputs, outputs, types, and formats are defined: the stored map shape, the `.NET` DTO/API, the Rust
      payload/runtime types, the canonical index semantics, and the Step 14 lookup contract.
- [ ] Versioning and migration are defined: optional v1 member, absent-field behaviour, the pre-map fixture,
      the forward-read check, and the first-release stop condition.
- [ ] No unresolved cross-step interface question remains, or the open items (closeout-evidence ID, manifest
      granularity) are recorded with owners.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The implementor adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (planner or implementor) completes this section before the orchestrator presents the merge/tag
approval request. It is not part of this design-review request.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The implementor fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict

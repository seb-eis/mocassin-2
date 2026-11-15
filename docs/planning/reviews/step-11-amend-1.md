# Step 11 Amendment 1 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-11-amend-1.md`. You may edit only your own metadata (`Reviewer`, `Review date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 — Simulation Package and `.msl` Writer (Amendment 1: .NET 11 toolchain and native Zstandard) |
| Amendment ID | `step-11-amend-1` |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Assigned for Step 11; the amendment feature branch is created after the design verdict |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `06774ee874786d5a95424705b19900cd6e0035cf` |
| Docs branch | `step/11-simulation-package-and-msl-writer` |
| Review date | 2026-11-15 |
| Reviewer | — |

Amendment page:
[`step-11-amend-1-dotnet-11-native-zstd.md`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md)

Parent step page:
[`Step 11 — Simulation Package and .msl Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

Parent design review record: [`step-11.md`](step-11.md) (`Ready with notes`, accepted 2026-11-15)

Frozen state: `06774ee874786d5a95424705b19900cd6e0035cf` is the design freeze, re-frozen on 2026-11-15 after the
human owner answered the amendment's five open questions. The record was first created at the earlier freeze
`2124e5a7109f44e2b59e5c14c12dc1181677bffa`; between that freeze and this one the amendment page, the amendment
index, and `05-open-decisions.md` changed to record the owner decisions (container-branch disposition, image-build
risk, ReportGenerator, Test SDK, reference harness). The request-update commit changes only this record: the
reviewed commit, the owner-decision checklist item, and this note. A reviewer can verify the frozen design with
`git diff --name-status 06774ee874786d5a95424705b19900cd6e0035cf HEAD`.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the amendment
> design and completed this request, and relays the verdict and owner decisions. The planner and step agent
> update the design docs, add resolutions without altering reviewer text, and ask the orchestrator for
> re-review until the auditor no longer identifies issues. The orchestrator does not edit the design or the
> review record. The human owner approves the final verdict and the start of the amendment.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation (amendment to Step 11) |
| Depends on | Step 11 features 1–6 squashed; feature 7 deliberately held; parent audit not started |
| Delivers | Repo-wide `net10.0` → `net11.0` retarget on SDK 11.0.100, removal of `ZstdSharp.Port`, and replacement of the `.msl` payload codec with the in-box `System.IO.Compression` Zstandard API — with no `.msl` format, manifest, canonical-signature, `package-summary-v1`, or solver-contract change |
| Sizing | Toolchain and dependency migration only; the amendment feature is one branch. The codec work is two internal encoder/decoder files plus test support. No expected person-week change to the Step 11 estimate |
| Review scope | The amendment delta and its impact on the parent step, not a full re-review of the Step 11 design |

## Delta scope

The review covers the amendment delta only:

1. **Toolchain** — `global.json`, the central `TargetFramework`, the one explicit project outside the solution,
   the ApiCompat tool and baseline path, DocFX input metadata, the three workflows (which carry no SDK
   literal), and the development image.
2. **Payload codec** — the single fixed encoding path, the quality mapping, decode bounds and allocation caps,
   error behaviour, and compatibility with the Rust reader and with packages written before the switch.
3. **Contract impact** — the explicit claim that no `.msl` schema, payload field, manifest entry, canonical
   signature, `package-summary-v1`, or solver contract changes, and the fixture consequences that follow.
4. **Benchmark gate and rollback** — the real-payload comparison required before acceptance, the 10⁴-job budget
   required before the audit freeze, the thresholds, and the rollback rule.
5. **Integration sequencing** — feature 7 held, no fixture generated on `net10.0`, and integration before the
   Step 11 audit freeze.

Out of scope for this review: re-reviewing Step 11's payload mapping, pair-link expansion, tracker order,
schema, publication barrier, verifier, `PKG` codes, parity rows, or the accepted parent design. Those were
reviewed in [`step-11.md`](step-11.md) and are unchanged.

## Design decisions reviewed

- [ ] Amendment rationale and timing — why the switch belongs in Step 11 rather than a later step, and why the
      held feature 7 makes this the cheapest point.
- [ ] Scope and non-goals — the retarget and codec replacement are in scope; no format, signature, summary,
      manifest, solver, or unrelated package change is claimed.
- [ ] Toolchain migration surface — the file inventory and counts are complete and correct: 21 solution
      projects via the central TFM, one explicit project outside the solution, two ApiCompat scripts, nine
      baseline assemblies, ten DocFX entries, three workflows, one image, and the named documentation.
- [ ] SDK pin and roll-forward policy — `11.0.100` with `latestPatch`, `allowPrerelease: false`, and the
      rationale for not using `latestFeature`.
- [ ] CI and platform gates — no SDK literal in the workflows, `actions/setup-dotnet@v6` uses
      `global-json-file`, the Windows gate is already required by R2-7, and macOS stays manual.
- [ ] Development image — 11.0.100 only, the `COPY global.json` pin assertion, the ReportGenerator 5.6.0 bump
      that the .NET 10 runtime removal forces, DocFX 2.81.0 unchanged, and the unverified image build.
- [ ] Encoder design — the one-shot `ZstandardEncoder` path, `SetSourceLength`, `GetMaxCompressedLength`,
      `isFinalBlock`, default window/checksum/dictionary settings, and why `ZstandardStream` is rejected.
- [ ] Quality mapping — identity mapping of `PackageBuildOptions.CompressionLevel` 1..19 to native `Quality`,
      the 3 and 9 pins, and the unchanged public range.
- [ ] Decoder design and bounds — `TryGetMaxDecompressedLength`, the guard, `Array.MaxLength`, exact versus
      upper-bound allocation, the documented foreign-frame relaxation, and `PKG004` mapping.
- [ ] Error behaviour — `InvalidOperationException` on encode invariant violation,
      `InvalidDataException` on decode failure, `ArgumentOutOfRangeException` for levels outside 1..19.
- [ ] Contract non-impact — canonical signatures are MessagePack projections, `package-summary-v1` excludes
      payload bytes, and `contracts/**` must not change.
- [ ] Fixture consequences — no checked-in `.msl` exists, feature 7 generates fixtures once on the amended
      toolchain, and the extracted payload bytes are not a signature input.
- [ ] Cross-language compatibility — standard frames, `zstd` CLI and Rust `zstd` crate read support, and
      packages written before the switch remaining readable.
- [ ] Benchmark gate — the real-payload A/B method, the metric thresholds, the 10⁴-job budget, and the rollback
      rule.
- [ ] Risks and mitigations — .NET 11 maturity, tool/package compatibility, Windows/macOS, image build,
      cross-patch determinism, ApiCompat churn, analyzer regressions, and rollback.
- [ ] Licensing and dependency policy — `ZstdSharp.Port` removal, no new dependency, no GPL content, and the
      `check_nuget_licenses.py`/`policy` gate.
- [ ] Documentation and records — the named toolchain docs, the quality-strategy update, and the parent
      step/status/decision/index updates.
- [ ] Integration sequencing — amendment branch name, squash into the parent step branch, and integration
      before the audit freeze with the combined commit recorded.
- [ ] `docs/dotnet-11-container` disposition — supersede/fold/land-first options and the recommendation.
- [ ] Owner decisions — the five previously open questions (container branch, image build, ReportGenerator,
      Test SDK, reference harness) are answered and recorded in the amendment's `Owner decisions` section;
      no open question remains for this amendment.

## Dependency and interface checks

- [ ] Parent step state — features 1–6 squashed at `eb8513d`, `9b754af`, `c0d8d64`, `bdafaa4`, `4fe6225`,
      `cbe345f`; feature 7 held; no parent audit has started, so the amendment window in
      [`amendments/README.md`](../amendments/README.md) §4 is open.
- [ ] Parent contracts are stable — the payload DTOs, payload format versions, writer/verifier API, `PKG`
      codes, and `package-summary-v1` are implemented and unchanged by the amendment.
- [ ] Inputs, outputs, and formats are defined — the codec's inputs (MessagePack bytes), output (one standard
      Zstd frame), bounds, and error types are explicit.
- [ ] Versioning and migration behaviour are defined — no `.msl` version bump, no migration, pre-switch packages
      readable, and the future fixture policy stated.
- [ ] Cross-step impact is nil — Step 12, 13, 18, 20, and the Rust reader consume the same frames and the same
      payload contract; no cross-step interface question is left open by the amendment.
- [ ] The ApiCompat and DocFX gates can run on `net11.0` — the tool versions and reference-pack paths exist, and
      the one-time cross-TFM proof is planned before the .NET 10 runtime is removed.
- [ ] The benchmark gate is runnable with today's artifacts — real payloads can be extracted from a package
      built by the current writer, and both codecs can be referenced from one `net11.0` probe.

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable.
- [ ] Design decisions are complete and internally consistent.
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined.
- [ ] Documentation, status, parity, and audit updates are planned.
- [ ] Licensing, provenance, and dependency checks are resolved.
- [ ] Risks and mitigations are realistic.
- [ ] Code layout and namespace plan follow the structure policy.
- [ ] No hidden scope, stale placeholders, or unowned decisions.

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

*(The reviewer states its independence and the frozen state it read here.)*

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round. A design-gate verdict does not require the
cumulative report; the amendment merge/tag request does.

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

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict

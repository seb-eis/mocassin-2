# Step 11 Amendment 2 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-11-amend-2.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 11 amendment 2 — Cluster Occupation Code Expansion Map (`step-11-amend-2`) |
| Request owner | Step agent (implementor) |
| Orchestrator | — (recorded by the orchestrator) |
| Step agent (implementor) | Step 11.1 implementation context |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `fix/step-11.1-cluster-code-expansion` (post-merge maintenance branch from `main` `2f3a6fe`) |
| Closeout evidence | `this record` — the A2-07 post-merge amendment rule; `docs/planning/closeout/step-11.json` is historical and is not rewritten |
| Audit date | — |
| Auditor | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge
candidate, merge-tree CI, platform CI, and tag. A post-merge amendment has no evidence file; this record is the
one source (see [`README.md`](README.md#31-post-merge-amendment-audits)).

Parent step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

Amendment: [`step-11-amend-2`](../amendments/step-11-amend-2-cluster-code-expansion.md)

Design review: [`step-11-amend-2`](../reviews/step-11-amend-2.md) — round 4 `Ready for implementation` at
`bf273ff5cf9f17f66ec48669e39d010006c4d0a9`.

### Post-merge evidence

| Field | Value |
| --- | --- |
| Audited commit | `aa625294ec201dd8806724e63a2f6235a954de2b` (the maintenance branch head at freeze; `b3d967e` plus the `origin/main` merge `63cdd06`) |
| Closeout CI run | [`44895491322`](https://github.com/seb-eis/mocassin-2/actions/runs/44895491322) — Ubuntu `CI`, conclusion `success` |
| Closeout CI `head_sha` | `aa625294ec201dd8806724e63a2f6235a954de2b` |
| `main` SHA at freeze | `63cdd0638b952e9175f75681d864bb95db0b4f68` (merged into the branch as `aa62529` before the gate) |
| Merge candidate | `aa625294ec201dd8806724e63a2f6235a954de2b` plus this record commit (documentation-only, `[skip ci]`); the code state is unchanged |
| Merge-tree CI | Not required: `main` `63cdd06` was merged before the freeze and the full gate plus closeout CI ran on the merged tree |
| Platform CI | **Not run** — no P/Invoke, native library, RID, path, GUI, or platform-conditional code is touched |
| Tag | `step-11.1` after owner approval and a green `main` CI run; `step-11` is never moved |

## Step summary

| Field | Value |
| --- | --- |
| Phase | C — Compilation (post-merge amendment) |
| Depends on | Step 11 `Done` and tagged `step-11`; Step 13 `Done` and tagged; no released `.msl` |
| Delivers | An optional, additive `code_expansions` member per `.msl` cluster table, the .NET writer/verifier/pre-scan path, the Rust reader/scan/lookup path, and the non-singleton and absence fixtures |

## Step-specific acceptance checklist

Copied from the amendment page, preserving the wording:

- [ ] `EnergyPayloadMapper` emits a complete, strictly ascending `code_expansions` map for the ceria two-member
      table; every enumerated raw tuple resolves; the two non-canonical codes map to their canonical indices and
      energies.
- [ ] Singleton tables emit `nil`; the writer never emits a partial, duplicate, or non-ascending map; a
      violation throws an internal invariant error.
- [ ] The property test proves `GroupLookupCodes` **completeness** against the reduction oracle for random small
      clusters, accepts superset orbit codes, and includes an asymmetric-allowed-set case (A2-05).
- [ ] The `.NET` verifier applies the structural rules and reports `PKG010` for every violation; a valid map
      verifies.
- [ ] The .NET decode pre-scan and the Rust scan enforce the cumulative `128 MiB` expansion budget before the
      typed arrays are allocated, with fail-closed errors and hostile-payload tests; the limit is raisable
      (A2-01).
- [ ] The Rust scan supports `code_expansions` as an **optional** key; a pre-map package passes the scan, and a
      malformed present map is rejected with the field path (A2-02).
- [ ] The Rust reader validates the same rules, exposes `ClusterTable::lookup` with the three-way
      `ClusterCodeLookup` result, and rejects hostile maps with a field-specific `PackageError` (A2-R2-02).
- [ ] The `.NET` four-argument `ClusterTablePayload` constructor and `Deconstruct` are preserved alongside the
      five-argument forms; ApiCompat/`PublicAPI.Unshipped.txt` are clean (A2-03).
- [ ] The new fixture is deterministic, carries the documented expectation, and its `package-summary-v1` and
      signatures are the logical values from before the change.
- [ ] The regenerated `step-13-populated` fixture's summary is unchanged and its README size/SHA table is
      refreshed; the no-cluster fixtures' extracted payloads/summaries are unchanged (package bytes are a
      same-host observation) (A2-12/A2-15).
- [ ] A byte-fixed **non-singleton pre-map** package built by the parent writer loads, resolves its six
      canonical codes, and returns the recompile-required result for the two non-canonical codes (A2-06).
- [ ] The pre-amendment reader reads the new fixture in the one-time forward check (recorded as evidence, not a
      PR tier) (A2-13).
- [ ] `signatures.model`, `signatures.parameterization`, `signatures.lattice`, `signature-vectors.json`,
      `tests/contracts/projections/**`, and `contracts/**` are unchanged; `verify.sh contracts` is green.
- [ ] `.NET` and Rust public API additions are recorded (`PublicAPI.Unshipped.txt`, ApiCompat, §5/§6.1 of the
      amendment page); the old `ClusterTablePayload` constructor and `Deconstruct` still exist (A2-09).
- [ ] The audit/closeout pages and template define the post-merge amendment evidence rule; no page claims a
      `step-11.1` JSON evidence file (A2-07).
- [ ] `samples/README.md` and `samples/step-11-cluster-expansion/README.md` are added (A2-10).
- [ ] `./tools/verify.sh all --require` is green at the audit commit; the single closeout CI run is green; the
      main CI run is green before `step-11.1` is tagged.
- [ ] Documentation is updated (§8) and `./tools/verify.sh docs --require` passes.
- [ ] The Step 14 pointer names `87be9de`/`1ea8090` and records F-29's formal closure as a Step 14 gate; the
      parity row's evidence is updated after implementation (A2-08).

## Standard audit checklist

- [ ] Required tests from the amendment page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the change has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run by the implementor before the freeze:

```text
./tools/verify.sh all --require        exit 0 (full closeout gate: .NET restore/build/test/format,
                                       ApiCompat and dependency policy, Rust fmt/clippy/test, Python,
                                       strict Zensical/DocFX/rustdoc, contracts/policy, AOT evidence)
dotnet build Mocassin.slnx -c Release  0 warnings, 0 errors
dotnet test Mocassin.slnx -c Release   1,989 tests pass (Contracts 38, Symmetry 223, Results 79,
                                       Core 685, Compilation 99, Cli 203, Packages 534,
                                       Architecture 7, App 121)
dotnet format Mocassin.slnx --verify-no-changes --no-restore   clean
cargo fmt --all --check                clean
cargo clippy --workspace --all-targets -- -D warnings          clean
cargo test --workspace                 all targets pass
cargo build --workspace --release      clean
./tools/verify.sh contracts            exit 0: 26 artifacts current, 60 process tests, SPDX 1010,
                                       secrets 1,498, cargo-deny OK
./tools/verify.sh docs                 exit 0: math OK, links OK, strict site/DocFX/rustdoc build
```

One-time forward read (parent toolchain at `bbc2d083ed96fa6e26526e335304107b20a7fdcb`):

```text
dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- verify <new fixture>
  -> all ten PKG checks pass, exit 0
cargo run -p mocassin-cli -- --db <new fixture> --job 1 --dry-run
  -> validation: ok
```

The single closeout CI run `44895491322` is green at `aa625294ec201dd8806724e63a2f6235a954de2b`
(`main` `63cdd0638b952e9175f75681d864bb95db0b4f68` was merged before the gate). Platform CI is not run
(no platform-conditional code).

Evidence file: this record (post-merge amendment, per the request row)

## Known notes for the auditor

- The new sample is a **P-1 origin-shifted** form of the ceria primary cluster, not the full space-group-225
  cell. The P-1 inversion fixes the centre and swaps the two member slots, producing the same eight raw codes
  and six canonical codes as the design table. The full 225 cell generated multiple symmetry-equivalent KMC
  paths from one start and tripped `CMP004`; the translated representation keeps the cluster identities,
  offsets, and codes and makes the fixture unambiguous. The sample/fixture READMEs record the deviation.
- The pre-map fixture `amend-2-pre-map` is byte-fixed and never regenerated. It is a **non-singleton** package
  with no `code_expansions`, so the tests exercise the real fail-closed absence path rather than a synthetic
  DTO.
- The fixture's `package-summary-v1` and both signatures are byte-for-byte equal to the pre-map fixture; only
  the model payload gains the optional member.
- `signatures.model` already binds every expansion code and permutation index, `signatures.parameterization`
  contains applied values only, `package-summary-v1` has no code-level fields, and `contracts/**` is unchanged.
- Step 14 consumes `ClusterTable::lookup`; F-29's formal closure is a Step 14 gate and is not a `step-11.1`
  acceptance criterion.
- Platform CI is not required: the change touches no platform-conditional code.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

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

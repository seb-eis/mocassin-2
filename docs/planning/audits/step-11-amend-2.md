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
| Audit date | 2026-11-21 |
| Auditor | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh audit context |

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
| Audited commit | `ef568beca62f6f51559460d01ba5627219e90345` (the maintenance branch head at freeze: `b3d967e`, the `origin/main` merge `63cdd06`, the audit-request record `5f9673f`, and the reduction-oracle property test `ef568be`) |
| Closeout CI run | [`44896189617`](https://github.com/seb-eis/mocassin-2/actions/runs/44896189617) — Ubuntu `CI`, conclusion `success` (the earlier `44895491322` at `aa62529` was superseded when the A2-05 property test was added) |
| Closeout CI `head_sha` | `ef568beca62f6f51559460d01ba5627219e90345` |
| `main` SHA at freeze | `63cdd0638b952e9175f75681d864bb95db0b4f68` (merged into the branch as `aa62529` before the gate) |
| Merge candidate | `ef568beca62f6f51559460d01ba5627219e90345` plus this record commit (documentation-only, `[skip ci]`); the code state is unchanged |
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

- [x] `EnergyPayloadMapper` emits a complete, strictly ascending `code_expansions` map for the ceria two-member
      table; every enumerated raw tuple resolves; the two non-canonical codes map to their canonical indices and
      energies.
      — Verified: the new fixture's table 0 has codes `8322,8323,8386,8387,12418,12419,12482,12483` and indices
      `0,1,2,3,2,3,4,5`; `8386→index 2 (12418)` and `8387→index 3 (12419)`; raw 6-bit encoding checked.
- [x] Singleton tables emit `nil`; the writer never emits a partial, duplicate, or non-ascending map; a
      violation throws an internal invariant error.
      — Verified: `step-13-populated` carries an explicit `code_expansions: nil` (bytes `af code_expansions c0`);
      `MapCodeExpansions` sorts, collapses same-index duplicates, and throws on cross-orbit duplicates, high
      bits, particle range, and missing canonical self-entry; writer tests pass.
- [x] The property test proves `GroupLookupCodes` **completeness** against the reduction oracle for random small
      clusters, accepts superset orbit codes, and includes an asymmetric-allowed-set case (A2-05).
      — Verified: `ClusterExpansionPropertyTests` (40 FsCheck cases plus the asymmetric `{O,Vc}`/`{O,Vc,H}`
      fact) ran in the 1,991-test gate and in the targeted `ClusterExpansion` filter (7/7).
- [x] The `.NET` verifier applies the structural rules and reports `PKG010` for every violation; a valid map
      verifies.
      — Verified: `ClusterExpansionVerificationTests` covers unequal lengths, non-ascending, out-of-range index,
      high bits, particle range, missing self-entry, superset acceptance, and the engine's `PKG010` mapping
      (23/23 targeted tests).
- [x] The .NET decode pre-scan and the Rust scan enforce the cumulative `128 MiB` expansion budget before the
      typed arrays are allocated, with fail-closed errors and hostile-payload tests; the limit is raisable
      (A2-01).
      — Verified: `PackagePayloadDecoder.TryDecodeModel` runs `ClusterExpansionScan.Validate` before
      `Deserialize<ModelPayload>`; `decode_model_payload` runs `scan_model` before `rmp_serde::from_slice`;
      the scan accumulates `Σ(8E + 4I)` and rejects `E != I`/duplicates; 95-byte rejection and 96-byte acceptance
      on the real fixture work on both paths; defaults are 128 MiB (`ReaderLimits` test `134_217_728`).
- [x] The Rust scan supports `code_expansions` as an **optional** key; a pre-map package passes the scan, and a
      malformed present map is rejected with the field path (A2-02).
      — Verified: `CLUSTER_TABLE_OPTIONAL_KEYS = ["code_expansions"]`; `CLUSTER_TABLE_KEYS` remains the four
      required keys; the pre-map fixture scans and decodes; duplicate optional/nested keys are rejected by the
      seen-mask (2/2 scan unit tests; 5/5 integration tests).
- [x] The Rust reader validates the same rules, exposes `ClusterTable::lookup` with the three-way
      `ClusterCodeLookup` result, and rejects hostile maps with a field-specific `PackageError` (A2-R2-02).
      — Verified: `map_cluster_expansions` checks length, ascending, indices, high bits, particles, self-entry,
      and the cumulative budget; `lookup` returns `Canonical`, `MissingFromMap`, and `ExpansionMapAbsent` as
      specified; the hostile-map tests name `energy.cluster_tables.code_expansions`.
- [x] The `.NET` four-argument `ClusterTablePayload` constructor and `Deconstruct` are preserved alongside the
      five-argument forms; ApiCompat/`PublicAPI.Unshipped.txt` are clean (A2-03).
      — Verified: `EnergyPayloadDtos.cs` has both constructors and both `Deconstruct` overloads; the
      `PublicAPI.Unshipped.txt` diff lists both; ApiCompat reports no breaking changes; the DTO surface test
      passes.
- [x] The new fixture is deterministic, carries the documented expectation, and its `package-summary-v1` and
      signatures are the logical values from before the change.
      — Verified: `ClusterExpansionPackageFixtureTests` rebuilds twice with equal SHA-256, the payload hash and
      index match, the expectation JSON matches, and the new package's summary is byte-identical to
      `amend-2-pre-map`; both model/parameterization signatures are identical in the two model payloads.
- [x] The regenerated `step-13-populated` fixture's summary is unchanged and its README size/SHA table is
      refreshed; the no-cluster fixtures' extracted payloads/summaries are unchanged (package bytes are a
      same-host observation) (A2-12/A2-15).
      — Verified: `git diff 63cdd06..ef568be -- .../step-13-populated/package-summary-v1.json` is empty; the
      README has the new 7,026-byte model and `7a9c…` SHA; `msl-v1`, `step-13-centered`, `amend-1-net10`, and
      `amend-1` have no diff.
- [x] A byte-fixed **non-singleton pre-map** package built by the parent writer loads, resolves its six
      canonical codes, and returns the recompile-required result for the two non-canonical codes (A2-06).
      — Verified: `amend-2-pre-map` has six canonical codes and no map; the Rust reader returns `Canonical` for
      the six and `ExpansionMapAbsent` for `8386`/`8387`; the .NET reader decodes `CodeExpansions == null`.
- [x] The pre-amendment reader reads the new fixture in the one-time forward check (recorded as evidence, not a
      PR tier) (A2-13).
      — Reproduced by the auditor at parent `bbc2d08`: .NET `verify` exit 0 with all ten PKG checks; Rust
      `--dry-run` printed `validation: ok`; both ignored the unknown key.
- [x] `signatures.model`, `signatures.parameterization`, `signatures.lattice`, `signature-vectors.json`,
      `tests/contracts/projections/**`, and `contracts/**` are unchanged; `verify.sh contracts` is green.
      — Verified: `git diff 63cdd06..ef568be` has no `contracts/**`, `signature-vectors`, or projection change;
      the gate reports 26 artifacts current and the projection checks OK.
- [x] `.NET` and Rust public API additions are recorded (`PublicAPI.Unshipped.txt`, ApiCompat, §5/§6.1 of the
      amendment page); the old `ClusterTablePayload` constructor and `Deconstruct` still exist (A2-09).
      — Verified: both Contracts/Results `PublicAPI.Unshipped.txt` entries are present; Rust
      `ClusterExpansions`/`ClusterCodeLookup` are re-exported; the hidden `expansions` field has a doc comment;
      ApiCompat and rustdoc are clean.
- [x] The audit/closeout pages and template define the post-merge amendment evidence rule; no page claims a
      `step-11.1` JSON evidence file (A2-07).
      — Verified: `audits/README.md` §3.1, `closeout/README.md`, and `step-audit-template.md` name the audit
      record as the evidence source; the only `step-11.1.json` mention is the amendment's historical rejected
      option.
- [x] `samples/README.md` and `samples/step-11-cluster-expansion/README.md` are added (A2-10).
      — Verified: both are present; the sample README records the derivation, the P-1 origin shift, the job, and
      the build command.
- [x] `./tools/verify.sh all --require` is green at the audit commit; the single closeout CI run is green; the
      main CI run is green before `step-11.1` is tagged.
      — Auditor ran `./tools/verify.sh all --require` at `1965eae` (docs-only above `ef568be`): exit 0, 1,991
      .NET tests, Rust/Python/contracts/docs/AOT evidence green. Closeout CI `44896189617` is green at
      `ef568be`. The `main` CI run and the tag are post-merge conditions; no `step-11.1` tag exists yet and
      `step-11` is unchanged.
- [x] Documentation is updated (§8) and `./tools/verify.sh docs --require` passes.
      — Verified: the data contract, architecture, ADR 0013 addendum, open decisions, `msl-format`, step pages,
      fixture/sample READMEs, and parity row are updated; the strict docs gate passed. Two documentation notes:
      A2A-01 and A2A-02.
- [x] The Step 14 pointer names `87be9de`/`1ea8090` and records F-29's formal closure as a Step 14 gate; the
      parity row's evidence is updated after implementation (A2-08).
      — Verified: the Step 14 page cross-step note names both commits and the gate; the parity row cites
      `08/11.1` and the expansion map.

## Standard audit checklist

- [x] Required tests from the amendment page pass.
      — Auditor ran the full gate plus targeted `ClusterExpansion` filters (Packages 7/7, Results 23/23), Rust
      `cluster_expansions` (5/5 integration, 7/7 map, 2/2 scan), and `verify.sh structure` (7/7).
- [x] Public/protected XML documentation policy is followed.
      — New public types/members have XML docs; `dotnet build` is 0 warnings; rustdoc `missing_docs = deny`
      passes.
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
      — Updated; the only findings are the two Minor README notes (A2A-01/A2A-02) and the checklist note
      (A2A-03).
- [x] Closeout CI is green at the audited commit.
      — API check: run `44896189617`, `workflow_dispatch`, `head_sha` `ef568be…`, conclusion `success`.
- [x] Closeout evidence matches the audited commit and the merge candidate.
      — The record's audited commit, CI `head_sha`, `main` SHA, and merge candidate match `ef568be`, `63cdd06`,
      and this record's docs-only commit.
- [x] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
      — `main` and `origin/main` are both `63cdd06`; no move after the freeze, so no merge-tree run is required.
- [x] Windows/macOS runs are recorded, or their absence is accepted because the change has no platform-specific
      code.
      — The run has six Ubuntu jobs; platform test steps are skipped; the change touches no P/Invoke, native
      library, RID, path, GUI, or platform-conditional code.
- [x] Format/version/fixture changes are documented and tested.
      — `format_version = 1`, manifest untouched, `.msl` v1 additive member, pre-map fixture byte-fixed, new and
      regenerated fixtures tested; no released `.msl` (`git tag -l 'v*'` empty).
- [x] Security, dependency, licence, and secret-scan gates pass.
      — Gate: SPDX headers in 1,011 files, no secret patterns in 1,499 files, cargo-deny advisories/bans/
      licenses/sources OK, NuGet licence policy OK.
- [x] Code structure policy passes; no new root-level or catch-all folders.
      — `verify.sh structure` 7/7; new files are in existing concept/test folders and under the 800-line limit.
- [x] No unrelated changes or unapproved scope.
      — The amendment commits touch the writer/DTO/scan/reader/tests/fixtures/sample/docs; the `step-23a` docs
      in the range come from the `origin/main` merge `aa62529`, not the amendment.
- [x] Branch, tag, and status hygiene are correct.
      — Branch `fix/step-11.1-cluster-code-expansion`; `step-11` still at `84ceb76`; no `step-11.1` or `v*` tag;
      status/amendment index say audit requested, not passed.

## Evidence

Commands run by the implementor before the freeze:

```text
./tools/verify.sh all --require        exit 0 (full closeout gate: .NET restore/build/test/format,
                                       ApiCompat and dependency policy, Rust fmt/clippy/test, Python,
                                       strict Zensical/DocFX/rustdoc, contracts/policy, AOT evidence)
dotnet build Mocassin.slnx -c Release  0 warnings, 0 errors
dotnet test Mocassin.slnx -c Release   1,991 tests pass (Contracts 38, Symmetry 223, Results 79,
                                       Core 685, Compilation 99, Cli 203, Packages 536,
                                       Architecture 7, App 121); this includes
                                       `ClusterExpansionPropertyTests` (40 randomized
                                       geometries against the reduction oracle plus the
                                       asymmetric-allowed-set superset case)
dotnet format Mocassin.slnx --verify-no-changes --no-restore   clean
cargo fmt --all --check                clean
cargo clippy --workspace --all-targets -- -D warnings          clean
cargo test --workspace                 all targets pass
cargo build --workspace --release      clean
./tools/verify.sh contracts            exit 0: 26 artifacts current, 60 process tests, SPDX 1,011,
                                       secrets 1,499, cargo-deny OK
./tools/verify.sh docs                 exit 0: math OK, links OK, strict site/DocFX/rustdoc build
```

One-time forward read (parent toolchain at `bbc2d083ed96fa6e26526e335304107b20a7fdcb`):

```text
dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- verify <new fixture>
  -> all ten PKG checks pass, exit 0
cargo run -p mocassin-cli -- --db <new fixture> --job 1 --dry-run
  -> validation: ok
```

The final closeout CI run `44896189617` is green at `ef568beca62f6f51559460d01ba5627219e90345`
(`main` `63cdd0638b952e9175f75681d864bb95db0b4f68` was merged before the gate). The earlier run
`44895491322` at `aa62529` is process history: the reduction-oracle property test (A2-05) was added after it, so
it was superseded by the final run. Platform CI is not run (no platform-conditional code).

Evidence file: this record (post-merge amendment, per the request row)

### Auditor evidence (fresh context, 2026-11-21)

Independence and frozen state:

- I am a fresh context. I did not author or implement the amendment, the Step 11/13 work, the design review, or
  the audit request, and I have no shared conversation with the planner, implementor, or orchestrator. I read the
  repository at `/home/node/worktrees/step-11.1-audit` and edited only this record.
- `git rev-parse ef568be` = `ef568beca62f6f51559460d01ba5627219e90345` (the audited code commit);
  `git rev-parse 1965eae` = `1965eaea56af241f75d96d8ceda415c45f7341c8` (branch head). `git diff --name-status
  ef568be..1965eae` is exactly three documentation files: the amendment page, this audit record, and the Step 11
  page; no code, test, configuration, contract, generated artifact, or fixture changed after the frozen code.
- `git diff --name-status 2f3a6fe..b3d967e` is the implementation (64 files: source, tests, fixtures, sample,
  docs); `git show --name-only ef568be` is the single property-test file. The `step-23a` documentation in the
  `2f3a6fe..ef568be` range comes from the `origin/main` merge `aa62529`, not the amendment.
- `git rev-parse 63cdd06` and `git rev-parse origin/main` are both
  `63cdd0638b952e9175f75681d864bb95db0b4f68`; `git cat-file -t step-11` is `tag`; `git rev-parse step-11^{}` is
  `84ceb76eaa3e14c7cbe42c7ccbdc9842611891bc`; `git tag --list 'step-11*'` is only `step-11`; `git tag --list 'v*'`
  is empty. No `step-11.1` tag exists and `step-11` is not moved.

Gates and tests run by the auditor:

| Command | Result |
| --- | --- |
| `./tools/verify.sh all --require` | exit 0. Full gate: .NET restore/build (0 warnings, 0 errors)/1,991 tests/format; ApiCompat no breaking changes; NuGet licence and vulnerability; Rust `fmt`/`clippy`/`test`/release build; Python tests/build; strict Zensical/DocFX/rustdoc; 26 contract artifacts current, 60 process tests, model/parameterization/lattice projections OK, space-group catalog current, SPDX 1,011 files, secrets 1,499 files, cargo-deny OK; AOT evidence (pre-existing 62-diagnostic real-CLI log recorded, smoke/control pass). |
| `dotnet test src/dotnet/Mocassin.Packages.Tests/... --filter "FullyQualifiedName~ClusterExpansion"` | 7/7 passed. Includes the 40-case reduction-oracle property and the asymmetric-allowed-set case. |
| `dotnet test src/dotnet/Mocassin.Results.Tests/... --filter "FullyQualifiedName~ClusterExpansion"` | 23/23 passed (fixture, budget, verifier, pre-map read, PKG004/PKG010 paths). |
| `cargo test --manifest-path src/rust/Cargo.toml -p mocassin-msl --test cluster_expansions` | 5/5 passed (new fixture, pre-map absence, optional-key scan, unequal pair, budget). |
| `cargo test ... -p mocassin-msl cluster_expansions` | 7/7 map validation/lookup unit tests passed. |
| `cargo test ... -p mocassin-msl --lib scan::tests` | 2/2 duplicate optional/nested scan tests passed. |
| `./tools/verify.sh structure --require` | exit 0; 7/7 architecture/structure tests. |

CI verification (GitHub API, read-only):

- Run [`44896189617`](https://api.github.com/repos/seb-eis/mocassin-2/actions/runs/44896189617):
  `workflow_dispatch`, `head_branch` `fix/step-11.1-cluster-code-expansion`, `head_sha`
  `ef568beca62f6f51559460d01ba5627219e90345`, `status` completed, `conclusion` success, six jobs all
  `ubuntu-latest`, platform test steps skipped (`skipped`). This matches the record's audited commit and
  `head_sha`.
- Run [`44895491322`](https://api.github.com/repos/seb-eis/mocassin-2/actions/runs/44895491322):
  `workflow_dispatch`, `head_sha` `aa625294ec201dd8806724e63a2f6235a954de2b`, conclusion success. This is the
  superseded run recorded transparently in the request; the property test (`ef568be`) post-dates it.

Format, fixture, and signature evidence:

- Python `msgpack` inspection of the committed `payloads/model-1.bin` files: `step-11-amend-2` has 1 cluster
  table, 6 occupation codes `[8322,8323,12418,12419,12482,12483]`, and `code_expansions` with 8 codes
  `[8322,8323,8386,8387,12418,12419,12482,12483]` / indices `[0,1,2,3,2,3,4,5]`; `amend-2-pre-map` has 6 codes
  and **no** key; `step-13-populated` has 12 codes and an explicit nil key; `msl-v1` and `step-13-centered` have
  0 cluster tables.
- `sha256sum` matches every recorded `.sha256` for `step-11-amend-2` (`b8a9…`), `amend-2-pre-map` (`97a9…`), and
  `step-13-populated` (`7a9c…`). The new and pre-map `package-summary-v1.json` files are byte-identical
  (`diff` exit 0). The `step-13-populated` summary has no diff from `main` `63cdd06`. The model/parameterization
  signatures in the new and pre-map payloads are both `90d4101a…` / `af2efa62…`.
- Raw bytes show the smallest-width encoding: `code_expansions` is `0xaf` + 15-byte key + `0x82`, `expanded_codes`
  values are `0xcd` (uint16) 3-byte codes, and `canonical_indices` are 1-byte fixints. `format_version` is 1;
  `application_id` is `MOCS`; `user_version` is 1; tables are `jobs, lattices, models, schema_info` (no results,
  no `-wal`/`-shm`).
- `git diff --name-status 63cdd06..ef568be` has no `contracts/**`, `signature-vectors`, `tests/contracts/projections`,
  `msl-v1`, `step-13-centered`, `amend-1`, or `amend-1-net10` change. `contracts/manifest.json` is untouched.

One-time forward read (reproduced by the auditor, parent toolchain `bbc2d08`):

- `.NET` at `bbc2d08`: `dotnet run --project src/dotnet/Mocassin.Cli -c Release --no-build -- verify
  tests/contracts/packages/step-11-amend-2/step-11-amend-2.msl` → all ten PKG checks PASS, exit 0. The parent
  reader ignores the unknown `code_expansions` key.
- Rust at `bbc2d08`: `cargo run -p mocassin-cli -- --db .../step-11-amend-2.msl --job 1 --dry-run` →
  `validation: ok`, exit 0. The parent scanner skips the unknown key.
- Both parent builds were in a scratch worktree, removed after the run; no repository file was changed.

Documentation and process evidence:

- `docs/planning/02-data-contracts.md` §3.3/§3.4, `01-target-architecture.md` §3.1/§7.6, the ADR 0013 addendum,
  `05-open-decisions.md` items 7/128/163-169, `docs/reference/msl-format.md`, the Step 11/14 pages, the parity
  row, the amendment index, the status dashboard, and the fixture/sample READMEs contain the expected text.
- `audits/README.md` §3.1, `closeout/README.md`, and `step-audit-template.md` define the audit record as the
  single evidence source for a post-merge amendment; the closeout checker is unchanged. The only
  `step-11.1.json` mention is the amendment's historical rejected option, not a claim.
- `git diff --name-status ef568be..1965eae` is documentation-only; `git status --short` in the audit worktree is
  empty before this record edit.

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
| **Minor** | **A2A-01 — The pre-map fixture README states the wrong model-payload size delta.** It says the parent-writer model payload is "(24 bytes smaller)" than the new fixture; the actual difference is **84 bytes** (`6,090 − 6,006`). The 24-byte figure matches only the eight 3-byte `expanded_codes` values, not the whole encoded map (which also carries the key/header bytes and the eight index bytes). The committed fixtures, summaries, and hashes are correct; only the parenthetical is wrong. | `tests/contracts/packages/amend-2-pre-map/README.md:15`; `tests/contracts/packages/step-11-amend-2/README.md:14`; `tests/contracts/packages/step-11-amend-2/payloads/index.json` (`rawBytes: 6090`); `tests/contracts/packages/amend-2-pre-map/payloads/index.json` (`rawBytes: 6006`); raw-byte layout: key 16 + map header 1 + `expanded_codes` key 15 + array header 1 + 8×3-byte codes 24 + `canonical_indices` key 18 + array header 1 + 8×1-byte indices 8 = 84. |
| **Minor** | **A2A-02 — The new fixture README's Provenance omits the writer commit required by the design.** Amendment §9.1 requires the new fixture `README.md` to record "provenance, the writer commit, SDK, `created_utc`, and the job count". The README records the source project, build path, request, toolchain, `created_utc`, job count, and logical equality, but not the commit that produced the package. The pre-map README does record its parent writer (`bbc2d08`), and the audit record/step page name `ef568be`/`b3d967e`, so the information exists elsewhere; the fixture's own provenance is incomplete. | `tests/contracts/packages/step-11-amend-2/README.md` §Provenance (no writer-commit line); amendment §9.1 ("README.md with provenance, the writer commit, SDK, `created_utc`, and the job count"); `tests/contracts/packages/amend-2-pre-map/README.md:24` ("Writer commit: `bbc2d083…`"). |
| **Note** | **A2A-03 — The amendment page's acceptance/documentation checkboxes remain unchecked although the implementation and audit request are complete.** The durable audit record's copied checklist is marked and the step page/status say "implementation complete, audit requested", but the amendment page's own `Acceptance criteria` and `Documentation` lists are still `[ ]`. This is a reading/consistency nit, not a code/format issue; ticking them when the step agent records resolutions would remove the ambiguity. | `docs/planning/amendments/step-11-amend-2-cluster-code-expansion.md` lines 642-680 and 690-700; `docs/planning/steps/step-11-simulation-package-and-msl-writer.md` A2 feature log; `docs/planning/status.md` Step 11 row. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Passed with notes — round 1 found no Blocker or Major. Two Minor documentation findings (A2A-01,
A2A-02) and one Note (A2A-03) remain for explicit owner acceptance or a docs-only correction; the code, format,
fixtures, and gates are sound.

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

I am that fresh context: I did not author or implement the amendment, the Step 11/13 work, the design review, or
the audit request, and I have no shared conversation with the planner, implementor, or orchestrator. I read the
repository at the frozen code commit `ef568be` with the docs-only request head `1965eae`, ran the checks in the
`Evidence` section myself, and edited only this record. I did not fix findings, merge, tag, push `main`, or
delete branches; the human owner still approves the verdict and the `step-11.1` tag.

## Verdict

- [ ] Passed
- [x] Passed with notes
- [ ] Fixes required
- [ ] Waived

**Verdict: Passed with notes.** The amendment is technically correct and complete. The writer builds the map
from the existing reduction data with no second symmetry pass; the new fixture has the expected 8 raw → 6
canonical codes and `8386→12418`/`8387→12419`; singleton tables emit an explicit nil; the .NET DTO preserves the
four-argument constructor and `Deconstruct`; the .NET pre-scan and Rust scan reject `E != I`/duplicates and
enforce the cumulative `Σ(8E + 4I)` budget before typed allocation; the Rust reader validates the typed map and
exposes the three-way `lookup`; the pre-map non-singleton fixture fails closed as designed; the signatures,
summary, and `contracts/**` are unchanged; the parent .NET verifier and Rust reader open the new fixture; the
full local gate and closeout CI are green; `step-11` is not moved and no `v*` release exists. No Blocker or Major
finding remains. The two Minor findings are documentation-only (a wrong byte delta in the pre-map README and a
missing writer-commit line in the new fixture README); A2A-03 is a Note about the amendment page's unchecked
boxes. They do not affect the package, reader, tests, or format, and may be accepted as notes or fixed in the
step agent's docs-only resolution commit.

Follow-up owner: Human owner (accept the notes or request the docs-only corrections); step agent for A2A-01/A2A-02
if the owner asks for a fix.
Follow-up issue/step: `step-11.1` merge/tag after owner approval; F-29's formal closure remains a Step 14 gate.
`step-11` stays at `84ceb76`; no `step-11.1` tag is created by this audit.

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

## Audit rounds

### Audit round 1

| Field | Value |
| --- | --- |
| Date | 2026-11-21 |
| Audited commit | `ef568beca62f6f51559460d01ba5627219e90345` |
| Request head | `1965eaea56af241f75d96d8ceda415c45f7341c8` (docs-only above the audited code) |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh audit context |
| Spawned by | Orchestrator |
| Evidence | `Evidence` above (gates, tests, CI, fixtures, forward read) |
| Findings | A2A-01 (Minor), A2A-02 (Minor), A2A-03 (Note); no Blocker/Major |
| Verdict | **Passed with notes** |

The round-1 audit read the code, tests, fixtures, docs, ADRs, contracts, and the design review at the frozen
commit, ran `./tools/verify.sh all --require` and the targeted .NET/Rust tests, reproduced the one-time forward read
with the parent `bbc2d08` toolchain, and verified the closeout CI via the GitHub API. The two Minor findings are
documentation-only. `step-11` remains at `84ceb76`; no `step-11.1` tag was created.

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
| Reviewed commit | `17e723cae686c15bdfa7e61fc648d7d1da8086ef` |
| Docs branch | `step/11-simulation-package-and-msl-writer` |
| Review date | 2026-11-15 |
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, or orchestrator |

Amendment page:
[`step-11-amend-1-dotnet-11-native-zstd.md`](../amendments/step-11-amend-1-dotnet-11-native-zstd.md)

Parent step page:
[`Step 11 — Simulation Package and .msl Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

Parent design review record: [`step-11.md`](step-11.md) (`Ready with notes`, accepted 2026-11-15)

Frozen state: `17e723cae686c15bdfa7e61fc648d7d1da8086ef` is the design freeze for round 2, after the round-1
`Changes required` resolutions. Round 1 reviewed `06774ee874786d5a95424705b19900cd6e0035cf`; the record has
since gained the reviewer's round-1 evidence, findings, and verdict, and the design pages changed to resolve
them (amendment page `36e6b44`, parent step/index/decisions/status `17e723c`). The resolution rows below list every
finding and its fix. A reviewer can verify the round-2 delta with
`git diff --name-status 06774ee HEAD`.

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
| Benchmark Part A | Complete 2026-11-15 and passing under the owner-approved small-payload rule (amendment §9.1); no longer an outstanding gate |

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

- [x] Amendment rationale and timing — why the switch belongs in Step 11 rather than a later step, and why the
      held feature 7 makes this the cheapest point. *Reviewed: rationale and timing are sound; the Part A timing
      claim is not — see A1-02.*
- [x] Scope and non-goals — the retarget and codec replacement are in scope; no format, signature, summary,
      manifest, solver, or unrelated package change is claimed. *Reviewed: scope is coherent; the documentation
      inventory is incomplete — see A1-01.*
- [x] Toolchain migration surface — the file inventory and counts are complete and correct: 21 solution
      projects via the central TFM, one explicit project outside the solution, two ApiCompat scripts, nine
      baseline assemblies, nine DocFX entries, three workflows, one image, and the named documentation.
      *Reviewed: 21/1/2/9/9/3/1 verified; the repository has nine DocFX paths, not ten (A1-09), and the named
      documentation omits current normative planning pages (A1-01).*
- [x] SDK pin and roll-forward policy — `11.0.100` with `latestPatch`, `allowPrerelease: false`, and the
      rationale for not using `latestFeature`. *Reviewed: valid and well-reasoned; setup-dotnet installs the
      exact pin.*
- [x] CI and platform gates — no SDK literal in the workflows, `actions/setup-dotnet@v6` uses
      `global-json-file`, the Windows gate is already required by R2-7, and macOS stays manual. *Reviewed:
      confirmed in ci.yml, benchmark.yml, and docs.yml; nightly.yml has no .NET setup.*
- [x] Development image — 11.0.100 only, the `COPY global.json` pin assertion, the ReportGenerator 5.6.0 bump
      that the .NET 10 runtime removal forces, DocFX 2.81.0 unchanged, and the unverified image build.
      *Reviewed: 5.6.0's net11 asset and 5.5.12's net10 ceiling verified; the assertion does not prove the
      "only" part — see A1-07; the unbuilt-image risk is recorded honestly.*
- [x] Encoder design — the one-shot `ZstandardEncoder` path, `SetSourceLength`, `GetMaxCompressedLength`,
      `isFinalBlock`, default window/checksum/dictionary settings, and why `ZstandardStream` is rejected.
      *Reviewed: reproduced byte-for-byte on the 418,611-byte payload and on 2/16/48 MiB inputs; the defaults
      and the `ZstandardStream` rejection are correct.*
- [x] Quality mapping — identity mapping of `PackageBuildOptions.CompressionLevel` 1..19 to native `Quality`,
      the 3 and 9 pins, and the unchanged public range. *Reviewed: 3 and 9 are byte-identical to ZstdSharp via
      both the quality constructor and the options object; 1..19 semantics and the public range are preserved.*
- [x] Decoder design and bounds — `TryGetMaxDecompressedLength`, the guard, `Array.MaxLength`, exact versus
      upper-bound allocation, the documented foreign-frame relaxation, and `PKG004` mapping. *Reviewed: the
      sequence works; the Results decoder wording is inconsistent (A1-05), the empty/guard checks are dropped
      (A1-06), and the lattice guard claim is wrong for the reader (A1-10).*
- [x] Error behaviour — `InvalidOperationException` on encode invariant violation,
      `InvalidDataException` on decode failure, `ArgumentOutOfRangeException` for levels outside 1..19.
      *Reviewed: encode/level behaviour is right; the Results decode contract is not as described (A1-05).*
- [x] Contract non-impact — canonical signatures are MessagePack projections, `package-summary-v1` excludes
      payload bytes, and `contracts/**` must not change. *Reviewed: confirmed in the manifest, data contracts
      §3.5, and the signature code; `verify.sh contracts` is green with no diff.*
- [x] Fixture consequences — no checked-in `.msl` exists, feature 7 generates fixtures once on the amended
      toolchain, and the extracted payload bytes are not a signature input. *Reviewed: no `.msl` is tracked;
      the cross-version test therefore has no fixture (A1-04).*
- [x] Cross-language compatibility — standard frames, `zstd` CLI and Rust `zstd` crate read support, and
      packages written before the switch remaining readable. *Reviewed: native frames decode through zstd CLI
      1.5.7 and a CLI frame decodes through the native decoder; the Rust reader consumes standard frames.*
- [x] Benchmark gate — the real-payload A/B method, the metric thresholds, the 10⁴-job budget, and the rollback
      rule. *Reviewed: Part A is not a design-acceptance gate as written (A1-02); byte identity contradicts
      the determinism rule (A1-03); Part B's net10 fallback is incomplete (A1-08).*
- [x] Risks and mitigations — .NET 11 maturity, tool/package compatibility, Windows/macOS, image build,
      cross-patch determinism, ApiCompat churn, analyzer regressions, and rollback. *Reviewed: risks are
      realistic and the image-build risk is explicit; see A1-07 for the assertion gap.*
- [x] Licensing and dependency policy — `ZstdSharp.Port` removal, no new dependency, no GPL content, and the
      `check_nuget_licenses.py`/`policy` gate. *Reviewed: ZstdSharp is MIT and removed; the in-box API adds no
      package; the policy gate is green (see the licensing note A1-13).*
- [x] Documentation and records — the named toolchain docs, the quality-strategy update, and the parent
      step/status/decision/index updates. *Reviewed: step page, status, amendment index, open decisions, and
      the parent review link are updated; the parity matrix and normative planning pages are missing (A1-01).*
- [x] Integration sequencing — amendment branch name, squash into the parent step branch, and integration
      before the audit freeze with the combined commit recorded. *Reviewed: branch name matches the convention;
      the parent audit and closeout have not started, so the amendment window is open.*
- [x] `docs/dotnet-11-container` disposition — supersede/fold/land-first options and the recommendation.
      *Reviewed: the branch exists at `9ab4bb8` with both SDKs; the supersede decision and carry-over are
      consistent, and the branch is not deleted before integration.*
- [x] Owner decisions — the five previously open questions (container branch, image build, ReportGenerator,
      Test SDK, reference harness) are answered and recorded in the amendment's `Owner decisions` section;
      no open question remains for this amendment. *Reviewed: all five are in the table and reflected in §6–§8,
      `05-open-decisions.md`, and the status/step pages; no owner question remains.*

## Dependency and interface checks

- [x] Parent step state — features 1–6 squashed at `eb8513d`, `9b754af`, `c0d8d64`, `bdafaa4`, `4fe6225`,
      `cbe345f`; feature 7 held; no parent audit has started, so the amendment window in
      [`amendments/README.md`](../amendments/README.md) §4 is open. *Verified: all six are first-parent
      ancestors of the step branch; `audits/step-11.md` and `closeout/step-11.json` do not exist.*
- [x] Parent contracts are stable — the payload DTOs, payload format versions, writer/verifier API, `PKG`
      codes, and `package-summary-v1` are implemented and unchanged by the amendment. *Verified: the DTOs,
      `PayloadEncoder`/`PackagePayloadDecoder`, `PKG001`–`PKG010`, and the summary exist; the amendment
      does not change their contract.*
- [x] Inputs, outputs, and formats are defined — the codec's inputs (MessagePack bytes), output (one standard
      Zstd frame), bounds, and error types are explicit. *Reviewed: explicit; the Results decoder's error
      surface is the one ambiguity (A1-05).*
- [x] Versioning and migration behaviour are defined — no `.msl` version bump, no migration, pre-switch packages
      readable, and the future fixture policy stated. *Verified: no format/version change; standard frames are
      readable; the future fixture policy is stated.*
- [x] Cross-step impact is nil — Step 12, 13, 18, 20, and the Rust reader consume the same frames and the same
      payload contract; no cross-step interface question is left open by the amendment. *Verified: standard
      Zstd frames and unchanged payload contracts; no cross-step interface change.*
- [x] The ApiCompat and DocFX gates can run on `net11.0` — the tool versions and reference-pack paths exist, and
      the one-time cross-TFM proof is planned before the .NET 10 runtime is removed. *Verified: DocFX 2.81.0
      ships a net11 asset; apicompat 11.0.100 runs on 11 and compares net10↔net11 in a temporary probe; the
      net10/net11 reference packs are present. The one-time proof is manual — see A1-15.*
- [x] The benchmark gate is runnable with today's artifacts — real payloads can be extracted from a package
      built by the current writer, and both codecs can be referenced from one `net11.0` probe. *Verified: the
      real writer builds `samples/jobs/ceria-temperature-sweep.mocprj`; `ZstdSharp.Port 0.8.8` loads on
      `net11.0`; the native probe is in place. Part A itself has not been run or recorded (A1-02).*

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable. *Mostly: the cross-version and
      Part A acceptance claims are not testable as written — see A1-02 and A1-04.*
- [x] Design decisions are complete and internally consistent. *Not fully: A1-02, A1-03, A1-05, A1-06.*
- [x] Required tests, property tests, golden fixtures, and performance targets are defined. *The test table is
      detailed, but the cross-version fixture is missing (A1-04), the quality-mapping golden capture is
      unspecified (A1-12), and two rows are mis-tiered (A1-11).*
- [x] Documentation, status, parity, and audit updates are planned. *Step/status/index/open decisions and the
      parent review link are updated; the parity matrix and current normative planning pages are omitted
      (A1-01); the audit request and closeout evidence are correctly deferred.*
- [x] Licensing, provenance, and dependency checks are resolved. *ZstdSharp (MIT) is removed, no new package is
      added, and no GPL content is involved; the "MIT-licensed .NET runtime" wording is imprecise (A1-13).*
- [x] Risks and mitigations are realistic. *Yes, including the honest unbuilt-image risk; the image assertion
      gap is A1-07.*
- [x] Code layout and namespace plan follow the structure policy. *Yes: the changes stay in the existing
      `SimulationPackage/Payloads`, `Packages/Internal`, and `Results/Packages` concept folders; no new
      `Utils`/`Common` folders or namespace changes.*
- [x] No hidden scope, stale placeholders, or unowned decisions. *No hidden scope or unowned decision was found;
      the documentation inventory gap (A1-01) and the unrun Part A gate (A1-02) are the residual risks.*

## Evidence

**Frozen state.** `git diff --name-status 06774ee HEAD` prints only `M docs/planning/reviews/step-11-amend-1.md`;
the request-update commit `1366dc9` changes only this record, as the request states. The step branch's first-parent
log contains the six feature squash commits named in the request (`eb8513d`, `9b754af`, `c0d8d64`, `bdafaa4`,
`4fe6225`, `cbe345f`). `docs/planning/audits/step-11.md` and `docs/planning/closeout/step-11.json` do not exist,
so the parent audit has not started and the amendment window in `amendments/README.md` §4 is open.

**Files reviewed.** `AGENTS.md` §3.8–§3.13; `docs/development/orchestration.md`; `docs/planning/amendments/README.md`
and the amendment page; `docs/planning/reviews/README.md`; the request record; the parent Step 11 page and review
record; `05-open-decisions.md`; `status.md`; `06-toolchain-requirements.md`; `02-data-contracts.md` §3.2–§3.5;
`03-quality-strategy.md` §9.9; `appendix-b-parity-matrix.md`; the ADRs named in the amendment; `global.json`;
`Directory.Build.props`; `Directory.Packages.props`; `.config/dotnet-tools.json`; `Mocassin.slnx`;
`tools/check-api-compat.sh`; `tools/update-api-baseline.sh`; `tools/api-projects.txt`; `tools/docfx/docfx.json`;
`.github/workflows/{ci,benchmark,docs,nightly}.yml`; `docs/opencode/mocassin.containerfile`;
`tools/reference/Mocassin.ReferenceHarness/Mocassin.ReferenceHarness.csproj`; `PayloadEncoder.cs`;
`PackagePayloadDecoder.cs`; `PackageTestSupport.cs`; `SimulationPackageWriter.cs`; `PackageReader.cs`;
`PackageVerificationOptions`; the checked-in API baselines; and the probe artefacts under `/tmp/opencode`.

**Commands and results.**

| Command | Result |
| --- | --- |
| `git log --oneline -4`; `git show --stat 2124e5a 06774ee 1366dc9` | Owner decisions added at `06774ee`; review record updated only at `1366dc9`. |
| `grep -c '<Project Path=' Mocassin.slnx` | **21** solution projects; the only tracked project with an explicit TFM outside the solution is `tools/reference/Mocassin.ReferenceHarness` (`net10.0` at the freeze). |
| `find tests/contracts/api-baseline -type f` | **9** baseline assemblies under `net10.0/`. |
| `cat tools/docfx/docfx.json` | **9** assembly paths with `TargetFramework: net10.0` (the request says "ten"; see A1-09). |
| `grep -rn 'setup-dotnet\|global-json-file\|dotnet-version' .github/workflows` | `ci.yml` (2 jobs), `benchmark.yml`, `docs.yml` use `actions/setup-dotnet@v6` + `global-json-file: global.json`; no SDK literal. `nightly.yml` has no .NET setup. |
| `cat global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json` | `10.0.403`/`latestFeature`/`net10.0`; `ZstdSharp.Port 0.8.8`; apicompat `10.0.403`; Test SDK `18.11.0` — all as the amendment describes. |
| `./tools/verify.sh docs` | **exit 0**; Zensical strict build, link/math checks, DocFX (421 files, **0 warnings, 0 errors**), rustdoc. |
| `./tools/verify.sh policy` | **exit 0**; NuGet licence policy, `dotnet list ... --vulnerable` phrase present, all 9 ApiCompat comparisons clean (with pre-existing "could not resolve MessagePack/ZstdSharp reference" advisory lines only), 19 contract artifacts current, 45 tool tests, SPDX 590 files, secrets 957 files, cargo-deny. |
| `dotnet --list-sdks`; `dotnet --list-runtimes`; `zstd --version` | SDKs `10.0.403` and `11.0.100`; runtimes `10.0.14`/`11.0.0`; `zstd` CLI 1.5.7. |
| `/tmp/opencode/zstd-dual` (`dotnet run -c Release`) | 2/16/48 MiB mixed payloads, levels 3 and 9: native one-shot frame sizes equal `ZstdSharp.Port 0.8.8` and `identical=True` on every row. |
| Probe in `/tmp/opencode/verify-probe` against the real 418,611-byte probe payload | q3: `port=182150 nativeCtor=182150 nativeOpts=182150 ctor==port=True opts==port=True`; q9: `port=178312 nativeCtor=178312 nativeOpts=178312` all equal. This is exactly the amendment's `new ZstandardCompressionOptions { Quality = n }` path. |
| `cmp /tmp/opencode/zstd-compare/zs-q3.frame.zst /tmp/opencode/zstd-probe/artifacts/default.frame.zst` | identical (md5 `35f061ceb6ffbee13639bfad25d9eb8f`); `zs-q9` is 178,312 bytes. |
| `zstd -t` / `cmp` on the probe `.dec` files | Native frames decode byte-identically through `zstd` CLI 1.5.7; a CLI frame decodes through the native decoder (probe output `CLI frame via stream/decoder: True`). |
| Probe `frame-probe` | One-shot frame bound = 418,611 (exact); stream frame bound = 655,360 (upper bound, actual 418,611); a 10 MiB content-size-less CLI frame bound = 10,485,760 and decodes; 4 arbitrary bytes, bad magic and a truncated frame return `false`; **empty input returns `true, bound=0`**, and `TryDecompress(empty)` returns `false`. |
| `dotnet tool install ... Microsoft.DotNet.ApiCompat.Tool 11.0.100` + `dotnet exec --fx-version 11.0.0 ... --left <net10 assembly> --right <net11 assembly>` | "APICompat ran successfully without finding any breaking changes." The 11.0.100 tool is itself a `tools/net10.0/any` asset with `rollForward: Major`; it runs on the .NET 11 runtime and compares net10↔net11 assemblies. |
| `unzip -l` of `microsoft.dotnet.apicompat.tool` 11.0.100 | Only `tools/net10.0/any`; `runtimeconfig` `tfm: net10.0`, `rollForward: Major`. |
| `unzip -l` of `dotnet-reportgenerator-globaltool` 5.5.12 / 5.6.0 | 5.5.12 stops at `tools/net10.0/any`; 5.6.0 adds `tools/net11.0/any`. Claim verified. |
| Local DocFX 2.81.0 store | Contains `tools/net11.0/any`; its runtimeconfig targets `Microsoft.NETCore.App 11.0.0-rc.1...` with `rollForward: Major`. Claim verified. |
| NuGet flat-container checks | `microsoft.dotnet.apicompat.tool` 11.0.100 and `microsoft.net.test.sdk` 18.12.0 exist. |
| `git branch -a --list '*dotnet-11*'`; `git show 9ab4bb8` | Local and `origin/docs/dotnet-11-container` at `9ab4bb8`, installing 10.0.403 **and** 11.0.100, with `COPY global.json` and both `--list-sdks` assertions. The amendment's §8 carry-over description matches the commit. |
| `git ls-files \| grep -E '\.(msl\|msr\|mcs)$'` | Empty — no checked-in `.msl` exists, so feature 7's fixture claim is correct. |
| `git status --short` after the gates | Empty; the reviewer changed no repository file. |

**Positive verifications not turned into findings.** The encoder claims are reproducible: the exact quality
mapping, content-size declaration, frame magic, `GetMaxCompressedLength`, determinism, `ZstandardStream` byte
difference, cross-decoder round-trips, and the quality-3/9 byte identity all hold. The contract non-impact claim
holds: `contracts/manifest.json` has no Zstd entry; `package-summary-v1` excludes payload bytes and Zstd/MessagePack
implementation details (`02-data-contracts.md:1152-1153`); signatures are MessagePack projections. The
`ReportGenerator 5.6.0`, `DocFX 2.81.0`, `apicompat 11.0.100`, and `Test.Sdk 18.12.0` availability claims are true.
The `docs/dotnet-11-container` branch exists exactly as described. The image-build risk is stated honestly: the
amendment says the image was **not** built and must not be described as build-verified.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| **Major** | **A1-01 — The "verified file inventory" of the migration surface omits normative planning pages that still state the target is .NET 10.** The inventory's Docs row names ten files, but `docs/planning/README.md:5` (".NET 10 / C#"), `docs/planning/00-scope-and-principles.md:43` (".NET 10 is the minimum runtime"), `docs/planning/01-target-architecture.md:251,278` ("Runtime \| .NET 10 LTS"; "CI ... .NET 10"), `docs/planning/03-quality-strategy.md:252,491,530` and `docs/planning/appendix-b-parity-matrix.md:247` (".NET 10 \| Y \| Planned") are current-tense normative statements and are not listed. After the retarget they contradict the code, which `AGENTS.md` §2 declares a stop condition. Historical pages (`07-reference-build.md`, ADRs, audits, `tests/reference/bench/baselines.json`, the `docs/performance/index.md` measurements) should stay .NET 10 and be distinguished in the amendment. | Amendment §6 Docs row and Documentation section; the five files and line numbers above; `grep -rn '\.NET 10' docs/planning`; `./tools/verify.sh docs` exit 0 (the docs gate does not detect stale prose). |
| **Major** | **A1-02 — The Part A gate is described as a design-acceptance gate but is scheduled inside the feature, and no Part A evidence exists at the freeze.** Amendment §9 says Part A "must pass **before the amendment is accepted for implementation**"; Dependencies says "before the design review can accept the amendment for implementation"; the request's step summary repeats it. Yet the "Order of work inside the feature" step 4 runs Part A after the codec replacement, and the frozen page contains only indicative synthetic-probe measurements, explicitly "not a substitute for Part A". By the design's own rule the verdict cannot be `Ready for implementation` until Part A is run and recorded. Either run and record Part A before the owner's start gate, or restate it as a pre-merge/pre-audit gate and update the three places that call it an acceptance gate. | Amendment §9, Dependencies bullet, "Order of work" step 4, Tests table; request record "Step summary"; no Part A evidence in the amendment, review record, or feature log at `06774ee`. |
| **Major** | **A1-03 — Part A's mandatory byte-identity threshold contradicts the project's documented determinism rule and the rollback rule.** Part A requires "compressed bytes byte-identical to the current codec on every tested payload" under "Round-trip correctness" (100 %), and the rollback rule says a Part A threshold breach means the amendment is **not** integrated, with a "correctness" failure an automatic rollback. But the parent step §4, `02-data-contracts.md` §3.5, and `03-quality-strategy.md` §9.9 all say stored bytes may differ in "SQLite/Zstd implementation details", and amendment §5 itself says "Across Zstd patch versions the *bytes* may legitimately differ; signatures and the canonical summary do not." A benign byte difference on a real payload would therefore force a rollback despite no format, signature, summary, or cross-language change. Keep byte identity as a reported observation on the tested corpus, and make the hard rollback depend on round-trip/cross-decoder/signature/summary correctness plus the size/time thresholds (or explicitly justify byte identity as an additional owner-accepted gate). | Amendment §9 thresholds and rollback paragraph; amendment §5 "Determinism rule"; Step 11 §4 determinism paragraph; `03-quality-strategy.md:411-413`; `02-data-contracts.md:1152-1153`. |
| **Major** | **A1-04 — The cross-version read test is claimed PR-fast but has no pre-amendment fixture, and the design forbids the only current source of one.** The Tests table asserts "Cross-version read \| PR-fast \| A package built by the pre-amendment writer verifies and inspects identically on the amended tree"; the acceptance criteria require both directions. No checked-in `.msl` exists (`git ls-files` empty), the amendment says feature 7 generates the fixture once **on the amended toolchain**, and the test project cannot build a pre-amendment package after `ZstdSharp` is removed. A PR-fast test therefore has nothing to read. Specify a small pre-amendment `.msl` (or a recorded package/payload frame) as a checked-in test fixture with its provenance, or tier/move the check to the Part A/feature-7 manual evidence and say so. | Amendment Tests table line "Cross-version read"; Acceptance criteria "A package written on the unamended tree verifies on the amended tree..."; `git ls-files \| grep -E '\.(msl\|msr\|mcs)$'` empty; amendment "Fixture consequences". |
| **Minor** | **A1-05 — §4 describes both decoders as throwing `InvalidDataException`, but the Results decoder returns `null` plus an error string and the verifier maps that `DecodeError` to `PKG004`.** `Mocassin.Results/Packages/Internal/PackagePayloadDecoder.TryDecode` has `TPayload? TryDecode(..., out string? error)` and never throws; `PackageVerifierEngine` consumes `DecodeError` for the `PKG004` check. Applying §4's "Both decoders ... `false` → `InvalidDataException`" literally would change `TryDecode`'s contract and the verifier's error path. The acceptance criterion still says "PKG004 in the verifier", so the intent is clear; §4 should state each decoder's behaviour explicitly. | Amendment §4 step 1 and §1; `PackagePayloadDecoder.cs:24-79`; `PackageVerifierEngine.cs:153-190`; `PackageTestSupport` and the PKG tests. |
| **Minor** | **A1-06 — The new decode sequence drops the existing empty-payload and non-positive-guard handling, and `TryGetMaxDecompressedLength` returns `true`/0 for an empty frame.** The writer's current `Decode` rejects `payload.IsEmpty` and `maxDecompressedBytes < 1`; the Results decoder returns "The payload blob is empty" / "The decompression guard is not positive". The amendment's three-step sequence starts at `TryGetMax`, which returns `true, bound=0` for empty input (verified), then allocates zero and `TryDecompress` fails with a generic "not a valid Zstd frame"; a non-positive guard changes from `ArgumentOutOfRangeException` (writer) to `InvalidDataException`. No crash, but the documented error contract and messages change without being listed as a deliberate change. Preserve the two checks in both decoders or document them. | `PayloadEncoder.cs:103-114`; `PackagePayloadDecoder.cs:29-39`; `/tmp/opencode/frame-probe` output `empty TryGetMax=True bound=0`; `empty TryDecompress=False`; amendment §4. |
| **Minor** | **A1-07 — The containerfile assertion does not prove "11.0.100 only", which is the acceptance criterion.** The §7 block asserts `dotnet --list-sdks \| grep -qE '^11\.0\.100 \['` and the pin, but both pass with `10.0.403` also installed; nothing asserts the 10.0.403 absence or the SDK count. Because the image build is the accepted-risk item and is not run, this assertion is the only designed evidence for "no 10.0.403". Add a negative grep or an exact-count check. | Amendment §7 assertion block and Acceptance criteria "The containerfile installs 11.0.100 only and asserts it"; `9ab4bb8` asserted both SDKs; `docs/opencode/mocassin.containerfile` current state. |
| **Minor** | **A1-08 — Part B's net10-reference comparison and its package-size threshold have no usable fallback.** Part B says compare the amended 10⁴-job run "against the same harness run on the unamended tree (or, if that is unavailable, against the Step 11 §7 targets)", but Step 11 §7 has no package-size target, and the "≤ +5 % wall clock" and "≤ +3 % package size" thresholds become untestable if the net10 run is skipped. The unamended tree is available in git, so the cleanest fix is to make the net10 reference run mandatory; otherwise give an explicit absolute fallback for package size. | Amendment §9 Part B; Step 11 §7 targets; Tests table "Benchmark Part B". |
| **Minor** | **A1-09 — The review request says "ten DocFX entries"; the repository and the amendment have nine.** `tools/docfx/docfx.json` lists nine assemblies (Core, Symmetry, Compilation, Contracts, Packages, Results, App.ViewModels, App.Views, Rendering), and amendment §6 says "9 assembly paths". The request's checklist item is only a scope typo, but it should be corrected so the inventory count is unambiguous. | Request record line 85; `tools/docfx/docfx.json`; amendment §6. |
| **Minor** | **A1-10 — The lattice decode-guard claim does not match the Results reader.** Amendment §4 says the allocation is "capped by the configured guard (model 512 MiB, job 8 MiB, lattice `4 × a × b × c × position_count`)" and the Risks table says "The guard still bounds every allocation". The writer does use `min(int.MaxValue, 4 × A × B × C × P)` (`SimulationPackageWriter.cs:208-211`), but `PackageReader.ReadLatticesAsync` passes `int.MaxValue` (`PackageReader.cs:274`) and `PackageVerificationOptions` has no lattice limit, so a hostile lattice frame can drive a ~2 GiB allocation in `inspect`/`verify` (bounded only by `Array.MaxLength`). The parent step's §9.9 "decompression bomb ... instead of an allocation" claim is also weakened for lattices. Either add a lattice guard or state the actual bound and add a bomb test for this path. | `SimulationPackageWriter.cs:208-211`; `PackageReader.cs:266-276`; `PackageInspection.cs:95-107`; `03-quality-strategy.md:420-422`; amendment §4 and Risks table. |
| **Note** | **A1-11 — Two test-table rows are not PR-fast checks.** "Cross-version read" (A1-04) and "Toolchain identity" ("the cross-TFM ApiCompat run reports no breaking changes") are one-time or cross-tree checks, not ordinary PR-fast tests; the cross-TFM proof is explicitly a one-time manual activity in §6. Retier them or describe them as recorded evidence. | Amendment Tests table; §6 "One-time cross-TFM ApiCompat proof". |
| **Note** | **A1-12 — Record the pre-amendment golden frame before removing `ZstdSharp`.** The quality-mapping test compares level 3/9 output to "the pre-amendment codec on a fixed payload vector"; once the package is removed, the comparison needs a checked-in golden (bytes or SHA-256) captured while the old codec still exists. State how the golden is captured and where it lives. | Amendment Tests table "Quality mapping"; §"Temporary implementation and migration". |
| **Note** | **A1-13 — "MIT-licensed .NET runtime" is imprecise for the native zstd.** `System.IO.Compression.Zstandard*` is the MIT-licensed managed API, but the native zstd implementation shipped inside the .NET runtime is BSD-3-Clause. The conclusion that `NOTICE.md` needs no change is still supportable because the repository does not redistribute the runtime; the licensing paragraph should say which layer it means. | Amendment "API and contract impact" licensing bullet; `NOTICE.md`; in-box `System.IO.Compression` docs/third-party notices. |
| **Note** | **A1-14 — The one-SDK image relies on the apicompat tool's own roll-forward.** `microsoft.dotnet.apicompat.tool` 11.0.100 is a `tools/net10.0/any` asset with `rollForward: Major`; it runs on the 11 runtime and compares net10↔net11 (verified), so the "install 11.0.100 only" image is workable. The amendment should record this dependency rather than leaving "apicompat 11.0.100" unexplained next to "remove .NET 10". | `.config/dotnet-tools.json`; downloaded 11.0.100 nupkg/runtimeconfig; `dotnet exec --fx-version 11.0.0` cross-TFM run. |
| **Note** | **A1-15 — State the order of the cross-TFM proof relative to the baseline rename.** §6 says "regenerate the `net11.0` baseline, then run `apicompat` with the checked-in `net10.0` baseline as `--left`"; after the rename the net10 baseline is no longer at that path. Preserve a copy (or read it from git) before renaming, and record the exact command. | Amendment §6 "One-time cross-TFM ApiCompat proof"; `tools/update-api-baseline.sh` `rm -rf "$DEST"`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Re-review required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

I am a fresh context spawned by the orchestrator for this focused amendment review. I did not author or implement
the amendment, I did not participate in the Step 11 design or its parent review, and I have no shared conversation
with the planner, implementor, or orchestrator. I read the repository at the frozen commit
`06774ee874786d5a95424705b19900cd6e0035cf` and verified the only later delta with
`git diff --name-status 06774ee HEAD` (this record). I edited only this record and changed no code, test,
configuration, design page, status page, or other file; `git status --short` was empty after the gates. The
human owner approves the final verdict and the start gate; this record is evidence, not an approval.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

**Verdict: Changes required.** The codec choice itself is sound and reproducible in this environment: the exact
`ZstandardEncoder` quality path is byte-identical to `ZstdSharp.Port 0.8.8` at levels 3 and 9 on the real probe
payload and on 2/16/48 MiB inputs, the frames are standard and cross-decode through `zstd` CLI 1.5.7, and the
contract, signature, and `package-summary-v1` non-impact claims hold. The blocking issues are not codec
feasibility: (1) the migration-surface inventory omits current normative planning pages that still state .NET 10;
(2) the Part A gate is its own design-acceptance condition but has not been run and is scheduled inside the
feature; (3) the mandatory byte-identity threshold contradicts the project's documented determinism rule and can
force a false rollback; and (4) the cross-version read test is claimed PR-fast without any pre-amendment fixture.
Six Minor and five Note findings are recorded above; the image-build risk, the superseded
`docs/dotnet-11-container` branch, the ReportGenerator 5.6.0 bump, the Test SDK 18.12.0 bump, and the
ReferenceHarness retarget are recorded honestly and consistently.

**Severity counts:** 4 Major, 6 Minor, 5 Note, 0 Blocker. No codec, licensing, or contract blocker was found.

Follow-up owner: Planner (design corrections); step agent owns the resolutions and the implementation after the
owner's decision.
Follow-up step: Re-review `step-11-amend-1` at a new frozen commit after the Major corrections; the parent Step 11
audit remains not started.

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

Round 1 returned `Changes required` at `06774ee`. The design was updated, re-frozen at
`17e723cae686c15bdfa7e61fc648d7d1da8086ef`, and every finding below is resolved in the design or explicitly
routed. Reviewer-authored evidence, findings, finding details, and verdict are unchanged.

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| A1-01 | **Closed.** The migration inventory is complete and split: §6.1 lists 13 active pages with their current statement and post-retarget wording (`README.md`, `CONTRIBUTING.md`, `AGENTS.md` §1/§8, `src/dotnet/README.md`, `docs/getting-started/index.md`, `docs/planning/README.md`, `00-scope-and-principles.md` §3.2, `01-target-architecture.md` §6, `03-quality-strategy.md` §9.3/§11/§13, `06-toolchain-requirements.md`, `appendix-b-parity-matrix.md` §10, `docs/reference/file-layout.md`, `tests/contracts/README.md`), and lists the historical pages that stay .NET 10 (step pages 00/02/08/09, `07-reference-build.md`, ADRs, audits, reviews, bench baselines, performance measurements). The Documentation checklist carries the edits. | Planner | `36e6b44` |
| A1-02 | **Closed.** Part A is recorded as a **completed pre-acceptance gate** with the durable evidence table, method, provenance, limitations, and the owner decision (§9.1); the request's step summary now says Part A is complete; "Order of work" no longer schedules it (step 4 says do not re-run); the Dependencies line says complete, and Part B remains the pre-audit-freeze gate. | Planner | `36e6b44` |
| A1-03 | **Closed.** Byte identity is an **observation**, explicitly not a gate or rollback trigger; the hard gates are round-trip, cross-decode, canonical signature/`package-summary-v1` equality, size, encode, decode (refined rule), and throughput. §5 states exactly what is regenerated if bytes differ (golden frames + feature-7 byte artifacts) and what is not (signatures, summary, pre-amendment fixture, `contracts/**`). | Planner | `36e6b44` |
| A1-04 | **Closed.** A pre-amendment `.msl` is captured with the net10 writer as the **first commit of the feature branch** and checked in at `tests/contracts/packages/amend-1-net10/` with its `package-summary-v1` and provenance; the net11 reader reads it as a PR-fast test. The reverse direction is a recorded one-time migration check from a temporary pre-retarget worktree. Feature order step 0 and §9.5 record the capture. | Planner | `36e6b44` |
| A1-05 | **Closed.** §4 now states each decoder's contract separately: the writer throws (`InvalidDataException`/`ArgumentOutOfRangeException`), while `PackagePayloadDecoder.TryDecode` returns `default` plus an `out string? error` and `PackageVerifierEngine` maps that error to `PKG004`. The signatures are not unified. | Planner | `36e6b44` |
| A1-06 | **Closed.** The empty-payload and non-positive-guard checks are restored as steps 1–2 in both decoders, with the reason recorded (`TryGetMaxDecompressedLength` returns `true, bound=0` for an empty span, so it cannot be first). The test table has an "Empty and non-positive guard" row. | Planner | `36e6b44` |
| A1-07 | **Closed.** §7's assertion block now proves "11.0.100 only": exact SDK count `1`, positive 11.0.100 grep, negative `10.` grep, global.json pin, and `dotnet` path check. | Planner | `36e6b44` |
| A1-08 | **Closed.** Part B's net10 reference run is **mandatory** (the unamended tree is available at the pre-retarget commit); the only fallback is a documented absolute set (10 min, 2 GB, ≥100 jobs/s, <50 MB model payload, <2 GB package) with the comparison recorded as not made, and the Part A ≤+3 % payload rule still bounds the codec. | Planner | `36e6b44` |
| A1-09 | **Closed.** The request now says nine DocFX entries; §6 says nine assembly paths; the repository has nine. | Planner | `36e6b44` |
| A1-10 | **Closed (routed).** §4 states the real lattice bound (writer `min(int.MaxValue, 4·A·B·C·P)`; reader passes `int.MaxValue`; bounded only by `Array.MaxLength`) and no longer claims the verification options bound it. A Step 11 fix feature `fix-lattice-decode-guard` derives the bound from the `lattices` row, applies it in `PackageReader.ReadLatticesAsync`, and adds the lattice bomb test before the audit freeze; it is a task on the parent step page and a dependency of the amendment. | Planner; step agent implements | `36e6b44`, `17e723c` |
| A1-11 | **Closed.** "Cross-version read" is PR-fast **because** it now reads a checked-in fixture; "Toolchain identity" and "Reverse-direction read" are retiered to `One-time (migration)` and described as recorded evidence, with a closing note that these are not recurring test tiers. | Planner | `36e6b44` |
| A1-12 | **Closed.** §9.4 records the pre-amendment golden frames and hashes for all 12 real payloads at levels 3 and 9, including the seed vector `sample-model-1` (3,475 B → 1,415 B / 1,387 B), states the check-in location (`tests/contracts/packages/amend-1/golden/` + `goldens.json` + `payloads/`), and gives the re-capture rule if a runtime patch changes bytes. | Planner | `36e6b44` |
| A1-13 | **Closed.** The licensing paragraph now distinguishes the MIT managed API/runtime from the **BSD-3-Clause native zstd**; `NOTICE.md` is unchanged because the repository is framework-dependent and does not redistribute the runtime; a self-contained/AOT artifact would have to carry the runtime's third-party notices, recorded as a Step 31 follow-up. | Planner | `36e6b44` |
| A1-14 | **Closed.** §6.2 records that `microsoft.dotnet.apicompat.tool 11.0.100` is itself a `tools/net10.0/any` asset with `rollForward: Major`; it runs on the .NET 11 runtime and compares net10↔net11 assemblies, so the one-SDK image does not break the ApiCompat gate. | Planner | `36e6b44` |
| A1-15 | **Closed.** §6.2 now orders the proof: preserve `tests/contracts/api-baseline/net10.0/` in `.tmp/net10-baseline/` (or read it from git) **before** `tools/update-api-baseline.sh` runs `rm -rf "$DEST"`, then generate net11, then compare with both reference packs; the exact command and the recorded-output requirement are stated. | Planner | `36e6b44` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

Fresh context, no shared conversation with the planner, implementor, or orchestrator; I did not author or
implement the amendment or the parent Step 11 design. The frozen design is
`06774ee874786d5a95424705b19900cd6e0035cf`; the only later commit is `1366dc9`, which changes this record alone
(`git diff --name-status 06774ee HEAD`). I read the amendment page, the parent step page and review record, the
amendment/review READMEs, `AGENTS.md` §3.8–§3.13, the open decisions, the toolchain and contract pages, the
relevant ADRs, and the code/configuration under review.

#### Commands and results

The full command/result table is in `Evidence`. The key results: `./tools/verify.sh docs` exit 0 (DocFX 421 files,
0 warnings); `./tools/verify.sh policy` exit 0 (9 ApiCompat comparisons clean, 19 current contract artifacts, 45
tool tests, SPDX 590 files, secrets 957 files, cargo-deny); 21 solution projects and the one out-of-solution
project; 9 API baselines; 9 DocFX assembly paths; 3 .NET workflows with `global-json-file: global.json` and no SDK
literal; `docs/dotnet-11-container` at `9ab4bb8` installing both SDKs; no checked-in `.msl`; no parent audit or
closeout file. The native encoder was checked with the existing probes and a temporary `/tmp` probe (never
committed): byte identity at levels 3/9 on the 418,611-byte payload (182,150/178,312 bytes) and on 2/16/48 MiB
inputs; exact `Quality`-object and quality-constructor equality; content-size declaration; cross-decoder
round-trips; empty/corrupt/truncated frame behaviour; and a net10↔net11 ApiCompat comparison with the 11.0.100
tool on the 11.0.0 runtime.

#### Findings

A1-01 (Major) documentation/migration-surface omissions; A1-02 (Major) Part A sequencing and missing evidence;
A1-03 (Major) byte-identity threshold versus the documented determinism rule; A1-04 (Major) cross-version read
test has no fixture; A1-05 (Minor) Results decoder error-contract description; A1-06 (Minor) dropped
empty/non-positive-guard checks; A1-07 (Minor) container assertion does not prove "11.0.100 only"; A1-08 (Minor)
Part B net10-reference fallback; A1-09 (Minor) "ten" versus nine DocFX entries; A1-10 (Minor) lattice decode
guard in the Results reader; A1-11..A1-15 (Notes) test tiers, golden capture, licensing wording, apicompat
roll-forward, and cross-TFM proof ordering. Details and evidence are in the top-level `Findings` table.

#### Verdict

**Changes required** — 4 Major, 6 Minor, 5 Note, 0 Blocker. The amendment's technical core is accepted as
feasible and contract-preserving; the Major findings are design, inventory, test-fixture, and gate-sequencing
corrections that must be made before the owner's implementation gate. Re-review at a new frozen commit after the
resolutions; earlier rounds are not overwritten.

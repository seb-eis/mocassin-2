# Step 11 Amendment 1 — .NET 11 Toolchain and Native Zstandard

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-11-amend-1` |
| Parent step | 11 — Simulation Package and `.msl` Writer |
| Status | Integrated (squash `00753e7`; owner accepted `Ready with notes` and approved implementation 2026-11-15) |
| Branch | `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd` |
| Design review | `docs/planning/reviews/step-11-amend-1.md` |
| Parent audit | `docs/planning/audits/step-11.md` |
| Integration commit | `00753e7` (`step-11: retarget to .NET 11 and replace the payload codec`) |
| Combined audit commit | — |
| Tag | `step-11` |
| Date | 2026-11-15 |
| Owner | Human owner |

Parent step page: [`Step 11 — Simulation Package and `.msl` Writer`](../steps/step-11-simulation-package-and-msl-writer.md)

## Rationale

`.NET 11` reached general availability on 2026-11-10 and ships `System.IO.Compression.ZstandardStream`,
`ZstandardEncoder`, `ZstandardDecoder`, `ZstandardCompressionOptions`, `ZstandardDecompressionOptions`, and
`ZstandardDictionary` in the shared framework. The parent design names `ZstdSharp.Port` as the payload codec
(Step 11 §3 and §6 item 14). The owner has decided to move the repository to .NET 11 and to the in-box codec if
that is feasible.

It is feasible, and the amendment records the evidence:

- The native one-shot encoder at `Quality = 3` and `Quality = 9` produced **byte-identical** frames to
  `ZstdSharp.Port 0.8.8` `Compressor(3/9).Wrap(...)` for every input tested: the 418,611-byte probe payload
  (182,150 / 178,312 bytes at both levels), and 2 MiB, 16 MiB, and 48 MiB mixed payloads. The frames are
  standard Zstd frames (`28 B5 2F FD`); `zstd` CLI 1.5.7 decodes the .NET frames byte-identically and the
  native decoder reads CLI frames.
- Decoding is faster (0.18 ms vs 0.43 ms for the probe payload) and encoding was 1.7–12× faster across the
  tested 2/16/48 MiB inputs. One third-party dependency edge also disappears from the package writer and the
  results reader.
- The toolchain packages the solution uses are compatible: `DocFX 2.81.0` ships a `net11.0` tool asset,
  `microsoft.dotnet.apicompat.tool 11.0.100` exists and compares `net11.0` assemblies, and a probe with
  Avalonia 12.1.6, MessagePack 3.1.12, Microsoft.Data.Sqlite 10.0.14, xunit 2.9.3, Microsoft.NET.Test.Sdk
  18.11.0, FsCheck 3.6.0, and FluentAssertions 7.2.0 builds and tests on `net11.0` with zero warnings.
- **Part A is measured and passes** on 12 real payloads from a package built by the unamended writer, at levels
  3 and 9, in two full runs: 240/240 required checks pass; size is unchanged (0.000 %, frames byte-identical
  24/24); encode is 4.20 %/5.88 % faster (levels 3/9); decode throughput is 302/299 MB/s; round-trip and
  cross-decoder are 100 %. The only threshold tension is the level-3 decode ratio on two 2,109-byte lattices,
  which the owner resolved with a small-payload absolute rule (§9).

Doing the switch now is materially cheaper than doing it later. Step 11 feature 7 — the checked-in `.msl`
fixture, the `package-summary-v1` golden, the extracted payloads, and the 10⁴-job benchmark — is deliberately
held, so no `.msl` fixture has to be regenerated. Steps 12 (build CLI), 13 (Rust reader acceptance), 19, 20,
and 23–28 (Avalonia) would each add net10→net11 migration surface. One toolchain, switched once, before the
first published package, is the cheapest point in the roadmap.

Waiting also has a cost: `.msl` has never been released, but the first released `.msl` starts the ADR 0013
current+previous-major discipline. Moving the writer's runtime after that release is a supported upgrade, not a
format change, but the fixture and evidence churn is larger.

## Scope

**In scope**

- Repository-wide retarget from `net10.0` to `net11.0`: the central `TargetFramework` and the one explicit
  project outside the solution.
- `global.json` → SDK `11.0.100` with a stated roll-forward policy.
- The development image: install `.NET SDK 11.0.100` only, and the tool versions that still run when the .NET 10
  runtime is removed.
- Remove `ZstdSharp.Port` from `Directory.Packages.props` and from both referencing projects.
- Replace the writer's `PayloadEncoder` compression/decompression and the reader's `PackagePayloadDecoder` with
  the in-box API, and replace the results-test support encoder.
- ApiCompat tool and baseline path/assembly update for `net11.0`.
- DocFX input paths and `TargetFramework` metadata.
- Toolchain references in repository documentation and the bug-report template.
- The benchmark gate, fixture consequences, and rollback rule described below.
- The amendment review record and the parent step/status/decision pages.

**Non-goals**

- No `.msl` schema, column, payload-field, or `format_version` change; no `contracts/manifest.json` change; no
  regenerated `contracts/generated/**` artifact. The codec change is invisible to the contract.
- No canonical signature change. Model, parameterization, and lattice signatures remain MessagePack projections
  (ADR 0010) and are independent of compression (Step 11 §4).
- No `package-summary-v1` change; it already excludes payload byte hashes and Zstd implementation details
  (`02-data-contracts.md` §3.5).
- No Rust/solver change. The Step 13 reader consumes standard frames and is unaffected.
- No `.mcs`/`.msr` change; `.mcs` is uncompressed and `.msr` is not part of this step.
- No Zstd dictionary, checksum, long-distance matching, custom framing, or streaming encoder path.
- No change to `PackageBuildOptions.CompressionLevel` or its 1..19 range.
- No unrelated package upgrades. Avalonia, MessagePack, Microsoft.Data.Sqlite, Microsoft.Extensions, xunit,
  FsCheck, FluentAssertions, BenchmarkDotNet, and JsonSchema.Net stay at their current versions.
- No `PublishAot`, single-file publish, or multi-targeting work; that is the Step 31 gate.
- No C# `LangVersion` pin change and no analyser-suppression sweep.
- No process-rule change in `AGENTS.md` §3; only the factual toolchain rows in §1 and §8 move.
- No feature 7 scope change. Feature 7 remains the owner of the fixture, the canonical-summary golden, the
  benchmark report, and the closeout; it runs after the amendment is integrated.

## Design

### 1. Verified native API surface

Measured on the installed `11.0.100` SDK / `11.0.0` runtime, `System.IO.Compression, Version=11.0.0.0`:

| Member | Observed behaviour |
| --- | --- |
| `ZstandardCompressionOptions.DefaultQuality` | `3` |
| `ZstandardCompressionOptions.MinQuality` / `MaxQuality` | `-131072` / `22` |
| `ZstandardCompressionOptions.DefaultWindowLog2` | `23`; window log range `[10, 31]` |
| `ZstandardEncoder.TryCompress(source, dest, out written)` | one-shot; content size declared; frame magic `28 B5 2F FD` |
| `ZstandardEncoder.GetMaxCompressedLength(n)` | 420,246 for a 418,611-byte input |
| `ZstandardDecoder.TryGetMaxDecompressedLength(frame, out n)` | exact declared size for a one-shot frame (418,611); an **upper bound** for a stream or content-size-less frame (655,360); `false` for corrupt or non-Zstd input |
| `ZstandardDecoder.TryDecompress(frame, dest, out written)` | succeeds when `dest` holds the decompressed size; `false` for corrupt input |
| `ZstandardStream` | valid but produces different frame bytes from the one-shot encoder (182,765 vs 182,150 for the same input) |

Reference artefacts: probe project and outputs under `/tmp/opencode/zstd-probe`, `/tmp/opencode/zstd-compare`,
`/tmp/opencode/zstd-dual`, and `/tmp/opencode/tfm-probe`. These are evidence, not repository files.

### 2. One fixed encoding path

The writer uses exactly one path, in `Mocassin.Packages.SimulationPackage.Payloads.PayloadEncoder`:

```csharp
var messagePack = MessagePackSerializer.Serialize(payload, serializerOptions);
var options = new ZstandardCompressionOptions { Quality = compressionLevel };
using var encoder = new ZstandardEncoder(options);
encoder.SetSourceLength(messagePack.Length);
var destination = new byte[ZstandardEncoder.GetMaxCompressedLength(messagePack.Length)];
var status = encoder.Compress(
    messagePack, destination, out int consumed, out int written, isFinalBlock: true);
// status == Done && consumed == messagePack.Length, else InvalidOperationException
return destination.AsSpan(0, written).ToArray();
```

Why this path:

- **Declared content size.** `SetSourceLength` writes the exact decompressed size into the frame, so the reader
  can bound its allocation exactly instead of at the guard. The current writer relies on the same property
  (`Decompressor.GetDecompressedSize`).
- **One path, one byte stream.** The one-shot encoder is deterministic across runs and invariant to how the
  caller chunks its input. `ZstandardStream` is rejected: it produces different bytes, declares only an upper
  bound, and would add a streaming branch that nothing in this step needs. Payloads are in-memory MessagePack
  buffers; the largest target is a 50 MB model payload.
- **Output compatibility with the current writer (observation, not a contract).** On the tested corpus the native
  one-shot output is byte-identical to `ZstdSharp.Port 0.8.8`: 24/24 payload-level combinations, including every
  in-package blob of the real packages (§9). That is a strong equivalence observation, and it means a package
  produced before the switch and one produced after it have identical payload bytes for the same logical input.
  It is **not** a correctness requirement: ADR 0010 and the data contracts exclude compressed bytes from
  signatures and from `package-summary-v1`, so a future Zstd runtime patch may change bytes without affecting the
  format or the readable content (§5).
- **Defaults are left alone.** `AppendChecksum = false`, `Dictionary = null`,
  `EnableLongDistanceMatching = false`, and `WindowLog2` stays at its default (`0`: the Zstd level default).
  Do not set `WindowLog2` explicitly; ZstdSharp called `ZSTD_compress(level)`, so the level default is what
  matches it, and an explicit `23` would diverge for inputs above the level's default window.

The `CompressionLevel` enum (`Fastest`, `Optimal`, `SmallestSize`, …) is **not** used. Its mapping to Zstd
levels is not the authored contract, and `PackageBuildOptions.CompressionLevel` is.

### 3. Quality mapping

`PackageBuildOptions.CompressionLevel` is a Zstd level and maps **by identity** to
`ZstandardCompressionOptions.Quality`:

| Authored value | Current implementation | Native implementation | Evidence |
| --- | --- | --- | --- |
| `3` (default) | `new Compressor(3).Wrap(...)` | `Quality = 3` | probe payload 182,150 bytes from both; real seed vector `sample-model-1` 3,475 B → 1,415 B from both |
| `9` (`--compact` / benchmark level) | `new Compressor(9).Wrap(...)` | `Quality = 9` | probe payload 178,312 bytes from both; seed vector 3,475 B → 1,387 B from both |
| `1`…`19` | `new Compressor(n)` | `Quality = n` | same Zstd level semantics; no clamping, no remap |

- Validation stays as it is: `PayloadEncoder` and `SimulationPackageWriterSupport.ValidateOptions` both reject
  values outside `1..19` with `ArgumentOutOfRangeException`. The native range is wider
  (`-131072..22`); the public option is deliberately not widened in this amendment, because widening it is a
  user-facing API change with no Step 11 requirement.
- The XML documentation on `PackageBuildOptions.CompressionLevel` gains one sentence: the value is the Zstd
  quality passed to the in-box encoder. No signature or default changes.

### 4. Error behaviour and bounds

**Encode.** Level validation is unchanged. A non-`Done` `OperationStatus` or a consumed-length mismatch throws
`InvalidOperationException`: with a `GetMaxCompressedLength` destination it cannot happen, so it is a programmer
error, like the current code's unhandled library exceptions. No new diagnostic code is introduced.

**Decode.** The two decoders keep their existing, different error contracts. The amendment does not unify
them, because the writer is fail-fast and the read-only reader must report and continue:

- **Writer** (`Mocassin.Packages.SimulationPackage.Payloads.PayloadEncoder.Decode<T>`) **throws**: empty payload
  → `InvalidDataException`; non-positive guard → `ArgumentOutOfRangeException`; invalid frame or a bound above
  the guard → `InvalidDataException`.
- **Reader** (`Mocassin.Results.Packages.Internal.PackagePayloadDecoder.TryDecode<T>`) **never throws on payload
  data**: it returns `default` plus an `out string? error` message. Empty → "The payload blob is empty.";
  non-positive guard → "The decompressed-size guard is not positive."; invalid frame → "The payload is not a
  valid Zstd frame: …"; bound above the guard → "The payload declares N decompressed bytes, above the M guard."
  `PackageVerifierEngine` maps a non-null decode error to `PKG004`.

Both apply the same ordered checks:

1. **Empty payload first.** `ZstandardDecoder.TryGetMaxDecompressedLength` returns `true` with bound `0` for an
   empty span (verified), so it cannot be the first check; the existing empty-payload rejection is preserved.
2. **Guard positive.** Reject a non-positive `maxDecompressedBytes` with the existing type/message per decoder.
3. `TryGetMaxDecompressedLength(frame, out long bound)`. `false` → invalid-frame error.
4. Reject when `bound > maxDecompressedBytes` or `bound > Array.MaxLength` **before** allocating. The current
   writer casts a caller-supplied `long` guard to `int`; the new code never casts an unbounded value.
5. Allocate `(int)bound`; `TryDecompress(frame, destination, out int written)`; `false` → invalid-frame error.
   Return `destination[..written]`.
6. Deserialize MessagePack with the existing options (`MessagePackSecurity.UntrustedData`); failures keep their
   current error type/message.

Deliberate, documented behaviour change: a valid frame **without** a declared content size is now accepted when
its upper bound fits the guard. Today `Decompressor.GetDecompressedSize` throws for such a frame, so the reader
rejects it. The new behaviour is a relaxation for foreign frames, never unbounded, and the writer always
declares the exact size. `TryGetMaxDecompressedLength` returns `false` only for invalid input (verified with a
corrupted header and with four arbitrary bytes), so the mapping to `PKG004` is unchanged for malformed data.

**Allocation.** For our own frames the allocation equals the exact decompressed size; for a foreign frame it
equals the frame's upper bound, capped by the guard. Model and job guards stay 512 MiB and 8 MiB. Peak encode
memory stays `≈ 2.1 × payload + compressed`, the same shape as today (source + `GetMaxCompressedLength`
destination + returned slice).

**Lattice bound (known gap, routed).** The writer guards its own lattice payloads with
`min(int.MaxValue, 4 × A × B × C × P)`, but the read-only reader (`PackageReader.ReadLatticesAsync`) passes
`int.MaxValue`, and `PackageVerificationOptions` has no lattice limit. A hostile lattice frame is therefore
bounded only by `Array.MaxLength` (≈2 GiB) in `inspect`/`verify` today. This amendment **does not** fix that
call site; it is routed to a Step 11 fix feature (`fix-lattice-decode-guard`; see Dependencies and the step page
tasks). The fix has a real dimension source and an explicit orphan-row fallback:

- For a lattice row referenced by a job, the bound is derived from that `jobs` row's `lattice_a`, `lattice_b`,
  and `lattice_c` and the referenced model payload's `structure.position_count` (already decoded within
  `MaxModelPayloadBytes`): `min(int.MaxValue, 4 × a × b × c × position_count)`.
- A lattice row with **no** referencing job has no dimension source. It must not fall back to `int.MaxValue`;
  the fix bounds it with an explicit `MaxLatticePayloadBytes` option, so an unreferenced row can never drive an
  unbounded allocation. No new diagnostic code is introduced and the verifier's diagnostics contract is
  unchanged.

Until that fix lands, no page claims the lattice path is bounded by the verification options, and the
`03-quality-strategy.md` §9.9 bomb claim is corrected to name the lattice exception.

**Resolved (2026-11-15)** by the Step 11 fix feature `fix-lattice-decode-guard` (squash `25a3787`).
`PackageReader.ReadLatticesAsync` now derives each lattice bound from the referencing job's `lattice_a/b/c` and
the referenced model payload's `position_count` with overflow-safe arithmetic, always caps it by the new
`PackageVerificationOptions.MaxLatticePayloadBytes` (default 512 MiB), and uses that cap for an orphan lattice
row. The bomb test asserts `PKG004` for a referenced frame above the derived bound and for an orphan frame above
the cap, and a legitimate 40×40×40 lattice still verifies. No format, payload, signature, or diagnostic code
changed.

**Round-trip self-check.** The writer's existing decode-before-insert self-check uses the new decoder, so the
new path is exercised on every payload it writes.

### 5. Compatibility with the reader, the Rust solver, and existing packages

- Frames are standard Zstd, no dictionary, no checksum requirement, single frame per payload. The Step 13
  `zstd` crate decodes them without configuration, and `zstd` CLI 1.5.7 round-trips in both directions.
- Packages written before the switch decode with the new reader: the decoder is driven by the frame header, not
  by a writer version. No migration, no `.msl` version bump.
- `model_signature`, the parameterization signature, and `lattice_signature` are MessagePack projections
  (ADR 0010) and do not include compressed bytes. `package-summary-v1` excludes payload byte hashes and
  "Zstd/MessagePack implementation details" (`02-data-contracts.md` §3.5). Neither changes.
- Fixture consequences:
  - **One deliberate pre-amendment artifact.** Before the retarget, the amendment captures a small `.msl` with
    the **unamended `net10.0` writer** and checks it in, so the net11 reader has a real pre-switch package to read
    after `ZstdSharp.Port` is removed. This is a compatibility fixture, not the feature-7 determinism fixture;
    its bytes are fixed by definition and are never regenerated (A1-04). Location and provenance are in §9.5.
  - **Golden codec frames.** The pre-amendment `ZstdSharp` frames for the 12 real payloads at levels 3 and 9 are
    checked in with their SHA-256 values (§9.4). This makes the quality-mapping equivalence test possible after
    the reference package is removed (A1-12), and it preserves the byte-identity observation as reproducible
    evidence.
  - `tests/contracts/signature-vectors.json`, `tests/contracts/projections/**`, and
    `contracts/generated/**` are unaffected; `./tools/verify.sh contracts` must stay green with **no** manifest
    or generated-artifact diff. A diff there is a contract change and a review finding, not an expected
    consequence.
  - Feature 7 generates its own `.msl` fixture, the `package-summary-v1` golden, and the extracted payloads once,
    on the amended toolchain. The extracted payload `.bin`/frame is a writer+runtime artifact and is never a
    signature input; the canonical summary and the signatures are the comparable artifacts.
- **If the compressed bytes differ** (a future runtime/Zstd patch, a different level, or a different writer
  binary), only artifacts that capture *bytes* are regenerated:
  - the checked-in golden frames and `goldens.json`, re-captured with a recorded deviation note;
  - feature 7's `.msl` fixture, its extracted payload frames, and the recorded size numbers.
  Nothing else is regenerated: the pre-amendment compatibility fixture is historical and stays byte-fixed, the
  canonical `package-summary-v1` and the model/parameterization/lattice signatures are logical projections and
  must stay equal, and `contracts/**` is never regenerated for a codec change.
- Determinism rule for the future: the same logical input and the same level produce the same compressed bytes
  on the same runtime patch. Across Zstd patch versions the *bytes* may legitimately differ; signatures and the
  canonical summary do not. This is why `package-summary-v1`, not file bytes, is the golden, and why byte
  identity is an observation rather than a gate (§9).

### 6. Toolchain migration surface

Verified file inventory. Counts are from the frozen design commit, not estimates.

| Area | File | Change |
| --- | --- | --- |
| SDK pin | `global.json` | `version` → `11.0.100`, `rollForward` → `latestPatch`, `allowPrerelease` stays `false` |
| Target framework | `Directory.Build.props` | `<TargetFramework>net11.0</TargetFramework>` (covers **21** solution projects) |
| Target framework | `tools/reference/Mocassin.ReferenceHarness/Mocassin.ReferenceHarness.csproj` | explicit `net11.0`; retargeted **now** with the rest (owner decision); the project is outside `Mocassin.slnx` and is verified when the GPL reference clone is available |
| ApiCompat gate | `tools/check-api-compat.sh` | baseline directory, reference-pack glob, current-assembly path → `net11.0` |
| ApiCompat gate | `tools/update-api-baseline.sh` | destination and `obj/Release/net11.0/ref` path |
| ApiCompat baseline | `tests/contracts/api-baseline/net10.0/` | rename to `net11.0/` and regenerate the **9** assemblies listed in `tools/api-projects.txt` |
| ApiCompat tool | `.config/dotnet-tools.json` | `microsoft.dotnet.apicompat.tool` `10.0.403` → `11.0.100` |
| Docs API | `tools/docfx/docfx.json` | 9 assembly paths and `TargetFramework` → `net11.0` |
| CI | `.github/workflows/ci.yml`, `benchmark.yml`, `docs.yml` | **no literal SDK version exists**; all three use `global-json-file: global.json`. Verify `actions/setup-dotnet@v6` installs 11.0.100 at the closeout run |
| Image | `docs/opencode/mocassin.containerfile` | install `11.0.100` only (the .NET 10 install is removed); **carry over** the `COPY global.json` pin step and its assertion block from `9ab4bb8` (the current containerfile has no such step); `ReportGenerator` `5.5.12` → `5.6.0`; DocFX unchanged. The image build is deferred to closeout as an accepted risk (§7). **Correction 2026-11-16:** the copied-pin step was removed after the image build failed with the opencode-container context; the image has no `COPY`/`ADD` step (§7) |
| Test tooling | `Directory.Packages.props` | `Microsoft.NET.Test.Sdk` `18.11.0` → `18.12.0` (owner-accepted net11 compatibility bump, §6 verification items) |
| Packages | `Directory.Packages.props` | remove the `ZstdSharp.Port` version |
| Packages | `Mocassin.Packages.csproj`, `Mocassin.Results.csproj` | remove the `ZstdSharp.Port` reference |
| Codec | `PayloadEncoder.cs`, `PackagePayloadDecoder.cs` | native encode/decode (sections 2–4) |
| Test support | `Mocassin.Results.Tests/.../PackageTestSupport.cs` | native encoder; no new package reference |
| Docs (active, current tense) | see §6.1 — includes the root guides, the published pages, the toolchain page, and the six planning pages that still say .NET 10 | SDK version, baseline path, dependency list, benchmark-environment note, parity row |

`global.json` final content:

```json
{
  "sdk": {
    "version": "11.0.100",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

`latestPatch` instead of the current `latestFeature`: a servicing patch must not change compiler behaviour, but a
feature-band jump (11.0.200+) can change analyzers and generated code. CI installs exactly `11.0.100` from
`global.json`, so the policy only constrains developer machines; failing fast there is the safer failure. The
first feature-band move becomes an explicit, recorded decision.

### 6.1 Documentation inventory

The migration changes every page that states the target runtime in the present tense. The split matters: step
pages, audits, ADRs, benchmark baselines, and recorded measurements describe what was true when they were
written and **stay .NET 10**.

**Active pages that must say .NET 11 / `net11.0` after the migration**

| Page | Current statement | Post-retarget wording |
| --- | --- | --- |
| `README.md` | stack table: "C# / .NET 10"; SDK row `10.0.403` | "C# / .NET 11"; SDK `11.0.100` |
| `CONTRIBUTING.md` | ".NET: .NET 10, nullable enabled…" | ".NET: .NET 11…" |
| `AGENTS.md` §1, §8 | stack table ".NET 10"; ".NET 10, nullable enabled…" | ".NET 11" in both factual rows; §3 process text unchanged |
| `src/dotnet/README.md` | "The .NET 10 solution is rooted at…" | "The .NET 11 solution…" |
| `docs/getting-started/index.md` | SDK `10.0.403` | `11.0.100` |
| `docs/planning/README.md` | "- **.NET 10 / C#** for the model-building system…" | "- **.NET 11 / C#** …" |
| `docs/planning/00-scope-and-principles.md` §3.2 | "**.NET 10** is the minimum runtime…" | "**.NET 11** is the minimum runtime … (target `net11.0`)" |
| `docs/planning/01-target-architecture.md` §6 | "Runtime \| .NET 10 LTS"; "CI … .NET 10, stable Rust" | "Runtime \| .NET 11 (STS)" — .NET 11 is an STS release, not LTS — and "CI … .NET 11, stable Rust" |
| `docs/planning/03-quality-strategy.md` §9.3, §11, §13 | ".NET 10 Release" (twice) and ".NET 10 SDK" | ".NET 11 Release" and ".NET 11 SDK" |
| `docs/planning/06-toolchain-requirements.md` §4/§9 | ".NET SDK 10.0.x", pin `10.0.403` | ".NET SDK 11.0.x", pin `11.0.100`; ApiCompat tool `11.0.100`; ReportGenerator `5.6.0`; remove `ZstdSharp` |
| `docs/planning/appendix-b-parity-matrix.md` §10 | "\| .NET 10 \| Y \| Planned \| 00 \| \|" | "\| .NET 11 \| Y \| Implemented \| 00/11 \| Retargeted by amendment 1 \|" |
| `docs/reference/file-layout.md` | `tests/contracts/api-baseline/net10.0/` | `tests/contracts/api-baseline/net11.0/` |
| `tests/contracts/README.md` | `api-baseline/net10.0/` | `api-baseline/net11.0/`; add the fixture/golden directories |
| `.github/ISSUE_TEMPLATE/bug_report.yml` | placeholder `.NET 10.0.403` | placeholder `.NET 11.0.100` |

**Historical pages that stay .NET 10 (do not edit)**

`docs/planning/steps/step-00-…` (deliverable and CI text), `step-02-…` (runner class), `step-08-…`/`step-09-…`
(benchmark environment), `docs/planning/07-reference-build.md` (the original reference build), all ADRs
(`0002` etc.), `docs/planning/audits/**`, `docs/planning/reviews/**`, `tests/reference/bench/baselines.json` and
the raw benchmark logs, and `docs/performance/index.md` (its measured numbers). The published performance page
gains a *note* that later measurements use .NET 11; the recorded numbers are not rewritten.

### 6.2 One-time cross-TFM ApiCompat proof

Run **before** the .NET 10 runtime is removed from the image and **before** the baseline directory is renamed:

1. Preserve the `net10.0` baseline first — copy `tests/contracts/api-baseline/net10.0/` to `.tmp/net10-baseline/`
   (or read it from git at the pre-retarget commit); `tools/update-api-baseline.sh` starts with
   `rm -rf "$DEST"` and would otherwise destroy the only copy.
2. Generate the `net11.0` baseline (`tools/update-api-baseline.sh`).
3. Compare the preserved net10 baseline (`--left`) with the net11 build (`--right`), with both reference packs
   installed:
   `dotnet apicompat --left .tmp/net10-baseline/<Assembly>.dll --right src/dotnet/<Project>/bin/Release/net11.0/<Assembly>.dll --left-assembly-references .tmp/net10-baseline,$DOTNET_ROOT/packs/Microsoft.NETCore.App.Ref/10.0.14/ref/net10.0 --right-assembly-references src/dotnet/<Project>/bin/Release/net11.0,$DOTNET_ROOT/packs/Microsoft.NETCore.App.Ref/11.0.0/ref/net11.0`,
   for every assembly in `tools/api-projects.txt`.
4. Expect "no breaking changes" and an unchanged `PublicAPI.*.txt`. Record the command and output in the feature
   log; then delete `.tmp/net10-baseline/`.

The `microsoft.dotnet.apicompat.tool` `11.0.100` package itself ships only a `tools/net10.0/any` asset with
`rollForward: Major` in its runtimeconfig. It runs on the .NET 11 runtime and compares net10↔net11 assemblies
(verified), so removing the .NET 10 runtime does not break the gate (A1-14).

Verification items that are gates, not assumptions:

- `dotnet build Mocassin.slnx -c Release` and `dotnet test` with zero warnings under 11.0.100; `dotnet format
  --verify-no-changes`.
- `dotnet list Mocassin.slnx package --vulnerable --include-transitive` still prints the phrase
  `has the following vulnerable packages` when a vulnerability exists; `tools/verify.sh` and `ci.yml` grep that
  exact phrase. Both places must be checked together if it changed.
- The Avalonia XAML compiler and the `PublicAPI` analyzers run cleanly under SDK 11.
- `Microsoft.NET.Test.Sdk` is `18.12.0`. The `18.11.0` run under net11.0 passed but printed
  `VSTEST_ARTIFACTS_DIRECTORY … this is most likely caused by an outdated Microsoft.NET.Test.Sdk package
  reference`. After the bump the full suite must run and coverage must be collected; the warning is expected to
  disappear, and if it persists it is re-checked and recorded as a benign test-host message, not suppressed.
- `tools/reference/Mocassin.ReferenceHarness` builds against `net11.0` whenever the GPL reference clone is
  present (`MOCASSIN_REFERENCE_WORK`/`ReferenceRoot`). It is not in `Mocassin.slnx`, so the solution gate does
  not cover it; if the clone is unavailable before the audit freeze, the verification is recorded as an
  unresolved Step 29 item.

### 7. Container final form

- One SDK: `11.0.100`, installed into `DOTNET_ROOT`; no `10.0.403`.
- Assertion block. It must prove "11.0.100 **only**", not merely that 11.0.100 is present (A1-07):

```sh
test "$(dotnet --list-sdks | wc -l)" -eq 1
dotnet --list-sdks | grep -qE '^11\.0\.100 \['
! dotnet --list-sdks | grep -qE '^10\.'
test "$(cd /opt/mocassin-sdk-pin && dotnet --version)" = "11.0.100"
test "$(command -v dotnet)" = "$DOTNET_ROOT/dotnet"
```

  The exact-count check is the primary negative evidence; the `10.` grep is a belt-and-braces guard against a
  base-image SDK. The `COPY global.json` step is **carried over from `9ab4bb8`** (it does not exist in the
  current containerfile): it makes the image fail if the pin and the SDK ever drift.

**Post-implementation correction (2026-11-16).** The `COPY global.json /opt/mocassin-sdk-pin/global.json` step
and the `test "$(cd /opt/mocassin-sdk-pin && dotnet --version)" = "11.0.100"` assertion are **removed** from
`docs/opencode/mocassin.containerfile`. The opencode-container Makefile builds the image with its own repository
as the context, which has no `global.json`; Podman failed at that `COPY`, so the image could not be built at all.
The image now has **no `COPY`/`ADD` step and no build-context dependency** and builds from any directory. The
self-contained assertions above remain: `dotnet --version`, `dotnet --list-sdks`, the exact count of 1, the
positive `11.0.100` grep, the negative `10.` grep, and the `$DOTNET_ROOT/dotnet` path. With only 11.0.100
installed, `dotnet --version` proves the pin resolves; no `global.json` is duplicated into the image.
`global.json` (`11.0.100`, `latestPatch`) stays as the repository pin for local and CI resolution. The owner
rebuilt `opencode:mocassin` successfully from `fix/container-build-context` on 2026-11-16. The change is
recorded under `fix/container-build-context` in the parent step page's deviation 2. This note corrects the
design text above; the reviewer-authored [design review](../reviews/step-11-amend-1.md) and the
[audit record](../audits/step-11.md) are unchanged.

- `docfx` stays `2.81.0` (the package ships `tools/net11.0/any`; with only the 11.0.0 runtime installed the tool
  host selects it).
- `dotnet-reportgenerator-globaltool` moves `5.5.12` → `5.6.0`, **accepted by the owner**. `5.5.12` ships at most a
  `net10.0` tool asset, so it needs the .NET 10 runtime that this amendment removes; `5.6.0` ships
  `tools/net11.0/any`, which removes the .NET 10 runtime dependence from the image toolchain. This is the one
  tool bump the runtime removal forces.
- **Image build: deferred to closeout as an accepted risk (owner decision).** There is no OCI/podman builder in
  the agent environment, so `opencode:mocassin` is **not** built from the amended containerfile and must not be
  described as build-verified. The accepted risk, its owner, the missing evidence, and the closeout check are
  recorded in the risks table; at closeout the evidence file `docs/planning/closeout/step-11.json` `notes`
  records either a built image reference/digest (if the owner builds it) or "image not built — accepted risk",
  and the Step 11 audit record carries the accepted risk. `python tools/check_closeout_evidence.py
  docs/planning/closeout/step-11.json` must still pass; `platform_ci` covers the Windows/macOS runs, not the
  image.

### 8. `docs/dotnet-11-container` disposition

**Decision: superseded (owner, 2026-11-15).** The pushed branch `docs/dotnet-11-container` (`9ab4bb8`) is not
merged. It installs 10.0.403 **and** 11.0.100 and asserts both, which is exactly the interim state this
amendment replaces.

- The amendment feature branch carries the final single-SDK containerfile: 11.0.100 only, the `.NET 10` install
  removed.
- Carry-over from `9ab4bb8`: the `11.0.100` install line and pin, the `COPY global.json /opt/mocassin-sdk-pin/global.json`
  step, the `dotnet --list-sdks` assertions, and the `dotnet --version` pin check — all reused, with the
  `10.0.403` half deleted. The `11.0.100` install line is not re-derived. **Post-implementation correction
  (2026-11-16):** the `COPY global.json` step and its `/opt/mocassin-sdk-pin` assertion were removed because the
  opencode-container build context has no `global.json`; see §7 for the replacement and the reason.
- After the amendment is integrated, `docs/dotnet-11-container` is deleted **unmerged** (local and remote) as an
  explicit cleanup step of the integration. It is not deleted before integration, so its commit remains
  available until the replacement is on the step branch.

### 9. Benchmark gate

The gate has two parts. **Part A is complete** — it was the pre-acceptance gate and is recorded in §9.1–§9.6.
**Part B** runs on the amended tree before the Step 11 audit freeze. The `/tmp` Part A report is not durable;
this page is.

#### 9.1 Part A — codec comparison on real payloads (completed 2026-11-15)

**Status: complete and passing under the refined small-payload rule.** The measurement ran at repository commit
`aa481143aff254e0a7faf2828e7dc150c2d82833` on the unamended `net10.0` writer; it was the pre-acceptance gate and
is the evidence recorded here. It is not repeated as a routine test; the checked-in golden frames (§9.4) preserve
its reproducible part.

**Provenance.** Packages were built by the **current unamended `net10.0` writer** (SDK `10.0.403` pinned by the
probe's `global.json`) from `samples/jobs/ceria-temperature-sweep.mocprj` through the Step 11 pipeline
(`ProjectJson` → `ModelCompiler` → `ParameterizationService` → `JobTemplateExpander` → `SupercellGenerator` →
`SimulationPackageWriter`), six jobs and six distinct lattices. A supplementary 40×40×40 lattice gives one
package-scale payload; it is real pipeline output, reported separately, and **not** part of the authored-sample
gate.

| Package | Bytes | SHA-256 | Contents |
| --- | ---: | --- | --- |
| `ceria-l3.msl` | 98,304 | `053e4c8f5bd4ee115061dd5cb2b38266a50b155e504fe2108017216963413e59` | sample, default level 3 |
| `ceria-l9.msl` | 98,304 | `878d06f525bc992b26389de07c7aea0d5cb0d42e69e4917d07199c28a3e9a039` | sample, `CompressionLevel = 9` |
| `supp-large-lattice-l3.msl` | 98,304 | `3696aff30e9b65e54ac0f188f0c474720fe9c27bf40652365f11f8ae307b42ec` | supplementary level 3 |
| `supp-large-lattice-l9.msl` | 98,304 | `432699ff8a9bbd823a9cc38f21fef89b6a4e932a20fb9a9c131fd1b5479d37c8` | supplementary level 9 |

Twelve distinct raw MessagePack payloads were extracted (byte-identical across the level-3 and level-9 packages).
Probes ran on SDK `11.0.100` / runtime `11.0.0`, `ZstdSharp.Port 0.8.8` (`netstandard2.0` asset on `net11.0`),
`zstd` CLI 1.5.7, AMD Ryzen 7 7840U, 16 logical CPUs, Linux 6.12.

**Method.** Paired design: for each payload/level/operation the reference (`new Compressor(level).Wrap`,
`new Decompressor().Unwrap`) and the candidate (`ZstandardCompressionOptions{Quality=level}` → `SetSourceLength`
→ `Compress(isFinalBlock: true)`; `TryGetMaxDecompressedLength` → `TryDecompress`) run in the **same** round with
alternating order; the reported value is the median of 25 per-round ratios. Warm-up ≥150 ms plus 250 ms settle;
primary run `DOTNET_TieredCompilation=0` so both codecs are fully optimized; a default-JIT/PGO sensitivity run;
a focused 10-repetition level-3 decode run; and CLI cross-checks. MessagePack serialization, SQLite I/O, and job
insertion are excluded (Part B covers those). Commands:

```sh
cd /tmp/opencode/part-a/probe-net10 && dotnet run -c Release           # build packages with the net10 writer
python3 /tmp/opencode/part-a/results/extract.py                        # extract BLOBs, decompress, index
cd /tmp/opencode/part-a/probe-net11 && DOTNET_TieredCompilation=0 timeout 300 dotnet run -c Release
cd /tmp/opencode/part-a/probe-net11 && timeout 300 dotnet run -c Release   # default-JIT sensitivity
cd /tmp/opencode/part-a/probe-targeted && DOTNET_TieredCompilation=0 timeout 120 dotnet run -c Release
cd /tmp/opencode/part-a/probe-cli && timeout 120 dotnet run -c Release     # zstd -t/-d and CLI→native
```

**Refined thresholds (owner decision, 2026-11-15).**

| Metric | Rule |
| --- | --- |
| Correctness | 100 % round-trip, cross-decode, and `zstd` CLI decode; canonical signatures and `package-summary-v1` equal |
| Compressed size | ≤ +3 % versus `ZstdSharp` at the same level |
| Encode time | ≤ +25 % at the same level |
| Decode time, payload **≥ 8,192 B** | median ratio ≤ +10 % |
| Decode time, payload **< 8,192 B** | median **absolute** penalty ≤ **0.15 µs per payload**; the relative ratio is recorded but not gated |
| Decode throughput | ≥ 100 MB/s on the corpus |
| Byte identity | an **observation**, recorded; not a gate and not a rollback trigger |

Why the small-payload exception: the authored payloads are 509–3,475 B, so decode is 0.3–4.2 µs and fixed reader
overhead dominates. On the two 2,109-byte lattices the level-3 ratio is +9.27 % (controlled) to +11.04 % (default
JIT), i.e. the +10 % line sits inside the run-to-run spread; the absolute penalty is 0.04–0.07 µs on a 2.5–3.8
GB/s decode path, while the model and all job payloads decode faster. Gating a sub-microsecond operation on a
relative ratio would make the verdict depend on host noise; gating it on an absolute 0.15 µs (≈2× the observed
worst) plus the 100 MB/s floor tests the property that matters. The rule is applied per payload, so one payload
cannot hide behind a fast corpus average.

**Results** (controlled run; default-JIT in parentheses where it differs):

| Origin | Level | Size Δ | Encode Δ | Decode Δ | Throughput | Worst absolute decode penalty | Verdict |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| sample | 3 | +0.000 % | −4.20 % | +9.27 % (+11.04 %) | 302 MB/s (304) | +0.063 µs (+0.057) | PASS (refined rule) |
| sample | 9 | +0.000 % | −5.88 % | +4.62 % (+6.10 %) | 299 MB/s (302) | +0.018 µs (+0.024) | PASS |
| supplementary | 3 | +0.000 % | −5.98 % (−83.61 %) | −2.00 % (−23.94 %) | 2,133 MB/s (4,616) | n/a (≥ 8 KB) | PASS |
| supplementary | 9 | +0.000 % | −10.51 % (−18.45 %) | −0.22 % (−2.86 %) | 2,323 MB/s (7,319) | n/a (≥ 8 KB) | PASS |

The absolute penalties are rounded from the raw JSON of the two runs: level 3 controlled `+0.06319 µs`
(`sample-lattice-6`) and default `+0.05689 µs` (`sample-lattice-4`); level 9 controlled `+0.01785 µs` and default
`+0.02360 µs` (`sample-lattice-5`). The focused 10-repetition run reports `0.77 → 0.84 µs` (`+0.07 µs` rounded)
for `sample-lattice-6`, which is why the prose above quotes a 0.04–0.07 µs range for the two 2 KB lattices.

- **Checks:** 264 checks per run — 240 required and 24 informational byte-identity — with **0 failed** in both
  full runs. Round-trip 100 %, cross-decode 100 %, `zstd -t` 48/48, `zstd -d` 48/48, CLI→native 24/24.
- **Byte identity:** 24/24 payload-level frames identical, including every in-package blob. Recorded as an
  observation, not a gate (§9.3).
- The two runs disagree on which 2,109-byte payload is worst (`sample-lattice-4` default, `sample-lattice-6`
  controlled), which is the measurement-spread signature described below.
- The focused 10-repetition run: `sample-lattice-6` level-3 decode ratio median 1.122 (min 1.093, max 1.134,
  9/10 repetitions above 1.10), absolute 0.77 → 0.84 µs.

**Limitations (recorded so the numbers can be trusted).**

1. Small payloads: 509–3,475 B authored; the 256 KB supplementary payload is reported separately.
2. JIT tiering changes the level-3 ratio by 1–2 pp; the primary run disables tiering so both codecs are fully
   optimized. This is the dominant uncertainty.
3. Shared host (load ≈3.4 on 16 logical CPUs, CPU scaling reported at 31 %); the paired design cancels drift,
   but the 2 KB spread is ±5 pp.
4. Probe calibration/extraction defects (batch sizing, level-tagged filenames) were found and fixed before the
   reported runs; the measurement report records them.
5. Scope excludes MessagePack serialization, SQLite I/O, and job insertion.

**Post-swap re-run (implementation confirmation, 2026-11-15).** After the codec replacement the same paired probe
was re-run on the amended tree against the **checked-in** payloads and golden frames
(`tests/contracts/packages/amend-1/`), so it measures the implemented path and the committed artefacts rather than
the `/tmp` capture. Twelve payloads × levels 3 and 9; **240/240 required checks per run, 0 failed**.

| Run | Level | Origin | Size Δ | Encode Δ (worst) | Decode < 8 KiB: worst absolute | Decode ≥ 8 KiB: worst ratio | Throughput (min) | Refined gate |
| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | --- |
| controlled (tiering off) | 3 | sample | +0.000 % | −1.38 % | **+0.0627 µs** | n/a | 300 MB/s | PASS |
| controlled (tiering off) | 9 | sample | +0.000 % | −8.44 % | +0.0100 µs | n/a | 297 MB/s | PASS |
| controlled (tiering off) | 3 | supplementary | +0.000 % | −6.44 % | n/a | 0.9423 | 2,059 MB/s | PASS |
| controlled (tiering off) | 9 | supplementary | +0.000 % | −10.14 % | n/a | 1.0208 | 2,188 MB/s | PASS |
| default JIT/PGO | 3 | sample | +0.000 % | +4.12 % | **+0.1055 µs** | n/a | 304 MB/s | PASS |
| default JIT/PGO | 9 | sample | +0.000 % | −12.01 % | +0.0404 µs | n/a | 305 MB/s | PASS |
| default JIT/PGO | 3 | supplementary | +0.000 % | −26.57 % | n/a | 0.9329 | 6,145 MB/s | PASS |
| default JIT/PGO | 9 | supplementary | +0.000 % | −15.22 % | n/a | 0.9902 | 7,222 MB/s | PASS |

- **Size:** exactly 0.000 % because every native frame is byte-identical to its checked-in golden (24/24). The
  `CodecGoldenTests` PR-fast test asserts the same through `PayloadEncoder` at levels 3 and 9, including the
  `sample-model-1` seed vector (3,475 B → 1,415 B / 1,387 B).
- **Encode:** within +4.12 % worst case (default-JIT level 3; the controlled run is faster on every row), far
  inside the ≤ +25 % rule.
- **Decode:** the worst small-payload absolute penalty is +0.1055 µs against the ≤ 0.15 µs rule; the ≥ 8 KiB
  supplementary payload is +2.08 % against the ≤ +10 % rule. Throughput is ≥ 297 MB/s against the ≥ 100 MB/s
  floor.
- **Cross-decoder:** 48/48 probe frames pass `zstd -t` and `zstd -d`, and the 24 checked-in goldens pass
  `zstd -t`; cross-decoding in both directions is part of the 240 checks.
- **Cross-version and invariance:** the pre-amendment `.msl` fixture verifies on the amended tree and its
  canonical summary matches its golden (PR-fast test). The reverse direction and the summary equality are
  recorded in §9.5. `./tools/verify.sh contracts` is green with no `contracts/**` diff.

#### 9.2 Part B — 10⁴-job budget (before the audit freeze)

Feature 7's benchmark runs on the amended tree and records wall clock, peak RSS, package size, model-payload
size, and job insertion rate for 10⁴ small jobs (10×10×10 ceria, ~1,000 positions). The **net10 reference run is
mandatory**: the unamended tree is available at the pre-retarget commit, but the feature-7 harness does not exist
there. The approved method is to copy (cherry-pick) the benchmark source into a temporary worktree of the
pre-retarget commit and build it there against the net10 writer; if only the read side is being compared, the
amended harness can instead read a package written by the net10 writer. The method used is recorded with the
numbers (A1-08).

| Budget | Target |
| --- | --- |
| 10⁴ jobs wall clock | < 10 min, and ≤ +5 % versus the net10 reference |
| Peak RSS | < 2 GB |
| Job insertion rate | ≥ 100 jobs/s |
| Package size | ≤ +3 % versus the net10 reference |
| Complex model payload | < 50 MB |

Fallback **only** if the reference run cannot be produced for a documented environmental reason: use absolute
budgets — 10⁴ jobs < 10 min, peak RSS < 2 GB, insertion ≥ 100 jobs/s, model payload < 50 MB, package size below
the 2 GB advisory — and state that the comparison was not made. The codec's size contribution is already bounded
by Part A's ≤ +3 % payload rule, so the absolute fallback does not hide a codec regression. Part B does not
measure per-payload decode; if feature 7 reports that metric, the §9.1 small-payload rule applies.

#### 9.3 Rollback threshold

- **Automatic rollback:** any correctness failure — round-trip, cross-decoder, canonical signature or
  `package-summary-v1` mismatch, or a failure to read the pre-amendment compatibility fixture (§9.5).
- **Threshold rollback:** compressed size, encode, decode (including the refined small-payload rule), throughput,
  or a Part B budget breached beyond measurement error.
- **Not a rollback trigger:** compressed bytes differing from the pre-amendment codec. Byte identity is an
  observation; the determinism rule in §5 explains why and what is regenerated if it changes.
- On rollback: keep `net10.0` + `ZstdSharp.Port`, abandon the feature branch, and record the decision and the
  numbers in `05-open-decisions.md`. After integration, a fix branch restores the previous state before the
  audit freeze; the parent step must not be audited with a known-regressed codec.

#### 9.4 Golden frames (pre-amendment codec)

These are the `ZstdSharp.Port 0.8.8` frames for the 12 real payloads at levels 3 and 9. The native frame at the
same level is byte-identical on the measured runtime, so either seeds the test vector. They are checked in under
`tests/contracts/packages/amend-1/golden/` with `goldens.json` (id, level, path, bytes, SHA-256, raw SHA-256) and
the raw payloads under `tests/contracts/packages/amend-1/payloads/`.

Source artefacts from the Part A run are under `/tmp/opencode/part-a/golden/` and `/tmp/opencode/part-a/payloads/`
(throwaway). If they are gone when the feature starts, re-capture them from the unamended commit **before** the
retarget (feature order step 0); ZstdSharp.Port 0.8.8 is deterministic, so the expected values are the hashes in
this table.

| Payload | Kind | Raw B | L3 B | L3 frame SHA-256 | L9 B | L9 frame SHA-256 | Raw SHA-256 |
| --- | --- | ---: | ---: | --- | ---: | --- | --- |
| `supplementary-lattice-1` | lattice | 256,063 | 2,857 | `fd639d3bc36cec738e8a00a0ed5b0dc5fa07bd5c711b7667a9171bbacf11575a` | 2,597 | `fa48e72e80d8b37397902aa50d3f4146a02885937f37385e4638e775a7f0b77b` | `42fbc111fe666c98b09959794fc8f7ccb3013ce1a87f348f94fe97667e2803be` |
| `sample-model-1` | model | 3,475 | 1,415 | `1dc0bf3b5c82b1146c3ee2db85863e035bff8761cfedaa10047569dd21fc943f` | 1,387 | `4b7e6106efcc23f56eb07d8462d0c09ef9bc9e9320a370fba31339874172400b` | `066e9f3e069e379802c7135f6ab5c4a4c01ac5fa08aa658c0b335aa5ba8a4180` |
| `sample-lattice-3` | lattice | 2,109 | 151 | `6833f0a7aaf9a9766d9e2c2c3e61d0170a09add360d0f7628af1a5913991bc3b` | 131 | `e40a17cb0e544f4d9b5b40aa420fd686c55aa19a5b323f73fd4eb0fb23a85cd4` | `22c7a5ccdb1be852b88f8f301620283046ba76b198905383fcf6170f3576b706` |
| `sample-lattice-4` | lattice | 2,109 | 143 | `b9e2d2990bfefcd1bfa082643d36cf01d2c3bbc48e02eaf735c439869a00ca24` | 138 | `4dcee2fb32e9afb2f73ee8878fd1fcad2b189dcac40f0fa83a6878bf2ef77813` | `24eb2504410c67ad4cf404cb2a2ac7f8b6972822b71226dc9d4c151d77745501` |
| `sample-lattice-6` | lattice | 2,109 | 159 | `d5ea702d5a2f2e57dceb71bea0cbdfaaa36ba3560288b7b750f66d9ffa14b9d4` | 149 | `222cc7ea1b77549c99e2beaf45deb4fb306b250611cde995ac0a781860bf872a` | `4c7659d905830ed04034081dce9edabb348e29c718adbb9a765db6c6b124337b` |
| `sample-lattice-1` | lattice | 925 | 90 | `4fa927345c29bf08c432398c2a6597f66532496a989ffeedb01b68a29a6ec7f8` | 90 | `497887de51c8bdbdd0c06e375e69fe58b5203a0937cdff43b506ae8a79359ec0` | `cf5de83a816d1ab4738887265309e9691fb2edbe4b74acfc86fe17db43a547de` |
| `sample-lattice-2` | lattice | 925 | 90 | `719a56f8cc08f62ddb9b8abba71615c15ea7296cfcdc0bc72469efff19972f5b` | 90 | `fcf5187b182b3224d4019a47f926e0664649046f65660f66f9c7df080b5ed9dc` | `5db23cdad7e08f8d0d579ab5fe3227527ad859cb5e285e040fe8555d91aef50c` |
| `sample-lattice-5` | lattice | 925 | 92 | `ca552bab130bc97e469fade83cb2580a3566e2b2edd668fbf4f8f5ae65942857` | 91 | `3196a807812f9ae6c440f52ed52dcad4a38c8eacfb30be5114a1b63e9ee82521` | `f72c39af9112f691dbe22d98658b9a68bb7e82a26d8ef63fcd73f4c35e17f08f` |
| `sample-job-2` | job | 544 | 408 | `e5f83795d9b94959571d45c48808b27a30fdaa1e237ca67d87c4d34e04aac5d4` | 407 | `5ecb8d6a1f993345a3c910120c201c68c67b5f829bfaa721b27aa7e33a6b3d88` | `c9cd9fe27b983707d57c02e9c2665335660bff9a7b6ae4926eb86a26599ab022` |
| `sample-job-4` | job | 536 | 408 | `32d0a610e3be924488ecdab651337336d891e2860a983164b268cee5c8cd95fd` | 408 | `26a3925ff4220837292bcb50b3d9674d5df4ea889a127a3726d2acc960105d89` | `62fe70d6714bd420e7339312a56b0586370b6d92a931aec2f77661d37b5bd105` |
| `sample-job-5` | job | 517 | 391 | `d5eedf17a840326d815f78705c864e48a62a09281d448271c496056c61ace7e0` | 387 | `b95070f98c90f85713c108473a08497a362ba03af26824124b5e53a3786976ff` | `7a1e48dd6c8912975d18bb7b0de01d91e479771e176333aa9a090b2314adca95` |
| `sample-job-6` | job | 509 | 391 | `21edcad1da1a08ae23586eb2a3f0d0d20bde8eed6731ca4b86087681da8b6c37` | 387 | `a63f83d5dfd6873d5f1fc6542c81c68a0aa251e2736e0db491098b46a106709f` | `29cc8d4d9cee0c41be4b71787a4ea5920a41d20689b50c256bd24f92d4086459` |

Seed vector for the quality-mapping test: **`sample-model-1`** — 3,475 B raw → **1,415 B at level 3** and
**1,387 B at level 9**. The test asserts that the native encoder reproduces the checked-in golden frame at each
level. If a future runtime patch changes the bytes, the test is re-baselined by re-capturing the golden with a
recorded note; the mapping (level → `Quality`) does not change, and no signature or summary is affected.

#### 9.5 Pre-amendment `.msl` compatibility fixture

Captured **before** the retarget, on the amendment feature branch, with the still-net10 writer:

- Location: `tests/contracts/packages/amend-1-net10/`.
- Files: `ceria-temperature-sweep.msl` (the six-job sample, default level), `ceria-temperature-sweep.sha256`,
  `package-summary-v1.json` (the inspector projection), and `README.md` with provenance (writer commit, SDK
  `10.0.403`, `created_utc`, `app_version`, job/lattice counts, SHA-256).
- The file is tracked through the existing `.gitignore` exception `!tests/contracts/**/*.msl`.
- Test (PR-fast): the net11 reader opens the fixture read-only, `PackageVerifier` passes, `PackageInspection`
  matches the recorded counts, and `BuildCanonicalSummaryAsync` equals the checked-in `package-summary-v1`.
- It is a **compatibility** fixture, not the feature-7 determinism fixture; it is never regenerated.
- The reverse direction (a net11-written package read by the pre-amendment net10 build) is a one-time migration
  check: check out the pre-retarget commit in a temporary worktree, run its reader on a native-written package,
  and record the result in the feature log. It is not a recurring test, because the pre-amendment build no
  longer exists on the branch.
- **Reverse-direction result (2026-11-15):** the amended `net11.0` writer produced a package with SHA-256
  `053e4c8f5bd4ee115061dd5cb2b38266a50b155e504fe2108017216963413e59` — identical to the fixture's, because the
  native frames are byte-identical and SQLite output is deterministic. The pre-retarget `net10.0` build at
  `e2b00ea` (temporary worktree) read it with `PackageVerifier`: `IsValid = true`, 10 checks, 0 diagnostics,
  1 model / 6 lattices / 6 jobs, and produced the same canonical-summary SHA-256
  `60c95e1bcb2438781a6d9a2597840fd04c8e0e6c3caec254cf76589ec2739d2d` as the checked-in golden. The forward
  direction (fixture read on `net11.0`) is the PR-fast `PreAmendmentPackageCompatibilityTests`.

#### 9.6 Synthetic probe (supplementary, not a gate)

The earlier probe remains as supporting evidence: a 418,611-byte mixed payload at levels 3 and 9, 2/16/48 MiB
payloads, byte-identical frames from both codecs, decode 0.18 ms native vs 0.43 ms `ZstdSharp`, encode 1.7–12×
faster. It is indicative only; §9.1 is the gate.

## API and contract impact

- No public API addition, removal, or signature change **from this amendment**. `PayloadEncoder` and
  `PackagePayloadDecoder` are `internal`. `PackageBuildOptions.CompressionLevel` keeps its name, type, default,
  and 1..19 range; only its XML documentation gains a sentence. The separately routed
  `fix-lattice-decode-guard` feature adds the `PackageVerificationOptions.MaxLatticePayloadBytes` init property
  (a non-breaking addition, recorded on the step page).
- No project/package/results/state format change. No `format_version` bump. `contracts/manifest.json` and
  `contracts/generated/**` are untouched. ADR 0009's version-bump rule is not engaged.
- ApiCompat: the baseline **directory and assemblies** move to `net11.0`. The public surface must be identical;
  the one-time cross-TFM check proves it. `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` must not change as a
  consequence of the retarget. If an analyzer under SDK 11 reports a genuine public-surface issue, it is a
  finding to resolve, not a baseline refresh.
- Dependency set: `ZstdSharp.Port` disappears from `Directory.Packages.props` and from the two project files.
  `check_nuget_licenses.py` output shrinks accordingly; the `policy` gate must stay green.
- Licensing: no GPL-derived content, and no new package. The licensing layers are different and the distinction
  matters:
  - the managed `System.IO.Compression.Zstandard*` API and the rest of the .NET runtime are MIT;
  - the **native zstd implementation** the runtime wraps is **BSD-3-Clause** (the .NET runtime's own
    third-party notices already carry it);
  - `ZstdSharp.Port` (MIT) is removed.

  The repository ships framework-dependent assemblies and does not redistribute or bundle the .NET runtime, so
  `NOTICE.md` needs no change for this amendment. If Step 31 ships a self-contained, single-file, or NativeAOT
  artifact, that artifact must include the runtime's third-party notices, including the BSD-3-Clause zstd
  notice; that requirement is recorded as a Step 31 follow-up rather than a Step 11 change.

## Temporary implementation and migration

What exists today: `PayloadEncoder` and `PackagePayloadDecoder` wrap `ZstdSharp.Port 0.8.8`
(`Compressor.Wrap` / `Decompressor.Unwrap` / `Decompressor.GetDecompressedSize`), and
`PackageTestSupport.Encode` uses the same library to build mutated payloads for the `PKG` tests.

What is temporary: the third-party codec and the `net10.0` toolchain pin.

How the amendment replaces it: sections 2–4 replace the codec at both ends and in test support; section 6
replaces the toolchain. No compatibility shim is required. There is no dual-encoding path, no format flag, and
no "try native then fall back" logic: one encoder, one decoder, standard frames.

Order of work inside the feature (so a rollback is a branch drop, not a revert tangle):

0. **Capture the pre-amendment artifacts with the still-net10 toolchain** and commit them: the `.msl`
   compatibility fixture and its `package-summary-v1` (§9.5), and the 12 payloads with their 24 golden frames and
   hashes (§9.4). This must be the first commit on the feature branch, before `global.json` moves; after the
   retarget the net10 writer can no longer be built. Record the pre-retarget commit hash (it is the net10 build
   used later for the reverse-direction read).
1. Retarget + `global.json` + tool/baseline/docs churn; full `./tools/verify.sh all --require` with the codec
   still on ZstdSharp (ZstdSharp is TFM-compatible with net11.0, so this step is independent and revertible).
2. Run the one-time cross-TFM ApiCompat proof (§6.2), preserving the net10 baseline first.
3. Replace the codec, add the golden-frame and compatibility-fixture tests, remove the references, and run the
   gates again. Then run the reverse-direction read: build the pre-retarget commit in a temporary worktree and
   open a package written by the amended tree with the old net10 reader; record the result.
4. Part A is **already complete** (§9.1); do not re-run it. Feature 7 runs Part B.
5. Land the routed lattice decode-bound fix (§4) on its own Step 11 fix feature before the audit freeze.

Step 0 and step 1 are the mitigation for the largest risks: the retarget can fail on its own without the codec
change, and vice versa; and the pre-switch evidence is captured before either can destroy its source.

## Acceptance criteria

- [x] `dotnet build Mocassin.slnx -c Release` and `dotnet test Mocassin.slnx -c Release` pass on SDK 11.0.100
      with zero warnings. The amended tree runs **1,506** tests (the pre-amendment branch point ran 1,497; the
      +9 are the amendment's codec/golden and decoder-bound tests) with 0 failures.
- [x] `global.json` pins `11.0.100` with `latestPatch`; `dotnet --version` resolves to `11.0.100` locally. The
      containerfile carries the exact-count and pin assertions; CI resolves the same pin from `global.json`.
      **Correction 2026-11-16:** the copied-`global.json` assertion was removed; the self-contained exact-count,
      positive `11.0.100`, negative `10.`, and `dotnet --version` checks remain (§7).
- [x] No `ZstdSharp` reference remains in `Directory.Packages.props`, any `.csproj`, or any source file
      (`grep` is empty); `check_nuget_licenses.py` reports **52** packages (was 58) and the policy gate is green.
- [x] Model, job, and lattice payloads encode and decode only through the in-box API, and the existing
      round-trip, determinism, guard, and corruption tests pass unchanged apart from the test-support encoder.
- [x] The decode guard still produces `InvalidDataException` in the writer and `PKG004` in the verifier for a
      truncated frame, a corrupt frame, and a frame whose bound exceeds the guard, with no unbounded allocation
      for model and job payloads. The lattice read-path gap is routed (§4).
- [x] Model, parameterization, and lattice signatures and the `package-summary-v1` projection are identical
      before and after for the same logical input; `contracts/**` has no diff and `verify.sh contracts` is green.
- [x] The checked-in pre-amendment `.msl` fixture (§9.5) opens read-only on the amended tree, verifies without a
      `PKG` error, and its `package-summary-v1` equals the checked-in golden (PR-fast test). The reverse-direction
      read is recorded in §9.5 as a one-time migration check.
- [x] `zstd` CLI 1.5.7 decodes a native payload byte-identically, and a CLI-produced frame decodes through the
      package reader (probe and `PackagePayloadDecoderTests`).
- [x] The quality-mapping test reproduces the checked-in golden frames at levels 3 and 9, including the
      `sample-model-1` seed vector (3,475 B → 1,415 B / 1,387 B).
- [ ] Part A is complete and recorded in this page (§9.1) under the refined small-payload rule; Part B is run on
      the amended tree with the mandatory net10 reference (or the documented absolute fallback). **Part A is
      complete and re-confirmed after the swap (§9.1); Part B belongs to feature 7 and is not part of this
      feature.**
- [x] The lattice decode-bound gap (§4) is closed by the Step 11 fix feature (`fix-lattice-decode-guard`),
      squashed as `25a3787`: derived per-job bound, `MaxLatticePayloadBytes` cap/fallback, bomb test, and the
      updated §5 check 4 wording.
- [x] The ApiCompat baseline is regenerated at `tests/contracts/api-baseline/net11.0/`; the one-time cross-TFM
      comparison reports no breaking changes; the retarget left `PublicAPI.*.txt` unchanged and `verify.sh api` is
      green. (The later routed `fix-lattice-decode-guard` feature adds `PackageVerificationOptions.MaxLatticePayloadBytes`
      to `PublicAPI.Unshipped.txt`; it is an intentional, ApiCompat-compatible addition recorded on the step page.)
- [x] `./tools/verify.sh all --require` passes on the amendment branch (exit 0).
- [x] The containerfile installs 11.0.100 only and asserts it; ReportGenerator 5.6.0 and DocFX 2.81.0 resolve;
      the image build is **not** claimed as verified — it is deferred to closeout as the accepted risk.
- [x] `Microsoft.NET.Test.Sdk` is `18.12.0`; the suite runs and coverage is collected
      (`coverage.cobertura.xml` produced). The `VSTEST_ARTIFACTS_DIRECTORY` message persists because this
      environment has no `/run/user/1000`; it is recorded as a benign, environment-specific test-host message,
      not suppressed.
- [x] `tools/reference/Mocassin.ReferenceHarness` is retargeted to `net11.0` and builds against a copy of the
      GPL reference clone placed outside the repository tree (`/tmp/opencode/mocassin-reference/original`, so the
      repository's central package management does not apply to the clone). The verification build used
      `-p:NuGetAudit=false` because the clone's legacy packages carry known-vulnerability audit warnings that
      `TreatWarningsAsErrors` would otherwise reject; it reports one RID warning from the clone's
      `SQLitePCLRaw.lib.e_sqlite3` assets. The clone itself is not part of the solution gate.
- [x] `NOTICE.md` is unchanged (framework-dependent build); the self-contained/AOT third-party-notice obligation
      is recorded as a Step 31 follow-up and in `06-toolchain-requirements.md` §4.
- [x] Toolchain documentation is updated and `./tools/verify.sh docs` is green.
- [x] Parent step page, amendment index, status dashboard, and open decisions reflect the amendment state.
- [ ] The Step 11 audit request names this amendment, the combined commit, the completed Part A, and the routed
      lattice fix. **Closeout task; not part of this feature.**

## Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Payload round-trip (model/job/lattice) | PR-fast | Native encode → native decode returns the identical logical payload; existing expectations unchanged |
| Encoder determinism | PR-fast | Two encodes at the same level are byte-identical; different levels change bytes but not the decoded payload |
| Quality mapping (golden frames) | PR-fast | At levels 3 and 9 the native encoder reproduces the checked-in pre-amendment golden frames, including `sample-model-1` 3,475 B → 1,415 B / 1,387 B; level 0/-1/20 throws `ArgumentOutOfRangeException`. A runtime-patch byte change re-baselines the golden with a recorded note; it does not change the level mapping |
| Decode-bomb guard (model/job) | PR-fast | A frame whose bound exceeds the guard fails with `InvalidDataException` (writer) and `PKG004` (verifier) before allocation |
| Empty and non-positive guard | PR-fast | An empty payload is rejected first (writer: `InvalidDataException`; reader: "The payload blob is empty."); a non-positive guard keeps its current type/message per decoder |
| Foreign-frame acceptance | PR-fast | A content-size-less standard frame within the guard decodes; one whose bound exceeds the guard is rejected |
| Corrupt/truncated frame | PR-fast | Garbage, a corrupted header, and a truncated frame fail cleanly with the decoder's documented error surface; no unhandled exception |
| Cross-version read (pre-amendment fixture) | PR-fast | The checked-in net10 package fixture opens read-only on the net11 reader, verifies, and matches its `package-summary-v1` golden |
| Cross-implementation decode | PR-medium | `zstd` CLI 1.5.7 and the native decoder both reproduce the same logical payload from the same frame; a CLI frame decodes through the reader |
| Signature and summary invariance | PR-fast | Signatures and `package-summary-v1` are unchanged for the same logical input; `contracts/**` has no diff |
| Toolchain identity (cross-TFM ApiCompat) | One-time (migration) | Recorded evidence, not a per-PR test: the preserved net10 baseline vs the net11 build reports no breaking changes and `PublicAPI.*.txt` is unchanged (§6.2) |
| Reverse-direction read | One-time (migration) | Recorded in the feature log: the pre-retarget net10 build reads a native-written package |
| Benchmark Part B | Nightly/release | 10⁴ jobs within the time, RSS, size, and insertion-rate budgets, against the mandatory net10 reference or the documented absolute fallback |

Part A is not a recurring test row: it is a completed pre-acceptance measurement recorded in §9.1, and its
reproducible part is the golden-frame test. The lattice decode-bound test belongs to the routed Step 11 fix
feature (§4), not to this amendment.

Every test declares its tier. The `One-time (migration)` rows are recorded evidence, not recurring test tiers:
they need the pre-retarget tree and are therefore run once during the migration and recorded in the feature log.
Payload tests use the real mappers, the real writer, and real SQLite files; the codec comparison uses the real
package payloads. No domain doubles.

## Documentation

- [ ] `docs/reference/msl-format.md` (feature 7) states the payload codec as the in-box Zstd encoder with the
      fixed one-shot path and the quality mapping, and that reads accept any valid standard frame. **Feature 7.**
- [x] `docs/reference/file-layout.md` points at `tests/contracts/api-baseline/net11.0/` and lists the
      `tests/contracts/packages/amend-1/` and `amend-1-net10/` fixture directories.
- [x] `tests/contracts/README.md` records the new baseline path and the golden/fixture layout, with the
      regeneration rule (bytes may be re-captured; signatures and summaries are not regenerated).
- [x] `docs/getting-started/index.md`, `README.md`, `CONTRIBUTING.md`, `src/dotnet/README.md`, and `AGENTS.md`
      §1/§8 state .NET 11 (active pages only; §6.1 lists the historical pages that stay .NET 10).
- [x] `docs/planning/README.md`, `00-scope-and-principles.md` §3.2, `01-target-architecture.md` §6, and
      `03-quality-strategy.md` §9.3/§11/§13 state .NET 11 / `net11.0` with the wording in §6.1; .NET 11 is
      described as STS, not LTS.
- [x] `docs/planning/appendix-b-parity-matrix.md` §10 row becomes `.NET 11 | Y | Implemented | 00/11` with the
      amendment note.
- [x] `docs/performance/index.md` keeps its historical .NET 10 measurements and adds a note that later
      measurements use .NET 11; historical numbers are not rewritten.
- [x] `.github/ISSUE_TEMPLATE/bug_report.yml` placeholder uses the new SDK version.
- [x] `docs/planning/06-toolchain-requirements.md` §4/§9 record SDK 11.0.100, the ApiCompat tool 11.0.100 (net10
      asset, `rollForward: Major`), ReportGenerator 5.6.0, remove `ZstdSharp` from the project-local list, and
      state the BSD-3-Clause native zstd licence and unchanged `NOTICE.md`.
- [x] `docs/planning/03-quality-strategy.md` §9.9 records the fixed encoder path, the quality mapping, the
      byte-identity-as-observation rule, the refined small-payload decode rule, and the lattice-bomb exception
      routed to the fix feature.
- [x] `docs/planning/04-roadmap.md` names no .NET 10 current-tense target (verified: no change expected).
- [x] This amendment page, the amendment index, the parent step page, `05-open-decisions.md`, the parent review
      record link, and `status.md` are updated.
- [ ] The Step 11 audit request names this amendment, the combined commit, the completed Part A, and the routed
      lattice fix. **Closeout task; not part of this feature.**

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| .NET 11 is five days old (GA 2026-11-10); servicing fixes and behaviour changes are unknown | Pin `11.0.100` with `latestPatch`; one closeout CI run; the retarget is a single revertible commit; rollback restores net10 |
| A tool or package is not net11-ready | Verified before acceptance: DocFX 2.81.0 `net11.0` asset, apicompat 11.0.100, ReportGenerator 5.6.0 `net11.0` asset, Avalonia 12.1.6 + MessagePack + Sqlite + xunit probe on net11. Remaining risk is the full build, covered by the migration gate |
| Windows/macOS break | Windows is already a required §3.13 platform gate (R2-7); the codec adds no native P/Invoke of our own. Dispatch the Windows platform job; macOS stays manual/optional as recorded |
| Image build cannot be executed in this environment (no OCI/podman builder) | **Accepted risk, owner: human owner.** Missing evidence: the amended `opencode:mocassin` image was never built, so the containerfile's install/assertion block is unproven. Closeout check: `docs/planning/closeout/step-11.json` `notes` records either a built image reference/digest or "image not built — accepted risk", the Step 11 audit record carries the risk, and `tools/check_closeout_evidence.py` still passes. The containerfile is not described as build-verified anywhere |
| `Microsoft.NET.Test.Sdk` 18.11.0 prints a `VSTEST_ARTIFACTS_DIRECTORY` warning on net11.0 | Bumped to 18.12.0 (owner-accepted); the suite and coverage must run, the warning is expected to disappear, and if it persists it is re-checked and recorded as benign rather than suppressed |
| A future runtime/Zstd patch changes compressed bytes | Accepted by design: bytes are an observation (ADR 0010, data contracts §3.5). Signatures and `package-summary-v1` are logical projections and stay equal; byte-capturing artifacts (golden frames, feature-7 extracted payloads) are re-captured with a recorded note; the pre-amendment compatibility fixture is byte-fixed; `contracts/**` is never regenerated for a codec change |
| ApiCompat baseline churn hides an API change | One-time cross-TFM comparison (`net10.0` baseline vs `net11.0` build) plus an unchanged `PublicAPI.*.txt`; any real delta is a finding, not a refresh |
| New analyzer/language diagnostics under SDK 11 break `warnings-as-errors` | The migration feature fixes or justifies each diagnostic; no blanket suppression; `all --require` is the gate |
| `dotnet list package --vulnerable` output format changes | Verify the grep phrase in `verify.sh` and `ci.yml` during the migration; update both together |
| Reference harness cannot be verified without the GPL reference clone | Retarget it; verify when the clone exists; if unavailable before the freeze, record it as a Step 29 item, not as verified |
| Rollback after integration | Part A is complete; the feature branch stays unmerged until the full gates and Part B-adjacent checks pass; post-integration rollback uses a fix branch before the audit freeze and is recorded |
| The level-3 decode ratio is inside host/JIT noise | The owner-approved small-payload rule gates absolutes (≤0.15 µs) and throughput (≥100 MB/s) below 8 KiB, and keeps the relative ≤10 % rule for ≥8 KiB; the measured worst absolute penalty is 0.06319 µs (controlled) / 0.05689 µs (default JIT) (§9.1) |
| Decoder relaxation accepts foreign frames | `TryGetMaxDecompressedLength` rejects non-Zstd input, the model/job guards bound the allocation, and the writer always declares the exact size. The lattice read path is bounded by the derived per-job bound and `MaxLatticePayloadBytes` (implemented by `fix-lattice-decode-guard`, `25a3787`); the bomb test covers a referenced row and an orphan row |

## Dependencies

- Parent step 11 is active: features 1–6 are squashed (`eb8513d`, `9b754af`, `c0d8d64`, `bdafaa4`, `4fe6225`,
  `cbe345f`), feature 7 is deliberately held, and the parent audit has **not** started. This is the required
  window: [`amendments/README.md`](README.md) §4 requires integration before the audit freeze.
- Feature 7 must not generate the checked-in `.msl` fixture or the canonical-summary golden before this
  amendment is integrated; otherwise the fixture is produced on a toolchain that is immediately replaced.
- No dependency on Steps 12/13/20 changes: the reader contract and the solver contract are unchanged.
- Owner decisions: **recorded 2026-11-15** (see Owner decisions). The `docs/dotnet-11-container` disposition, the
  image-build path, the ReportGenerator bump, the Test SDK bump, the ReferenceHarness retarget, and the refined
  small-payload decode rule are decided; no owner decision blocks implementation.
- **Part A is complete** (2026-11-15, §9.1) and was the pre-acceptance gate. It is evidence at this freeze, not a
  future dependency. Its reproducible part is the golden-frame test; Part B remains a pre-audit-freeze gate.
- **Step 11 fix feature `fix-lattice-decode-guard`** is required before the audit freeze: derive the lattice
  bound from a referencing `jobs` row (`lattice_a/b/c`) times the model payload's `position_count`, apply it in
  `PackageReader.ReadLatticesAsync`, bound an orphan lattice row (no referencing job) with an explicit
  `MaxLatticePayloadBytes` option instead of `int.MaxValue`, and add the lattice bomb test (A1-10, R2-01). The parent step page lists it; the amendment records the routing
  and does not claim the fix.
- The pre-amendment `.msl` fixture and golden frames must be captured on the feature branch **before** the
  retarget (feature order step 0); the net10 writer is the only source.

## Integration

- Branch: `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd`
- Parent step branch: `step/11-simulation-package-and-msl-writer`
- Approval: owner accepted `Ready with notes` and approved implementation on 2026-11-15; the feature branch is
  created from the approved step branch. Owner decisions 1–6 are recorded above.
- Approval commit: `683b7c4` (`docs: record step-11-amend-1 approval and round-2 resolutions [skip ci]`)
- Squash commit: `00753e7` (`step-11: retarget to .NET 11 and replace the payload codec`)
- Authorization: the human owner authorized the `.NET 11` stack/convention update in `AGENTS.md` §1/§8 as part of
  the approved amendment on 2026-11-15; it is a factual toolchain row, not a process-rule change (owner decision
  7).
- Parent audit request updated: after integration, refresh the Step 11 implementation status, the feature log,
  the closeout evidence path, and the audit request with the combined commit
- Combined frozen commit: —

## Audit

- Parent audit record: `docs/planning/audits/step-11.md`
- Audit verdict: not requested
- Merge commit: —
- Tag: `step-11` (on the combined merged commit)
- Post-merge fallback required: no

## Owner decisions (recorded 2026-11-15)

Six questions were answered by the human owner. They are decisions, not options; the design above is updated to
match, and no open question remains for this amendment.

| # | Question | Decision | Consequence in the design |
| --- | --- | --- | --- |
| 1 | `docs/dotnet-11-container` disposition | **Superseded.** The amendment's containerfile (11 only, .NET 10 removed) replaces it; the branch is deleted unmerged after integration. | §8; the `11.0.100` install line, `COPY global.json` pin check, and `--list-sdks` assertions are carried over verbatim; the branch is not merged |
| 2 | Image build verification | **Deferred to closeout as an accepted risk.** The containerfile is not build-verified here. | §7 and the Risks table; owner, missing evidence, and the closeout check are named; no page claims the image was built |
| 3 | ReportGenerator 5.6.0 | **Accepted.** `5.5.12` has no `net11.0` tool asset. | §7; the .NET 10 runtime dependence is removed from the image toolchain |
| 4 | Microsoft.NET.Test.Sdk | **Bumped to 18.12.0.** | §6 verification items and Risks; the suite and coverage must run, and the `VSTEST_ARTIFACTS_DIRECTORY` message is expected to disappear or be re-checked and recorded as benign |
| 5 | ReferenceHarness | **Retargeted now** to `net11.0`. | §6; the project is outside `Mocassin.slnx` and is verified when the GPL reference clone is available, otherwise the gap is a Step 29 item |
| 6 | Part A level-3 decode ratio on 2,109-byte lattices (2026-11-15, after the Part A measurement) | **Keep the switch and refine the small-payload gate.** For payloads under ~8 KB, gate on an absolute penalty (≤0.15 µs per payload) plus the ≥100 MB/s floor; record the relative ratio but do not gate it. Payloads ≥8 KB keep the ≤+10 % rule. | §9.1 thresholds and owner-decision rationale; Part A is complete and passes; the measured worst absolute penalty is 0.06319 µs (controlled) / 0.05689 µs (default JIT); Part B keeps its budgets and does not use the per-payload decode rule |
| 7 | Authorization for the `.NET 11` stack/convention update in `AGENTS.md` (2026-11-15) | **Authorized as part of the approved amendment.** The owner approved the `AGENTS.md` §1 stack table and §8 .NET convention row moving from .NET 10 to .NET 11 as a factual consequence of `step-11-amend-1`; no process rule changes. | §6.1 and the `AGENTS.md` §1/§8 edits; the change is recorded in the Step 11 feature log and does not engage `AGENTS.md` §3.15. |

The decision-6 exception rationale is recorded in full in §9.1: the two 2,109-byte lattices decode in 0.6–0.8 µs,
so a 10 % relative threshold is 60–84 ns and sits inside the host/JIT run-to-run spread; the absolute penalty is
sub-0.1 µs on a 2.5–3.8 GB/s path, and every job and model payload decodes faster on the native path.

Residual items that are **not** owner decisions and remain gates: Part B before the audit freeze, the image-build
evidence (or the named accepted risk) in the closeout evidence file, and the routed lattice decode-bound fix
before the audit freeze.

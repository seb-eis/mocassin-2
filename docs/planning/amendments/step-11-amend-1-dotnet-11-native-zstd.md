# Step 11 Amendment 1 — .NET 11 Toolchain and Native Zstandard

## Request

| Field | Value |
| --- | --- |
| Amendment ID | `step-11-amend-1` |
| Parent step | 11 — Simulation Package and `.msl` Writer |
| Status | Planned |
| Branch | `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd` |
| Design review | `docs/planning/reviews/step-11-amend-1.md` |
| Parent audit | `docs/planning/audits/step-11.md` |
| Integration commit | — |
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
- **Continuity with the current writer.** On every tested input and at both authored levels, the native
  one-shot output is byte-identical to `ZstdSharp.Port 0.8.8`. A package produced before the switch and a
  package produced after it have identical payload bytes for the same logical model, job, and lattice.
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
| `3` (default) | `new Compressor(3).Wrap(...)` | `Quality = 3` | 182,150 bytes from both; frames byte-identical |
| `9` (`--compact` / benchmark level) | `new Compressor(9).Wrap(...)` | `Quality = 9` | 178,312 bytes from both; frames byte-identical |
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

**Decode.** Both decoders (`Mocassin.Packages` and `Mocassin.Results`) use the same sequence:

1. `ZstandardDecoder.TryGetMaxDecompressedLength(frame, out long bound)`. `false` → `InvalidDataException`
   ("not a valid Zstd frame"), which the verifier maps to `PKG004`.
2. Reject when `bound > maxDecompressedBytes` or `bound > Array.MaxLength` **before** allocating. The current
   code casts a caller-supplied `long` guard to `int`; the new code never casts an unbounded value.
3. Allocate `(int)bound`, call `TryDecompress(frame, destination, out int written)`. `false` →
   `InvalidDataException`. Return `destination[..written]`.

Deliberate, documented behaviour change: a valid frame **without** a declared content size is now accepted when
its upper bound fits the guard. Today `Decompressor.GetDecompressedSize` throws for such a frame, so the reader
rejects it. The new behaviour is a relaxation for foreign frames, never unbounded, and the writer always
declares the exact size. `TryGetMaxDecompressedLength` returns `false` only for invalid input (verified with a
corrupted header and with four arbitrary bytes), so the mapping to `PKG004` is unchanged for malformed data.

**Allocation.** For our own frames the allocation equals the exact decompressed size. For a foreign frame it
equals the frame's upper bound, capped by the configured guard (model 512 MiB, job 8 MiB, lattice
`4 × a × b × c × position_count`). Peak encode memory stays `≈ 2.1 × payload + compressed`, the same shape as
today (source + `GetMaxCompressedLength` destination + returned slice).

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
  - No checked-in `.msl` exists yet (feature 7 is held), so there is nothing to regenerate and no binary churn.
  - `tests/contracts/signature-vectors.json`, `tests/contracts/projections/**`, and
    `contracts/generated/**` are unaffected; `./tools/verify.sh contracts` must stay green with **no** manifest
    or generated-artifact diff. A diff there is a contract change and a review finding, not an expected
    consequence.
  - Feature 7 generates the `.msl` fixture, the `package-summary-v1` golden, and the extracted payloads once,
    on the amended toolchain. The extracted payload `.bin` is a writer+runtime artifact and is never a
    signature input; the canonical summary and the signatures are the comparable artifacts.
- Determinism rule for the future: the same logical input and the same level produce the same compressed bytes
  on the same runtime patch. Across Zstd patch versions the *bytes* may legitimately differ; signatures and the
  canonical summary do not. This is why `package-summary-v1`, not file bytes, is the golden.

### 6. Toolchain migration surface

Verified file inventory. Counts are from the frozen design commit, not estimates.

| Area | File | Change |
| --- | --- | --- |
| SDK pin | `global.json` | `version` → `11.0.100`, `rollForward` → `latestPatch`, `allowPrerelease` stays `false` |
| Target framework | `Directory.Build.props` | `<TargetFramework>net11.0</TargetFramework>` (covers **21** solution projects) |
| Target framework | `tools/reference/Mocassin.ReferenceHarness/Mocassin.ReferenceHarness.csproj` | explicit `net11.0` (outside `Mocassin.slnx`) |
| ApiCompat gate | `tools/check-api-compat.sh` | baseline directory, reference-pack glob, current-assembly path → `net11.0` |
| ApiCompat gate | `tools/update-api-baseline.sh` | destination and `obj/Release/net11.0/ref` path |
| ApiCompat baseline | `tests/contracts/api-baseline/net10.0/` | rename to `net11.0/` and regenerate the **9** assemblies listed in `tools/api-projects.txt` |
| ApiCompat tool | `.config/dotnet-tools.json` | `microsoft.dotnet.apicompat.tool` `10.0.403` → `11.0.100` |
| Docs API | `tools/docfx/docfx.json` | 9 assembly paths and `TargetFramework` → `net11.0` |
| CI | `.github/workflows/ci.yml`, `benchmark.yml`, `docs.yml` | **no literal SDK version exists**; all three use `global-json-file: global.json`. Verify `actions/setup-dotnet@v6` installs 11.0.100 at the closeout run |
| Image | `docs/opencode/mocassin.containerfile` | install `11.0.100` only; keep the `COPY global.json` pin assertion; `ReportGenerator` `5.5.12` → `5.6.0`; DocFX unchanged |
| Packages | `Directory.Packages.props` | remove the `ZstdSharp.Port` version |
| Packages | `Mocassin.Packages.csproj`, `Mocassin.Results.csproj` | remove the `ZstdSharp.Port` reference |
| Codec | `PayloadEncoder.cs`, `PackagePayloadDecoder.cs` | native encode/decode (sections 2–4) |
| Test support | `Mocassin.Results.Tests/.../PackageTestSupport.cs` | native encoder; no new package reference |
| Docs | `README.md`, `CONTRIBUTING.md`, `AGENTS.md` §1/§8, `src/dotnet/README.md`, `docs/getting-started/index.md`, `docs/performance/index.md`, `docs/reference/file-layout.md`, `tests/contracts/README.md`, `.github/ISSUE_TEMPLATE/bug_report.yml`, `docs/planning/06-toolchain-requirements.md` | SDK version, baseline path, dependency list, benchmark-environment note |

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

One-time cross-TFM ApiCompat proof, run **before** the .NET 10 runtime is removed from the image: regenerate the
`net11.0` baseline, then run `apicompat` with the checked-in `net10.0` baseline as `--left` and the `net11.0`
build as `--right` (both reference packs still installed). Expect "no breaking changes" and an unchanged
`PublicAPI.*.txt`. This proves the retarget is TFM-only. Record the output in the feature log; then delete the
`net10.0` baseline directory.

Verification items that are gates, not assumptions:

- `dotnet build Mocassin.slnx -c Release` and `dotnet test` with zero warnings under 11.0.100; `dotnet format
  --verify-no-changes`.
- `dotnet list Mocassin.slnx package --vulnerable --include-transitive` still prints the phrase
  `has the following vulnerable packages` when a vulnerability exists; `tools/verify.sh` and `ci.yml` grep that
  exact phrase. Both places must be checked together if it changed.
- The Avalonia XAML compiler and the `PublicAPI` analyzers run cleanly under SDK 11.

### 7. Container final form

- One SDK: `11.0.100`, installed into `DOTNET_ROOT`; no `10.0.403`.
- Assertion block: `dotnet --list-sdks | grep -qE '^11\.0\.100 \['`,
  `test "$(cd /opt/mocassin-sdk-pin && dotnet --version)" = "11.0.100"`, `test "$(command -v dotnet)" = "$DOTNET_ROOT/dotnet"`.
  The `COPY global.json` step stays: it makes the image fail if the pin and the SDK ever drift.
- `docfx` stays `2.81.0` (the package ships `tools/net11.0/any`; with only the 11.0.0 runtime installed the tool
  host selects it).
- `dotnet-reportgenerator-globaltool` moves `5.5.12` → `5.6.0`: `5.5.12` ships at most a `net10.0` tool asset, so
  it needs the .NET 10 runtime that this amendment removes; `5.6.0` ships `tools/net11.0/any`. This is the one
  tool bump the runtime removal forces.
- The image build itself is **unverified in this environment** (no OCI builder). See the open question.

### 8. `docs/dotnet-11-container` disposition

The pushed branch `docs/dotnet-11-container` (`9ab4bb8`) installs 10.0.403 **and** 11.0.100 and asserts both.
That is exactly the interim state the amendment replaces.

Options:

1. **Supersede (recommended).** Do not merge `docs/dotnet-11-container`. The amendment feature branch carries the
   final single-SDK containerfile, including the `11.0.100` install line, the `COPY global.json` pin check, and
   the `--list-sdks` assertions taken from that commit. Delete the branch only with owner approval.
2. **Fold in.** Merge `docs/dotnet-11-container` to `main` first, then have the amendment remove the .NET 10
   install. Creates a main commit that is reverted days later and makes the amendment's containerfile diff
   harder to read.
3. **Land first as supporting tooling.** Justified only if someone needs SDK 11 on `main` before the amendment.
   Nothing does: no repository file requires 11 until the amendment lands.

Recommendation: option 1. The decision is the owner's (it is a merge decision), so it is recorded as an open
question rather than assumed.

### 9. Benchmark gate

The gate has two parts. Part A must pass **before the amendment is accepted for implementation**. Part B must
pass **before the Step 11 audit freeze**, when feature 7's benchmark exists.

**Part A — codec comparison on real payloads (before acceptance).**

Method:

1. On the unamended `net10.0` tree, build a real package from `samples/jobs/ceria-temperature-sweep.mocprj`
   with the existing writer and extract the model, lattice, and job payload blobs (the three largest distinct
   payloads). Store them under `.tmp/` — they are evidence, never committed.
2. In a `net11.0` probe project, reference both `ZstdSharp.Port 0.8.8` and the in-box API, so the comparison
   isolates the codec from the runtime (ZstdSharp targets `netstandard2.0`/`net8.0` and loads on `net11.0`;
   verified).
3. For each payload and each level in `{3, 9}`: encode with both codecs, then decode with both; record
   compressed size, encode time (best of 10 after one warm-up), decode time, and byte equality.
4. Also round-trip the native frames through `zstd` CLI 1.5.7 and a CLI frame through the native decoder.

Thresholds (all must hold):

| Metric | Threshold |
| --- | --- |
| Round-trip correctness | 100 %; compressed bytes byte-identical to the current codec on every tested payload |
| Compressed size | ≤ +3 % versus `ZstdSharp` at the same level |
| Encode time | ≤ +25 % at the same level |
| Decode time | ≤ +10 % (native measured faster in the probe) and ≥ 100 MB/s |
| Cross-decoder | `zstd` CLI and native decoder agree byte-for-byte |

**Part B — 10⁴-job budget (before the audit freeze).**

Feature 7's benchmark runs on the amended tree and records: wall clock, peak RSS, package size, model-payload
size, and job insertion rate for 10⁴ small jobs (10×10×10 ceria, ~1,000 positions). Compare package size and
wall clock against the same harness run on the unamended tree (or, if that is unavailable, against the
Step 11 §7 targets).

| Budget | Target |
| --- | --- |
| 10⁴ jobs wall clock | < 10 min, and ≤ +5 % versus the net10 reference |
| Peak RSS | < 2 GB |
| Job insertion rate | ≥ 100 jobs/s |
| Package size | ≤ +3 % versus the net10 reference |
| Complex model payload | < 50 MB |

**Rollback threshold.** If Part A or Part B breaches a threshold with no measurement error, the amendment is
**not** integrated: keep `net10.0` + `ZstdSharp.Port`, abandon the feature branch, and record the decision and
the numbers in `05-open-decisions.md` as a deferred item. If the breach appears only after integration, a fix
branch restores the previous state before the Step 11 audit freeze; the parent step must not be audited with a
known-regressed codec. A correctness, cross-language, or signature failure is an automatic rollback regardless
of the performance numbers.

Indicative pre-gate measurements (probe inputs, not a substitute for Part A): 418,611-byte payload — 182,150
bytes at level 3 and 178,312 at level 9 from both codecs, byte-identical; decode 0.18 ms native vs 0.43 ms
ZstdSharp; 2/16/48 MiB payloads — byte-identical at both levels, native encode 1.7–12× faster. This indicates a
very comfortable margin; the gate exists to prove it on real package payloads.

## API and contract impact

- No public API addition, removal, or signature change. `PayloadEncoder` and `PackagePayloadDecoder` are
  `internal`. `PackageBuildOptions.CompressionLevel` keeps its name, type, default, and 1..19 range; only its XML
  documentation gains a sentence.
- No project/package/results/state format change. No `format_version` bump. `contracts/manifest.json` and
  `contracts/generated/**` are untouched. ADR 0009's version-bump rule is not engaged.
- ApiCompat: the baseline **directory and assemblies** move to `net11.0`. The public surface must be identical;
  the one-time cross-TFM check proves it. `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` must not change as a
  consequence of the retarget. If an analyzer under SDK 11 reports a genuine public-surface issue, it is a
  finding to resolve, not a baseline refresh.
- Dependency set: `ZstdSharp.Port` disappears from `Directory.Packages.props` and from the two project files.
  `check_nuget_licenses.py` output shrinks accordingly; the `policy` gate must stay green.
- Licensing: no GPL-derived content. `System.IO.Compression.Zstandard*` is part of the MIT-licensed .NET
  runtime; `ZstdSharp.Port` (MIT) is removed. `NOTICE.md` needs no change.

## Temporary implementation and migration

What exists today: `PayloadEncoder` and `PackagePayloadDecoder` wrap `ZstdSharp.Port 0.8.8`
(`Compressor.Wrap` / `Decompressor.Unwrap` / `Decompressor.GetDecompressedSize`), and
`PackageTestSupport.Encode` uses the same library to build mutated payloads for the `PKG` tests.

What is temporary: the third-party codec and the `net10.0` toolchain pin.

How the amendment replaces it: sections 2–4 replace the codec at both ends and in test support; section 6
replaces the toolchain. No compatibility shim is required. There is no dual-encoding path, no format flag, and
no "try native then fall back" logic: one encoder, one decoder, standard frames.

Order of work inside the feature (so a rollback is a branch drop, not a revert tangle):

1. Retarget + `global.json` + tool/baseline/docs churn; full `./tools/verify.sh all --require` with the codec
   still on ZstdSharp (ZstdSharp is TFM-compatible with net11.0, so this step is independent and revertible).
2. Run the one-time cross-TFM ApiCompat proof.
3. Replace the codec, remove the references, and run the gates again.
4. Run Part A and record the numbers; feature 7 runs Part B.

Step 1 is the mitigation for the largest risk: the retarget can fail on its own without the codec change, and
vice versa.

## Acceptance criteria

- [ ] `dotnet build Mocassin.slnx -c Release` and `dotnet test Mocassin.slnx -c Release` pass on SDK 11.0.100
      with zero warnings, and the test counts match the pre-amendment run.
- [ ] `global.json` pins `11.0.100` with `latestPatch`; `dotnet --version` resolves to `11.0.100` locally, in CI,
      and in the image assertion path.
- [ ] No `ZstdSharp` reference remains in `Directory.Packages.props`, any `.csproj`, or any source file, and
      `check_nuget_licenses.py` reports the reduced package set with the policy gate green.
- [ ] Model, job, and lattice payloads encode and decode only through the in-box API, and the existing
      round-trip, determinism, guard, and corruption tests pass unchanged apart from the test-support encoder.
- [ ] The decode guard still produces `InvalidDataException` in the writer and `PKG004` in the verifier for a
      truncated frame, a corrupt frame, and a frame whose bound exceeds the guard, with no unbounded allocation.
- [ ] Model, parameterization, and lattice signatures and the `package-summary-v1` projection are identical
      before and after for the same logical input; `contracts/**` has no diff and `verify.sh contracts` is green.
- [ ] A package written on the unamended tree verifies on the amended tree, and a package written on the amended
      tree verifies on the unamended tree (cross-version read).
- [ ] `zstd` CLI 1.5.7 decodes a native payload byte-identically, and a CLI-produced frame decodes through the
      package reader.
- [ ] Part A of the benchmark gate passes with the recorded evidence, or the rollback decision is recorded.
- [ ] The ApiCompat baseline is regenerated at `tests/contracts/api-baseline/net11.0/`; the one-time cross-TFM
      comparison reports no breaking changes; `PublicAPI.*.txt` is unchanged; `verify.sh api` is green.
- [ ] `./tools/verify.sh all --require` passes on the amendment branch.
- [ ] The containerfile installs 11.0.100 only and asserts it; ReportGenerator 5.6.0 and DocFX 2.81.0 resolve;
      the image build is verified by the owner or recorded as unavailable.
- [ ] Toolchain documentation is updated and `./tools/verify.sh docs` is green.
- [ ] Parent step page, amendment index, status dashboard, and open decisions reflect the amendment state.

## Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Payload round-trip (model/job/lattice) | PR-fast | Native encode → native decode returns the identical logical payload; existing expectations unchanged |
| Encoder determinism | PR-fast | Two encodes at the same level are byte-identical; different levels change bytes but not the decoded payload |
| Quality mapping | PR-fast | `CompressionLevel` 3 and 9 produce frames byte-identical to the pre-amendment codec on a fixed payload vector; level 0/-1/20 throws `ArgumentOutOfRangeException` |
| Decode-bomb guard | PR-fast | A frame whose bound exceeds the guard fails with `InvalidDataException` (writer) and `PKG004` (verifier) before allocation |
| Foreign-frame acceptance | PR-fast | A content-size-less standard frame within the guard decodes; one whose bound exceeds the guard is rejected |
| Corrupt/truncated frame | PR-fast | Garbage, a corrupted header, and a truncated frame fail cleanly; no unhandled exception, no `PKG`-less crash |
| Cross-version read | PR-fast | A package built by the pre-amendment writer verifies and inspects identically on the amended tree |
| Cross-implementation decode | PR-medium | `zstd` CLI 1.5.7 and the native decoder both reproduce the same logical payload from the same frame; a CLI frame decodes through the reader |
| Signature and summary invariance | PR-fast | Signatures and `package-summary-v1` are unchanged for the same logical input; `contracts/**` has no diff |
| Toolchain identity | PR-fast | `PublicAPI.*.txt` is unchanged; the cross-TFM ApiCompat run reports no breaking changes |
| Benchmark Part A | PR-medium | Real model/lattice/job payloads meet the size, encode, and decode thresholds at levels 3 and 9 |
| Benchmark Part B | Nightly/release | 10⁴ jobs within the time, RSS, size, and insertion-rate budgets |

Every test declares its tier. Payload tests use the real mappers, the real writer, and real SQLite files; the
codec comparison uses the real package payloads. No domain doubles.

## Documentation

- [ ] `docs/reference/msl-format.md` (feature 7) states the payload codec as the in-box Zstd encoder with the
      fixed one-shot path and the quality mapping, and that reads accept any valid standard frame.
- [ ] `docs/reference/file-layout.md` points at `tests/contracts/api-baseline/net11.0/`.
- [ ] `docs/getting-started/index.md`, `README.md`, `CONTRIBUTING.md`, `src/dotnet/README.md`, and `AGENTS.md`
      §1/§8 state .NET 11.
- [ ] `docs/performance/index.md` keeps its historical .NET 10 measurements and notes the toolchain change;
      historical numbers are not rewritten.
- [ ] `.github/ISSUE_TEMPLATE/bug_report.yml` placeholder uses the new SDK version.
- [ ] `docs/planning/06-toolchain-requirements.md` §4/§9 record SDK 11.0.100, the ApiCompat tool 11.0.100,
      ReportGenerator 5.6.0, and remove `ZstdSharp` from the project-local list.
- [ ] `docs/planning/03-quality-strategy.md` §9.9 records the fixed encoder path, the quality mapping, the
      byte-identity evidence rule, and the benchmark gate.
- [ ] This amendment page, the amendment index, the parent step page, `05-open-decisions.md`, the parent review
      record link, and `status.md` are updated.
- [ ] The Step 11 audit request names this amendment and the combined commit.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| .NET 11 is five days old (GA 2026-11-10); servicing fixes and behaviour changes are unknown | Pin `11.0.100` with `latestPatch`; one closeout CI run; the retarget is a single revertible commit; rollback restores net10 |
| A tool or package is not net11-ready | Verified before acceptance: DocFX 2.81.0 `net11.0` asset, apicompat 11.0.100, ReportGenerator 5.6.0 `net11.0` asset, Avalonia 12.1.6 + MessagePack + Sqlite + xunit probe on net11. Remaining risk is the full build, covered by the migration gate |
| Windows/macOS break | Windows is already a required §3.13 platform gate (R2-7); the codec adds no native P/Invoke of our own. Dispatch the Windows platform job; macOS stays manual/optional as recorded |
| Image build cannot be verified here (no OCI builder) | The containerfile change is small and assertion-checked; the owner builds it, or the audit records the risk. Do not claim the image is verified until it is |
| Cross-SDK-patch determinism | Signatures and summaries exclude compressed bytes; fixtures compare canonical summaries; CI installs the exact pin |
| ApiCompat baseline churn hides an API change | One-time cross-TFM comparison (`net10.0` baseline vs `net11.0` build) plus an unchanged `PublicAPI.*.txt`; any real delta is a finding, not a refresh |
| New analyzer/language diagnostics under SDK 11 break `warnings-as-errors` | The migration feature fixes or justifies each diagnostic; no blanket suppression; `all --require` is the gate |
| `dotnet list package --vulnerable` output format changes | Verify the grep phrase in `verify.sh` and `ci.yml` during the migration; update both together |
| Reference harness cannot be verified without the GPL reference clone | Retarget it; verify when the clone exists; if unavailable before the freeze, record it as a Step 29 item, not as verified |
| Rollback after integration | The feature branch stays unmerged until Part A and the gates pass; post-integration rollback uses a fix branch before the audit freeze and is recorded |
| Decoder relaxation accepts foreign frames | The guard still bounds every allocation, `TryGetMaxDecompressedLength` rejects non-Zstd input, and the writer always declares the exact size |

## Dependencies

- Parent step 11 is active: features 1–6 are squashed (`eb8513d`, `9b754af`, `c0d8d64`, `bdafaa4`, `4fe6225`,
  `cbe345f`), feature 7 is deliberately held, and the parent audit has **not** started. This is the required
  window: [`amendments/README.md`](README.md) §4 requires integration before the audit freeze.
- Feature 7 must not generate the checked-in `.msl` fixture or the canonical-summary golden before this
  amendment is integrated; otherwise the fixture is produced on a toolchain that is immediately replaced.
- No dependency on Steps 12/13/20 changes: the reader contract and the solver contract are unchanged.
- Owner decisions required before implementation: the `docs/dotnet-11-container` disposition, the image-build
  verification path, and the ReportGenerator bump (see open questions).
- Part A of the benchmark gate must pass before the design review can accept the amendment for implementation.

## Integration

- Branch: `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd`
- Parent step branch: `step/11-simulation-package-and-msl-writer`
- Approval: pending design review and owner start approval
- Squash commit: —
- Parent audit request updated: after integration, refresh the Step 11 implementation status, the feature log,
  the closeout evidence path, and the audit request with the combined commit
- Combined frozen commit: —

## Audit

- Parent audit record: `docs/planning/audits/step-11.md`
- Audit verdict: not requested
- Merge commit: —
- Tag: `step-11` (on the combined merged commit)
- Post-merge fallback required: no

## Open questions for the owner

1. **`docs/dotnet-11-container`.** Supersede the branch and carry its single-SDK content into the amendment
   feature branch (recommended), merge it to `main` first and remove the .NET 10 install in the amendment, or
   leave it unmerged but undeleted? The recommendation avoids a main commit that is reverted within days.
2. **Image build verification.** There is no OCI builder in this environment, so the final containerfile is
   unverified. Options: the owner builds `opencode:mocassin` before the design review accepts the amendment; or
   the build moves to the closeout and, if unavailable, is recorded as an accepted risk in the audit. The
   second option keeps the design gate moving but leaves the image claim unproven until closeout.
3. **ReportGenerator 5.6.0.** Removing the .NET 10 runtime forces `dotnet-reportgenerator-globaltool` from
   `5.5.12` to `5.6.0` (the first release with a `net11.0` tool asset). Accept this as a required tooling bump,
   or keep `5.5.12` and the .NET 10 runtime in the image (which contradicts "remove .NET 10")? The amendment
   assumes the bump.
4. **Microsoft.NET.Test.Sdk.** `18.11.0` passes on net11.0 in the probe but prints a benign
   `VSTEST_ARTIFACTS_DIRECTORY` warning. Keep it (no unrelated upgrade) and revisit only if the full suite
   misbehaves, or bump to `18.12.0` as a compatibility fix? The amendment keeps `18.11.0`.
5. **Reference harness.** `tools/reference/Mocassin.ReferenceHarness` targets `net10.0` explicitly and is not in
   `Mocassin.slnx`; it needs the GPL reference clone to build. Retarget it to `net11.0` and record "verified when
   the clone is available", or leave it out of scope until Step 29? The amendment retargets it for consistency
   and records the verification gap.

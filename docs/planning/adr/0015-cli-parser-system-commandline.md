# ADR 0015 — `System.CommandLine` for the .NET CLI

- **Status:** Accepted
- **Date:** 2026-11-17
- **Deciders:** Project owner

## Context

Step 12 needs one command surface for `new`, `validate`, `compile`, `build`, `inspect`, `verify`, `doctor`,
`clean --dry-run`, help, and version, plus reserved later-step commands. The round-2 design chose a
hand-written, table-driven parser. Its main reasons were avoiding a new package and an assumed AOT/trimming
risk for a CLI that Step 31 may publish as a single file.

The owner re-verified the AOT assumption. `System.CommandLine` 3.0.0 publishes under Native AOT on
`net11.0`/`linux-x64` with `TrimMode=full` and zero IL2xxx/IL3xxx warnings; a positive control emitted
IL2026/IL3050, so the analyzers and ILC warning channel were active. The native binary parsed typed and
required options, printed help/version, reported parse errors, and answered the `[suggest]` directive like the
JIT build. The `net10.0` asset declares `IsTrimmable` and `IsAotCompatible`, and the `net10.0` dependency group
is empty. The hand-written parser therefore duplicated a standard library and carried its own grammar, help,
and completion maintenance.

## Decision

1. Use `System.CommandLine` 3.0.0, pinned in central package management and referenced only by `Mocassin.Cli`.
2. The CLI owns the command graph, typed `ParseResult` binding, `CLI001` wording, and exit-code policy. The
   library supplies command-line tokenization (after response-file expansion), arity, help, and the
   `[suggest]` directive; the CLI supplies the root `--version` action.
3. Remap the library's parse-error exit code (1) to `CLI001`/exit 2. Help and root `--version` remain 0;
   `--version` is not inherited by subcommands, so `mocassin <command> --version` is `CLI001`/2.
4. Support response files (`@file`) as part of the Step 12 CLI contract. The CLI owns response-file
   tokenization and recursive expansion; the library's response-file hook is disabled. Limit 1 MiB of raw
   bytes per file, 4 MiB of raw response-file bytes across the command line, nesting depth 8, 10,000 tokens
   contributed by response files, and 32 files. Missing, unreadable, invalid, or oversized files are
   `CLI001`/exit 2, not `CLI002`/3. The user-facing contract is in the step page §5.3 and `reference/cli.md`.
5. Ship no `completion` command or shell scripts in Step 12. The library provides the `[suggest]` protocol for
   `dotnet-suggest`, not script generation; shell integration is deferred to Step 31 packaging.

## Consequences

**Positive**

- One maintained command graph; command-line tokenization and help are not reimplemented, and the CLI supplies
  the root `--version` action instead of inheriting the library's version-only default.
- MIT-licensed with no transitive dependency for `net10.0+`; the package is centrally pinned.
- A required `verify.sh aot` gate publishes a zero-warning smoke host and a separate IL2026/IL3050 positive
  control (the control project opts out of warnings-as-errors; the host stays strict), and runs the host's
  `CLI001`/2 parse-error case. clang/lld/ILCompiler are Tier 1, so **every step closeout from Step 12** needs
  them because `aot` is in `verify.sh all`; their 19.1.7/11.0.0 versions are observed apt/NuGet resolutions
  checked by `06` §9, not a containerfile pin, and the gate fails on absence rather than a version mismatch. The
  gate is local (the closeout CI does not run `verify.sh`) and is fail-not-skip when the toolchain is absent.
- Response files are a documented, bounded contract rather than an accidental library default.

**Negative and limits**

- `requireLicenseAcceptance=true` is package metadata; the NuGet licence gate must keep the MIT entry.
- Only `linux-x64` AOT was exercised. Windows/macOS AOT and the `netstandard2.0` asset are untested. The real
  `Mocassin.Cli` publish is recorded as evidence, not a gate, because `Microsoft.Data.Sqlite` and `MessagePack`
  are unverified; Step 31 owns the platform matrix and the AOT/single-file decision.
- Library behaviour is not the contract: parse-error text, `[suggest]` scoping, response-file syntax, and
  tokenization are pinned by CLI tests. A package upgrade is a recorded dependency change.
- Step 12 ships no shell completion.

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
   library supplies tokenization, arity, help/version, and the `[suggest]` directive.
3. Remap the library's parse-error exit code (1) to `CLI001`/exit 2; help and version remain 0.
4. Support response files (`@file`) as part of the Step 12 CLI contract. The library's default reader is
   replaced by a bounded reader with documented syntax and CLI-owned limits: 1 MiB per file, 4 MiB total,
   nesting depth 8, 10,000 expanded tokens, and 32 files. Missing, unreadable, invalid, or oversized files are
   `CLI001`/exit 2, not `CLI002`/3. The user-facing contract is in the step page §5.3 and `reference/cli.md`.
5. Ship no `completion` command or shell scripts in Step 12. The library provides the `[suggest]` protocol for
   `dotnet-suggest`, not script generation; shell integration is deferred to Step 31 packaging.

## Consequences

**Positive**

- One maintained command graph; tokenization and help/version are not reimplemented.
- MIT-licensed with no transitive dependency for `net10.0+`; the package is centrally pinned.
- A minimal AOT smoke host with a positive control guards the dependency claim.
- Response files are a documented, bounded contract rather than an accidental library default.

**Negative and limits**

- `requireLicenseAcceptance=true` is package metadata; the NuGet licence gate must keep the MIT entry.
- Only `linux-x64` AOT was exercised. Windows/macOS AOT and the `netstandard2.0` asset are untested. The real
  `Mocassin.Cli` publish is recorded as evidence, not a gate, because `Microsoft.Data.Sqlite` and `MessagePack`
  are unverified; Step 31 owns the platform matrix and the AOT/single-file decision.
- Library behaviour is not the contract: parse-error text, `[suggest]` scoping, response-file syntax, and
  tokenization are pinned by CLI tests. A package upgrade is a recorded dependency change.
- Step 12 ships no shell completion.

# 05 — Open Decisions and Deferred Scope

This page tracks architecture decisions that are not yet final, plus items that are intentionally deferred.
It complements the ADRs: an ADR records a final decision; this page records what is still open and where the
decision will be made.

## 1. Status vocabulary

| Status | Meaning |
| --- | --- |
| **Resolved** | Decision recorded in an ADR or contract; implementation can proceed. |
| **Gated** | Decision is intentionally deferred to a named step with a spike or acceptance gate. |
| **Deferred** | Out of the first release; target release noted. |
| **Open** | A decision is needed before the named step can be completed. |

## 2. Resolved by ADR or contract

| Topic | Decision | Record |
| --- | --- | --- |
| Canonical signatures | Canonical MessagePack projection, fixed widths/order, domain-separated SHA-256 | [ADR 0010](adr/0010-canonical-messagepack-signatures.md) |
| `.msl` mutability | Immutable package; results in `.msr` sidecar; no WAL dependence for `.msl` | [ADR 0011](adr/0011-immutable-package-and-results-store.md) |
| Transport/MSD | Origin-based cumulative MSD; per-species and total transport; tracked-ensemble normalization | [ADR 0012](adr/0012-transport-and-msd-definitions.md) |
| Format lifetime | `.msl`/`.msr` current + previous major; `.mcs` current-only; SemVer for public APIs | [ADR 0013](adr/0013-format-compatibility-and-api-versioning.md) |
| Extension boundary | C ABI, panic containment, opt-in in-process loading, versioned host table | [ADR 0014](adr/0014-extension-abi-and-trust-boundary.md) |
| Contract manifest | `contracts/manifest.json` plus generated checks/fixtures | [ADR 0009](adr/0009-contract-manifest-and-contracts-library.md) |
| Project persistence | `ProjectStore` in `Mocassin.Packages`, not `Core` | [Step 01](steps/step-01-domain-kernel-and-project-format.md) |
| Primitive geometry | `Fractional3`/`Cartesian3`/`Vector4I` in `Core.Geometry`; generic kernel internally | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Space-group catalog | spglib as the sole source; 274-setting compatibility subset; committed canonical JSON, dev-only generator, CI drift check, checksum, and BSD-3-Clause attribution | [Step 02](steps/step-02-symmetry-and-geometry.md) |
| Space-group reference | Project stores structured `{number, axis, origin}`; HM/Hall/system/operations are catalog-derived | [Step 02](steps/step-02-symmetry-and-geometry.md) / [Step 03](steps/step-03-particle-and-structure-models.md) |
| Cross-process job contract | Job folder, `solver.json`, JSONL events, `mmcfe.db` | [`02-data-contracts.md`](02-data-contracts.md) §5 |
| Large state spill | Embed `.mcs` up to 1 GiB; larger states are external files referenced by `.msr` | ADR 0011 / data contracts §3.6 |
| MMCFE log | SQLite `mmcfe.db` with a versioned, resumable schema | [`02-data-contracts.md`](02-data-contracts.md) §5.4 |
| Security model | Untrusted parsers, extension trust boundary, path/zip/decompression guards | [`01-target-architecture.md`](01-target-architecture.md) §7.5, ADR 0014 |
| Settings schema | Versioned `mocassin.settings` with migrations and retention defaults | [Step 23](steps/step-23-avalonia-shell-and-project-lifecycle.md) |
| Accessibility | WCAG 2.2 AA target plus manual screen-reader checks | [`03-quality-strategy.md`](03-quality-strategy.md) |
| Test strategy | Testing trophy; mocks only at true boundaries | [`03-quality-strategy.md`](03-quality-strategy.md) |

## 3. Gated decisions

| Topic | Gate | What must be decided | Fallback |
| --- | --- | --- | --- |
| 3D backend | Step 27 spike | Silk.NET + Avalonia native host vs. an OpenGL control or other backend; macOS/Metal path | ADR 0006 fallback: keep the abstraction and ship the working backend; defer advanced effects |
| AOT/single-file CLI | Step 31 | Whether SQLite/MessagePack dependencies permit `PublishAot`/single-file for CLI and tools | Normal self-contained publish; GUI is not AOT |
| Update mechanism | Step 31 | Whether to ship any updater at all | Offline-first: no auto-update; users install new releases via the package/installer |
| Remote deploy | Step 26/28 | Whether SSH/SFTP deploy ships in the first release | Local build + HPC scripts only; mark deferred in the parity matrix |
| `.msr` format | Step 20 | Journal mode, locking, and state spill are decided; embedded-size cap and retention defaults may be tuned after benchmarks | `journal_mode=DELETE`, one collector, embed up to 1 GiB then external state files, no auto-delete |

## 4. Deferred scope

These are intentionally out of the first parity release. They must remain marked in
[`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md).

| Item | Target | Rationale |
| --- | --- | --- |
| Append to existing package | post-1.0 | Rebuild is safer; `.msl` is immutable |
| GPU acceleration | post-1.0 | Not in the original; CPU baseline first |
| Parquet export | post-1.0 | CSV/JSON first; optional dependency |
| External result storage in `.msr` | post-1.0 | Keep v1 self-contained; add for very large HPC campaigns |
| Advanced 3D effects (SSAO/shadows) | post-1.0 | Quality subset first |
| Localization | post-1.0 | English first; resources structured for later |
| Time-lagged MSD | post-1.0 | Origin-based MSD matches the original; separate API later |
| Remote deploy | post-1.0 | Local + HPC scripts first |
| Unified docs search across narrative + APIs | post-1.0 | Each generated API keeps its own search |
| PDF/offline documentation | post-1.0 | Current-version site first |

## 5. Remaining owner input

The following are the only topics still expected to need an explicit owner decision:

1. **3D backend** after the Step 27 spike (choose Silk.NET host, OpenGL control, or another backend).
2. **Remote deploy** scope for the first release (ship or defer).
3. **Update mechanism** policy (no updater vs. signed installer/package channel).
4. **AOT/single-file CLI** if dependency constraints make it feasible.

Everything else is either resolved above or specified in the data contracts, architecture, or step pages.
When a decision is made, add an ADR (if expensive to reverse) and update this page.

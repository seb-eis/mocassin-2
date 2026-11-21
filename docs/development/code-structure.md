# Code Structure

This page defines where source files live and how they are named. The goal is that a new agent can open a
project and understand its concepts without a manual cleanup later.

The short rule: **the project root is a lobby, not a home.** Code is grouped by domain concept, and the folder,
namespace, and file name tell the same story.

## 1. Hard rules

| Rule | Enforcement |
| --- | --- |
| Source project roots contain only entry points and project metadata | Structure test |
| Every other source file lives in a domain folder | Structure test |
| No catch-all folder names | Structure test |
| At most **20 direct `.cs` files per folder** | Structure test |
| At most **4 folder levels** below the project root | Structure test |
| File name matches its primary type, or a permitted concept file | Structure test |
| Test project root has at most **20 `.cs` files** | Structure test |
| At most **800 lines per file** | Structure test |
| No `#region` as a filing system | Structure test |
| New and existing files use the namespace that matches their folder | `IDE0130` error + structure test |
| File-scoped namespaces | `IDE0161` error + `dotnet format` |
| No unused using directives | `IDE0005` + `dotnet format` |

Subfolders reset the file count: a folder may contain 20 files, or split them into subfolders with up to 20
each. Depth is counted in folder segments below the project root:

```text
Geometry/                              depth 1
Geometry/Matrices/                     depth 2
Geometry/Matrices/Internal/            depth 3
Geometry/Matrices/Internal/Details/    depth 4
Geometry/Matrices/Internal/Details/X/  depth 5  (rejected)
```

## 2. Allowed project-root files

Source projects may keep only:

- `AssemblyInfo.cs`
- `GlobalUsings.cs` / `Usings.cs`
- `<Project>Module.cs` — the composition root
- `Program.cs` for executables
- framework-mandated app files such as `App.axaml.cs` and `ViewLocator.cs`
- `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `README.md`

Everything else belongs in a folder.

The current `Mocassin.Symmetry` layout is the reference shape:

```text
Mocassin.Symmetry/
  AssemblyInfo.cs
  SymmetryModule.cs
  Caching/
  Contracts/
  Data/
  Diagnostics/
  Geometry/
  Reduction/
  SpaceGroups/
```

### 2.1 `Mocassin.Cli` layout (Step 12)

The CLI is a command-domain project, not a `Commands/` bucket. Each command area owns its command, report
types, and human/JSON writer:

```text
Mocassin.Cli/
  Program.cs
  AssemblyInfo.cs
  CommandLine/            # System.CommandLine graph, typed binding, parse-error mapping, help/version, exit codes
  CommandLine/Reports/    # shared diagnostic/envelope DTOs and writers
  Diagnostics/            # CLI diagnostic construction
  NewProject/
  Compile/
  Build/
  Validation/
  Inspect/
  Verify/
  Doctor/                 # checks split into Doctor/Checks/ when needed
  Clean/
```

- Folders are PascalCase command/domain concepts. `Commands/`, `Handlers/`, and `Output/` are not allowed;
  `CommandLine/Reports` names the shared report concept, and each command folder owns its own output.
- `CommandLine` owns the `System.CommandLine` command graph, typed `ParseResult` binding, parse-error mapping,
  the library help action, the CLI root `--version` action, and exit-code policy. There is no hand-written
  parser, `CommandCatalog`, `CommandDefinition`, `OptionDefinition`, `ParsedCommand`, `HelpWriter`, or
  `CompletionWriter`.
- Namespaces match folders (`Mocassin.Cli.CommandLine`, `Mocassin.Cli.Build`, `Mocassin.Cli.Doctor.Checks`).
- The 20-file and 800-line limits apply per folder and file. `Doctor/Checks/` resets the count; a large
  `BuildCommand` is split into a command, report writer/factory, exit mapper, event writer, progress renderer,
  and the CLI-owned `BuildEtaEstimator` (the library `ProgressEstimator` only throttles). `Doctor` owns the
  internal `IDoctorCheck`/`DoctorCheckResult` seam so tests inject outcomes.
- `Mocassin.Packages/Build/` follows the same rules: concept files at depth 1 and an `Internal/` subfolder
  for the scheduler, diagnostic-merger, and shared `BuildPreviewBuilder` seams. `BuildPreview.cs` is a
  multi-record concept file following the `PackageInspection.cs` precedent.
- `Mocassin.Packages/Projects/Templates/` owns `ProjectTemplate`, `ProjectTemplateFactory`, and the embedded
  `Assets/blank.mocprj`/`Assets/ceria.mocprj` resources. The `.mocprj` assets are not counted as `.cs` files;
  the public factory and template records are XML-documented and listed in `PublicAPI.Unshipped.txt`.

### 2.2 Rust workspace layout (Step 13)

The Rust workspace is rooted at `src/rust/Cargo.toml`; crates live under `crates/`. The same lobby rule applies:

```text
crates/mocassin-msl/
  Cargo.toml
  README.md
  src/lib.rs               # crate entry: module declarations, re-exports, crate docs
  src/package.rs           # SQLite open/schema/job queries
  src/scan.rs              # bounded MessagePack scan
  src/decode.rs            # Zstd frame handling + rmp-serde decode
  src/validate.rs          # typed index/length/finite checks
  src/projection.rs        # canonical projection writer + signatures
  src/error.rs
  tests/reader_acceptance.rs
  benches/load.rs
  fuzz/                    # cargo-fuzz package, excluded from the workspace
```

Rules:

- Crate roots contain only `Cargo.toml`, `README.md`, `src/lib.rs` (library) or `src/main.rs` (binary), and the
  `tests/`, `benches/`, and `fuzz/` directories. Everything else lives in a concept module.
- Module names are snake_case and name the domain concept (`package`, `scan`, `decode`, `validate`,
  `projection`, `seed`, `stream`, `state`, `dry_run`). `utils`, `helpers`, `common`, `models`, `traits`, and
  `misc` are forbidden.
- A module directory (`src/scan/mod.rs` plus submodules) is allowed inside a named concept; `internal` is a
  visibility choice, not a folder.
- Keep files below 800 lines and concept modules below 20 direct files; split by sub-concept, not by type kind.
- Unit tests live beside the private helper; public acceptance/property tests live in `tests/`; shared fixture
  helpers live in `tests/support/`. There is no root-file limit for Rust tests.
- Criterion benchmarks live in `benches/` and are named after the measured path (`draws.rs`, `load.rs`).
- The fuzz crate lives under the crate it exercises and is excluded from the workspace; it is not part of the
  normal `cargo test` build.
- Public items carry doc comments; `missing_docs` is denied by the workspace lint. Every new source file starts
  with `SPDX-License-Identifier: MIT`.
- Enforcement in Step 13 is `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`,
  `cargo test --workspace`, and review. A mechanical Rust layout test is deferred to Step 29; do not add a
  second structure tool now.

Step 14 adds the first real `mocassin-solver` modules. The context is one domain concept with sub-concepts; the
crate root stays an entry point:

```text
crates/mocassin-solver/
  Cargo.toml
  README.md
  src/lib.rs                     # crate entry: module declarations, re-exports, crate docs
  src/context/mod.rs             # SimulationContext, InitOutcome, the phase pipeline
  src/context/dimensions.rs      # [A,B,C,P], checked index/wrap helpers
  src/context/lattice.rs         # particle/flag/tracker SoA arenas
  src/context/links.rs           # compact per-P1 link CSR + target resolution
  src/context/clusters.rs        # instances, memberships, codes, code ids
  src/context/energy.rs          # site table, site+pair vectors, per-centre cluster energy, initial total
  src/context/pools.rs           # DirectionPool population and swap-remove helpers
  src/context/jumps.rs           # per-(P1,direction) jump status + path resolution
  src/context/goals.rs           # counters, goals, normalization, dt
  src/context/resume.rs          # ResumeState/ResumePhase validation
  src/context/limits.rs          # ContextLimits, MemoryReport
  src/context/error.rs
  tests/context_acceptance.rs
  tests/allocation.rs            # own test binary with the counting global allocator
  benches/context.rs
```

- `context/` is the concept; the submodules name the data they own, not a type kind. `energy` here is the
  *runtime tables and vectors*, not the Step 15 delta engine; Step 15 adds `energy/` as its own concept module.
- `dimensions.rs` owns `EnvId` (absolute supercell environment) and the checked index/wrap helpers; `links.rs`
  owns the owner-major CSR and the inverse routing index; `limits.rs` owns `MemoryPlan`/`MemoryReport`;
  `pools.rs` owns `SelectionPool`/`PoolMember` (the Step 13 `DirectionPool` placeholder is retired).
- No `utils`/`helpers`/`common` module. The index/wrap helpers live in `dimensions.rs` because they are the
  dimension contract.
- Keep every file below 800 lines and the `context/` directory below 20 direct files; split by sub-concept if a
  file grows.
- Public items carry doc comments; every new file starts with `SPDX-License-Identifier: MIT`.

### 2.3 `Mocassin.App*` layout (Steps 23A/23B)

The Avalonia track is split so that view models stay UI-framework-free and the composition root stays the only
place that resolves DI. **23A** creates the host, workspace, settings, and lifecycle folders; **23B** adds the
editing, diagnostics, recovery, and scheduling folders.

```text
src/dotnet/Mocassin.App/                 # composition root + app-local I/O
  Program.cs
  App.axaml
  App.axaml.cs
  ViewLocator.cs
  AssemblyInfo.cs
  Composition/     # 23A: AppHost, service registration, startup options, app paths, logging setup, log retention
  Settings/        # 23A: AppSettingsStore, settings write policy (atomic + fsync + size cap)
  Files/           # 23A: ProjectFileProbe, file-stamp provider
  Startup/         # 23A: startup coordination and crash handler
  Recovery/        # 23B: RecoveryStore, recovery manifest, CrashRecoveryWriter, recovery scan

src/dotnet/Mocassin.App.ViewModels/      # no Avalonia, no System.IO
  Shell/           # 23A: shell/region/status-bar view models, window state, layout persistence
  Documents/       # 23A: document model, keys, tab collection, welcome
  Documents/Project/  # 23A: project overview, project JSON; 23B: metadata editor
  Projects/        # 23A: project session, session service, outcomes, stamps, external change, recents
  Navigation/      # 23A: navigation service/contracts, history, target provider, explorer/inspector seams
  Settings/        # 23A: settings model, validator, migrations, JSON context, store/theme contracts, state
  Editing/         # 23B: project edit coordinator, edit diagnostics, project-changed event
  Diagnostics/     # 23B: validation coordinator, problems, diagnostic items, validation state, help links
  Recovery/        # 23B: recovery store contract, candidates, sources, retention policy (pure)
  Scheduling/      # 23B: autosave scheduler and debouncer
  Commands/        # command catalog, shell command, gesture descriptors, contribution seams, palette
  Consoles/        # console channels, entries, console service; 23B Problems channel
  Dialogs/         # dialog/picker/dispatcher/progress contracts and view models

src/dotnet/Mocassin.App.Views/
  Shell/  Documents/  Navigation/  Consoles/  Settings/  Dialogs/  Controls/  Themes/
  Theming/         # AvaloniaThemeApplier and theme resource dictionaries
  Platform/        # AvaloniaStorageService, AvaloniaUiDispatcher, key-binding binder
```

Rules:

- `Mocassin.App.ViewModels` has no Avalonia, view, or control reference, and no `System.IO`/`Environment`/
  `Application`/resource-dictionary use. Dialogs, pickers, the UI dispatcher, clocks, the settings/recovery
  stores, the file probe, and the theme applier are interfaces in this project; their implementations live in
  `Mocassin.App` (settings, recovery, file probe, paths) or `Mocassin.App.Views` (picker, dispatcher, theme,
  key binding). An architecture test enforces the direct-reference direction, the absence of view types, and a
  source-token ban on the file-system/UI tokens.
- Library APIs that perform I/O (`ProjectStore`, `ProjectValidationPipeline`, `ProjectTemplateFactory`) are
  allowed in view models because they are the same public contracts the CLI uses; re-implementing their file
  access is what the rule forbids.
- `Mocassin.App` keeps only `Program.cs`, `App.axaml`, `App.axaml.cs`, `ViewLocator.cs`, and `AssemblyInfo.cs`
  at the root; everything else lives in `Composition/` or `Startup/`. `App.axaml.cs` and `ViewLocator.cs` are
  the framework-mandated root files already permitted by the policy.
- `Mocassin.App.Views` owns controls and resource dictionaries. Code-behind is limited to view-only concerns
  (focus, splitter keyboard handling, drag-reorder plumbing); no library calls and no scientific logic.
- Folder names are concept nouns. `Services/`, `Helpers/`, `Common/`, `ViewModels/`, and `Views/` buckets are
  not added inside `Mocassin.App.ViewModels`; the area folder is the concept.
- The 20-file/800-line limits and the single-type file rule apply unchanged. A file that would need several
  types is split, or uses one of the permitted multi-type suffixes from `tools/structure-policy.json`.
- Test support for the UI track lives in `Mocassin.App.Tests/TestSupport/` (manual clock, inline dispatcher,
  recording dialog service, stub storage service, in-memory settings/recovery stores for non-persistence tests,
  temporary workspace, headless app builder, fixtures); `Visual/Baselines/` holds the screenshot PNGs. Test
  root files stay within the 20-file limit. Persistence tests use the real App-layer stores against a temporary
  data directory, not the in-memory ones.
- Every **new** type in the three App projects **other than `App`** is `internal`;
  `InternalsVisibleTo("Mocassin.App.Tests")` gives the test project access. `App` is the only new public App
  type; `Program` is pre-existing. The public exceptions are the pre-existing `AppViewModelsModule`/`AppViewsModule`
  markers (in `PublicAPI.Shipped.txt`, baselined, ApiCompat-checked) and `Mocassin.App.Program`; 23A **adds**
  `public sealed class App : Application` with `App.axaml(.cs)`, and `Mocassin.App` is not an ApiCompat project
  (no `PublicAPI` list, no baseline), so that addition is not a compatibility change. The ApiCompat-checked App
  assemblies (`Mocassin.App.ViewModels`, `Mocassin.App.Views`) keep their `PublicAPI.*` and
  `tests/contracts/api-baseline/net11.0/*.dll` unchanged. Making a marker internal or adding a public type to an
  ApiCompat project is an owner-approved public-API change with a baseline refresh; if a later step genuinely
  needs a separate assembly, it records an amendment.
- The I/O token test uses whole-identifier matching (negative lookbehind, so `LastOpenDirectory.Trim` does not
  match `Directory.`), strips comments and string literals, and detects `using static System.IO` /
  `using static System.Environment`. A `Mocassin.App.ViewModels/**/*.cs` file must not contain `System.IO`,
  `File.`/`FileInfo`, `Directory.`/`DirectoryInfo`, `Path.Combine`/`Path.Get`, `FileStream`, `StreamReader`,
  `StreamWriter`, `Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, or
  `ResourceDictionary`. `IUiDispatcher` and bare `Dispatcher`/`Application` identifiers are permitted, and a
  positive control proves the scanner reports a seeded violation. The scan is a tripwire, not a compiler
  guarantee; the direct-reference check and review remain the primary enforcement.

## 3. Folder names and ownership

Folder names are PascalCase domain nouns. A folder should answer “which concept owns this code?”

Allowed examples:

`Geometry`, `SpaceGroups`, `Reduction`, `Project`, `Diagnostics`, `Units`, `Editing`, `Ids`, `Caching`,
`Contracts`, `Data`.

Forbidden as folders:

| Folder | Why it is forbidden | Where the code belongs |
| --- | --- | --- |
| `Utils`, `Utilities`, `Helpers` | No domain owner; attracts unrelated code | The concept folder the helper supports, named after that concept |
| `Common`, `Shared`, `Misc` | Semantically empty; merges unrelated concepts | The owning domain folder |
| `Models` | Files by type kind, not by concept | The domain folder (`Project/`, `Geometry/`, `Editing/`) |
| `Interfaces`, `Implementations`, `Abstractions` | Files by C# construct; splits one concept vertically | The domain folder that owns the contract and implementation |
| `Extensions` | Usually becomes a second `Helpers` | `Geometry/GeometryExtensions.cs`, not `Extensions/GeometryExtensions.cs` |
| `Internal` | Visibility is not a concept | Allowed only inside a named concept folder, never at the root |

A useful distinction:

- `Model/` is allowed — it names the domain model concept.
- `Models/` is forbidden — it is a bucket for data classes.
- `Contracts/` is allowed — it names a bounded public contract surface.
- `Interfaces/` is forbidden — it names a C# construct, not a concept.

If a project genuinely needs a bounded role-named folder, add an explicit exception to
`tools/structure-policy.json`:

```json
"allowedFolders": {
  "Mocassin.App.ViewModels": ["Models"]
}
```

Record the reason in this page. Exceptions should be rare.

## 4. File names and types

- A file with one top-level type is named after that type: `CellGeometry.cs`, `SpaceGroupCatalog.cs`.
- A partial type uses `<Type>.<Aspect>.cs`; all parts live in the same folder.
- A cohesive type family may share a concept file named with an allowed suffix: `CatalogDtos.cs`,
  `TypedIds.cs`, `UnitValues.cs`, `Templates.cs`.
- If a file contains multiple top-level types, its name must match one of them or end with an allowed suffix.
- Test files are named `<Type>Tests.cs` or `<Area>Tests.cs`. Test support files live in `TestSupport/` or are
  listed as permitted root files.

Do not use `#region` to simulate folders. Split the file instead.

## 5. Namespaces

New and existing files use namespaces that match their folder:

```text
Mocassin.Symmetry/Geometry/CellGeometry.cs  ->  Mocassin.Symmetry.Geometry
Mocassin.Core/Project/Project.cs            ->  Mocassin.Core.Project
```

Step 02 was completed and tagged before this policy existed. A post-step maintenance update aligned the
`Mocassin.Symmetry` namespaces with its folders — `Caching`, `Contracts`, `Diagnostics`, `Geometry`,
`Reduction`, and `SpaceGroups`. This accepted a public API break because the project has no external consumers
yet.

The `step-02` tag remains on the original commit. The maintenance update is recorded on `main`, in the Step 02
page, and in the Step 02 audit record. There is no namespace baseline: `IDE0130` is an error and the structure
test enforces the folder namespace for every file.

## 6. Test projects

- Test projects mirror the source folder for the code under test once they introduce folders.
- The test-project root may contain at most 20 files.
- Root test files must be named `*Tests.cs` or be a permitted support file (`RepositoryPaths.cs`,
  `TestSupport.cs`, `AssemblyInfo.cs`, `GlobalUsings.cs`, `Usings.cs`).
- Fixtures and helpers belong in `TestSupport/` or a domain folder.
- Flat test projects are allowed under the 20-file limit; split them when the limit is reached.

## 7. Enforcement

### Format tools

`.editorconfig` enables:

- file-scoped namespaces as errors;
- `IDE0161` as an error;
- `IDE0005` unused usings as a warning;
- `IDE0130` namespace/folder mismatch as an error.

`dotnet format --verify-no-changes` is already part of `tools/verify.sh dotnet` and CI. It now fails on
namespace style, unused usings, and formatting.

### Structure test

`Mocassin.Architecture.Tests` parses every `.cs` file with Roslyn and checks the hard rules. It reads
[`tools/structure-policy.json`](https://github.com/seb-eis/mocassin-2/blob/main/tools/structure-policy.json).

Run it directly:

```sh
./tools/verify.sh structure
```

It also runs as part of `dotnet test Mocassin.slnx`, so `./tools/verify.sh dotnet` and the CI `.NET` job cover
it automatically.

A typical failure:

```text
Structure violation: src/dotnet/Mocassin.Symmetry/CellGeometry.cs
  Source files may not live in the project root.
  Move to: Geometry/CellGeometry.cs (namespace Mocassin.Symmetry.Geometry)
```

The test does not move files automatically. It reports the expected location so the agent can move the file
without breaking partial classes or case-sensitive paths.

## 8. Workflow integration

- **Feature start:** write a directory plan — new folders, new files, moved files, namespaces.
- **Pre-implementation review:** confirm the layout follows this page.
- **Feature record:** record the directory plan and `tools/verify.sh structure` result in the step page; no
  separate approval is required.
- **Audit:** confirm the structure test passes and no new root-level or catch-all folders were added.
- **Definition of done:** code is grouped by domain concept and the structure test passes.

The branch, approval, and audit gates are in `AGENTS.md` §3; this page only defines the structure policy and its
local check.

## 9. Folder selection rule

1. Is it the project composition root? Put it at the root.
2. Is it an embedded asset? Put it in `Data/`.
3. Does an existing concept own it? Put it in that folder.
4. Is it a new concept? Create a PascalCase domain-noun folder.
5. Is it a helper? Put it in the concept folder it helps and name it after that concept.

## 10. Common fixes

| Violation | Fix |
| --- | --- |
| New source file in the project root | Move it to the concept folder that matches its namespace |
| More than 20 files in a folder | Split into sub-concepts, not by type kind |
| `Helpers/` or `Utils/` folder | Move each file to the concept it supports and rename it |
| Namespace does not match folder | Prefer moving the file if the concept is clear; otherwise fix the namespace |
| Folder depth 5 | Flatten or split the intermediate concept |
| File over 800 lines | Extract collaborators into the same concept folder |
| `#region` used as a filing system | Split the file or extract a type |

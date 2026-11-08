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
- **Feature approval:** report the directory plan and `tools/verify.sh structure` result.
- **Audit:** confirm the structure test passes and no new root-level or catch-all folders were added.
- **Definition of done:** code is grouped by domain concept and the structure test passes.

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

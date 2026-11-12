# Documentation tooling

Pinned requirements live in [`requirements.txt`](requirements.txt). Build scripts:

```sh
./tools/docs/build-docs.sh      # bash
./tools/docs/build-docs.ps1     # PowerShell
./tools/docs/serve.sh           # local preview
./tools/docs/serve.ps1          # local preview
```

The combined build runs Zensical first, then copies DocFX and rustdoc output into `site/api/dotnet` and
`site/api/rust` when those projects/crates exist. `site/` and generated API HTML are never committed.

The DocFX stage loads the Release assemblies, so build the solution first when it is present:

```sh
dotnet build Mocassin.slnx -c Release
```

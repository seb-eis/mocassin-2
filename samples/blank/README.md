# Blank sample

`blank.mocprj` is the minimal valid Step 01 project: project metadata, an empty model, and default view
settings. It is produced by the real project writer and is also mirrored as
`tests/contracts/project-minimal.mocprj`.

The `mocassin validate` command arrives with Step 12. Until then, the fixture is exercised by the project
format and schema tests, and by `ProjectStore.LoadAsync` in `Mocassin.Packages.Tests`.

The model body, templates, and build workflow are added by later steps. The sample is intentionally
valid with an empty model so it can be used as a round-trip and schema fixture from day one.

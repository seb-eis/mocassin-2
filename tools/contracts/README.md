# Contract tooling

`contracts/manifest.json` is the single machine-readable source for format field names, types,
required/optional flags, versions, canonical signature-projection layouts, event names, and fixture
descriptors (ADR 0009, ADR 0010).

## Commands

```sh
# Validate the manifest and regenerate artifacts under contracts/generated/
python tools/contracts/generate.py

# Fail when generated artifacts or the signature-vector scaffold are missing/stale
python tools/contracts/generate.py --check

# Check restored NuGet package licences against the project allow-list
python tools/contracts/check_nuget_licenses.py
```

The generator uses the Python standard library only in Step 00. Add a dependency only with a justification
and licence check.

## Generated outputs

- `contracts/generated/schema/*.schema.json` — JSON Schema fragments for project, package, results, state,
  solver, job-folder, events, and MMCFE formats, plus the `validation-v1` CLI report;
- `contracts/generated/signatures/*-v1.json` — canonical signature-projection layouts;
- `contracts/generated/constants/csharp/ContractVersions.g.cs` — C# version constants;
- `contracts/generated/constants/rust/contract_versions.rs` — Rust version constants;
- `contracts/generated/constants/python/contract_versions.py` — Python version constants;
- `contracts/generated/fixtures/descriptors.json` — language-neutral fixture descriptors;
- `tests/contracts/signature-vectors.json` — initial signed-type vector scaffold (vectors arrive with Steps
  08, 09, and 11).

Generated artifacts are checked in and must be regenerated, never hand-edited. CI runs the `--check` command
and fails on drift.

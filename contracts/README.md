# Contracts

`manifest.json` is the single source for project/package/results/state/job-folder/event/MMCFE field names,
types, versions, and canonical signature-projection layouts. Generated artifacts live under `generated/` and
are produced by [`tools/contracts`](../tools/contracts/README.md).

Commands:

```sh
python tools/contracts/generate.py
python tools/contracts/generate.py --check
```

Rules:

- update the manifest, not the generated artifacts;
- a format change requires an ADR or decision note, a version bump, updated fixtures, cross-language tests,
  and updated documentation;
- CI fails when generated artifacts are stale or a language DTO disagrees with the manifest.

See [`docs/planning/02-data-contracts.md`](../docs/planning/02-data-contracts.md) and
[ADR 0009](../docs/planning/adr/0009-contract-manifest-and-contracts-library.md).

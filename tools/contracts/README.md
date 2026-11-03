# Contract tooling

`contracts/manifest.json` is the single machine-readable source for format field names, types,
required/optional flags, versions, and canonical signature-projection layouts (ADR 0009, ADR 0010).

Planned commands:

```sh
python tools/contracts/generate.py          # regenerate artifacts under contracts/generated/
python tools/contracts/generate.py --check  # fail if generated artifacts are stale
python tools/contracts/validate.py          # validate the manifest itself
```

Generated artifacts are checked in and must be regenerated, never hand-edited. The tooling uses the Python
standard library only in Step 00; add a dependency only with a justification and licence check.

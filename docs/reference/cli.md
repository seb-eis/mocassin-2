# Command-line interface

The `mocassin` CLI is introduced with validation in Step 07. Step 12 extends the command surface; the
`validate` semantics and the `validation-v1` report are stable.

## `mocassin validate`

```text
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
```

The command loads the project read-only. It never writes, acquires the editing lock, migrates in place, or
mutates the model. The load and model stages run exactly once: the CLI uses a `ProjectStore` with
`ValidateOnLoad = false` and passes the result to `ProjectValidationPipeline.ValidateFileAsync`.

| Option | Meaning |
| --- | --- |
| `--json` | Print exactly one `validation-v1` JSON report to stdout. Human errors go to stderr. |
| `--warnings-as-errors` | Treat every warning as invalid for the exit code and `valid` flag. `summary` keeps the raw counts. |
| `--stats` | Compute and include advisory model statistics. It never changes diagnostics or `valid`. |

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Valid under the selected policy. |
| `1` | Validation/model errors, promoted warnings, or project content that cannot be parsed or migrated. |
| `2` | Usage or argument error. |
| `3` | The file is missing, unreadable, oversized, or failed with an operating-system/I/O error. |

A usage error with `--json` still prints one report with `exitCode: 2`, `valid: false`, and null identity
fields.

### Human output

The default output is a stable table with severity, code, message, object/property path, and the per-instance
hint, followed by the raw summary, an optional statistics table, and the policy-adjusted result.

### `validation-v1` report

```json
{
  "schemaVersion": 1,
  "kind": "validation",
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80",
  "modelHash": "9f2c…",
  "documentHash": "4a71…",
  "valid": false,
  "exitCode": 1,
  "summary": { "errors": 2, "warnings": 1, "infos": 0 },
  "diagnostics": [],
  "statistics": null
}
```

- `projectId`, `modelHash`, and `documentHash` are `null` before a successful load. Hashes are raw lowercase
  hexadecimal without a prefix.
- `diagnostics` is ordered by severity, code, domain, object ID, property, and message. Exact duplicate
  `(code, path, message)` triples are removed.
- `statistics` is non-null only when `--stats` was requested and statistics could be computed: the document
  loaded, the model stage produced no error, and every required preview succeeded. A model that is invalid only
  because of feasibility/budget errors or promoted warnings can still carry statistics.
- Usage errors keep `diagnostics` empty when no store diagnostic exists.

The report is the `validation-v1` contract. Its generated JSON Schema is
`contracts/generated/schema/validation-v1.schema.json`; the manifest entry lives under `reports`, not
`formats`.

## `mocassin build --preview`

```text
mocassin build <project.mocprj> --preview [--template <jobTemplateId>] [--json]
```

Step 10 adds the preview path only. The command loads, validates, compiles, expands, and previews one job
template. It never opens, writes, or replaces a `.msl` package and never calls the Step 11 writer. Step 12 owns
the full package-building command and the `--jobs`, `--parallel`, `--force`, and `--compact` options.

| Option | Meaning |
| --- | --- |
| `--preview` | Required. Selects the no-write build-preview path. |
| `--template <jobTemplateId>` | Selects one template when the project contains several. A project with exactly one template may omit it. |
| `--json` | Print exactly one `build-preview-v1` JSON report to stdout. |

The command previews:

- the selected template, its collections, configurations, and total expanded job count;
- each collection's job count, compiled dual-degree-of-freedom flag, and selection-optimizer
  target/removed-start count;
- each configuration's lattice size, generated position count, job count, requested and applied doping counts,
  effective fraction, charge before and after doping, and neutrality;
- the validation, compilation, expansion, and preview diagnostics, including `JOB` warnings.

The preview generates **one lattice per configuration**, not one per duplicate job. The per-configuration job
count still reflects duplicate expansion, and lattice diagnostics are deduplicated in the report. Seeds are
derived from the template and project in the same way as a real build; the preview never creates or persists a
seed and never records generated particle positions.

`--template` takes the job-template GUID, not an index or a name. It is required when the project has more than
one template; a project with exactly one template may omit it. A project with no template is a validation error
(exit 1), and an unknown template identifier is reported as a `PROJ010` unknown-reference diagnostic.

Exit codes follow `validate`:

| Code | Meaning |
| --- | --- |
| `0` | The preview completed; warnings do not invalidate it. |
| `1` | Validation, compilation, expansion, template-selection, or lattice-preview errors. |
| `2` | Usage or argument error, including several templates without `--template`. |
| `3` | The project file is missing, unreadable, oversized, or failed with an operating-system/I/O error. |

The `build-preview-v1` report mirrors the `validation-v1` envelope (`schemaVersion`, `kind`, identity hashes,
`valid`, `exitCode`, `summary`, `diagnostics`) and adds a `preview` payload. It is an interim Step 10 report;
Step 12 may replace it with the final build report contract.

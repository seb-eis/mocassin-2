# Troubleshooting

Troubleshooting pages are added with the features they describe. Validation problems are covered by
[validation errors](errors.md); the full diagnostic catalog and CLI exit codes are in the
[reference](../reference/index.md) section.

## General checks

1. Confirm the tool versions listed in [Getting started](../getting-started/index.md).
2. Run the relevant tests and capture the full output.
3. For solver problems, keep the job folder (`solver.json`, `run.mcs`, `prerun.mcs`, `stdout.log`,
   `stderr.log`) and the package signature.
4. Report the OS, architecture, commit/tag, package version, and a minimal reproduction.

Do not attach secrets or private project data to issues.

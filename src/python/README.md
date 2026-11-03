# Python analysis package

This directory contains the `mocassin-analysis` package. Step 00 creates a minimal, installable placeholder so
the Python CI job is meaningful; Step 22 replaces it with the read-only `.mcs`/`.msr` reader and basic
multi-file evaluations.

The package must never write simulations, packages, or result stores. See
[`docs/planning/01-target-architecture.md`](../../docs/planning/01-target-architecture.md) §2 and
[`docs/planning/steps/step-22-python-analysis-api.md`](../../docs/planning/steps/step-22-python-analysis-api.md).

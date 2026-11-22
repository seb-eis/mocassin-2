<!-- SPDX-License-Identifier: MIT -->

# Step 24 layout spike (test-only)

This folder is a **decision-input spike**, not production code. It builds the five Step 24 editor-layout
candidates from §0 of the step page, captures them, and records the evidence in
[`docs/planning/maintenance/2026-11-22-step-24-layout-poc.md`](../../../../../../docs/planning/maintenance/2026-11-22-step-24-layout-poc.md).

- Nothing here ships. No production project references it.
- It adds no dependency and does not touch `Visual/Baselines/`.
- After the owner records the layout pick in ADR 0003, delete this folder and the
  `fix/step-24-layout-poc` branch. The durable artefacts are the maintenance record and its PNG subset.

Run the evidence locally:

```bash
dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release \
  --filter "FullyQualifiedName~LayoutSpike"
```

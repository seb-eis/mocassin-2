# Step 13 centered fixture sample

This sample builds the Step 13 C-centered `.msl` fixture in
[`tests/contracts/packages/step-13-centered/`](../../tests/contracts/packages/step-13-centered/). It is derived
from the Step 08 sample [`samples/simulations/centered-exchange.mocprj`](../simulations/centered-exchange.mocprj):

- space group 5 (C2), cell `a = 5`, `b = 6`, `c = 7`, `beta = 100°`;
- two sites, A and B, at `(0.1, 0.2, 0.3)` and `(0.1, 0.2, 0.35)`, so the C-centering operations produce
  eight canonical P1 positions;
- one MMC A/B exchange at 1000 K and a 2×2×2 job template;
- site B uses the extended `{O, Vc, H}` set while A uses `{O, Vc}`, giving the package a non-trivial per-node
  mask difference as well as translation-equivalent jump starts.

The real writer emits a `jump_mapping` with cross-bucket entries (for example, a direction whose
`position_id` is `6` appears in the start bucket at position `2`), which is the Step 13 F-1 property. The
fixture also asserts the per-node mask difference, so one real package carries both required properties.

Regenerate with:

```sh
MOCASSIN_REGENERATE_CENTERED_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateCenteredPackageFixture"
```

The command rebuilds the package, the canonical summary, the SHA-256 line, and the extracted payloads. The
`.msl` bytes are a writer/runtime artifact; the signatures and canonical summary are the logical contract.

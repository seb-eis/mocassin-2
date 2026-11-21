# Step 14 ceria 10×10×10 sample

This sample builds the Step 14 context acceptance `.msl` fixture in
[`tests/contracts/packages/step-14-ceria-10x10x10/`](../../tests/contracts/packages/step-14-ceria-10x10x10/).
It is derived from the Step 11 job sample
[`samples/jobs/ceria-temperature-sweep.mocprj`](../jobs/ceria-temperature-sweep.mocprj):

- the same ceria primary model, particle table, structure, transition mechanism, and KMC simulation;
- one KMC job template collection (`T773`, config `x001`) with **one** job at a **10×10×10** supercell;
- the same fixed `created_utc = 2026-11-21T12:00:00Z` used by the fixture builder.

The model signature is the msl-v1 ceria model; the package has no pair or cluster tables (the ceria primary
model's cluster is not part of this job's mechanism), so the fixture is a table-free 10×10×10 acceptance fixture.
The populated-model 10×10×10 measurement uses the `step-13-populated` model cloned onto a hand-built 10×10×10
lattice; see the Step 14 page §20.

Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_STEP14_CERIA_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateStep14CeriaFixture"
```

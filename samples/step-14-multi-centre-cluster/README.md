# Step 14 multi-centre cluster sample

This sample builds the Step 14 multi-centre cluster `.msl` fixture in
[`tests/contracts/packages/step-14-multi-centre-cluster/`](../../tests/contracts/packages/step-14-multi-centre-cluster/).
It is derived from the Step 13 C-centered sample
[`samples/step-13-centered/step-13-centered.mocprj`](../step-13-centered/step-13-centered.mocprj), which stays
byte-identical:

- the same space group 5 (C2) A/B C-centered structure and MMC exchange;
- an added third stable site `C`, so a cluster can have two **distinct** member sites as the project model's
  `ENM008` validator requires (2–8 surrounding offsets); a one-member cluster is not a valid project model;
- one cluster centred on the four-position A site with member-site offsets `[0,0,0.05]` (B) and `[0,0,0.1]` (C);
- one MMC job at 2×2×2.

The real writer emits one slot-0 cluster link for **every P1 position of the A site** (four links) and one slot
link per member site (one B position and one C position). The context therefore builds four instances; each
instance resolves its own member-site image from the centre's pair links and has its own code/energy. This is the
multi-centre instance acceptance fixture; the two-images-of-one-member-site ambiguity remains the rejected OD-1
case (the amendment-2 ceria fixture covers the non-canonical code expansion).

Regenerate with the real writer:

```sh
MOCASSIN_REGENERATE_STEP14_MULTI_CENTRE_FIXTURE=1 \
  dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release \
  --filter "FullyQualifiedName~RegenerateStep14MultiCentreFixture"
```

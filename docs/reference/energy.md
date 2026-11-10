# Energy Model Reference

This page defines the project section shape, defaults, limits, and identities of the energy model. It does
not duplicate the diagnostic catalog; the complete `ENM` reference is added with Step 07.

## Sections

The energy model is flat under `model`:

```json
{
  "stableEnvironment": {
    "cutoff": 2.7,
    "filters": [
      { "start": 0.0, "end": 1.0, "siteAId": "…", "siteBId": "…" }
    ]
  },
  "unstableEnvironments": [
    { "siteId": "…", "cutoff": 1.5, "filters": [] }
  ],
  "clusters": [
    {
      "id": "…",
      "name": "Oxygen migration edge",
      "centerSiteId": "…",
      "offsetVectors": [[-0.25, 0.0, 0.0], [0.25, 0.0, 0.0]]
    }
  ],
  "siteEnergies": [
    { "siteId": "…", "entries": [{ "particleId": "…", "energyEv": -0.5 }] }
  ]
}
```

## Defaults and ordering

- A missing `stableEnvironment` loads `1.0 Å` with no filters.
- The default environment is omitted on write when it equals the default. Empty `unstableEnvironments`,
  `clusters`, and `siteEnergies` arrays are omitted.
- A model with no energy sections therefore keeps its previous canonical JSON and hashes.
- `offsetVectors` is semantic and never re-sorted.
- `unstableEnvironments` sort by `siteId`, `clusters` by `id`, and `siteEnergies` by `siteId`.
- Filters sort by `(start, end, siteAId, siteBId)`; site-energy entries sort by `particleId`.
- `ProjectJson.CanonicalizeModel` applies these rules before save and hashing.
- `ProjectHasher.ComputeEnergy(ModelDefinition)` hashes only the canonical energy projection and is invariant
  to particle, structure, and transition edits.

## Limits

| Limit | Warning | Hard |
| --- | ---: | ---: |
| Stable positions | 500 | 5000 |
| Unstable positions | 100 | 1000 |
| Cluster raw permutations | 500 | 5000 |
| Clusters per centre | 4 | 10 |
| Cluster surrounding offsets | — | 2..8 |

Site energies must be finite and within `-100..100 eV`. All cost counts are `long`; the largest legal
cluster (`63⁹`) fits without overflow.

## Identities

For every directed pair candidate:

- `GeometryId` is a lowercase SHA-256 over the domain-separated canonical projection of
  `(centerSiteId, centerP, partnerSiteId, partnerP, offsetA, offsetB, offsetC, distanceBucketFm)`.
  The P1 indices distinguish shells that share a site pair, cell offset, and distance bucket.
- `EnergyId` is the shared energy-table identity. For an achiral pair it equals `GeometryId`. For a chiral
  pair it is the lexicographically smaller of the two directions' `GeometryId` values.
- `ChiralPartnerGeometryId` is the inverse direction's `GeometryId` when a chiral partner exists.

Step 08 uses `EnergyId` as the compiled `object_id`; Step 09 stores one parameter value per `EnergyId`.

## API surface

- `Mocassin.Core.Model.Energy` owns the records, `EnergyLimits`, and `IEnergyQuery`.
- `Mocassin.Symmetry.Energy` owns the sampler, geometry validator, cost preview, and pair preview.
- `IEnergyPreviewService.TryCreatePreview(..., out diagnostics)` returns `null` on invalid user data and never
  throws for user data.
- The preview cache key includes the energy hash, structure hash, requested cell counts, catalog version, and
  symmetry contract version.

The ceria and barium-zirconate sample projects include energy sections and mirror the contract fixtures.

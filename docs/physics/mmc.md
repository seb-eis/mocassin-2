# Metropolis Monte Carlo (MMC)

MMC samples equilibrium configurations with two-site exchanges. It does not advance physical time and does not
use kinetic attempt frequencies.

## Two-site exchange

A Metropolis transition binds exactly two reference sites:

```json
{
  "id": "…",
  "name": "O exchange",
  "chainId": "…",
  "siteAId": "…",
  "siteBId": "…",
  "reverseTransitionId": null
}
```

- The chain must be `metropolis2` or a two-position custom chain.
- Both sites must exist and be stable.
- Selecting the same site twice is valid and represents an intra-sublattice exchange.
- A Metropolis transition does not store a `referencePath`; the preview derives its path from the two sites.

The exchange is accepted with probability `min(1, exp(-ΔE / k_B T))`. Energy differences come from Step 05 and
the interaction model in the compiled package.

## State changes

A Metropolis chain has two positions. Each position uses a state-change group, and the group must satisfy the
same homogeneity and stability rules as kinetic positions. A two-site MMC exchange commonly uses stable-role
pairs such as `[A, V_A]` and `[B, V_B]`.

## Symmetry

The authored model stores one site pair. Compilation expands it into all symmetry-equivalent site pairs. The
preview reports the expanded pair count and echoes the requested `(A, B, C)` counts so a preview cannot be
confused with another supercell or another transition sharing the same chain.

## Validation

- `TRN015` — the chain is not Metropolis-compatible.
- `TRN016` — a site is unknown, unstable, or otherwise invalid.
- `TRN017` — duplicate name within the Metropolis collection.
- `TRN019` — more than 100 kinetic and Metropolis transitions combined.
- `TRN027` — invalid reverse link.

Same-site exchange is deliberately accepted; the original self-exchange check is not reintroduced.

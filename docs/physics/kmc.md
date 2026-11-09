# Kinetic Monte Carlo (KMC)

KMC advances physical time by executing transitions with rates derived from the energy model and attempt
frequencies. The authored transition model supplies the mechanism and reference geometry; Step 08 supplies the
compiled rules and Step 05/06 supply energies and frequencies.

## Reference paths

A kinetic transition stores an ordered `referencePath` in unit-cell fractional coordinates. The path length
must equal the chain position count. Endpoints are stable; unstable positions are transition states.

A built-in pattern fixes the stability sequence:

| Pattern | Stability sequence |
| --- | --- |
| `migration3` | S, U, S |
| `interstitialcy5` | S, U, S, U, S |
| `interstitialcy7` | S, U, S, U, S, U, S |
| `vehicle5` | S, S, U, S, S |
| `vehicle6` | S, U, S, S, U, S |

A custom kinetic chain infers its sequence from the resolved reference path, then applies the same endpoint,
consecutive-unstable, and no-unstable checks.

## Path matching

The resolver matches each authored point to exactly one canonical P1 position and owning reference site:

- each finite component uses the nearest integer cell shift, with ties toward zero;
- all component differences must be at most `PathMatchTolerance = 1e-6` fractional units;
- the encoded position is `(cellA, cellB, cellC, P)`;
- the persisted point is never wrapped or rewritten.

A point in a different periodic cell has a different encoding and is a distinct physical position. Ring
detection compares every pair of full encodings, so an interior return or a literal endpoint repeat is
rejected.

## Reverse links

`metropolis2` and `migration3` generate an automatic inverse rule during compilation. Complex mechanisms
(`interstitialcy5`, `interstitialcy7`, `vehicle5`, `vehicle6`, `custom`) do not; the author must link an
explicit reverse transition with `reverseTransitionId`. A missing link warns `TRN018`, and an invalid link is
`TRN027`.

The reverse link is an authoring relationship, not an inverse rule generated inside the forward transition.
Step 08 reads the flag but the validator guarantees it agrees with the pattern.

## Preview

The kinetic preview shows the reference path, resolved P1 positions, symmetry-expanded paths, state-change
matrices, and the expected rule count before conservation filtering. It is advisory; compilation remains
authoritative.

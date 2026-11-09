# Transition samples

These projects exercise the Step 04 transition model with the current `.mocprj` schema.

| File | Mechanism |
| --- | --- |
| `ceria-kmc.mocprj` | Ceria 3-site oxygen migration (KMC) |
| `ceria-mmc.mocprj` | Ceria O/V two-site Metropolis exchange (MMC), including same-site exchange |
| `polaron-hop.mocprj` | Same-symbol charge-state hop (Ce³⁺/Ce⁴⁺) |
| `vehicle5.mocprj` | 5-site, 2-species vehicle with a `D,S,S,D` chain and `S,S,U,S,S` path |
| `interstitialcy5.mocprj` | 5-site interstitialcy with an `S,U,S,U,S` path |

The files are produced by the real project writer. They are valid under Core and Symmetry validation, and every
transition preview succeeds. Compilation into rules, conservation filtering, and automatic inverse rules remain
Step 08 work. The ceria samples reuse the canonical [ceria structure](../ceria/ceria.mocprj) and its dedicated
transition-state occupation set.

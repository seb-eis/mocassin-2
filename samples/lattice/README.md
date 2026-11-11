# Lattice samples

These projects exercise the Step 06 lattice and doping model with the current `.mocprj` schema.

| File | Doping model |
| --- | --- |
| `ceria-reduction.mocprj` | Ce⁴⁺ → Ce³⁺ primary exchange with an O²⁻ → V_O dependent exchange and charge compensation |
| `ceria-hydration.mocprj` | Illustrative O²⁻ → OH⁻ protonation with a V_O → O²⁻ dependent exchange |
| `bzo-doping.mocprj` | Y³⁺ acceptor doping on the Zr site of BaZrO₃ with oxygen-vacancy compensation |

The ceria samples reuse the canonical ceria structure and add a Ce³⁺ particle and oxygen-vacancy exchange.
The barium-zirconate sample uses a Pm‑3m perovskite cell with Ba, Zr/Y, and O sites. All samples validate at
the model level with Core and Symmetry validation. Fraction application, integer truncation, compensation
counts, and the lattice preview remain Step 10 work.

The ceria reduction sample is also mirrored as `tests/contracts/project-lattice-populated.mocprj`.

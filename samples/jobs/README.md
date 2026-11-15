# Job samples

These projects exercise Step 10 job templates and supercell generation with the current `.mocprj` schema.

| File | Sweep |
| --- | --- |
| `ceria-temperature-sweep.mocprj` | Two KMC collections at 773 K and 973 K, two lattice sizes, and 1–3 % Ce⁴⁺ → Ce³⁺ reduction with oxygen-vacancy compensation |

`ceria-temperature-sweep.mocprj` extends the ceria KMC simulation sample with Ce⁴⁺/Ce³⁺ species, the
Ce⁴⁺ → Ce³⁺ and O²⁻ → V_O occupation exchanges, a compensated reduction doping, and one job template. The
template uses collection job-count overrides, a temperature sweep, a lattice-size sweep, duplicate jobs, and a
selection optimizer on the Ce site.

Preview it without writing a package:

```bash
mocassin build samples/jobs/ceria-temperature-sweep.mocprj --preview
mocassin build samples/jobs/ceria-temperature-sweep.mocprj --preview --json
```

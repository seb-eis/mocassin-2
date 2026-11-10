# Ceria sample

`ceria.mocprj` is the canonical reduced-ceria project. Step 03 populates the particle and structure sections:
Ce⁴⁺, O²⁻, and one uncharged vacancy particle (`Vacancy`, symbol `Vc`) with a Ce/O crystal structure, a stable
oxygen site, and an unstable oxygen transition site. The unstable site uses a dedicated
`Oxygen transition site` occupation set. Step 04 keeps parameterization and job templates for later steps;
transition examples live under [`samples/transitions/`](../transitions/README.md).

Step 05 adds a `2.7 Å` stable environment, a `1.5 Å` unstable environment for the oxygen transition site, and
an `Oxygen migration edge` cluster that resolves to the two nearest stable oxygen positions. Sparse site
energies on the stable oxygen site demonstrate the site-energy shape. The project passes Core and Symmetry
energy validation without errors or warnings.

The file is produced by the real project writer and is mirrored as
`tests/contracts/project-ceria.mocprj`. Do not copy the original user-guide project or its database content;
author the new project against the MIT-licensed schema.

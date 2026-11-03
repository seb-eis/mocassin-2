# Migration and Clean Break

The reimplementation deliberately does **not** read or write the original `.mocprj`, `.msl`, or `.mcs`
formats. There is no compatibility shim, no legacy importer, and no silent reinterpretation of old files.

## Why

The original formats depend on .NET Framework/WPF assumptions, unmanaged struct layouts, and a legacy
database design. Carrying them forward would constrain the new architecture and make cross-platform,
reproducible behaviour harder. New formats are versioned from day one and covered by cross-language tests.

## What to do with old work

- Recreate the scientific model in the new project editor or JSON schema.
- Re-enter energies and attempt frequencies in the new parameterization templates.
- Rebuild the simulation package and re-run simulations.
- Export old results to a neutral format if you need to compare them; do not expect them to be read directly.

The original project remains available under GPL-3.0-or-later and is attributed in `NOTICE.md`.

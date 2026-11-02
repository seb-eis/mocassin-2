# ADR 0007 — MIT License for the Reimplementation

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original MOCASSIN repository is distributed under **GPL-3.0-or-later**. The reimplementation is a new
codebase, but it is informed by the original's architecture, scientific behaviour, user documentation, and
published results.

The project owner has chosen the **MIT License** for the reimplementation. This is only defensible if the new
repository is an independent implementation that does not copy GPL-covered source code, and if any material
that is copied or adapted from the original is clearly separated and remains under its original license.

## Decision

1. **New code and original documentation in this repository are MIT-licensed.** See `LICENSE`.
2. **The original project is not relicensed.** It remains GPL-3.0-or-later. `NOTICE.md` records the original
   authorship, project URL, and license.
3. **No original source files are copied into the MIT codebase.** The reimplementation is written from
   behavioural specifications, published papers, the user guide's scientific descriptions, and black-box
   observation of the reference solver.
4. **The space-group catalog is generated from an authoritative crystallographic source** (e.g. International
   Tables/spglib), not copied from the original `Mocassin.Symmetry.db`. If the original database is ever used
   as a data source, that data must be treated as GPL-covered and its provenance/license documented before
   distribution.
5. **Any copied or adapted material is isolated.** If a file contains text, data, or code derived from the
   original under GPL, it must carry a clear GPL-3.0-or-later header, live in a separate directory, and be
   excluded from the MIT license grant. Mixed licensing must be documented in `NOTICE.md`.
6. **Dependencies must be MIT-compatible.** Prefer MIT/Apache-2.0/BSD dependencies. GPL/AGPL dependencies
   require an explicit review and must not force the core MIT code to become GPL.
7. **Scientific attribution remains mandatory.** The original authors request citation of the release paper
   when MOCASSIN is used in research; this is independent of the software license.

## Consequences

**Positive**

- The reimplementation can be used, embedded, and redistributed under permissive terms.
- Clear separation from the original GPL code reduces legal ambiguity for downstream users.
- Attribution and citation preserve scientific provenance.

**Negative**

- Contributors cannot copy GPL-covered original code into the MIT codebase, even when it would be convenient.
- Any later decision to reuse original code/data requires a licensing review and a mixed-license layout.
- The project must maintain a clear provenance record for data, formulas, and documentation.
- Some users may assume the reimplementation inherits the original GPL; `NOTICE.md` and the README must state
  the boundary explicitly.

## Rules

- Every new source file carries the MIT SPDX identifier (`SPDX-License-Identifier: MIT`).
- Files with third-party or GPL-derived content carry their own SPDX identifier and attribution.
- A dependency license check runs in CI.
- A contribution that ports original code must be rejected or relicensed with the owner's explicit approval;
  a clean-room rewrite is the default path.
- The release checklist verifies `LICENSE`, `NOTICE.md`, dependency licenses, and the citation block.

# Notice

This repository is a modern reimplementation of MOCASSIN, originally developed at
Forschungszentrum Jülich GmbH (IEK-12, Helmholtz Institute Münster) and RWTH Aachen University.

Original project: https://github.com/seb-eis/mocassin
Original copyright: Forschungszentrum Jülich GmbH and the MOCASSIN authors.
Original licence: GNU General Public License v3 or later.

## Licensing boundary

The **new reimplementation code and original documentation in this repository are licensed under the MIT
License** (see LICENSE). The reimplementation is independently written; it does not include or link the
original MOCASSIN source code.

The original MOCASSIN project remains licensed under GPL-3.0-or-later. It is not relicensed by this
repository. Any material copied or adapted from the original project, its source code, its documentation, or
its distributed data remains subject to the original GPL-3.0-or-later terms and must be attributed
accordingly. In particular, do not copy original source files into this repository unless the resulting file
is clearly marked as GPL-3.0-or-later and kept separate from the MIT-licensed code.

The space-group catalog is generated from an authoritative crystallographic source, not copied from the
original SQLite database. If that decision changes, the provenance and licence of the data must be reviewed
before distribution.

## Third-party dependencies

### Space-group catalog

The embedded space-group catalog is generated from spglib, pinned to release `2.8.0`
(commit `3ebd8ac3318793932baeee90b44f453a7c35b5b4`). The exact release and commit are recorded in the
generated catalog provenance (`src/dotnet/Mocassin.Symmetry/Data/README.md`). spglib is distributed under the
BSD-3-Clause licence.

Source: https://github.com/spglib/spglib

Citation: A. Togo, K. Shinohara, and I. Tanaka, “Spglib: a software library for crystal symmetry search”,
Sci. Technol. Adv. Mater. Methods 4, 2384822 (2024).

The generated catalog remains subject to the spglib BSD-3-Clause attribution requirements; it is not
relicensed as MIT. The original MOCASSIN SQLite database is not used as a source and is not copied. The
spglib database acknowledges Yusuke Seto’s crystallographic database.

NuGet dependencies are checked by `tools/contracts/check_nuget_licenses.py` and the CI licence/vulnerability
gate. The project policy allows MIT-compatible licences; any exception is recorded here.

The test project uses `JsonSchema.Net 9.6.0`, `JsonPointer.Net 7.0.4`, and `Json.More.Net 3.0.1`. Their source
is MIT-licensed. The NuGet binary releases carry the Open Source Maintenance Fee EULA (`OSMFEULA.txt`): the fee
applies only to revenue-generating users with annual gross revenue of at least US$10,000. This free,
non-commercial project uses the packages only for test validation and does not redistribute them. The exact
versions are pinned in the licence gate; changing them requires a new licence review. If the project becomes
revenue-generating or starts distributing these binaries, this decision must be revisited.

## Citation

If you publish scientific work using this software, please cite the original MOCASSIN release paper:

S. Eisele and S. Neitzel-Grieshammer, J. Comput. Chem. 2021, 42, 737–748.
https://doi.org/10.1002/jcc.26418

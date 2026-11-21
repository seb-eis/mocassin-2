# Diagnostics

Every diagnostic emitted by project validation, model compilation, and the CLI has a stable code. The
catalog below is generated from the same descriptors the application uses (`CoreDiagnosticCatalog`, including
the Step 09 parameterization descriptors and the Step 10 job-template descriptors; `SymmetryDiagnosticCatalog`;
the Step 08 compilation descriptors; the Step 07 descriptors; and the Step 12 CLI descriptors); a documentation-drift test fails when this
page and the catalog disagree.
Per-instance `hint` values are supplied by the producing rule and may be more specific.

The desktop Validation tab (internally the Problems channel) renders the same catalog entries; a known code links to its anchor below and an
unknown/unregistered code is intentionally unlinked. Edit-time structural rejections are transient `edit`
entries with the same code/message; the read-only edit refusal is an app-level session message and deliberately
has no diagnostic code. No GUI-specific codes are added.

| Code | Severity | Owner | Title | Help |
| --- | --- | --- | --- | --- |
| [`CMP001`](#cmp001) | error | Step 08 — model compilation pipeline | A required compilation precondition failed | Correct the referenced model object or the compiler input, then compile again. |
| [`CMP002`](#cmp002) | error | Step 08 — model compilation pipeline | The structure cannot be encoded or P1-expanded for compilation | Correct the structure, space group, cell, or site coordinates and revalidate. |
| [`CMP003`](#cmp003) | error | Step 08 — model compilation pipeline | Pair or cluster geometry cannot be resolved during compilation | Correct the environment, cluster, or site geometry and revalidate. |
| [`CMP004`](#cmp004) | error | Step 08 — model compilation pipeline | Two rules in one collection share a start state but differ in outcome | Split or correct the overlapping transition rules so one start state has one outcome. |
| [`CMP005`](#cmp005) | error | Step 08 — model compilation pipeline | A transition produces no valid rules after conservation filtering | Check the state changes, particle symbols, and total charge for mass and charge conservation. |
| [`CMP006`](#cmp006) | error | Step 08 — model compilation pipeline | A configured compilation limit is exceeded | Reduce the model size or raise the documented compiler limit knowingly. |
| [`CMP007`](#cmp007) | error | Step 08 — model compilation pipeline | A transition path cannot be expanded into compiled directions | Place every path point on a canonical P1 position and revalidate the transition. |
| [`CMP008`](#cmp008) | error | Step 08 — model compilation pipeline | Mass or charge conservation cannot be evaluated | Correct the state changes, particle symbols, or charges and revalidate. |
| [`CMP009`](#cmp009) | warning | Step 08 — model compilation pipeline | A transition is not referenced by any simulation and was not compiled | Reference the transition from a simulation or remove it. |
| [`CMP010`](#cmp010) | warning | Step 08 — model compilation pipeline | The advisory rule or table budget is exceeded | Review the model size; the hard compilation limit has not been exceeded. |
| [`CMP011`](#cmp011) | error | Step 08 — model compilation pipeline | The compiled model cannot be signed | Check the compiled model for missing provenance or non-finite values. |
| [`CMP012`](#cmp012) | warning | Step 08 — model compilation pipeline | A cached stage result failed its integrity check and was recomputed | No action is required; the compiler recomputed the affected stage. |
| [`CLI001`](#cli001) | error | Step 12 — build orchestration and CLI | The command line is not valid | Correct the command line; see the command help and the response-file rules in the CLI reference. |
| [`CLI002`](#cli002) | error | Step 12 — build orchestration and CLI | An input or output operation failed | Check the path, permissions, free disk space, and that the requested input exists. |
| [`CLI003`](#cli003) | error | Step 12 — build orchestration and CLI | The output already exists and overwriting was not requested | Choose another output path or pass --force to replace the existing file. |
| [`CLI004`](#cli004) | error | Step 12 — build orchestration and CLI | The build recipe selection is missing, ambiguous, or of the wrong kind | Pass an explicit --template, --job-template, or --parameterization ID, or make the project recipe unique. |
| [`CLI005`](#cli005) | error | Step 12 — build orchestration and CLI | The job selection is malformed, empty, or out of range | Use "all" or a comma-separated list of 1-based job IDs and inclusive ascending ranges. |
| [`CLI006`](#cli006) | info | Step 12 — build orchestration and CLI | The operation was cancelled | No action is required; rerun the command to complete it. |
| [`CLI007`](#cli007) | error | Step 12 — build orchestration and CLI | An unexpected internal error occurred | Set MOCASSIN_DEBUG=1 to print a stack trace and report the failure. |
| [`CLI008`](#cli008) | error | Step 12 — build orchestration and CLI | The command or option is reserved for a later step | Use the command or option in the step named in the message. |
| [`CLI009`](#cli009) | error | Step 12 — build orchestration and CLI | The input cannot be inspected as a simulation package | Run "mocassin verify <package>" for package-specific detail, or rebuild the package. |
| [`ENM001`](#enm001) | error | Step 05 — energy model | An environment cutoff is not positive and finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM002`](#enm002) | warning | Step 05 — energy model | An environment cutoff exceeds half the smallest cell dimension | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM003`](#enm003) | error | Step 05 — energy model | A filter range is inverted or extends beyond the owning environment cutoff | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM004`](#enm004) | error | Step 05 — energy model | A filter references an unknown or identical site, or an unstable filter is not centred on its site | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM005`](#enm005) | warning | Step 05 — energy model | A filter duplicates or redundantly mirrors an earlier filter in the same environment | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM006`](#enm006) | error | Step 05 — energy model | An unstable environment references an unknown or stable site, or duplicates another environment | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM007`](#enm007) | error | Step 05 — energy model | A cluster centre is unknown or an unstable centre has no unstable environment | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM008`](#enm008) | error | Step 05 — energy model | A cluster has fewer than two or more than eight surrounding offsets | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM009`](#enm009) | error | Step 05 — energy model | Two cluster offsets resolve to the same encoded surrounding position | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM010`](#enm010) | error | Step 05 — energy model | A cluster surrounding position is unknown, unstable, filtered, or outside the centre cutoff | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM011`](#enm011) | error | Step 05 — energy model | A cluster full geometry contains a centre-equivalent or otherwise duplicate position | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM012`](#enm012) | warning | Step 05 — energy model | A cluster raw-permutation count exceeds the warning limit | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM013`](#enm013) | error | Step 05 — energy model | A cluster raw-permutation count exceeds the hard limit | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM014`](#enm014) | error | Step 05 — energy model | A site-energy entry references an unknown site or particle, or repeats a site/particle pair | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM015`](#enm015) | error | Step 05 — energy model | A site energy is non-finite or outside the accepted energy range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM016`](#enm016) | warning | Step 05 — energy model | A site energy names a particle the site cannot occupy | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM017`](#enm017) | warning | Step 05 — energy model | An environment sampling estimate is below one or above the warning limit | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM018`](#enm018) | warning | Step 05 — energy model | A KMC transition path leaves the relevant stable or unstable environment cutoff | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM019`](#enm019) | error | Step 05 — energy model | An environment sampling estimate exceeds the hard limit | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM020`](#enm020) | warning | Step 05 — energy model | More than four clusters share one centre site | Review the referenced model object, correct the reported condition, and revalidate. |
| [`ENM021`](#enm021) | error | Step 05 — energy model | More than ten clusters share one centre site | Review the referenced model object, correct the reported condition, and revalidate. |
| [`JOB001`](#job001) | error | Step 10 — job templates and supercells | Template has no collections | Add at least one collection with one configuration. |
| [`JOB002`](#job002) | error | Step 10 — job templates and supercells | Collection references an unknown simulation | Reference an existing simulation of the matching kind. |
| [`JOB003`](#job003) | error | Step 10 — job templates and supercells | Collection has no configurations | Add at least one configuration to the collection. |
| [`JOB004`](#job004) | error | Step 10 — job templates and supercells | Cell size outside 1–256, or total positions > 500,000 | Use cell counts in 1..256 and keep the generated supercell within 500,000 positions. |
| [`JOB005`](#job005) | error | Step 10 — job templates and supercells | Doping fraction is non-finite or outside [0,1] | Use a finite doping fraction in the inclusive range 0..1. |
| [`JOB006`](#job006) | error | Step 10 — job templates and supercells | Doping reference unknown | Reference an existing doping definition. |
| [`JOB007`](#job007) | error | Step 10 — job templates and supercells | Duplicate collection/config name | Give every collection and every configuration a unique, non-empty name. |
| [`JOB008`](#job008) | error | Step 10 — job templates and supercells | Job count outside 1–1000 | Use a job count in 1..1000. |
| [`JOB009`](#job009) | error | Step 10 — job templates and supercells | Optimizer reference invalid or removes every usable start option for a required mechanism | Use a stable site and a particle allowed at that site, and keep a usable start option. |
| [`JOB010`](#job010) | warning | Step 10 — job templates and supercells | Requested doping cannot be realized; 0 applied | Increase the available source population or lower the requested fraction. |
| [`JOB011`](#job011) | warning | Step 10 — job templates and supercells | Effective doping fraction differs from requested | Review the requested fraction; applied counts are truncated downward. |
| [`JOB012`](#job012) | error | Step 10 — job templates and supercells | Compensation pair cannot be realized, or an applied compensated pair is not charge-balanced | Choose dopings, charges, and fractions that admit an integer balanced pair. |
| [`JOB013`](#job013) | warning | Step 10 — job templates and supercells | Total jobs exceed 10,000 | Reduce the configurations or jobs per configuration unless the large run is intentional. |
| [`JOB014`](#job014) | warning | Step 10 — job templates and supercells | Supercell positions exceed 100,000 | Reduce the cell size unless the large supercell is intentional. |
| [`JOB015`](#job015) | error | Step 10 — job templates and supercells | Inherited settings are missing, invalid, or not applicable to the simulation kind | Set finite values within the documented ranges and use only fields for the simulation kind. |
| [`JOB016`](#job016) | warning | Step 10 — job templates and supercells | Final lattice is not charge-neutral because an uncompensated charged doping was applied | Enable charge compensation or accept the non-neutral lattice knowingly. |
| [`JOB017`](#job017) | error | Step 10 — job templates and supercells | Duplicate doping reference or optimizer entry | Remove duplicate doping references and duplicate (site, particle) optimizers. |
| [`JOB018`](#job018) | error | Step 10 — job templates and supercells | Extension id is empty/duplicated, config is not an object, or an extension bound is exceeded | Use a unique non-empty id (at most 64 characters), an object config of at most 64 KiB, at most 16 entries, and at most 256 KiB in total. |
| [`JOB019`](#job019) | error | Step 10 — job templates and supercells | Total expanded jobs exceed 99,999 | Reduce the template to at most 99,999 expanded jobs. |
| [`JOB020`](#job020) | error | Step 10 — job templates and supercells | Template/collection/config/optimizer/doping count exceeds a structural cap | Reduce the template to the documented Step 10 structural caps. |
| [`LAT001`](#lat001) | error | Step 06 — lattice and simulation models | An occupation exchange references an unknown site or particle | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT002`](#lat002) | error | Step 06 — lattice and simulation models | An occupation exchange references an unstable site | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT003`](#lat003) | error | Step 06 — lattice and simulation models | An occupation-exchange source particle is not in the site's particle set | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT004`](#lat004) | error | Step 06 — lattice and simulation models | An occupation-exchange target particle is not in the site's particle set | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT005`](#lat005) | error | Step 06 — lattice and simulation models | An occupation exchange uses the same source and target particle | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT006`](#lat006) | error | Step 06 — lattice and simulation models | A doping priority is used more than once | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT007`](#lat007) | error | Step 06 — lattice and simulation models | A doping references an unknown occupation exchange | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT008`](#lat008) | error | Step 06 — lattice and simulation models | The dependent exchange and chargeCompensation disagree | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT009`](#lat009) | error | Step 06 — lattice and simulation models | A compensating dependent exchange has a zero or same-sign charge delta | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT010`](#lat010) | error | Step 06 — lattice and simulation models | A doping priority is outside 0..9999 | Review the referenced model object, correct the reported condition, and revalidate. |
| [`LAT011`](#lat011) | error | Step 06 — lattice and simulation models | An occupation-exchange or doping name is empty or duplicated case-insensitively | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR001`](#par001) | error | Step 03 — particle and structure models | A particle name is empty or used more than once (case-insensitive) | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR002`](#par002) | error | Step 03 — particle and structure models | The model defines more than the maximum number of authored particles | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR004`](#par004) | warning | Step 03 — particle and structure models | A vacancy and its default occupation are both charged | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR005`](#par005) | error | Step 03 — particle and structure models | A particle set is empty or contains the implicit void particle | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR006`](#par006) | error | Step 03 — particle and structure models | A particle set references a particle that does not exist | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR007`](#par007) | error | Step 03 — particle and structure models | The model defines more than the maximum number of authored particle sets | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR008`](#par008) | warning | Step 03 — particle and structure models | A particle symbol is empty, too long, or does not match the recommended pattern | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR009`](#par009) | warning | Step 03 — particle and structure models | Two authored particle sets have the same ordered particle list | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR010`](#par010) | error | Step 03 — particle and structure models | An authored object reuses a reserved implicit-object identifier | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR011`](#par011) | error | Step 03 — particle and structure models | A particle-set name is empty or used more than once (case-insensitive) | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PAR012`](#par012) | error | Step 03 — particle and structure models | A particle set contains the same particle identifier more than once | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PKG001`](#pkg001) | error | Step 11 — simulation package | The package is not a readable SQLite database | Rebuild the package and confirm that the file is a published .msl package; run SQLite integrity_check. |
| [`PKG002`](#pkg002) | error | Step 11 — simulation package | The package schema or format version is missing or unsupported | Rebuild the package with a compatible mocassin release. |
| [`PKG003`](#pkg003) | error | Step 11 — simulation package | A required schema element is missing or malformed | Rebuild the package; a published package must contain every required table, index, column, and schema-info row. |
| [`PKG004`](#pkg004) | error | Step 11 — simulation package | A package payload cannot be decoded | Rebuild the package. The payload must be a standard Zstd frame containing a MessagePack map within its size bound. |
| [`PKG005`](#pkg005) | error | Step 11 — simulation package | A stored signature does not match its payload projection | Rebuild the package with the same compiler and parameterization; the stored payload is inconsistent. |
| [`PKG006`](#pkg006) | error | Step 11 — simulation package | A package reference or identifier is invalid | Rebuild the package and confirm unique ascending job ids and valid model/lattice foreign keys. |
| [`PKG007`](#pkg007) | error | Step 11 — simulation package | A payload-derived job column disagrees with the payload | Rebuild the package; the denormalized columns must be copied from the resolved job payload. |
| [`PKG008`](#pkg008) | error | Step 11 — simulation package | The package is not immutable | Publish results to a separate .msr store and republish the package without a results table or WAL sidecar. |
| [`PKG009`](#pkg009) | warning | Step 11 — simulation package | The package exceeds the advisory size limit | Reduce duplicate jobs or supercell sizes, or rebuild with --compact compression. |
| [`PKG010`](#pkg010) | error | Step 11 — simulation package | A package payload value is out of bounds | Rebuild the package; table lengths, indices, tracker order, and mappings must match the declared dimensions. |
| [`PRM001`](#prm001) | error | Step 09 — parameterization templates | A template model signature does not match the compiled model | Rebase or recreate the template against the current compiled model. |
| [`PRM002`](#prm002) | warning | Step 09 — parameterization templates | A template entry references an unknown key or disagrees with the current model | Remove the stale entry or rebase the template against the current model. |
| [`PRM003`](#prm003) | warning | Step 09 — parameterization templates | Required pair, group, or kinetic-rule values are unset and defaults are applied | Set the missing values explicitly, or review the applied defaults before building. |
| [`PRM004`](#prm004) | error | Step 09 — parameterization templates | An energy is non-finite or outside the accepted range | Set the energy to a finite value within the accepted -100..100 eV range. |
| [`PRM005`](#prm005) | error | Step 09 — parameterization templates | A frequency is non-positive, non-finite, or too large for the canonical projection | Set the attempt frequency to a positive, finite value that survives 12-decimal quantization. |
| [`PRM006`](#prm006) | warning | Step 09 — parameterization templates | An explicit template contains no explicit values | Set at least one value, or build with the implicit default parameterization. |
| [`PRM008`](#prm008) | warning | Step 09 — parameterization templates | A site-energy value targets an unknown or forbidden site/particle | Correct the site/particle key or remove the site-energy override. |
| [`PRM009`](#prm009) | info | Step 09 — parameterization templates | A parameterization template was rebased against the current compiled model | Review every copied, defaulted, dropped, and ambiguous value before saving the rebased template. |
| [`PRM010`](#prm010) | warning | Step 09 — parameterization templates | Two parameterization templates use the same name | Give each parameterization template a distinct name. |
| [`PRM011`](#prm011) | warning | Step 09 — parameterization templates | A Metropolis transition rule set is not parameterizable and is ignored | Remove the Metropolis rule set; MMC rules are not parameterizable. |
| [`PRM012`](#prm012) | warning | Step 09 — parameterization templates | A pair or group entry names a particle the reference site cannot occupy | Use a particle the reference site can occupy, or remove the entry. |
| [`PRM013`](#prm013) | error | Step 09 — parameterization templates | A template resource limit is exceeded | Reduce the template size or split the work into smaller templates. |
| [`PRM014`](#prm014) | error | Step 09 — parameterization templates | A stable key is malformed, duplicated, or missing | Use lowercase 64-character hexadecimal stable keys and remove duplicate value keys. |
| [`PRM015`](#prm015) | warning | Step 09 — parameterization templates | Explicit pair or group values are suspiciously uniform | Check for a bulk-fill or paste error and vary the reported values deliberately. |
| [`PRM016`](#prm016) | warning | Step 09 — parameterization templates | An attempt frequency is outside the plausible band but still positive and finite | Check the attempt frequency; the accepted physical range is 1e-3..1e18 Hz. |
| [`PROJ001`](#proj001) | error | Step 01 — project format | The document declares a schema version newer than this application supports | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ002`](#proj002) | error | Step 01 — project format | The document is malformed or cannot be parsed as a project | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ003`](#proj003) | error | Step 01 — project format | Two objects in the same domain share an identifier | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ010`](#proj010) | error | Step 01 — project format | A reference points to an identifier that does not exist | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ011`](#proj011) | error | Step 01 — project format | A reference points to an identifier that exists in a different domain | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ020`](#proj020) | error | Step 01 — project format | Project metadata is missing or invalid | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ021`](#proj021) | error | Step 01 — project format | View settings are invalid or incomplete | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ030`](#proj030) | error | Step 01 — project format | A file save failed; the previous file was left intact | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ031`](#proj031) | error | Step 01 — project format | Another process holds the project file lock | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ032`](#proj032) | warning | Step 01 — project format | A stale project file lock was reclaimed | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ033`](#proj033) | error | Step 01 — project format | An autosave recovery file operation failed | Review the referenced model object, correct the reported condition, and revalidate. |
| [`PROJ034`](#proj034) | warning | Step 01 — project format | Project symmetry metadata differs from the embedded catalog or contract versions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM001`](#sim001) | error | Step 06 — lattice and simulation models | A simulation references an unknown transition | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM002`](#sim002) | error | Step 06 — lattice and simulation models | A simulation references a transition of the other kind | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM003`](#sim003) | warning | Step 06 — lattice and simulation models | A simulation has no transitions and cannot be built until Step 07/10 feasibility passes | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM004`](#sim004) | error | Step 06 — lattice and simulation models | Temperature is non-finite or outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM005`](#sim005) | error | Step 06 — lattice and simulation models | Target MCSP is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM006`](#sim006) | error | Step 06 — lattice and simulation models | Job count is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM007`](#sim007) | error | Step 06 — lattice and simulation models | The KMC field direction is non-finite or zero with a non-zero modulus | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM008`](#sim008) | error | Step 06 — lattice and simulation models | KMC pre-run MCSP is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM009`](#sim009) | error | Step 06 — lattice and simulation models | A simulation name is empty, whitespace-only, or duplicated | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM010`](#sim010) | error | Step 06 — lattice and simulation models | Write-out count is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM011`](#sim011) | error | Step 06 — lattice and simulation models | The wall-clock time limit is present but not positive | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM012`](#sim012) | error | Step 06 — lattice and simulation models | The minimum success rate is negative, non-finite, or above  | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM013`](#sim013) | error | Step 06 — lattice and simulation models | The KMC field modulus is negative or non-finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM014`](#sim014) | error | Step 06 — lattice and simulation models | The KMC normalization energy is negative or non-finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM015`](#sim015) | error | Step 06 — lattice and simulation models | The KMC maximum attempt frequency is zero, negative, or non-finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM016`](#sim016) | error | Step 06 — lattice and simulation models | MMC break tolerance is non-finite or outside 0..1 | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM017`](#sim017) | error | Step 06 — lattice and simulation models | MMC break sample length is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM018`](#sim018) | error | Step 06 — lattice and simulation models | MMC break sample interval is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM019`](#sim019) | error | Step 06 — lattice and simulation models | MMC result sample is outside the hard range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM020`](#sim020) | error | Step 06 — lattice and simulation models | The RNG seed is longer than 100 characters | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM021`](#sim021) | error | Step 06 — lattice and simulation models | A simulation references more than 100 transitions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM022`](#sim022) | error | Step 06 — lattice and simulation models | A simulation references the same transition more than once | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM023`](#sim023) | error | Step 06 — lattice and simulation models | The persisted kind-specific shape disagrees with the concrete simulation record | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM024`](#sim024) | warning | Step 06 — lattice and simulation models | Temperature is outside the recommended band | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM025`](#sim025) | warning | Step 06 — lattice and simulation models | Target MCSP is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM026`](#sim026) | warning | Step 06 — lattice and simulation models | Job count is outside the recommended band | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM027`](#sim027) | warning | Step 06 — lattice and simulation models | KMC pre-run MCSP is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM028`](#sim028) | warning | Step 06 — lattice and simulation models | Write-out count is outside the recommended band; 0 is allowed | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM029`](#sim029) | warning | Step 06 — lattice and simulation models | The KMC field modulus is outside the recommended band | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM030`](#sim030) | warning | Step 06 — lattice and simulation models | MMC break tolerance is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM031`](#sim031) | warning | Step 06 — lattice and simulation models | MMC break sample length is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM032`](#sim032) | warning | Step 06 — lattice and simulation models | MMC break sample interval is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM033`](#sim033) | warning | Step 06 — lattice and simulation models | MMC result sample is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM034`](#sim034) | warning | Step 06 — lattice and simulation models | KMC normalization energy is above the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM035`](#sim035) | error | Step 06 — lattice and simulation models | The project execution-flag value contains a duplicate, derived, or compiler-owned flag | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM036`](#sim036) | warning | Step 06 — lattice and simulation models | A simulation references 11 to 100 transitions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM037`](#sim037) | warning | Step 06 — lattice and simulation models | The minimum success rate is positive but below the recommended value | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SIM038`](#sim038) | warning | Step 07 — validation and diagnostics | A KMC simulation with transitions has no mobile start particle | Add a mobile particle to the simulation's start pool or remove the simulation. |
| [`SIM039`](#sim039) | warning | Step 07 — validation and diagnostics | No particle is mobile in any KMC simulation | Make at least one particle mobile in a KMC simulation so the model can start. |
| [`SIM040`](#sim040) | error | Step 07 — validation and diagnostics | The estimated compiled rule count exceeds the advisory budget | Reduce the transition or particle-set count, or raise MaxEstimatedRuleCount knowingly. |
| [`SIM041`](#sim041) | error | Step 07 — validation and diagnostics | The estimated energy-table memory exceeds the advisory budget | Reduce clusters or table sizes, or raise MaxEstimatedMemoryBytes knowingly. |
| [`STR001`](#str001) | error | Step 02 — symmetry and geometry | The structured space-group reference does not identify a catalog setting | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR002`](#str002) | error | Step 02 — symmetry and geometry | A cell length is outside the supported range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR003`](#str003) | error | Step 02 — symmetry and geometry | Cell angles, determinant, conditioning, or crystal-system constraints are invalid | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR004`](#str004) | error | Step 03 — particle and structure models | A reference site lies outside [0,1) beyond the fractional trim tolerance, or is not finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR005`](#str005) | error | Step 03 — particle and structure models | A site name is empty or used more than once (case-insensitive) | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR006`](#str006) | error | Step 03 — particle and structure models | A site has no particle set, or uses the reserved empty particle-set identifier | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR007`](#str007) | error | Step 02 — symmetry and geometry | Two reference sites expand to the same P1 orbit position | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR009`](#str009) | warning | Step 02 — symmetry and geometry | The total P1 position count exceeds the performance budget | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR010`](#str010) | error | Step 03 — particle and structure models | A dummy position is outside [0,1) beyond the fractional trim tolerance, or is not finite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR012`](#str012) | error | Step 03 — particle and structure models | The structure defines more than the maximum number of reference sites | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR013`](#str013) | error | Step 03 — particle and structure models | The structure name is empty or whitespace-only | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR014`](#str014) | error | Step 02 — symmetry and geometry | The requested supercell exceeds the signed 32-bit linear-index limit | Review the referenced model object, correct the reported condition, and revalidate. |
| [`STR015`](#str015) | error | Step 03 — particle and structure models | A dummy-position name is empty or used more than once (case-insensitive) | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SYM001`](#sym001) | error | Step 02 — symmetry and geometry | A fractional coordinate was NaN or infinite | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SYM002`](#sym002) | warning | Step 02 — symmetry and geometry | A fractional coordinate was rounded to the nearest exact-grid point | Review the referenced model object, correct the reported condition, and revalidate. |
| [`SYM003`](#sym003) | error | Step 02 — symmetry and geometry | A fractional coordinate is outside the representable exact-grid range | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN001`](#trn001) | error | Step 04 — transition model | A state change references an unknown particle | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN002`](#trn002) | error | Step 04 — transition model | A self state change is not at an interior stable spectator position | Use a self state change only at an interior stable spectator position with unstable neighbours. |
| [`TRN003`](#trn003) | error | Step 04 — transition model | Both donor and acceptor are the implicit void particle | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN004`](#trn004) | warning | Step 04 — transition model | A void donor is replaced by a charged acceptor, which is likely an implicit reverse pair | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN005`](#trn005) | error | Step 04 — transition model | A state-change group is empty or references an unknown state change | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN006`](#trn006) | error | Step 04 — transition model | A state-change group mixes physical exchanges with charge-state exchanges | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN007`](#trn007) | error | Step 04 — transition model | A chain's group count and linker count do not satisfy linkers = groups - 1 | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN008`](#trn008) | error | Step 04 — transition model | A chain pattern has the wrong number of positions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN009`](#trn009) | error | Step 04 — transition model | A chain's linker sequence is invalid for its pattern or for the custom grammar | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN010`](#trn010) | error | Step 04 — transition model | A kinetic reference path has a different length than its chain | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN011`](#trn011) | error | Step 04 — transition model | A kinetic reference path endpoint is not stable | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN012`](#trn012) | error | Step 04 — transition model | A kinetic path position's group stability does not match the resolved site stability | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN013`](#trn013) | error | Step 04 — transition model | A kinetic reference path contains consecutive unstable positions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN014`](#trn014) | error | Step 04 — transition model | A kinetic reference path contains a repeated full encoded position | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN015`](#trn015) | error | Step 04 — transition model | A Metropolis transition does not use a Metropolis-compatible chain | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN016`](#trn016) | error | Step 04 — transition model | A Metropolis site reference is unknown, unstable, or otherwise invalid | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN017`](#trn017) | error | Step 04 — transition model | A name is empty or duplicated within its own named collection | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN018`](#trn018) | warning | Step 04 — transition model | A complex transition has no explicitly defined reverse link | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN019`](#trn019) | error | Step 04 — transition model | The model defines more than the supported number of transitions | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN020`](#trn020) | error | Step 04 — transition model | A kinetic path point does not match exactly one P1 position and owning reference site | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN021`](#trn021) | error | Step 04 — transition model | A chain references an unknown state-change group | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN022`](#trn022) | error | Step 04 — transition model | A kinetic chain has no unstable position | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN023`](#trn023) | error | Step 04 — transition model | A kinetic transition uses a Metropolis or two-position custom chain | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN024`](#trn024) | error | Step 04 — transition model | A chain's generatesReverse flag disagrees with its pattern | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN025`](#trn025) | error | Step 04 — transition model | A state-change group mixes stable-role and unstable-role state changes | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN026`](#trn026) | error | Step 04 — transition model | An unstable position's group does not contain exactly one [D, ∅] state change | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN027`](#trn027) | error | Step 04 — transition model | A reverse link is unknown, cross-kind, asymmetric, or self-referential | Review the referenced model object, correct the reported condition, and revalidate. |
| [`TRN028`](#trn028) | error | Step 04 — transition model | A resolved kinetic path stability sequence does not match the built-in pattern sequence | Review the referenced model object, correct the reported condition, and revalidate. |

## Codes

### CMP001

**A required compilation precondition failed.** Correct the referenced model object or the compiler input, then compile again.

### CMP002

**The structure cannot be encoded or P1-expanded for compilation.** Correct the structure, space group, cell, or site coordinates and revalidate.

### CMP003

**Pair or cluster geometry cannot be resolved during compilation.** Correct the environment, cluster, or site geometry and revalidate.

### CMP004

**Two rules in one collection share a start state but differ in outcome.** Split or correct the overlapping transition rules so one start state has one outcome.

### CMP005

**A transition produces no valid rules after conservation filtering.** Check the state changes, particle symbols, and total charge for mass and charge conservation.

### CMP006

**A configured compilation limit is exceeded.** Reduce the model size or raise the documented compiler limit knowingly.

### CMP007

**A transition path cannot be expanded into compiled directions.** Place every path point on a canonical P1 position and revalidate the transition.

### CMP008

**Mass or charge conservation cannot be evaluated.** Correct the state changes, particle symbols, or charges and revalidate.

### CMP009

**A transition is not referenced by any simulation and was not compiled.** Reference the transition from a simulation or remove it.

### CMP010

**The advisory rule or table budget is exceeded.** Review the model size; the hard compilation limit has not been exceeded.

### CMP011

**The compiled model cannot be signed.** Check the compiled model for missing provenance or non-finite values.

### CMP012

**A cached stage result failed its integrity check and was recomputed.** No action is required; the compiler recomputed the affected stage.

### CLI001

**The command line is not valid.** Correct the command line; see the command help and the response-file rules in the CLI reference.

### CLI002

**An input or output operation failed.** Check the path, permissions, free disk space, and that the requested input exists.

### CLI003

**The output already exists and overwriting was not requested.** Choose another output path or pass --force to replace the existing file.

### CLI004

**The build recipe selection is missing, ambiguous, or of the wrong kind.** Pass an explicit --template, --job-template, or --parameterization ID, or make the project recipe unique.

### CLI005

**The job selection is malformed, empty, or out of range.** Use "all" or a comma-separated list of 1-based job IDs and inclusive ascending ranges.

### CLI006

**The operation was cancelled.** No action is required; rerun the command to complete it.

### CLI007

**An unexpected internal error occurred.** Set MOCASSIN_DEBUG=1 to print a stack trace and report the failure.

### CLI008

**The command or option is reserved for a later step.** Use the command or option in the step named in the message.

### CLI009

**The input cannot be inspected as a simulation package.** Run "mocassin verify <package>" for package-specific detail, or rebuild the package.

### ENM001

**An environment cutoff is not positive and finite.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM002

**An environment cutoff exceeds half the smallest cell dimension.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM003

**A filter range is inverted or extends beyond the owning environment cutoff.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM004

**A filter references an unknown or identical site, or an unstable filter is not centred on its site.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM005

**A filter duplicates or redundantly mirrors an earlier filter in the same environment.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM006

**An unstable environment references an unknown or stable site, or duplicates another environment.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM007

**A cluster centre is unknown or an unstable centre has no unstable environment.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM008

**A cluster has fewer than two or more than eight surrounding offsets.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM009

**Two cluster offsets resolve to the same encoded surrounding position.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM010

**A cluster surrounding position is unknown, unstable, filtered, or outside the centre cutoff.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM011

**A cluster full geometry contains a centre-equivalent or otherwise duplicate position.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM012

**A cluster raw-permutation count exceeds the warning limit.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM013

**A cluster raw-permutation count exceeds the hard limit.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM014

**A site-energy entry references an unknown site or particle, or repeats a site/particle pair.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM015

**A site energy is non-finite or outside the accepted energy range.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM016

**A site energy names a particle the site cannot occupy.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM017

**An environment sampling estimate is below one or above the warning limit.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM018

**A KMC transition path leaves the relevant stable or unstable environment cutoff.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM019

**An environment sampling estimate exceeds the hard limit.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM020

**More than four clusters share one centre site.** Review the referenced model object, correct the reported condition, and revalidate.

### ENM021

**More than ten clusters share one centre site.** Review the referenced model object, correct the reported condition, and revalidate.

### JOB001

**Template has no collections.** Add at least one collection with one configuration.

### JOB002

**Collection references an unknown simulation.** Reference an existing simulation of the matching kind.

### JOB003

**Collection has no configurations.** Add at least one configuration to the collection.

### JOB004

**Cell size outside 1–256, or total positions > 500,000.** Use cell counts in 1..256 and keep the generated supercell within 500,000 positions.

### JOB005

**Doping fraction is non-finite or outside [0,1].** Use a finite doping fraction in the inclusive range 0..1.

### JOB006

**Doping reference unknown.** Reference an existing doping definition.

### JOB007

**Duplicate collection/config name.** Give every collection and every configuration a unique, non-empty name.

### JOB008

**Job count outside 1–1000.** Use a job count in 1..1000.

### JOB009

**Optimizer reference invalid or removes every usable start option for a required mechanism.** Use a stable site and a particle allowed at that site, and keep a usable start option.

### JOB010

**Requested doping cannot be realized; 0 applied.** Increase the available source population or lower the requested fraction.

### JOB011

**Effective doping fraction differs from requested.** Review the requested fraction; applied counts are truncated downward.

### JOB012

**Compensation pair cannot be realized, or an applied compensated pair is not charge-balanced.** Choose dopings, charges, and fractions that admit an integer balanced pair.

### JOB013

**Total jobs exceed 10,000.** Reduce the configurations or jobs per configuration unless the large run is intentional.

### JOB014

**Supercell positions exceed 100,000.** Reduce the cell size unless the large supercell is intentional.

### JOB015

**Inherited settings are missing, invalid, or not applicable to the simulation kind.** Set finite values within the documented ranges and use only fields for the simulation kind.

### JOB016

**Final lattice is not charge-neutral because an uncompensated charged doping was applied.** Enable charge compensation or accept the non-neutral lattice knowingly.

### JOB017

**Duplicate doping reference or optimizer entry.** Remove duplicate doping references and duplicate (site, particle) optimizers.

### JOB018

**Extension id is empty/duplicated, config is not an object, or an extension bound is exceeded.** Use a unique non-empty id (at most 64 characters), an object config of at most 64 KiB, at most 16 entries, and at most 256 KiB in total.

### JOB019

**Total expanded jobs exceed 99,999.** Reduce the template to at most 99,999 expanded jobs.

### JOB020

**Template/collection/config/optimizer/doping count exceeds a structural cap.** Reduce the template to the documented Step 10 structural caps.

### LAT001

**An occupation exchange references an unknown site or particle.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT002

**An occupation exchange references an unstable site.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT003

**An occupation-exchange source particle is not in the site's particle set.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT004

**An occupation-exchange target particle is not in the site's particle set.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT005

**An occupation exchange uses the same source and target particle.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT006

**A doping priority is used more than once.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT007

**A doping references an unknown occupation exchange.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT008

**The dependent exchange and chargeCompensation disagree.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT009

**A compensating dependent exchange has a zero or same-sign charge delta.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT010

**A doping priority is outside 0..9999.** Review the referenced model object, correct the reported condition, and revalidate.

### LAT011

**An occupation-exchange or doping name is empty or duplicated case-insensitively.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR001

**A particle name is empty or used more than once (case-insensitive).** Review the referenced model object, correct the reported condition, and revalidate.

### PAR002

**The model defines more than the maximum number of authored particles.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR004

**A vacancy and its default occupation are both charged.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR005

**A particle set is empty or contains the implicit void particle.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR006

**A particle set references a particle that does not exist.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR007

**The model defines more than the maximum number of authored particle sets.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR008

**A particle symbol is empty, too long, or does not match the recommended pattern.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR009

**Two authored particle sets have the same ordered particle list.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR010

**An authored object reuses a reserved implicit-object identifier.** Review the referenced model object, correct the reported condition, and revalidate.

### PAR011

**A particle-set name is empty or used more than once (case-insensitive).** Review the referenced model object, correct the reported condition, and revalidate.

### PAR012

**A particle set contains the same particle identifier more than once.** Review the referenced model object, correct the reported condition, and revalidate.

### PKG001

**The package is not a readable SQLite database.** Rebuild the package and confirm that the file is a published .msl package; run SQLite integrity_check.

### PKG002

**The package schema or format version is missing or unsupported.** Rebuild the package with a compatible mocassin release.

### PKG003

**A required schema element is missing or malformed.** Rebuild the package; a published package must contain every required table, index, column, and schema-info row.

### PKG004

**A package payload cannot be decoded.** Rebuild the package. The payload must be a standard Zstd frame containing a MessagePack map within its size bound.

### PKG005

**A stored signature does not match its payload projection.** Rebuild the package with the same compiler and parameterization; the stored payload is inconsistent.

### PKG006

**A package reference or identifier is invalid.** Rebuild the package and confirm unique ascending job ids and valid model/lattice foreign keys.

### PKG007

**A payload-derived job column disagrees with the payload.** Rebuild the package; the denormalized columns must be copied from the resolved job payload.

### PKG008

**The package is not immutable.** Publish results to a separate .msr store and republish the package without a results table or WAL sidecar.

### PKG009

**The package exceeds the advisory size limit.** Reduce duplicate jobs or supercell sizes, or rebuild with --compact compression.

### PKG010

**A package payload value is out of bounds.** Rebuild the package; table lengths, indices, tracker order, and mappings must match the declared dimensions.

### PRM001

**A template model signature does not match the compiled model.** Rebase or recreate the template against the current compiled model.

### PRM002

**A template entry references an unknown key or disagrees with the current model.** Remove the stale entry or rebase the template against the current model.

### PRM003

**Required pair, group, or kinetic-rule values are unset and defaults are applied.** Set the missing values explicitly, or review the applied defaults before building.

### PRM004

**An energy is non-finite or outside the accepted range.** Set the energy to a finite value within the accepted -100..100 eV range.

### PRM005

**A frequency is non-positive, non-finite, or too large for the canonical projection.** Set the attempt frequency to a positive, finite value that survives 12-decimal quantization. A finite value whose twelve-decimal scaling overflows is rejected before the signature is computed.

### PRM006

**An explicit template contains no explicit values.** Set at least one value, or build with the implicit default parameterization.

### PRM008

**A site-energy value targets an unknown or forbidden site/particle.** Correct the site/particle key or remove the site-energy override.

### PRM009

**A parameterization template was rebased against the current compiled model.** Review every copied, defaulted, dropped, and ambiguous value before saving the rebased template.

### PRM010

**Two parameterization templates use the same name.** Give each parameterization template a distinct name.

### PRM011

**A Metropolis transition rule set is not parameterizable and is ignored.** Remove the Metropolis rule set; MMC rules are not parameterizable.

### PRM012

**A pair or group entry names a particle the reference site cannot occupy.** Use a particle the reference site can occupy, or remove the entry.

### PRM013

**A template resource limit is exceeded.** Reduce the template size or split the work into smaller templates.

### PRM014

**A stable key is malformed, duplicated, or missing.** Use lowercase 64-character hexadecimal stable keys and remove duplicate value keys.

### PRM015

**Explicit pair or group values are suspiciously uniform.** Check for a bulk-fill or paste error and vary the reported values deliberately.

### PRM016

**An attempt frequency is outside the plausible band but still positive and finite.** Check the attempt frequency; the accepted physical range is 1e-3..1e18 Hz.

### PROJ001

**The document declares a schema version newer than this application supports.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ002

**The document is malformed or cannot be parsed as a project.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ003

**Two objects in the same domain share an identifier.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ010

**A reference points to an identifier that does not exist.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ011

**A reference points to an identifier that exists in a different domain.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ020

**Project metadata is missing or invalid.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ021

**View settings are invalid or incomplete.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ030

**A file save failed; the previous file was left intact.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ031

**Another process holds the project file lock.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ032

**A stale project file lock was reclaimed.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ033

**An autosave recovery file operation failed.** Review the referenced model object, correct the reported condition, and revalidate.

### PROJ034

**Project symmetry metadata differs from the embedded catalog or contract versions.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM001

**A simulation references an unknown transition.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM002

**A simulation references a transition of the other kind.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM003

**A simulation has no transitions and cannot be built until Step 07/10 feasibility passes.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM004

**Temperature is non-finite or outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM005

**Target MCSP is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM006

**Job count is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM007

**The KMC field direction is non-finite or zero with a non-zero modulus.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM008

**KMC pre-run MCSP is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM009

**A simulation name is empty, whitespace-only, or duplicated.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM010

**Write-out count is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM011

**The wall-clock time limit is present but not positive.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM012

**The minimum success rate is negative, non-finite, or above .** Review the referenced model object, correct the reported condition, and revalidate.

### SIM013

**The KMC field modulus is negative or non-finite.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM014

**The KMC normalization energy is negative or non-finite.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM015

**The KMC maximum attempt frequency is zero, negative, or non-finite.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM016

**MMC break tolerance is non-finite or outside 0..1.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM017

**MMC break sample length is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM018

**MMC break sample interval is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM019

**MMC result sample is outside the hard range.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM020

**The RNG seed is longer than 100 characters.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM021

**A simulation references more than 100 transitions.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM022

**A simulation references the same transition more than once.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM023

**The persisted kind-specific shape disagrees with the concrete simulation record.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM024

**Temperature is outside the recommended band.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM025

**Target MCSP is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM026

**Job count is outside the recommended band.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM027

**KMC pre-run MCSP is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM028

**Write-out count is outside the recommended band; 0 is allowed.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM029

**The KMC field modulus is outside the recommended band.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM030

**MMC break tolerance is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM031

**MMC break sample length is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM032

**MMC break sample interval is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM033

**MMC result sample is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM034

**KMC normalization energy is above the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM035

**The project execution-flag value contains a duplicate, derived, or compiler-owned flag.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM036

**A simulation references 11 to 100 transitions.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM037

**The minimum success rate is positive but below the recommended value.** Review the referenced model object, correct the reported condition, and revalidate.

### SIM038

**A KMC simulation with transitions has no mobile start particle.** Add a mobile particle to the simulation's start pool or remove the simulation.

### SIM039

**No particle is mobile in any KMC simulation.** Make at least one particle mobile in a KMC simulation so the model can start.

### SIM040

**The estimated compiled rule count exceeds the advisory budget.** Reduce the transition or particle-set count, or raise MaxEstimatedRuleCount knowingly.

### SIM041

**The estimated energy-table memory exceeds the advisory budget.** Reduce clusters or table sizes, or raise MaxEstimatedMemoryBytes knowingly.

### STR001

**The structured space-group reference does not identify a catalog setting.** Review the referenced model object, correct the reported condition, and revalidate.

### STR002

**A cell length is outside the supported range.** Review the referenced model object, correct the reported condition, and revalidate.

### STR003

**Cell angles, determinant, conditioning, or crystal-system constraints are invalid.** Review the referenced model object, correct the reported condition, and revalidate.

### STR004

**A reference site lies outside [0,1) beyond the fractional trim tolerance, or is not finite.** Review the referenced model object, correct the reported condition, and revalidate.

### STR005

**A site name is empty or used more than once (case-insensitive).** Review the referenced model object, correct the reported condition, and revalidate.

### STR006

**A site has no particle set, or uses the reserved empty particle-set identifier.** Review the referenced model object, correct the reported condition, and revalidate.

### STR007

**Two reference sites expand to the same P1 orbit position.** Review the referenced model object, correct the reported condition, and revalidate.

### STR009

**The total P1 position count exceeds the performance budget.** Review the referenced model object, correct the reported condition, and revalidate.

### STR010

**A dummy position is outside [0,1) beyond the fractional trim tolerance, or is not finite.** Review the referenced model object, correct the reported condition, and revalidate.

### STR012

**The structure defines more than the maximum number of reference sites.** Review the referenced model object, correct the reported condition, and revalidate.

### STR013

**The structure name is empty or whitespace-only.** Review the referenced model object, correct the reported condition, and revalidate.

### STR014

**The requested supercell exceeds the signed 32-bit linear-index limit.** Review the referenced model object, correct the reported condition, and revalidate.

### STR015

**A dummy-position name is empty or used more than once (case-insensitive).** Review the referenced model object, correct the reported condition, and revalidate.

### SYM001

**A fractional coordinate was NaN or infinite.** Review the referenced model object, correct the reported condition, and revalidate.

### SYM002

**A fractional coordinate was rounded to the nearest exact-grid point.** Review the referenced model object, correct the reported condition, and revalidate.

### SYM003

**A fractional coordinate is outside the representable exact-grid range.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN001

**A state change references an unknown particle.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN002

**A self state change is not at an interior stable spectator position.** Use a self state change only at an interior stable spectator position with unstable neighbours.

### TRN003

**Both donor and acceptor are the implicit void particle.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN004

**A void donor is replaced by a charged acceptor, which is likely an implicit reverse pair.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN005

**A state-change group is empty or references an unknown state change.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN006

**A state-change group mixes physical exchanges with charge-state exchanges.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN007

**A chain's group count and linker count do not satisfy linkers = groups - 1.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN008

**A chain pattern has the wrong number of positions.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN009

**A chain's linker sequence is invalid for its pattern or for the custom grammar.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN010

**A kinetic reference path has a different length than its chain.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN011

**A kinetic reference path endpoint is not stable.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN012

**A kinetic path position's group stability does not match the resolved site stability.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN013

**A kinetic reference path contains consecutive unstable positions.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN014

**A kinetic reference path contains a repeated full encoded position.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN015

**A Metropolis transition does not use a Metropolis-compatible chain.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN016

**A Metropolis site reference is unknown, unstable, or otherwise invalid.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN017

**A name is empty or duplicated within its own named collection.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN018

**A complex transition has no explicitly defined reverse link.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN019

**The model defines more than the supported number of transitions.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN020

**A kinetic path point does not match exactly one P1 position and owning reference site.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN021

**A chain references an unknown state-change group.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN022

**A kinetic chain has no unstable position.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN023

**A kinetic transition uses a Metropolis or two-position custom chain.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN024

**A chain's generatesReverse flag disagrees with its pattern.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN025

**A state-change group mixes stable-role and unstable-role state changes.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN026

**An unstable position's group does not contain exactly one [D, ∅] state change.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN027

**A reverse link is unknown, cross-kind, asymmetric, or self-referential.** Review the referenced model object, correct the reported condition, and revalidate.

### TRN028

**A resolved kinetic path stability sequence does not match the built-in pattern sequence.** Review the referenced model object, correct the reported condition, and revalidate.

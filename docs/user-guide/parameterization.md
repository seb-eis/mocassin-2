# Parameterization Templates

A parameterization template supplies the numerical values that a compiled model leaves open: pair interaction
energies, cluster energies, site energies, and kinetic attempt frequencies. The model defines geometry and
semantics; the template defines the values that are applied to it.

> **Draft scope.** Step 09 is implemented in features. This page documents template creation, the completeness
> summary, applying a template, site-energy overrides/reset, and diff/rebase with the explicit rebase report.
> The checked-in golden templates, final benchmark run, and closeout documentation remain the final Step 09
> feature.

## What a template contains

A template stores stable compiled identities, compact display descriptors, and sparse values:

- **Stable and unstable pair sets**, keyed by the shared pair `energyId`. A reciprocal chiral pair shares one
  `energyId` and one value; independent A→B and B→A values do not exist.
- **Group sets**, keyed by `clusterEnergyId` plus the canonical particle occupation tuple.
- **Site-energy overrides**, keyed by `(siteId, particleId)`.
- **Kinetic transition rule sets**, keyed by `(transitionKind, transitionId)` and `ruleGeometryId`.

Creating a template writes one **set header** for every compiled pair table, cluster table, and kinetic
transition collection, but no values. Headers make the template self-describing for the editor and for a later
diff; values stay sparse.

Sparse means "not parameterized":

| Missing value | Effective default |
| --- | --- |
| Pair energy | `0 eV` |
| Group energy | `0 eV` |
| Kinetic rule frequency | the compiled default (`1e13 Hz` in v1) |
| Site-energy override | the authored model background |

An explicit `0 eV` or an explicit default frequency is a real entry. It records that the value was reviewed,
which is different from leaving it unset.

Templates are stored in the project's `parameterizations` array. There is no separate template file. See
[Project format](../reference/project-format.md#parameterization-template-values-step-09) for the JSON shape,
canonical ordering, and limits.

## Creating a template

Template creation is a library workflow in Step 09; the `mocassin` CLI and the graphical editor are later
steps. The factory consumes the same project snapshot that produced the compiled model, because the summary
sees the authored site-energy background:

```csharp
var project = await store.LoadAsync("samples/ceria/ceria.mocprj");
var compilation = new ModelCompiler().Compile(project.Project!);
var factory = new ParameterizationFactory();

var result = factory.Create(
    project.Project!,
    compilation.Model!,
    new ParameterizationTemplateOptions { Name = "Ceria parameters" });

foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine(diagnostic);
}
```

Creation:

- assigns a UUIDv7 identifier (or uses the supplied `Id`);
- records `TimeProvider.GetUtcNow()` (or the supplied `CreatedUtc`);
- records the compiled `ModelSignature`;
- partitions pair tables into stable and unstable sets by the centre reference site's stability;
- writes one header per compiled pair table, cluster table, and kinetic transition collection;
- never writes site-energy headers and never fills values;
- reports a case-insensitive duplicate name as the `PRM010` warning;
- does not mutate the project.

A newly created template therefore describes exactly the same effective values as an unparameterized build:
zero pair and group energies, compiled rule frequencies, and the authored site-energy background.

The template name is a label, not a reference. Identifiers are what build templates use. Two templates may
target the same compiled model; a duplicate name is only a warning. Commit a template through
`ProjectEditor.AddParameterizationTemplate` and let project validation re-check its structure.

## Reading the summary

`ParameterizationFactory.Describe(project, model, template)` returns the same `ParameterizationSummary` type
that creation returns. Passing `null` describes the implicit default template. The summary reports:

- stable and unstable pair-table counts, the number of required pair slots, and the explicitly set pair values;
- group-table and group-slot counts, and explicitly set group values;
- kinetic rule-set and kinetic rule-slot counts, and explicitly set rule frequencies;
- site-energy slot and override counts;
- the chiral pair-table count;
- the total required, set, unset, and non-default value counts;
- effective energy and frequency ranges, including the authored site background and explicit overrides;
- the estimated dense applied-value size in bytes;
- the share of explicit pair/group values taken by their most frequent non-default value;
- the count of non-parameterizable Metropolis rule collections;
- `HasExplicitValues`, which is false for a template with only set headers.

Effective ranges are what applying the template would produce, not a copy of the authored template text. The
median frequency uses the middle value for an odd rule count and the mean of the two middle values for an even
count.

Required pair slots follow the compiled table:

- an achiral or unstable–stable table requires `|centre mask| × |partner mask|` ordered values;
- a chiral table requires the distinct unordered particle pairs, because the solver applies the shared table
  in both directions.

The summary is deterministic and culture-independent. It is a read-only view; it never applies, saves, or
mutates a template or project.

## Applying a template

`ParameterizationService.Apply` composes the compiled model and the applied values into a build-time
`ParameterizedModel`. It is a library operation; Step 12 calls it after compilation, and the graphical editor
is a later step.

```csharp
var service = new ParameterizationService();
var result = service.Apply(project, compilation.Model!, template);

if (!result.IsSuccess)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine(diagnostic);
    }

    return;
}

ParameterizedModel applied = result.Model!;
Console.WriteLine(applied.Signature);
```

A template is applied as follows:

- A `null` template selects the **implicit default**. Projects such as `samples/ceria/ceria.mocprj` that have
  `parameterizations: []` therefore remain buildable: pair and group energies are `0 eV`, kinetic rule
  frequencies are the compiled default (`1e13 Hz` in v1), and the site-energy background is the authored
  `model.siteEnergies` table.
- The template's `modelSignature` must equal the compiled `CompiledModel.ModelSignature`. A mismatch returns
  `PRM001`, no `ParameterizedModel`, and a `ParameterizationDiff` on the result that lists the added, removed,
  and matched stable keys and any related geometry changes. Values are never silently reused across a changed
  model; rebase is an explicit operation.
- Missing sparse values use the defaults. An explicit `0 eV` or explicit default frequency is a real entry.
- Every effective value is quantized to 12 decimal places with round-half-away-from-zero, and negative zero is
  normalized to positive zero. The quantized values are stored in `ParameterizationValues` and signed, so the
  package payload and the signature cannot drift.
- The result carries the completeness summary even when the apply fails with `PRM001` or a resource-limit
  error, so a caller can explain what would have been applied.
- Warnings do not fail an apply. A template entry with an unknown stable key or a particle a reference site
  cannot occupy is ignored with `PRM002` or `PRM012`; an invalid site-energy target is ignored with `PRM008`.
  The ignored entry does not change the applied values or the signature.

The parameterization signature is distinct from the compiled-model signature. A value-only change, including an
authored site-energy change, changes `ParameterizedModel.Signature` but not `ModelSignature`; a geometry,
particle, transition, encoding, or catalog change invalidates the template.

`ParameterizationOptions.MaxAppliedValueBytes` bounds the dense applied-value arrays before allocation. A model
whose dense values exceed the limit returns `PRM013` and no `ParameterizedModel`.

`ParameterizationService.Describe` returns the same `ParameterizationSummary` as
`ParameterizationFactory.Describe`, including for the implicit default.

## Site-energy overrides and reset

The authored `model.siteEnergies` table is the default site-energy background. A template may override one
`(siteId, particleId)` value; the override applies to every compiled P1 position owned by that reference site.
Particle `0` (void) and particles the site cannot occupy stay `0 eV`.

```csharp
var edited = ParameterizationTemplateEditor.SetSiteEnergyOverride(
    template,
    siteId,
    particleId,
    new EnergyEv(0.1));

var applied = service.Apply(project, model, edited.Template);
```

Reset-to-model is the removal of the override, not writing the old value:

```csharp
var reset = ParameterizationTemplateEditor.RemoveSiteEnergyOverride(template, siteId, particleId);
```

Removing the override restores the authored background value. An override that names an unknown site or
particle, or a particle the site cannot occupy, is a `PRM008` warning and is ignored; the rest of the
background is unchanged. This keeps stale project data and a mismatched project/model pair from silently
shifting the background.

The applied background is the dense `[position][particle]` table in ascending P order, row-major, exactly as
Step 11 writes it to `energy.defect_background` and Step 15 consumes it.

## Diff and rebase

A template stores the compiled-model signature it was created against. When the model changes, `Apply` rejects
the template with `PRM001` before any stable ID is matched. `ParameterizationService.Diff` explains the
difference without applying or saving anything:

```csharp
var diff = new ParameterizationService().Diff(project, model, template);
```

The diff classifies every pair, group, kinetic-rule, and site-override slot:

- **Added** — the compiled model requires the slot, but the template has no entry; applying would use the
  default.
- **Removed** — the template has an entry, but the compiled model no longer has that stable identity; applying
  would ignore it.
- **Matched** — the stable identity and the stored descriptor agree; the value is unchanged.
- **Geometry changes** — a removed and an added pair or group key share a conservative logical descriptor, such
  as the same unordered site pair. This is display information only. It does not authorize copying a value.

`Diff` with a `null` template describes the implicit default: every required compiled slot is **Added**, and
nothing is **Removed**. Diffs are deterministic and ordered by value kind and stable key.

### Rebase strategies

`ParameterizationService.Rebase` returns a new template, its report, and a `PRM009` informational diagnostic.
It never mutates the input template or model and never saves the result; the caller decides whether to commit
the returned template through the project editor.

| Strategy | Copy behavior | Use when |
| --- | --- | --- |
| `KeepMatching` (default) | Copy only exact stable-identity matches. New slots default and removed entries are dropped. | Catalog or contract changes where the stable IDs survive. |
| `NearestGeometry` | Do everything `KeepMatching` does, then copy a removed **pair** value into a unique added pair slot with the same logical site pair and chiral flag, the same particle pair, and a distance-bucket difference within `NearestDistanceToleranceFm`. | A cell or geometry change where the pair identity changes but the geometry is still recognizably the same. |
| `Reset` | Keep the template ID, name, and creation time, adopt the new model signature and set headers, and clear every value and override. | Start over without breaking build-template references. |

`NearestGeometry` is deliberately conservative:

- it matches pair values only — groups, kinetic rules, and site overrides require exact IDs;
- a candidate must use the same particle pair; the strategy never copies a value between different particles;
- more than one candidate is ambiguous, is reported, and copies nothing;
- chiral entries are exact-ID only. A changed chiral link or a changed chiral geometry drops the old shared
  value and defaults the new slots, because the template stores no reciprocal-link identity that could prove
  the link is unchanged.

`Reset` is not the same as creating a new template. `ParameterizationFactory.Create` assigns a new
`ParameterizationId`; `Reset` preserves the existing identity so build templates keep working.

### Reading the rebase report

`ParameterizationRebaseReport` reports:

- the strategy and the pre-rebase diff;
- `CopiedValues` — explicit values copied by exact identity or by a unique nearest descriptor;
- `DefaultedValues` — current required slots left unset, which therefore use defaults;
- `DroppedValues` — template entries discarded without a value copy (for `Reset`, every entry);
- `AmbiguousMatches` — nearest pairs with more than one candidate, or several stale entries competing for one
  target; nothing was copied for them;
- `DescriptorMatches` — the nearest-descriptor subset of the copies;
- `DescriptorMatchDetails` — every nearest copy, with the removed key, added key, value, and source/target
  distance buckets. The list length equals `DescriptorMatches`.

`PRM009` (info) carries the same counts in its message. Treat it as a review prompt, not a failure: inspect the
dropped and ambiguous counts before saving the rebound template, especially when the model geometry changed.

`Reset` preserves the template identity but reports every previous entry as dropped and every required slot as
defaulted, so a reset is visible in the report rather than looking like a silent loss.

## Limits and diagnostics

The structural template limits and malformed stable keys are reported by the `PRM` diagnostic family.
Apply reports `PRM001` for a stale model signature, `PRM002` for unknown keys or descriptor disagreements,
`PRM003` when required slots are unset, `PRM004`/`PRM005` for invalid effective energies or frequencies,
`PRM006` when an explicit template has no explicit values, `PRM008` for invalid site-energy targets,
`PRM011` for ignored Metropolis rule sets, `PRM012` for particles a reference site cannot occupy, `PRM013` for
resource limits, `PRM015` for suspiciously uniform values, and `PRM016` for implausible frequencies. Rebase
additionally reports `PRM009` with the copied, defaulted, dropped, and ambiguous counts.

See the [diagnostics reference](../reference/diagnostics.md) for the registered codes and the
[energy model](energy.md#site-energies) for the authored site-energy background that a template overrides.

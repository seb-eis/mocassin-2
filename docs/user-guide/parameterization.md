# Parameterization Templates

A parameterization template supplies the numerical values that a compiled model leaves open: pair interaction
energies, cluster energies, site energies, and kinetic attempt frequencies. The model defines geometry and
semantics; the template defines the values that are applied to it.

> **Draft scope.** Step 09 is implemented in features. This page documents template creation, the completeness
> summary, applying a template, and site-energy overrides/reset. The full compiled-bound validation pass,
> diff/rebase, and the remaining completeness diagnostics land with the later Step 09 features.

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

## Limits and diagnostics

The structural template limits and malformed stable keys are reported by the `PRM` diagnostic family.
Apply reports `PRM001` for a stale model signature, `PRM002` for unknown keys or descriptor disagreements,
`PRM004`/`PRM005` for invalid effective energies or frequencies, `PRM008` for invalid site-energy targets,
`PRM012` for particles a reference site cannot occupy, and `PRM013` for resource limits. The remaining
completeness and plausibility warnings (`PRM003`, `PRM006`, `PRM011`, `PRM015`, `PRM016`) are added by the
validation feature and will appear in a later revision of this page.

See the [diagnostics reference](../reference/diagnostics.md) for the registered codes and the
[energy model](energy.md#site-energies) for the authored site-energy background that a template overrides.

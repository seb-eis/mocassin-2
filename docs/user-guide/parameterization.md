# Parameterization Templates

A parameterization template supplies the numerical values that a compiled model leaves open: pair interaction
energies, cluster energies, site energies, and kinetic attempt frequencies. The model defines geometry and
semantics; the template defines the values that are applied to it.

> **Draft scope.** Step 09 is implemented in features. This page documents template creation and the
> completeness summary. Applying a template, editing site-energy overrides, validation diagnostics, and
> diff/rebase land with the later Step 09 features. Until then, the library returns a template that is ready to
> edit but is not yet applied to a package.

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

## Limits and diagnostics

The structural template limits and malformed stable keys are reported by the `PRM` diagnostic family. Creation
itself reports only the duplicate-name warning. Compiled-bound checks such as a stale model signature,
unknown particle keys, or unset required values are added by the validation feature and will appear in a later
revision of this page.

See the [diagnostics reference](../reference/diagnostics.md) for the registered codes and the
[energy model](energy.md#site-energies) for the authored site-energy background that a template overrides.

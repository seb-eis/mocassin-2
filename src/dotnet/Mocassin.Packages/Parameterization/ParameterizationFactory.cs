// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Creates sparse parameterization templates from a compiled model and describes their completeness.
/// </summary>
/// <param name="timeProvider">
/// The clock used for new template timestamps; <see langword="null"/> uses the system clock.
/// </param>
/// <remarks>
/// The factory is pure and synchronous. It takes the same project snapshot that produced the compiled model so
/// the summary sees the authored site-energy background, and it never mutates the project. Creation writes one
/// set header per compiled pair table, cluster table, and kinetic transition collection; values stay sparse.
/// </remarks>
public sealed class ParameterizationFactory(TimeProvider? timeProvider = null)
{
    /// <summary>
    /// Creates a template for the supplied project and compiled model.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="options">The template name and optional identity/timestamp.</param>
    /// <returns>The template, its summary, and creation diagnostics.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><see cref="ParameterizationTemplateOptions.Name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The compiled model violates a contract invariant.</exception>
    public TemplateCreationResult Create(
        Project project,
        CompiledModel model,
        ParameterizationTemplateOptions options)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);

        CompiledModelContract.Validate(model);
        var descriptors = CompiledModelContract.DescribePairTables(model);
        var createdUtc = (options.CreatedUtc ?? (timeProvider ?? TimeProvider.System).GetUtcNow()).ToUniversalTime();
        var id = options.Id ?? ParameterizationId.Create(Uuid7.NewGuid(createdUtc));
        var template = CreateTemplate(model, descriptors, id, options.Name, createdUtc);
        var summary = ParameterizationSummaryBuilder.BuildCore(project, model, template, descriptors);
        return new TemplateCreationResult(template, summary, CreateDiagnostics(project, template));
    }

    /// <summary>
    /// Describes the required, explicit, and effective values for a template.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">
    /// The explicit template, or <see langword="null"/> for the implicit default template.
    /// </param>
    /// <returns>The summary.</returns>
    /// <exception cref="ArgumentNullException">The project or model is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compiled model violates a contract invariant.</exception>
    public static ParameterizationSummary Describe(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null) =>
        ParameterizationSummaryBuilder.Build(project, model, template);

    private static ParameterizationTemplate CreateTemplate(
        CompiledModel model,
        ImmutableArray<PairTableDescriptor> descriptors,
        ParameterizationId id,
        string name,
        DateTimeOffset createdUtc)
    {
        var stable = ImmutableArray.CreateBuilder<PairEnergySet>();
        var unstable = ImmutableArray.CreateBuilder<PairEnergySet>();
        foreach (var descriptor in descriptors)
        {
            var set = new PairEnergySet(
                descriptor.EnergyId,
                descriptor.CenterSiteId,
                descriptor.PartnerSiteId,
                descriptor.DistanceBucketFm,
                descriptor.IsChiral);
            if (descriptor.Stability == SiteStability.Stable)
            {
                stable.Add(set);
            }
            else
            {
                unstable.Add(set);
            }
        }

        var stablePairSets = stable
            .OrderBy(static set => set.EnergyId, StringComparer.Ordinal)
            .ToImmutableArray();
        var unstablePairSets = unstable
            .OrderBy(static set => set.EnergyId, StringComparer.Ordinal)
            .ToImmutableArray();
        var groupSets = model.Energy.ClusterTables
            .Select(table => new GroupEnergySet(
                table.ClusterEnergyId,
                table.ClusterId,
                model.Structure.Sites[table.CenterSiteIndex].Id))
            .OrderBy(static set => set.ClusterEnergyId, StringComparer.Ordinal)
            .ToImmutableArray();
        var ruleSets = model.Transitions.Collections
            .Where(static collection => collection.Transition.Kind == TransitionKind.Kinetic)
            .Select(static collection => new TransitionRuleSet(collection.Transition.Kind, collection.Transition.Id))
            .OrderBy(static set => set.TransitionKind)
            .ThenBy(static set => set.TransitionId)
            .ToImmutableArray();
        return new ParameterizationTemplate(
            id,
            name,
            model.ModelSignature,
            createdUtc,
            stablePairSets,
            unstablePairSets,
            groupSets,
            [],
            ruleSets);
    }

    private static ImmutableArray<Diagnostic> CreateDiagnostics(Project project, ParameterizationTemplate template)
    {
        var parameterizations = project.Parameterizations;
        if (parameterizations.IsDefault)
        {
            return [];
        }

        var bag = new DiagnosticBag();
        foreach (var existing in parameterizations)
        {
            if (existing is null ||
                string.IsNullOrWhiteSpace(existing.Name) ||
                !string.Equals(existing.Name, template.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.DuplicateName,
                DiagnosticSeverity.Warning,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Template name '{template.Name}' is already used by parameterization template '{existing.Id.Value:D}'."),
                new ModelPath("parameterization", template.Id.Value, "name"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.DuplicateName)));
            break;
        }

        return bag.Sorted;
    }
}

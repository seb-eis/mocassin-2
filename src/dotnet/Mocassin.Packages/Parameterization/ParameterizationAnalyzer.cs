// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Performs the compiled-bound validation shared by <c>Apply</c> and <c>Validate</c>.
/// </summary>
/// <remarks>
/// The analyzer never compiles, never mutates its inputs, and never computes the parameterization signature.
/// When dense targets are requested it also fills the applied arrays; otherwise it performs the same checks
/// without allocating them, which keeps the validation preflight cheap and the diagnostics identical.
/// </remarks>
internal static class ParameterizationAnalyzer
{
    /// <summary>
    /// Analyzes one project, compiled model, and template combination.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The explicit template, or <see langword="null"/> for the implicit default.</param>
    /// <param name="options">The apply and quality options.</param>
    /// <param name="buildTargets">Whether to allocate and fill the dense applied arrays.</param>
    /// <param name="cancellationToken">Cancels the operation before large allocations.</param>
    /// <returns>The analysis.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compiled model violates a contract invariant.</exception>
    public static ParameterizationAnalysis Analyze(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template,
        ParameterizationOptions options,
        bool buildTargets,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        CompiledModelContract.Validate(model);
        var descriptors = CompiledModelContract.DescribePairTables(model);
        var summary = ParameterizationSummaryBuilder.BuildCore(project, model, template, descriptors);
        var effectiveTemplate = template ?? ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature);
        var templateId = effectiveTemplate.Id.Value;
        var bag = new DiagnosticBag();

        if (!string.Equals(effectiveTemplate.ModelSignature, model.ModelSignature, StringComparison.Ordinal))
        {
            var diff = ParameterizationDiffBuilder.Build(model, effectiveTemplate);
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.ModelSignatureMismatch,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Template model signature '{effectiveTemplate.ModelSignature}' does not match the compiled model signature '{model.ModelSignature}'; the template has {diff.Removed.Length} stale, {diff.Added.Length} new, and {diff.Matched.Length} matching slots."),
                new ModelPath("parameterization", templateId, "modelSignature"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.ModelSignatureMismatch)));
            return new ParameterizationAnalysis(summary, bag.Sorted, diff, null);
        }

        if (options.MaxAppliedValueBytes < 0 ||
            summary.EstimatedValueBytes > options.MaxAppliedValueBytes)
        {
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.LimitExceeded,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The applied values require {summary.EstimatedValueBytes} bytes, which exceeds the configured limit of {options.MaxAppliedValueBytes} bytes."),
                new ModelPath("parameterization", templateId, "appliedValues"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.LimitExceeded)));
            return new ParameterizationAnalysis(summary, bag.Sorted, null, null);
        }

        var siteValueLength = (long)model.Structure.PositionCount * model.Particles.Count;
        if (siteValueLength > int.MaxValue)
        {
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.LimitExceeded,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The dense site-energy background requires {siteValueLength} values, which exceeds the supported array length."),
                new ModelPath("parameterization", templateId, "siteEnergyOverrides"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.LimitExceeded)));
            return new ParameterizationAnalysis(summary, bag.Sorted, null, null);
        }

        ParameterizationTargets? targets = null;
        if (buildTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            targets = new ParameterizationTargets(
                AllocatePairValues(model),
                AllocateGroupValues(model),
                AllocateRuleFrequencies(model),
                new double[(int)siteValueLength]);
        }

        var walker = new ParameterizationEntryWalker(project, model, effectiveTemplate, bag, options.Quality, descriptors, targets);
        walker.Run();
        if (template is not null)
        {
            AppendCompletenessDiagnostics(bag, templateId, summary, options);
        }

        return new ParameterizationAnalysis(summary, bag.Sorted, null, targets);
    }

    private static void AppendCompletenessDiagnostics(
        DiagnosticBag bag,
        Guid templateId,
        ParameterizationSummary summary,
        ParameterizationOptions options)
    {
        if (options.EmitCompletenessWarnings)
        {
            if (summary.UnsetValueCount > 0)
            {
                var pairUnset = Math.Max(0, summary.PairSlotCount - summary.ExplicitPairValueCount);
                var groupUnset = Math.Max(0, summary.GroupSlotCount - summary.ExplicitGroupValueCount);
                var ruleUnset = Math.Max(0, summary.KineticRuleSlotCount - summary.ExplicitRuleFrequencyCount);
                bag.Add(new Diagnostic(
                    ParameterizationDiagnosticCodes.RequiredValuesUnset,
                    DiagnosticSeverity.Warning,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The template leaves {summary.UnsetValueCount} of {summary.RequiredSlotCount} required values unset ({pairUnset} pair, {groupUnset} group, {ruleUnset} kinetic-rule); defaults are applied."),
                    new ModelPath("parameterization", templateId),
                    ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.RequiredValuesUnset)));
            }

            if (!summary.HasExplicitValues)
            {
                bag.Add(new Diagnostic(
                    ParameterizationDiagnosticCodes.NoExplicitValues,
                    DiagnosticSeverity.Warning,
                    "The explicit template contains no explicit values; every required pair, group, and kinetic-rule slot uses its default.",
                    new ModelPath("parameterization", templateId),
                    ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.NoExplicitValues)));
            }
        }

        var explicitPairAndGroupValues = (long)summary.ExplicitPairValueCount + summary.ExplicitGroupValueCount;
        if (explicitPairAndGroupValues >= options.Quality.IdenticalValueMinimumCount &&
            summary.IdenticalValueShare > 0.0 &&
            summary.IdenticalValueShare >= options.Quality.IdenticalValueShareThreshold)
        {
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.UniformValues,
                DiagnosticSeverity.Warning,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{explicitPairAndGroupValues} explicit pair/group values are dominated by one non-default value ({summary.IdenticalValueShare * 100.0:0.#}%); check for a bulk-fill or paste error."),
                new ModelPath("parameterization", templateId),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.UniformValues)));
        }
    }

    private static double[][] AllocatePairValues(CompiledModel model)
    {
        var tables = new double[model.Energy.PairTables.Length][];
        for (var index = 0; index < tables.Length; index++)
        {
            var table = model.Energy.PairTables[index];
            tables[index] = new double[checked(table.ParticleCount * table.ParticleCount)];
        }

        return tables;
    }

    private static double[][] AllocateGroupValues(CompiledModel model)
    {
        var tables = new double[model.Energy.ClusterTables.Length][];
        for (var index = 0; index < tables.Length; index++)
        {
            tables[index] = new double[model.Energy.ClusterTables[index].OccupationCount];
        }

        return tables;
    }

    private static double[] AllocateRuleFrequencies(CompiledModel model)
    {
        var frequencies = new double[model.Transitions.RuleCount];
        for (var index = 0; index < frequencies.Length; index++)
        {
            frequencies[index] = model.Transitions.Rules[index].FrequencyHz;
        }

        return frequencies;
    }

}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Default <see cref="IParameterizationService"/> implementation for applying sparse parameterization templates.
/// </summary>
/// <remarks>
/// Apply resolves every required pair, group, kinetic-rule, and site-energy slot, quantizes the effective values
/// to 12 decimals with round-half-away-from-zero, and signs the dense result. It never mutates the project,
/// compiled model, or template. Missing values use the sparse defaults: <c>0 eV</c>, the compiled rule frequency,
/// and the authored site-energy background.
/// </remarks>
public sealed partial class ParameterizationService : IParameterizationService
{
    /// <inheritdoc />
    public ParameterizationResult Apply(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        ParameterizationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        options ??= ParameterizationOptions.Default;
        cancellationToken.ThrowIfCancellationRequested();

        CompiledModelContract.Validate(model);
        var descriptors = CompiledModelContract.DescribePairTables(model);
        var summary = ParameterizationSummaryBuilder.BuildCore(project, model, template, descriptors);
        var effectiveTemplate = template ?? ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature);
        var bag = new DiagnosticBag();
        var templateId = effectiveTemplate.Id.Value;

        if (!string.Equals(effectiveTemplate.ModelSignature, model.ModelSignature, StringComparison.Ordinal))
        {
            var diff = ParameterizationDiffBuilder.Build(model, effectiveTemplate);
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.ModelSignatureMismatch,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Template model signature '{effectiveTemplate.ModelSignature}' does not match the compiled model signature '{model.ModelSignature}'; " +
                    $"the diff reports {diff.Added.Length} added, {diff.Removed.Length} removed, and {diff.Matched.Length} matched slots."),
                new ModelPath("parameterization", templateId, "modelSignature"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.ModelSignatureMismatch)));
            return new ParameterizationResult(null, bag.Sorted, summary, diff);
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
            return new ParameterizationResult(null, bag.Sorted, summary, null);
        }

        var siteIndexById = BuildSiteIndexById(model);
        var siteMasks = BuildSiteMasks(model);
        var pairTableByEnergyId = BuildPairTableIndex(model);
        var clusterTableByEnergyId = BuildClusterTableIndex(model);
        var ruleIndexByGeometryId = BuildRuleIndex(model);
        var collectionIndexByTransition = BuildCollectionIndex(model);
        var descriptorByTable = descriptors.ToDictionary(static descriptor => descriptor.TableIndex);

        var particleCount = model.Particles.Count;
        var positionCount = model.Structure.PositionCount;
        var siteValueLength = (long)positionCount * particleCount;
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
            return new ParameterizationResult(null, bag.Sorted, summary, null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var pairValues = AllocatePairValues(model);
        var groupValues = AllocateGroupValues(model);
        var ruleFrequencies = AllocateRuleFrequencies(model);
        var siteEnergies = new double[(int)siteValueLength];

        ResolveAuthoredBackground(
            project,
            model,
            siteIndexById,
            siteMasks,
            siteEnergies,
            particleCount,
            templateId,
            bag);
        ApplySiteOverrides(
            effectiveTemplate,
            model,
            siteIndexById,
            siteMasks,
            siteEnergies,
            particleCount,
            templateId,
            bag);
        ApplyPairValues(
            effectiveTemplate,
            model,
            descriptorByTable,
            pairTableByEnergyId,
            pairValues,
            templateId,
            bag);
        ApplyGroupValues(
            effectiveTemplate,
            model,
            clusterTableByEnergyId,
            siteMasks,
            groupValues,
            templateId,
            bag);
        ApplyRuleFrequencies(
            effectiveTemplate,
            model,
            collectionIndexByTransition,
            ruleIndexByGeometryId,
            ruleFrequencies,
            templateId,
            bag);

        var hasErrors = ValidateEffectiveValues(
            model,
            pairValues,
            groupValues,
            siteEnergies,
            ruleFrequencies,
            particleCount,
            templateId,
            bag);
        if (hasErrors)
        {
            return new ParameterizationResult(null, bag.Sorted, summary, null);
        }

        Quantize(pairValues);
        Quantize(groupValues);
        Quantize(ruleFrequencies);
        Quantize(siteEnergies);

        var values = new ParameterizationValues(
            [.. pairValues.Select(static table => table.ToImmutableArray())],
            [.. groupValues.Select(static table => table.ToImmutableArray())],
            siteEnergies.ToImmutableArray(),
            ruleFrequencies.ToImmutableArray());
        var signature = ParameterizationSignatureV1.Compute(model.ModelSignature, values);
        return new ParameterizationResult(
            new ParameterizedModel(model, values, signature),
            bag.Sorted,
            summary,
            null);
    }

    /// <inheritdoc />
    public ParameterizationSummary Describe(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        return ParameterizationFactory.Describe(project, model, template);
    }
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Signatures;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

public sealed partial class ParameterizationService
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

        var analysis = ParameterizationAnalyzer.Analyze(
            project,
            model,
            template,
            options,
            buildTargets: true,
            cancellationToken);
        if (analysis.Targets is null || analysis.HasErrors)
        {
            return new ParameterizationResult(null, analysis.Diagnostics, analysis.Summary, analysis.Diff);
        }

        var targets = analysis.Targets;
        Quantize(targets.PairValues);
        Quantize(targets.GroupValues);
        Quantize(targets.RuleFrequencies);
        Quantize(targets.SiteEnergies);

        var values = new ParameterizationValues(
            [.. targets.PairValues.Select(static table => table.ToImmutableArray())],
            [.. targets.GroupValues.Select(static table => table.ToImmutableArray())],
            targets.SiteEnergies.ToImmutableArray(),
            targets.RuleFrequencies.ToImmutableArray());
        var signature = ParameterizationSignatureV1.Compute(model.ModelSignature, values);
        return new ParameterizationResult(
            new ParameterizedModel(model, values, signature),
            analysis.Diagnostics,
            analysis.Summary,
            null);
    }

    private static void Quantize(double[][] tables)
    {
        foreach (var table in tables)
        {
            for (var index = 0; index < table.Length; index++)
            {
                table[index] = CanonicalProjectionWriter.Quantize(table[index]);
            }
        }
    }

    private static void Quantize(double[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = CanonicalProjectionWriter.Quantize(values[index]);
        }
    }
}

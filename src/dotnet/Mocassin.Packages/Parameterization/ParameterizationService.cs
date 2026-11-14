// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Default <see cref="IParameterizationService"/> implementation for applying and validating sparse
/// parameterization templates.
/// </summary>
/// <remarks>
/// Apply resolves every required pair, group, kinetic-rule, and site-energy slot, quantizes the effective values
/// to 12 decimals with round-half-away-from-zero, and signs the dense result. Validate runs the same compiled-bound
/// checks without allocating the dense tables or computing the signature. Describe, Diff, and Rebase compare
/// stable identities without applying values. None of the operations mutates the project, compiled model, or
/// template. Missing values use the sparse defaults: <c>0 eV</c>, the compiled rule frequency, and the authored
/// site-energy background.
/// </remarks>
public sealed partial class ParameterizationService : IParameterizationService
{
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

    /// <inheritdoc />
    public ParameterizationDiff Diff(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();
        CompiledModelContract.Validate(model);
        var effectiveTemplate = template ?? ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature);
        return ParameterizationDiffBuilder.Build(model, effectiveTemplate);
    }

    /// <inheritdoc />
    public ParameterizationRebaseResult Rebase(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationRebaseOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(options);
        if (template.Id == default)
        {
            throw new ArgumentException(
                "The implicit default template has no persisted identity and cannot be rebased; pass an explicit template.",
                nameof(template));
        }

        if (options.NearestDistanceToleranceFm < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.NearestDistanceToleranceFm,
                "The nearest-geometry distance tolerance must be non-negative.");
        }

        if (!Enum.IsDefined(options.Strategy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.Strategy,
                "The rebase strategy is not defined.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        CompiledModelContract.Validate(model);
        return ParameterizationRebaseBuilder.Rebuild(model, template, options, cancellationToken);
    }
}

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
/// checks without allocating the dense tables or computing the signature. Neither operation mutates the project,
/// compiled model, or template. Missing values use the sparse defaults: <c>0 eV</c>, the compiled rule frequency,
/// and the authored site-energy background.
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
}

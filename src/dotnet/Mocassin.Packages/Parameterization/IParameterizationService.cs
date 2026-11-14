// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Applies parameterization values to a compiled model and describes their completeness.
/// </summary>
/// <remarks>
/// The service is stateless and safe to reuse concurrently: every call reads immutable inputs and allocates its
/// own result. A <see langword="null"/> template selects the implicit default. The full validation, diff, and
/// rebase surface is added by later Step 09 features on this interface.
/// </remarks>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Template is the accepted Step 09 domain term and public API parameter name.",
    Scope = "type",
    Target = "~T:Mocassin.Packages.Parameterization.IParameterizationService")]
public interface IParameterizationService
{
    /// <summary>
    /// Applies a template to a compiled model and computes the parameterization signature.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The explicit template, or <see langword="null"/> for the implicit default.</param>
    /// <param name="options">The apply options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the operation before large allocations.</param>
    /// <returns>The applied model or diagnostics, the summary, and the mismatch diff when applicable.</returns>
    public ParameterizationResult Apply(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        ParameterizationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes the required, explicit, and effective values of a template against a compiled model.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The explicit template, or <see langword="null"/> for the implicit default.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The summary.</returns>
    public ParameterizationSummary Describe(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        CancellationToken cancellationToken = default);
}

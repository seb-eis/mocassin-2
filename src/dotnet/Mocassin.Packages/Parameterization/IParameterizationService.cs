// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Applies parameterization values to a compiled model and describes their completeness.
/// </summary>
/// <remarks>
/// The service is stateless and safe to reuse concurrently: every call reads immutable inputs and allocates its
/// own result. A <see langword="null"/> template selects the implicit default. Diff and rebase are pure
/// read-only operations; rebase returns a new template and never saves it.
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
    /// Validates a template against a compiled model without applying it or computing the signature.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The explicit template, or <see langword="null"/> for the implicit default.</param>
    /// <param name="options">The validation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the operation before the entry walk.</param>
    /// <returns>
    /// The diagnostics in stable order. A signature-mismatch <c>PRM001</c> carries no diff object; use
    /// <see cref="Apply"/> when the diff is required.
    /// </returns>
    public ImmutableArray<Diagnostic> Validate(
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

    /// <summary>
    /// Compares a template with a compiled model by stable identity without applying it.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The explicit template, or <see langword="null"/> for the implicit default.</param>
    /// <param name="cancellationToken">Cancels the operation before the slot walk.</param>
    /// <returns>
    /// The added, removed, and matched slots plus related geometry changes. A <see langword="null"/> template is
    /// described as the implicit default, so every required compiled slot is reported as added.
    /// </returns>
    public ParameterizationDiff Diff(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds a template against a current compiled model with an explicit copy/keep/reset strategy.
    /// </summary>
    /// <param name="project">The project snapshot that produced <paramref name="model"/>.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The template to rebase.</param>
    /// <param name="options">The strategy and nearest-descriptor tolerance.</param>
    /// <param name="cancellationToken">Cancels the operation before the slot walk.</param>
    /// <returns>The rebound template, its report, and the <c>PRM009</c> informational diagnostic.</returns>
    /// <remarks>
    /// Rebase never mutates the input and never saves the result. Values are copied only for exact identity
    /// matches or, with <see cref="ParameterizationRebaseStrategy.NearestGeometry"/>, unique reported pair
    /// descriptor matches. Removed entries are always reported and dropped; they are never silently reused.
    /// </remarks>
    public ParameterizationRebaseResult Rebase(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationRebaseOptions options,
        CancellationToken cancellationToken = default);
}

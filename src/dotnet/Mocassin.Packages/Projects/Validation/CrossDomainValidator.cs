// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Runs the cross-domain checks that cannot live in one model domain.
/// </summary>
/// <remarks>
/// The rule set is added by the cross-domain-validation feature. The pipeline keeps the stage so its order
/// and stage reporting are stable from the first feature.
/// </remarks>
internal sealed class CrossDomainValidator
{
    /// <summary>
    /// Validates cross-domain relationships.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="options">The run options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cross-domain diagnostics.</returns>
    public static ImmutableArray<Diagnostic> Validate(
        Project project,
        ValidationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        return ImmutableArray<Diagnostic>.Empty;
    }
}

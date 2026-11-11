// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// An additional validation stage plugging into <see cref="ValidationStage.Build"/>.
/// </summary>
/// <remarks>
/// Steps 09–12 own the build checks. Step 07 only provides the extension point; a stage that declares any
/// other stage is rejected by the pipeline constructor.
/// </remarks>
public interface IValidationStage
{
    /// <summary>
    /// Gets the stage this extension belongs to. The value must be <see cref="ValidationStage.Build"/>.
    /// </summary>
    public ValidationStage Stage { get; }

    /// <summary>
    /// Validates the loaded project.
    /// </summary>
    /// <param name="context">The immutable validation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The diagnostics produced by the stage.</returns>
    public ImmutableArray<Diagnostic> Validate(ValidationContext context, CancellationToken cancellationToken);
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

public sealed partial class ParameterizationService
{
    /// <inheritdoc />
    public ImmutableArray<Diagnostic> Validate(
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

        var analysis = ParameterizationAnalyzer.Analyze(
            project,
            model,
            template,
            options,
            buildTargets: false,
            cancellationToken);
        return analysis.Diagnostics;
    }
}

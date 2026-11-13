// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Core.Project;

namespace Mocassin.Compilation.Context;

/// <summary>
/// Immutable input passed from one compilation stage to the next.
/// </summary>
/// <param name="Project">The validated project snapshot.</param>
/// <param name="Options">The effective compilation options.</param>
internal sealed record CompilationContext(Project Project, CompilationOptions Options);

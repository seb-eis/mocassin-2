// SPDX-License-Identifier: MIT

using Mocassin.Core.Project;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Compiles a validated project into a deterministic, densely indexed <see cref="CompiledModel"/>.
/// </summary>
/// <remarks>
/// The compiler is pure and synchronous: it performs no file I/O, does not reference the package or contract
/// layers, and does not run the validation pipeline. Callers pass a project that has already passed
/// <c>ProjectValidationPipeline</c>.
/// </remarks>
public interface IModelCompiler
{
    /// <summary>
    /// Compiles one validated project.
    /// </summary>
    /// <param name="project">The validated project snapshot.</param>
    /// <param name="options">Optional compilation options; <see langword="null"/> uses the defaults.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The complete compilation result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="project"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The options violate a documented range.</exception>
    /// <exception cref="OperationCanceledException">Compilation was cancelled; no partial model is returned.</exception>
    /// <exception cref="InvalidOperationException">A stage violated the compilation contract.</exception>
    public CompilationResult Compile(
        Project project,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default);
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// The complete outcome of one model compilation.
/// </summary>
/// <remarks>
/// A successful result always carries a <see cref="CompiledModel"/>; a failed result never carries one.
/// Warning-only <c>CMP009</c>, <c>CMP010</c>, and <c>CMP012</c> diagnostics do not turn a successful
/// compilation into a failure. Cancellation throws <see cref="OperationCanceledException"/> and never produces
/// a partial result.
/// </remarks>
public sealed record CompilationResult
{
    /// <summary>
    /// Initializes a compilation result.
    /// </summary>
    /// <param name="status">The overall status.</param>
    /// <param name="model">The compiled model when the compilation succeeded.</param>
    /// <param name="diagnostics">The sorted compilation diagnostics.</param>
    /// <param name="report">The compilation report.</param>
    /// <exception cref="ArgumentException">The status, model, and diagnostic combination is inconsistent.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <see langword="null"/>.</exception>
    public CompilationResult(
        CompilationStatus status,
        CompiledModel? model,
        ImmutableArray<Diagnostic> diagnostics,
        CompilationReport report)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown compilation status.");
        }

        ArgumentNullException.ThrowIfNull(report);
        var items = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;

        if (status == CompilationStatus.Succeeded && model is null)
        {
            throw new ArgumentException("A successful compilation must carry a compiled model.", nameof(model));
        }

        if (status == CompilationStatus.Failed && model is not null)
        {
            throw new ArgumentException("A failed compilation must not carry a compiled model.", nameof(model));
        }

        if (status == CompilationStatus.Failed
            && !items.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new ArgumentException(
                "A failed compilation must carry at least one error diagnostic.",
                nameof(diagnostics));
        }

        Status = status;
        Model = model;
        Diagnostics = items;
        Report = report;
    }

    /// <summary>
    /// Gets the overall status.
    /// </summary>
    public CompilationStatus Status { get; }

    /// <summary>
    /// Gets the compiled model when the compilation succeeded, otherwise <see langword="null"/>.
    /// </summary>
    public CompiledModel? Model { get; }

    /// <summary>
    /// Gets the sorted compilation diagnostics.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the compilation report.
    /// </summary>
    public CompilationReport Report { get; }

    /// <summary>
    /// Gets a value indicating whether the compilation succeeded and carries a complete model.
    /// </summary>
    public bool IsSuccess => Status == CompilationStatus.Succeeded && Model is not null;
}

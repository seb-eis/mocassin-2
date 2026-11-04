// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// A stable, machine-readable message about user data.
/// </summary>
public sealed record Diagnostic
{
    /// <summary>
    /// Initializes a new diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code, for example <c>PROJ001</c>.</param>
    /// <param name="severity">The severity of the message.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="path">The optional path to the offending object or property.</param>
    /// <param name="hint">An optional actionable hint.</param>
    /// <param name="related">Optional additional paths that help explain the diagnostic.</param>
    /// <exception cref="ArgumentException"><paramref name="code"/> or <paramref name="message"/> is empty.</exception>
    public Diagnostic(
        string code,
        DiagnosticSeverity severity,
        string message,
        ModelPath? path = null,
        string? hint = null,
        ImmutableArray<ModelPath> related = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Severity = severity;
        Message = message;
        Path = path;
        Hint = hint;
        Related = related.IsDefault ? ImmutableArray<ModelPath>.Empty : related;
    }

    /// <summary>
    /// Gets the stable diagnostic code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the severity of the message.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the human-readable message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional path to the offending object or property.
    /// </summary>
    public ModelPath? Path { get; }

    /// <summary>
    /// Gets an optional actionable hint.
    /// </summary>
    public string? Hint { get; }

    /// <summary>
    /// Gets additional paths that help explain the diagnostic.
    /// </summary>
    public ImmutableArray<ModelPath> Related { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        var location = Path is null ? string.Empty : string.Create(System.Globalization.CultureInfo.InvariantCulture, $" at {Path}");
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Code} [{Severity}] {Message}{location}");
    }
}

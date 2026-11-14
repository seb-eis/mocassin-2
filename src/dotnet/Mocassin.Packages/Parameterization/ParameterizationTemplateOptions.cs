// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Controls how <see cref="ParameterizationFactory.Create"/> names and identifies a new template.
/// </summary>
/// <remarks>
/// The name is a label, not a reference. Identifiers are UUIDv7 values and are preserved by rebase so build
/// templates keep working. When <see cref="Id"/> or <see cref="CreatedUtc"/> is omitted, the factory selects it.
/// </remarks>
public sealed record ParameterizationTemplateOptions
{
    /// <summary>
    /// Gets the user-visible template name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional stable identifier. When omitted, the factory creates a UUIDv7 value.
    /// </summary>
    public ParameterizationId? Id { get; init; }

    /// <summary>
    /// Gets the optional creation timestamp. When omitted, the factory uses its injected
    /// <see cref="TimeProvider"/>.
    /// </summary>
    public DateTimeOffset? CreatedUtc { get; init; }

    /// <summary>
    /// Gets the default options with the name <c>Parameters</c>.
    /// </summary>
    public static ParameterizationTemplateOptions Default { get; } = new() { Name = "Parameters" };
}

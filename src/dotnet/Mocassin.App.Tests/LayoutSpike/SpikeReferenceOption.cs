// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One searchable reference-picker choice: a name plus the details that distinguish it from its siblings.
/// </summary>
/// <param name="Id">The referenced model identifier.</param>
/// <param name="Name">The user-visible name.</param>
/// <param name="Details">The distinguishing details, never the raw identifier.</param>
internal sealed record SpikeReferenceOption(Guid Id, string Name, string Details)
{
    /// <summary>
    /// Gets the combined display text.
    /// </summary>
    public string Display => $"{Name} — {Details}";

    /// <summary>
    /// Gets a value indicating whether the option matches a search query.
    /// </summary>
    /// <param name="query">The lower-case query.</param>
    /// <returns><see langword="true"/> when the option matches.</returns>
    public bool Matches(string query) =>
        string.IsNullOrEmpty(query)
        || Display.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override string ToString() => Display;
}

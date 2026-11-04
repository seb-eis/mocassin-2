// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Project;

/// <summary>
/// Project identity, seed, naming, and timestamp metadata.
/// </summary>
/// <param name="ProjectId">The stable project UUID.</param>
/// <param name="MasterSeed">The 32-byte lowercase hex master seed.</param>
/// <param name="Name">The user-visible project name.</param>
/// <param name="Description">The optional project description.</param>
/// <param name="CreatedUtc">The UTC creation timestamp.</param>
/// <param name="ModifiedUtc">The UTC last-modified timestamp.</param>
/// <param name="Author">The optional author.</param>
/// <param name="Notes">Optional free-text notes.</param>
public sealed record ProjectMetadata(
    Guid ProjectId,
    string MasterSeed,
    string Name,
    string? Description,
    [property: JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))] DateTimeOffset CreatedUtc,
    [property: JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))] DateTimeOffset ModifiedUtc,
    string? Author = null,
    string? Notes = null)
{
    /// <summary>
    /// Creates a new random 32-byte master seed encoded as lowercase hexadecimal.
    /// </summary>
    /// <returns>The 64-character seed string.</returns>
    public static string CreateMasterSeed() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Determines whether a seed is a 64-character lowercase hexadecimal value.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> for a valid seed.</returns>
    public static bool IsValidMasterSeed(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiHexDigitLower(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a copy with a new modified timestamp.
    /// </summary>
    /// <param name="modifiedUtc">The new UTC timestamp.</param>
    /// <returns>The updated metadata.</returns>
    public ProjectMetadata WithModifiedUtc(DateTimeOffset modifiedUtc) => this with { ModifiedUtc = modifiedUtc.ToUniversalTime() };

    /// <summary>
    /// Formats the project identifier as a lowercase UUID.
    /// </summary>
    /// <returns>The formatted identifier.</returns>
    public string FormatProjectId() => ProjectId.ToString("D", CultureInfo.InvariantCulture);
}

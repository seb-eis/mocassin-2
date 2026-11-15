// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Derives the deterministic master and job seeds of the Step 10 job-expansion contract.
/// </summary>
/// <remarks>
/// The master seed text is resolved as the first non-empty of the collection seed, the simulation seed, and the
/// project master seed. A text that is exactly 64 lowercase hexadecimal characters is decoded to 32 bytes; every
/// other non-empty text is hashed as <c>SHA-256("mocassin/seed-text/v1" || UTF8(text))</c>. The job seed is
/// <c>SHA-256(master[32] || u32_le(collectionIndex) || u32_le(configIndex) || u32_le(jobIndex) ||
/// UTF8("mocassin/job/v1"))</c>. The build never creates or persists a seed.
/// </remarks>
public static class JobSeedDeriver
{
    /// <summary>
    /// The length of a master or job seed in bytes.
    /// </summary>
    public const int SeedByteLength = 32;

    /// <summary>
    /// The domain-separation prefix for an authored seed text that is not a 64-character lowercase hex value.
    /// </summary>
    public const string SeedTextDomain = "mocassin/seed-text/v1";

    /// <summary>
    /// The terminal domain-separation string of a job seed.
    /// </summary>
    public const string JobDomain = "mocassin/job/v1";

    /// <summary>
    /// Resolves the master seed text from the collection, simulation, and project layers.
    /// </summary>
    /// <param name="collectionSeed">The collection seed text, or <see langword="null"/> to inherit.</param>
    /// <param name="simulationSeed">The simulation seed text, or <see langword="null"/> to inherit.</param>
    /// <param name="projectSeed">The project master seed text.</param>
    /// <returns>The first non-empty seed text, or <see langword="null"/> when all three are empty.</returns>
    public static string? ResolveSeedText(string? collectionSeed, string? simulationSeed, string? projectSeed)
    {
        if (!string.IsNullOrEmpty(collectionSeed))
        {
            return collectionSeed;
        }

        if (!string.IsNullOrEmpty(simulationSeed))
        {
            return simulationSeed;
        }

        return string.IsNullOrEmpty(projectSeed) ? null : projectSeed;
    }

    /// <summary>
    /// Determines whether a seed text is exactly 64 lowercase hexadecimal characters.
    /// </summary>
    /// <param name="seedText">The seed text to test.</param>
    /// <returns><see langword="true"/> for a canonical 32-byte hexadecimal seed.</returns>
    public static bool IsCanonicalHexSeed(string? seedText) => ProjectMetadata.IsValidMasterSeed(seedText);

    /// <summary>
    /// Derives the 32-byte master seed from one seed text.
    /// </summary>
    /// <param name="seedText">The resolved, non-empty seed text.</param>
    /// <returns>The 32-byte master seed.</returns>
    /// <exception cref="ArgumentException"><paramref name="seedText"/> is empty or whitespace-only.</exception>
    public static ImmutableArray<byte> DeriveMasterSeed(string seedText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedText);

        return IsCanonicalHexSeed(seedText)
            ? [.. Convert.FromHexString(seedText)]
            : [.. SHA256.HashData(Encoding.UTF8.GetBytes(SeedTextDomain + seedText))];
    }

    /// <summary>
    /// Derives the 32-byte seed of one job from a seed text.
    /// </summary>
    /// <param name="seedText">The master seed text.</param>
    /// <param name="collectionIndex">The zero-based collection index.</param>
    /// <param name="configIndex">The zero-based configuration index.</param>
    /// <param name="jobIndex">The zero-based duplicate index.</param>
    /// <returns>The 32-byte job seed.</returns>
    public static ImmutableArray<byte> DeriveJobSeed(string seedText, int collectionIndex, int configIndex, int jobIndex) =>
        DeriveJobSeed(DeriveMasterSeed(seedText).AsSpan(), collectionIndex, configIndex, jobIndex);

    /// <summary>
    /// Derives the 32-byte seed of one job.
    /// </summary>
    /// <param name="masterSeed">The 32-byte master seed.</param>
    /// <param name="collectionIndex">The zero-based collection index.</param>
    /// <param name="configIndex">The zero-based configuration index.</param>
    /// <param name="jobIndex">The zero-based duplicate index.</param>
    /// <returns>The 32-byte job seed.</returns>
    /// <exception cref="ArgumentException"><paramref name="masterSeed"/> is not 32 bytes.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is negative.</exception>
    public static ImmutableArray<byte> DeriveJobSeed(
        ReadOnlySpan<byte> masterSeed,
        int collectionIndex,
        int configIndex,
        int jobIndex)
    {
        if (masterSeed.Length != SeedByteLength)
        {
            throw new ArgumentException(
                $"The master seed must contain exactly {SeedByteLength} bytes.",
                nameof(masterSeed));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(collectionIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(configIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(jobIndex);

        var domain = Encoding.UTF8.GetBytes(JobDomain);
        var projection = new byte[SeedByteLength + (3 * sizeof(uint)) + domain.Length];
        masterSeed.CopyTo(projection);
        BinaryPrimitives.WriteUInt32LittleEndian(projection.AsSpan(SeedByteLength), (uint)collectionIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(
            projection.AsSpan(SeedByteLength + sizeof(uint)),
            (uint)configIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(
            projection.AsSpan(SeedByteLength + (2 * sizeof(uint))),
            (uint)jobIndex);
        domain.CopyTo(projection.AsSpan(SeedByteLength + (3 * sizeof(uint))));

        return [.. SHA256.HashData(projection)];
    }
}

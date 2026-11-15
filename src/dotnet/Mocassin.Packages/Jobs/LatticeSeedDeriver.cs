// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Derives the deterministic Step 10 lattice seed from a job seed and a lattice-request hash.
/// </summary>
/// <remarks>
/// The projection is <c>SHA-256("mocassin/lattice/v1" || job_seed[32] || lattice_request_hash[32])</c>. It is
/// build-side only; the solver's ADR 0005 ChaCha12 stream is unchanged.
/// </remarks>
public static class LatticeSeedDeriver
{
    /// <summary>
    /// The length of a job seed, a lattice-request hash, or a lattice seed in bytes.
    /// </summary>
    public const int SeedByteLength = 32;

    /// <summary>
    /// The domain-separation prefix of the lattice seed.
    /// </summary>
    public const string LatticeDomain = "mocassin/lattice/v1";

    private static readonly byte[] domainBytes = Encoding.UTF8.GetBytes(LatticeDomain);

    /// <summary>
    /// Derives the 32-byte lattice seed of one job.
    /// </summary>
    /// <param name="jobSeed">The 32-byte job seed.</param>
    /// <param name="latticeRequestHash">The 32-byte canonical lattice-request hash.</param>
    /// <returns>The 32-byte lattice seed.</returns>
    /// <exception cref="ArgumentException">An input is not exactly 32 bytes.</exception>
    public static ImmutableArray<byte> DeriveLatticeSeed(
        ReadOnlySpan<byte> jobSeed,
        ReadOnlySpan<byte> latticeRequestHash)
    {
        ValidateInput(jobSeed, nameof(jobSeed));
        ValidateInput(latticeRequestHash, nameof(latticeRequestHash));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(domainBytes);
        hash.AppendData(jobSeed);
        hash.AppendData(latticeRequestHash);
        return [.. hash.GetHashAndReset()];
    }

    private static void ValidateInput(ReadOnlySpan<byte> value, string parameterName)
    {
        if (value.Length != SeedByteLength)
        {
            throw new ArgumentException(
                $"The input must contain exactly {SeedByteLength} bytes.",
                parameterName);
        }
    }
}

// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The deterministic build-side sampler of the Step 10 lattice generator.
/// </summary>
/// <remarks>
/// The stream is a SHA-256 counter DRBG:
/// <c>block_i = SHA-256("mocassin/lattice-rng/v1" || lattice_seed[32] || u64_le(counter++))</c> and
/// <c>next_u64</c> is the first eight bytes of the block interpreted little-endian. <see cref="NextBounded"/>
/// uses Lemire's 64-bit multiply-and-reject method with a 128-bit intermediate. Sampling without replacement is
/// a partial Fisher-Yates shuffle over the candidate list sorted by ascending linear position. This type is not
/// thread-safe and is deliberately separate from the solver's ChaCha12 stream.
/// </remarks>
public sealed class LatticeSampler
{
    /// <summary>
    /// The required seed length in bytes.
    /// </summary>
    public const int SeedByteLength = 32;

    /// <summary>
    /// The domain-separation prefix of the counter stream.
    /// </summary>
    public const string RandomDomain = "mocassin/lattice-rng/v1";

    private const int counterByteLength = sizeof(ulong);

    private static readonly byte[] domainBytes = Encoding.UTF8.GetBytes(RandomDomain);

    private readonly byte[] state;
    private readonly int counterOffset;
    private ulong counter;

    /// <summary>
    /// Initializes the counter stream from a lattice seed.
    /// </summary>
    /// <param name="seed">The 32-byte lattice seed.</param>
    /// <exception cref="ArgumentException"><paramref name="seed"/> is not exactly 32 bytes.</exception>
    public LatticeSampler(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != SeedByteLength)
        {
            throw new ArgumentException(
                $"The lattice seed must contain exactly {SeedByteLength} bytes.",
                nameof(seed));
        }

        state = new byte[domainBytes.Length + SeedByteLength + counterByteLength];
        domainBytes.CopyTo(state, 0);
        seed.CopyTo(state.AsSpan(domainBytes.Length));
        counterOffset = domainBytes.Length + SeedByteLength;
        counter = 0;
    }

    /// <summary>
    /// Returns the next 64-bit value of the counter stream.
    /// </summary>
    /// <returns>The little-endian first eight bytes of the next SHA-256 block.</returns>
    public ulong NextUInt64()
    {
        BinaryPrimitives.WriteUInt64LittleEndian(state.AsSpan(counterOffset), counter);
        counter++;
        var block = SHA256.HashData(state);
        return BinaryPrimitives.ReadUInt64LittleEndian(block);
    }

    /// <summary>
    /// Returns a uniformly distributed value in <c>[0, upper)</c>.
    /// </summary>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>The bounded value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="upper"/> is zero.</exception>
    public ulong NextBounded(ulong upper)
    {
        ArgumentOutOfRangeException.ThrowIfZero(upper);

        var threshold = (0UL - upper) % upper;
        while (true)
        {
            var product = (UInt128)NextUInt64() * upper;
            var low = (ulong)product;
            if (low >= threshold)
            {
                return (ulong)(product >> 64);
            }
        }
    }

    /// <summary>
    /// Selects a deterministic, uniformly distributed sample without replacement.
    /// </summary>
    /// <param name="candidates">
    /// The candidate linear positions. The list is copied and sorted before sampling, so caller order never
    /// changes the result.
    /// </param>
    /// <param name="count">The number of values to select.</param>
    /// <returns>The selected candidate values in selection order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is negative or exceeds the candidate count.
    /// </exception>
    public ImmutableArray<int> SampleWithoutReplacement(ReadOnlySpan<int> candidates, int count)
    {
        if (count < 0 || count > candidates.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                $"The sample size must be within 0..{candidates.Length}.");
        }

        if (count == 0)
        {
            return [];
        }

        var buffer = candidates.ToArray();
        Array.Sort(buffer);
        for (var index = 0; index < count; index++)
        {
            var swapIndex = index + (int)NextBounded((ulong)(buffer.Length - index));
            (buffer[index], buffer[swapIndex]) = (buffer[swapIndex], buffer[index]);
        }

        return [.. buffer.AsSpan(0, count)];
    }
}

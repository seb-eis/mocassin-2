// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Signatures;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// Computes the canonical Step 11 lattice signature over the ADR 0010 MessagePack projection.
/// </summary>
/// <remarks>
/// The projection is a fixed five-field MessagePack array:
/// <c>formatVersion</c>, <c>modelSignature</c>, <c>latticeSize</c>, <c>positionCount</c>, and
/// <c>particles</c>. It binds the payload format version, the referenced compiled-model identity, the supercell
/// size, the P1 position count, and the row-major particle array. Build metadata and the doping summary are
/// excluded, so byte-identical initial lattices deduplicate across jobs. The domain prefix is hashed before the
/// projection.
/// </remarks>
public static class LatticeSignatureV1
{
    /// <summary>The signature domain prefix.</summary>
    public const string Domain = "mocassin/lattice-signature/v1";

    /// <summary>The projection format version written by this implementation.</summary>
    public const ulong FormatVersion = PackageFormatConstants.LatticePayloadFormatVersion;

    /// <summary>
    /// Computes the canonical lattice projection bytes.
    /// </summary>
    /// <param name="modelSignature">The referenced compiled-model signature.</param>
    /// <param name="payload">The initial-lattice payload.</param>
    /// <returns>The canonical projection bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="modelSignature"/> is empty, the payload version is unsupported, the lattice size is not
    /// three positive values, the position count is zero, or the particle array length does not equal
    /// <c>a × b × c × positionCount</c>.
    /// </exception>
    public static byte[] ComputeProjection(string modelSignature, LatticePayload payload)
    {
        Validate(modelSignature, payload);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new CanonicalProjectionWriter(buffer);
        writer.WriteArrayHeader(5);
        writer.WriteUInt64(payload.FormatVersion);
        writer.WriteString(modelSignature);
        writer.WriteArrayHeader(3);
        foreach (var size in payload.LatticeSize)
        {
            writer.WriteUInt64(size);
        }

        writer.WriteUInt64(payload.PositionCount);
        writer.WriteBinary(payload.Lattice.AsSpan());
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Computes the domain-separated SHA-256 lattice signature.
    /// </summary>
    /// <param name="modelSignature">The referenced compiled-model signature.</param>
    /// <param name="payload">The initial-lattice payload.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    public static string Compute(string modelSignature, LatticePayload payload)
    {
        var projection = ComputeProjection(modelSignature, payload);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(Domain));
        hash.AppendData([0]);
        hash.AppendData(projection);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Validate(string modelSignature, LatticePayload payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelSignature);
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.FormatVersion != PackageFormatConstants.LatticePayloadFormatVersion)
        {
            throw new ArgumentException(
                $"Unsupported lattice payload format version {payload.FormatVersion}.",
                nameof(payload));
        }

        if (payload.LatticeSize.IsDefault || payload.LatticeSize.Length != 3)
        {
            throw new ArgumentException("The lattice size must contain exactly three values.", nameof(payload));
        }

        if (payload.Lattice.IsDefault)
        {
            throw new ArgumentException("The particle array is not initialized.", nameof(payload));
        }

        if (payload.PositionCount == 0)
        {
            throw new ArgumentException("The position count must be positive.", nameof(payload));
        }

        ulong product = 1;
        foreach (var size in payload.LatticeSize)
        {
            if (size == 0)
            {
                throw new ArgumentException("Every lattice size must be positive.", nameof(payload));
            }

            product *= size;
        }

        product *= payload.PositionCount;
        if (product > int.MaxValue)
        {
            throw new ArgumentException(
                "The linearized lattice length exceeds the signed 32-bit index limit.",
                nameof(payload));
        }

        if (product != (ulong)payload.Lattice.Length)
        {
            throw new ArgumentException(
                "The particle array length must equal a × b × c × positionCount.",
                nameof(payload));
        }
    }
}

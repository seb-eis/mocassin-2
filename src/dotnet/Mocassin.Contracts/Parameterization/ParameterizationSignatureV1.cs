// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Signatures;

namespace Mocassin.Contracts.Parameterization;

/// <summary>
/// Computes the canonical Step 09 parameterization signature over the ADR 0010 MessagePack projection.
/// </summary>
/// <remarks>
/// The projection is a fixed six-field MessagePack array: <c>formatVersion</c>, <c>modelSignature</c>,
/// <c>pairTables</c>, <c>groupSets</c>, <c>siteEnergies</c>, and <c>transitionRules</c>. It contains the
/// compiled-model identity and the applied values only. Every float is float64, quantized to 12 decimals with
/// round half away from zero, and negative zero is normalized to positive zero. The domain prefix is hashed
/// before the projection.
/// </remarks>
public static class ParameterizationSignatureV1
{
    /// <summary>The signature domain prefix.</summary>
    public const string Domain = "mocassin/parameterization-signature/v1";

    /// <summary>The projection format version.</summary>
    public const ulong FormatVersion = 1;

    /// <summary>
    /// Computes the canonical parameterization projection bytes.
    /// </summary>
    /// <param name="modelSignature">The Step 08 compiled-model signature.</param>
    /// <param name="values">The dense applied values.</param>
    /// <returns>The canonical projection bytes.</returns>
    public static byte[] ComputeProjection(string modelSignature, ParameterizationValues values)
    {
        Validate(modelSignature, values);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new CanonicalProjectionWriter(buffer);
        WriteProjection(modelSignature, values, ref writer);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Computes the domain-separated SHA-256 parameterization signature.
    /// </summary>
    /// <param name="modelSignature">The Step 08 compiled-model signature.</param>
    /// <param name="values">The dense applied values.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    /// <remarks>
    /// The projection is written directly into an <see cref="IncrementalHash"/>-backed buffer, so the method
    /// does not materialize a second copy of large tables. Use <see cref="ComputeProjection"/> only for golden
    /// fixtures and diagnostics.
    /// </remarks>
    public static string Compute(string modelSignature, ParameterizationValues values)
    {
        Validate(modelSignature, values);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes(Domain));
        hash.AppendData([0]);
        var buffer = new IncrementalHashBufferWriter(hash);
        var writer = new CanonicalProjectionWriter(buffer);
        WriteProjection(modelSignature, values, ref writer);
        writer.Flush();
        buffer.Flush();
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Validate(string modelSignature, ParameterizationValues values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelSignature);
        ArgumentNullException.ThrowIfNull(values);
    }

    private static void WriteProjection(
        string modelSignature,
        ParameterizationValues values,
        ref CanonicalProjectionWriter writer)
    {
        writer.WriteArrayHeader(6);
        writer.WriteUInt64(FormatVersion);
        writer.WriteString(modelSignature);
        WriteNestedDoubleArrays(ref writer, values.PairTables);
        WriteNestedDoubleArrays(ref writer, values.GroupSets);
        WriteDoubleArray(ref writer, values.SiteEnergies);
        WriteDoubleArray(ref writer, values.TransitionRuleFrequencies);
    }

    private static void WriteNestedDoubleArrays(
        ref CanonicalProjectionWriter writer,
        ImmutableArray<ImmutableArray<double>> tables)
    {
        var rows = tables.IsDefault ? ImmutableArray<ImmutableArray<double>>.Empty : tables;
        writer.WriteArrayHeader(rows.Length);
        foreach (var row in rows)
        {
            WriteDoubleArray(ref writer, row);
        }
    }

    private static void WriteDoubleArray(ref CanonicalProjectionWriter writer, ImmutableArray<double> values)
    {
        var items = values.IsDefault ? ImmutableArray<double>.Empty : values;
        writer.WriteArrayHeader(items.Length);
        foreach (var value in items)
        {
            writer.WriteDouble(value);
        }
    }

    private sealed class IncrementalHashBufferWriter : IBufferWriter<byte>
    {
        private const int initialBufferSize = 64 * 1024;
        private readonly IncrementalHash hash;
        private byte[] buffer = new byte[initialBufferSize];
        private int written;

        public IncrementalHashBufferWriter(IncrementalHash hash)
        {
            this.hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public void Advance(int count)
        {
            if (count < 0 || written + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            written += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsMemory(written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return buffer.AsSpan(written);
        }

        public void Flush()
        {
            if (written == 0)
            {
                return;
            }

            hash.AppendData(buffer.AsSpan(0, written));
            written = 0;
        }

        private void EnsureCapacity(int sizeHint)
        {
            var required = Math.Max(sizeHint, 1);
            if (buffer.Length - written >= required)
            {
                return;
            }

            Flush();
            if (buffer.Length < required)
            {
                buffer = new byte[required];
            }
        }
    }
}

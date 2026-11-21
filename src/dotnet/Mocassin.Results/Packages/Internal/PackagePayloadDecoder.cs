// SPDX-License-Identifier: MIT

using System.IO.Compression;
using MessagePack;
using Mocassin.Contracts.Packages;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Read-only Zstd+MessagePack payload decoding with decompressed-size guards.
/// </summary>
/// <remarks>
/// The decoder is driven by the frame header, so it reads both packages written by the in-box encoder and
/// pre-amendment packages written by the former third-party codec. It rejects an empty payload, a
/// non-positive guard,
/// a malformed frame, or a declared bound above the guard before allocating the result. It returns the failure as
/// an error message; <c>PackageVerifierEngine</c> maps it to <c>PKG004</c>.
/// </remarks>
internal static class PackagePayloadDecoder
{
    private static readonly MessagePackSerializerOptions serializerOptions =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    /// <summary>
    /// Decodes a payload, returning <see langword="null"/> and an error message on any failure.
    /// </summary>
    /// <typeparam name="TPayload">The contract payload type.</typeparam>
    /// <param name="payload">The compressed bytes.</param>
    /// <param name="maxDecompressedBytes">The decompressed-size guard.</param>
    /// <param name="error">The failure reason, or <see langword="null"/>.</param>
    /// <returns>The decoded payload, or <see langword="null"/>.</returns>
    public static TPayload? TryDecode<TPayload>(
        ReadOnlySpan<byte> payload,
        long maxDecompressedBytes,
        out string? error)
    {
        if (!TryDecompress(payload, maxDecompressedBytes, out var destination, out var written, out error))
        {
            return default;
        }

        return Deserialize<TPayload>(destination.AsMemory(0, written), out error);
    }

    /// <summary>
    /// Decodes a model payload after applying the bounded cluster-expansion scan.
    /// </summary>
    /// <param name="payload">The compressed bytes.</param>
    /// <param name="maxDecompressedBytes">The decompressed-size guard.</param>
    /// <param name="maxClusterExpansionBytes">
    /// The cumulative typed cluster-expansion budget, <c>Σ(8E + 4I)</c>.
    /// </param>
    /// <param name="error">The failure reason, or <see langword="null"/>.</param>
    /// <returns>The decoded payload, or <see langword="null"/>.</returns>
    public static ModelPayload? TryDecodeModel(
        ReadOnlySpan<byte> payload,
        long maxDecompressedBytes,
        long maxClusterExpansionBytes,
        out string? error)
    {
        if (!TryDecompress(payload, maxDecompressedBytes, out var destination, out var written, out error))
        {
            return default;
        }

        try
        {
            ClusterExpansionScan.Validate(
                destination.AsMemory(0, written),
                maxClusterExpansionBytes);
        }
        catch (Exception exception) when (
            exception is MessagePackSerializationException or EndOfStreamException or
                InvalidOperationException or OverflowException)
        {
            error = $"The model payload cluster expansion scan failed: {exception.Message}";
            return default;
        }

        return Deserialize<ModelPayload>(destination.AsMemory(0, written), out error);
    }

    private static TPayload? Deserialize<TPayload>(ReadOnlyMemory<byte> payload, out string? error)
    {
        error = null;
        try
        {
            return MessagePackSerializer.Deserialize<TPayload>(payload, serializerOptions);
        }
        catch (MessagePackSerializationException exception)
        {
            error = $"The payload is not a valid MessagePack map: {exception.Message}";
            return default;
        }
    }

    private static bool TryDecompress(
        ReadOnlySpan<byte> payload,
        long maxDecompressedBytes,
        out byte[] destination,
        out int written,
        out string? error)
    {
        destination = [];
        written = 0;
        error = null;
        if (payload.IsEmpty)
        {
            error = "The payload blob is empty.";
            return false;
        }

        if (maxDecompressedBytes < 1)
        {
            error = "The decompressed-size guard is not positive.";
            return false;
        }

        if (!ZstandardDecoder.TryGetMaxDecompressedLength(payload, out long bound))
        {
            error = "The payload is not a valid Zstd frame.";
            return false;
        }

        if (bound > maxDecompressedBytes)
        {
            error = $"The payload declares {bound} decompressed bytes, above the {maxDecompressedBytes} guard.";
            return false;
        }

        if (bound > Array.MaxLength)
        {
            error = $"The payload declares {bound} decompressed bytes, above the maximum array length.";
            return false;
        }

        destination = new byte[(int)bound];
        if (!ZstandardDecoder.TryDecompress(payload, destination, out written))
        {
            error = "The payload is not a valid Zstd frame.";
            return false;
        }

        return true;
    }
}

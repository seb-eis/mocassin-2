// SPDX-License-Identifier: MIT

using System.IO.Compression;
using MessagePack;

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
        error = null;
        if (payload.IsEmpty)
        {
            error = "The payload blob is empty.";
            return default;
        }

        if (maxDecompressedBytes < 1)
        {
            error = "The decompressed-size guard is not positive.";
            return default;
        }

        if (!ZstandardDecoder.TryGetMaxDecompressedLength(payload, out long bound))
        {
            error = "The payload is not a valid Zstd frame.";
            return default;
        }

        if (bound > maxDecompressedBytes)
        {
            error = $"The payload declares {bound} decompressed bytes, above the {maxDecompressedBytes} guard.";
            return default;
        }

        if (bound > Array.MaxLength)
        {
            error = $"The payload declares {bound} decompressed bytes, above the maximum array length.";
            return default;
        }

        var destination = new byte[(int)bound];
        if (!ZstandardDecoder.TryDecompress(payload, destination, out int written))
        {
            error = "The payload is not a valid Zstd frame.";
            return default;
        }

        try
        {
            return MessagePackSerializer.Deserialize<TPayload>(
                destination.AsMemory(0, written),
                serializerOptions);
        }
        catch (MessagePackSerializationException exception)
        {
            error = $"The payload is not a valid MessagePack map: {exception.Message}";
            return default;
        }
    }
}

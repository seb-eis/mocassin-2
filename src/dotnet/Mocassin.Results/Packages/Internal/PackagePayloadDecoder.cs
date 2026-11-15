// SPDX-License-Identifier: MIT

using MessagePack;
using ZstdSharp;

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Read-only Zstd+MessagePack payload decoding with decompressed-size guards.
/// </summary>
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

        using var decompressor = new Decompressor();
        ulong declared;
        try
        {
            declared = Decompressor.GetDecompressedSize(payload);
        }
        catch (ZstdException exception)
        {
            error = $"The payload is not a valid Zstd frame: {exception.Message}";
            return default;
        }

        if (declared > (ulong)maxDecompressedBytes)
        {
            error = $"The payload declares {declared} decompressed bytes, above the {maxDecompressedBytes} guard.";
            return default;
        }

        byte[] messagePack;
        try
        {
            messagePack = decompressor.Unwrap(payload, (int)maxDecompressedBytes).ToArray();
        }
        catch (ZstdException exception)
        {
            error = $"The payload is not a valid Zstd frame: {exception.Message}";
            return default;
        }

        try
        {
            return MessagePackSerializer.Deserialize<TPayload>(messagePack, serializerOptions);
        }
        catch (MessagePackSerializationException exception)
        {
            error = $"The payload is not a valid MessagePack map: {exception.Message}";
            return default;
        }
    }
}

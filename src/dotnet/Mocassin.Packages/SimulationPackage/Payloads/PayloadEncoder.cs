// SPDX-License-Identifier: MIT

using MessagePack;
using Mocassin.Contracts.Packages;
using ZstdSharp;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Encodes and decodes the <c>.msl</c> payloads as standard Zstd frames around MessagePack maps.
/// </summary>
/// <remarks>
/// No dictionaries and no custom framing are used. Decoding rejects a frame whose declared or bounded
/// decompressed size exceeds the configured guard before allocating the result.
/// </remarks>
internal sealed class PayloadEncoder
{
    /// <summary>The default maximum decoded model-payload size.</summary>
    public const long DefaultMaxModelPayloadBytes = 512L * 1024 * 1024;

    private static readonly MessagePackSerializerOptions serializerOptions =
        MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

    private readonly int compressionLevel;

    /// <summary>
    /// Initializes an encoder.
    /// </summary>
    /// <param name="compressionLevel">The Zstd level in 1..19.</param>
    /// <exception cref="ArgumentOutOfRangeException">The level is outside 1..19.</exception>
    public PayloadEncoder(int compressionLevel)
    {
        if (compressionLevel is < 1 or > 19)
        {
            throw new ArgumentOutOfRangeException(
                nameof(compressionLevel),
                compressionLevel,
                "The compression level must be in 1..19.");
        }

        this.compressionLevel = compressionLevel;
    }

    /// <summary>
    /// Encodes a model payload as a Zstd-compressed MessagePack map.
    /// </summary>
    /// <param name="payload">The model payload.</param>
    /// <returns>The compressed bytes.</returns>
    public byte[] EncodeModel(ModelPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var messagePack = MessagePackSerializer.Serialize(payload, serializerOptions);
        using var compressor = new Compressor(compressionLevel);
        return compressor.Wrap(messagePack).ToArray();
    }

    /// <summary>
    /// Decodes a Zstd-compressed model payload.
    /// </summary>
    /// <param name="payload">The compressed bytes.</param>
    /// <param name="maxDecompressedBytes">The decompressed-size guard.</param>
    /// <returns>The decoded payload.</returns>
    /// <exception cref="InvalidDataException">The frame is not a valid Zstd frame or exceeds the guard.</exception>
    public static ModelPayload DecodeModel(
        ReadOnlySpan<byte> payload,
        long maxDecompressedBytes = DefaultMaxModelPayloadBytes)
    {
        if (payload.IsEmpty)
        {
            throw new InvalidDataException("The model payload is empty.");
        }

        if (maxDecompressedBytes < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDecompressedBytes),
                maxDecompressedBytes,
                "The decompressed-size guard must be positive.");
        }

        using var decompressor = new Decompressor();
        ulong declared;
        try
        {
            declared = Decompressor.GetDecompressedSize(payload);
        }
        catch (ZstdException exception)
        {
            throw new InvalidDataException("The model payload is not a valid Zstd frame.", exception);
        }

        if (declared > (ulong)maxDecompressedBytes)
        {
            throw new InvalidDataException(
                $"The model payload declares {declared} decompressed bytes, above the {maxDecompressedBytes} guard.");
        }

        byte[] messagePack;
        try
        {
            messagePack = decompressor.Unwrap(payload, (int)maxDecompressedBytes).ToArray();
        }
        catch (ZstdException exception)
        {
            throw new InvalidDataException("The model payload is not a valid Zstd frame.", exception);
        }

        try
        {
            return MessagePackSerializer.Deserialize<ModelPayload>(messagePack, serializerOptions);
        }
        catch (MessagePackSerializationException exception)
        {
            throw new InvalidDataException("The model payload is not a valid MessagePack model map.", exception);
        }
    }
}

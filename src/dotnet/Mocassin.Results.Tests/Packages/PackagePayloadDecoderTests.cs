// SPDX-License-Identifier: MIT

using System.IO.Compression;
using MessagePack;
using Mocassin.Contracts.Packages;
using Mocassin.Results.Packages.Internal;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises the read-only payload decoder's bounds, error surface, and foreign-frame acceptance.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class PackagePayloadDecoderTests
{
    private const string fixtureDirectory = "tests/contracts/packages/amend-1";

    [Fact]
    public void EmptyPayloadAndNonPositiveGuardReturnTheirDocumentedErrors()
    {
        var empty = PackagePayloadDecoder.TryDecode<JobPayload>(
            ReadOnlySpan<byte>.Empty,
            8L * 1024 * 1024,
            out var emptyError);

        Assert.Null(empty);
        Assert.Equal("The payload blob is empty.", emptyError);

        var frame = File.ReadAllBytes(
            RepositoryPaths.Combine(fixtureDirectory, "golden", "sample-job-2-l3.zst"));
        var zeroGuard = PackagePayloadDecoder.TryDecode<JobPayload>(frame, 0, out var guardError);

        Assert.Null(zeroGuard);
        Assert.Equal("The decompressed-size guard is not positive.", guardError);
    }

    [Fact]
    public void FrameAboveTheGuardReturnsTheDeclaredBoundError()
    {
        var frame = File.ReadAllBytes(
            RepositoryPaths.Combine(fixtureDirectory, "golden", "sample-job-2-l3.zst"));

        var decoded = PackagePayloadDecoder.TryDecode<JobPayload>(frame, 8, out var error);

        Assert.Null(decoded);
        Assert.NotNull(error);
        Assert.Contains("above the 8 guard", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentSizeLessFrameDecodesWithinTheGuard()
    {
        var raw = File.ReadAllBytes(
            RepositoryPaths.Combine(fixtureDirectory, "payloads", "sample-job-2.bin"));
        var expected = MessagePackSerializer.Deserialize<JobPayload>(raw);
        var frame = CompressWithoutContentSize(raw);

        Assert.True(ZstandardDecoder.TryGetMaxDecompressedLength(frame, out long bound));
        Assert.True(bound >= raw.Length, $"The stream frame bound {bound} should be an upper bound.");

        var decoded = PackagePayloadDecoder.TryDecode<JobPayload>(frame, 8L * 1024 * 1024, out var error);

        Assert.Null(error);
        Assert.NotNull(decoded);
        Assert.Equal(expected.TemperatureK, decoded!.TemperatureK);
        Assert.Equal(expected.SimulationIndex, decoded.SimulationIndex);
        Assert.Equal(expected.Flags, decoded.Flags);
    }

    [Fact]
    public void CorruptAndTruncatedFramesReturnAnError()
    {
        var frame = File.ReadAllBytes(
            RepositoryPaths.Combine(fixtureDirectory, "golden", "sample-job-2-l3.zst"));

        Assert.Null(PackagePayloadDecoder.TryDecode<JobPayload>([0x01, 0x02, 0x03], 8L * 1024 * 1024, out var garbage));
        Assert.Contains("not a valid Zstd frame", garbage, StringComparison.Ordinal);

        Assert.Null(PackagePayloadDecoder.TryDecode<JobPayload>(
            frame.AsSpan(0, frame.Length / 2),
            8L * 1024 * 1024,
            out var truncated));
        Assert.Contains("not a valid Zstd frame", truncated, StringComparison.Ordinal);
    }

    private static byte[] CompressWithoutContentSize(byte[] raw)
    {
        using var stream = new MemoryStream();
        using (var compressor = new ZstandardStream(stream, CompressionMode.Compress, leaveOpen: true))
        {
            compressor.Write(raw);
        }

        return stream.ToArray();
    }
}

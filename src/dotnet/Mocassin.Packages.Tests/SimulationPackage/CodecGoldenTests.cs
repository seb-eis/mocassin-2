// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Text.Json;
using MessagePack;
using Mocassin.Contracts.Packages;
using Mocassin.Packages.SimulationPackage.Payloads;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Verifies the in-box Zstandard encoder against the pre-amendment golden frames and the decode bounds.
/// </summary>
/// <remarks>
/// The goldens in <c>tests/contracts/packages/amend-1/</c> were captured with the former third-party codec from
/// the real payload bytes. Byte equality is an observation, not a format contract: a later runtime patch may
/// change the bytes and re-baseline the goldens with a recorded note, but the level → quality mapping must not
/// change (amendment 1 §9.4).
/// </remarks>
[Trait("Tier", "PR-fast")]
public sealed class CodecGoldenTests
{
    private const string fixtureDirectory = "tests/contracts/packages/amend-1";

    [Fact]
    public void NativeEncoderReproducesEveryCheckedInGoldenFrame()
    {
        var directory = RepositoryPaths.Combine(fixtureDirectory);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "goldens.json")));
        var payloads = manifest.RootElement.GetProperty("payloads").EnumerateArray()
            .ToDictionary(
                static payload => payload.GetProperty("id").GetString()!,
                static payload => (Kind: payload.GetProperty("kind").GetString()!,
                                   Path: payload.GetProperty("path").GetString()!));
        var checkedFrames = 0;
        foreach (var frame in manifest.RootElement.GetProperty("frames").EnumerateArray())
        {
            var id = frame.GetProperty("payloadId").GetString()!;
            var level = frame.GetProperty("level").GetInt32();
            var payload = payloads[id];
            var raw = File.ReadAllBytes(Path.Combine(directory, payload.Path));
            var expected = File.ReadAllBytes(Path.Combine(directory, frame.GetProperty("path").GetString()!));

            var encoded = Encode(raw, payload.Kind, level);

            Assert.True(
                expected.AsSpan().SequenceEqual(encoded),
                $"The level-{level} frame for '{id}' does not match the checked-in golden.");
            checkedFrames++;
        }

        Assert.Equal(24, checkedFrames);
    }

    [Fact]
    public void GoldenManifestSeedVectorIsTheSampleModel()
    {
        var directory = RepositoryPaths.Combine(fixtureDirectory);
        var raw = File.ReadAllBytes(Path.Combine(directory, "payloads", "sample-model-1.bin"));

        var level3 = Encode(raw, "model", 3);
        var level9 = Encode(raw, "model", 9);

        Assert.Equal(1415, level3.Length);
        Assert.Equal(1387, level9.Length);
        Assert.NotEqual(level3, level9);
    }

    [Fact]
    public void AuthoredLevelOutsideOneToNineteenThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadEncoder(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadEncoder(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PayloadEncoder(20));
        _ = new PayloadEncoder(1);
        _ = new PayloadEncoder(19);
    }

    [Fact]
    public void EmptyPayloadAndNonPositiveGuardKeepTheWriterContract()
    {
        var directory = RepositoryPaths.Combine(fixtureDirectory);
        var raw = File.ReadAllBytes(Path.Combine(directory, "payloads", "sample-model-1.bin"));
        var frame = Encode(raw, "model", 3);

        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel(ReadOnlySpan<byte>.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => PayloadEncoder.DecodeModel(frame, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PayloadEncoder.DecodeModel(frame, -1));
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel([0x01, 0x02, 0x03]));
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel(frame.AsSpan(0, frame.Length / 2)));
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel(frame, maxDecompressedBytes: 8));
    }

    [Fact]
    public void ContentSizeLessFrameIsAcceptedWithinTheGuard()
    {
        var directory = RepositoryPaths.Combine(fixtureDirectory);
        var raw = File.ReadAllBytes(Path.Combine(directory, "payloads", "sample-job-2.bin"));
        var expected = MessagePackSerializer.Deserialize<JobPayload>(raw);
        var streamFrame = CompressWithoutContentSize(raw);

        Assert.True(ZstandardDecoder.TryGetMaxDecompressedLength(streamFrame, out long bound));
        Assert.True(bound >= raw.Length, $"The stream frame bound {bound} should be an upper bound.");

        var decoded = PayloadEncoder.DecodeJob(streamFrame);
        Assert.Equal(expected.TemperatureK, decoded.TemperatureK);
        Assert.Equal(expected.SimulationIndex, decoded.SimulationIndex);
        Assert.Equal(expected.Flags, decoded.Flags);

        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeJob(streamFrame, maxDecompressedBytes: 8));
    }

    private static byte[] Encode(byte[] raw, string kind, int level)
    {
        var encoder = new PayloadEncoder(level);
        return kind switch
        {
            "model" => encoder.EncodeModel(MessagePackSerializer.Deserialize<ModelPayload>(raw)),
            "lattice" => encoder.EncodeLattice(MessagePackSerializer.Deserialize<LatticePayload>(raw)),
            "job" => encoder.EncodeJob(MessagePackSerializer.Deserialize<JobPayload>(raw)),
            _ => throw new InvalidOperationException($"Unknown payload kind '{kind}'."),
        };
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

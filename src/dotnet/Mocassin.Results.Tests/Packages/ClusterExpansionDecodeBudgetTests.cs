// SPDX-License-Identifier: MIT

using System.Buffers;
using System.IO.Compression;
using MessagePack;
using Mocassin.Results.Packages;
using Mocassin.Results.Packages.Internal;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises the non-allocating cluster expansion pre-scan and its decode-time budget.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterExpansionDecodeBudgetTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine("tests/contracts/packages/step-11-amend-2");

    [Fact]
    public void TheRealFixtureLoadsAtItsExactTypedCostAndIsRejectedOneByteBelow()
    {
        var frame = Compress(File.ReadAllBytes(Path.Combine(FixtureDirectory, "payloads", "model-1.bin")));

        var accepted = PackagePayloadDecoder.TryDecodeModel(
            frame,
            512L * 1024 * 1024,
            maxClusterExpansionBytes: 96,
            out var acceptedError);
        Assert.Null(acceptedError);
        Assert.NotNull(accepted);

        var rejected = PackagePayloadDecoder.TryDecodeModel(
            frame,
            512L * 1024 * 1024,
            maxClusterExpansionBytes: 95,
            out var rejectedError);
        Assert.Null(rejected);
        Assert.NotNull(rejectedError);
        Assert.Contains("decode budget", rejectedError, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnequalPairIsRejectedAtScanTime()
    {
        var payload = BuildPayload((ref MessagePackWriter writer) =>
        {
            writer.WriteMapHeader(2);
            writer.Write("expanded_codes");
            writer.WriteArrayHeader(2);
            writer.Write(1UL);
            writer.Write(2UL);
            writer.Write("canonical_indices");
            writer.WriteArrayHeader(1);
            writer.Write(0U);
        });

        var error = DecodeError(payload, maxClusterExpansionBytes: 1024);
        Assert.Contains("expanded_codes has 2 entries but canonical_indices has 1", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ADuplicateOuterKeyIsRejectedAtScanTime()
    {
        var payload = BuildPayload(
            static (ref MessagePackWriter writer) => writer.WriteNil(),
            duplicateOuterKey: true);

        var error = DecodeError(payload, maxClusterExpansionBytes: 1024);
        Assert.Contains("repeats energy.cluster_tables.code_expansions", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ADuplicateNestedKeyIsRejectedAtScanTime()
    {
        var payload = BuildPayload((ref MessagePackWriter writer) =>
        {
            writer.WriteMapHeader(3);
            writer.Write("expanded_codes");
            writer.WriteArrayHeader(1);
            writer.Write(1UL);
            writer.Write("expanded_codes");
            writer.WriteArrayHeader(1);
            writer.Write(2UL);
            writer.Write("canonical_indices");
            writer.WriteArrayHeader(1);
            writer.Write(0U);
        });

        var error = DecodeError(payload, maxClusterExpansionBytes: 1024);
        Assert.Contains("repeats energy.cluster_tables.code_expansions.expanded_codes", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ANestedMessagePackBombIsRejectedByTheDepthBound()
    {
        var payload = BuildPayload((ref MessagePackWriter writer) =>
        {
            writer.WriteMapHeader(3);
            writer.Write("expanded_codes");
            writer.WriteArrayHeader(0);
            writer.Write("canonical_indices");
            writer.WriteArrayHeader(0);
            writer.Write("unknown");
            for (var index = 0; index < 64; index++)
            {
                writer.WriteArrayHeader(1);
            }

            writer.WriteNil();
        });

        var error = DecodeError(payload, maxClusterExpansionBytes: 1024);
        Assert.Contains("decode scan depth", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ALoweredReaderOptionMapsToPkg004AndARaisedOptionAccepts()
    {
        var package = Path.Combine(FixtureDirectory, "step-11-amend-2.msl");

        var rejected = await new PackageVerifier().VerifyAsync(
            package,
            new PackageVerificationOptions { MaxClusterExpansionBytes = 95 });
        Assert.False(rejected.IsValid);
        Assert.Contains(rejected.Diagnostics, static diagnostic => diagnostic.Code == "PKG004");

        var accepted = await new PackageVerifier().VerifyAsync(
            package,
            new PackageVerificationOptions { MaxClusterExpansionBytes = 96 });
        Assert.True(accepted.IsValid);
    }

    private static string DecodeError(byte[] payload, long maxClusterExpansionBytes)
    {
        var frame = Compress(payload);
        var decoded = PackagePayloadDecoder.TryDecodeModel(
            frame,
            512L * 1024 * 1024,
            maxClusterExpansionBytes,
            out var error);
        Assert.Null(decoded);
        Assert.NotNull(error);
        return error;
    }

    private delegate void WriterAction(ref MessagePackWriter writer);

    private static byte[] BuildPayload(
        WriterAction writeCodeExpansions,
        bool duplicateOuterKey = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteMapHeader(1);
        writer.Write("energy");
        writer.WriteMapHeader(1);
        writer.Write("cluster_tables");
        writer.WriteArrayHeader(1);
        writer.WriteMapHeader(duplicateOuterKey ? 2 : 1);
        writer.Write("code_expansions");
        writeCodeExpansions(ref writer);

        if (duplicateOuterKey)
        {
            writer.Write("code_expansions");
            writer.WriteNil();
        }

        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static byte[] Compress(byte[] raw)
    {
        using var stream = new MemoryStream();
        using (var compressor = new ZstandardStream(stream, CompressionMode.Compress, leaveOpen: true))
        {
            compressor.Write(raw);
        }

        return stream.ToArray();
    }
}

// SPDX-License-Identifier: MIT

using System.Buffers;
using System.IO.Compression;
using FsCheck.Xunit;
using MessagePack;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Project;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Property tests for the model payload: arbitrary real parameterized models round-trip losslessly, and the
/// parameterization signature is stable under negative zero, shuffled sparse-map order, and the Zstd level.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class PayloadPropertyTests
{
    private static readonly Lazy<(Project Project, CompiledModel Compiled)> ceria =
        new(() => ParameterizationTestSupport.LoadAndCompile(ParameterizationTestSupport.Ceria));

    [Property(MaxTest = 12)]
    public void ArbitraryParameterizedModelsEncodeAndDecodeToTheSameLogicalPayload(int seed)
    {
        var (project, compiled) = ceria.Value;
        var template = ParameterizationTestSupport.WithSparseExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, compiled),
            compiled,
            seed);
        var applied = new ParameterizationService().Apply(project, compiled, template).Model!;
        var payload = ModelPayloadMapper.Map(applied);
        var canonicalMessagePack = MessagePackSerializer.Serialize(payload);

        foreach (var level in (int[])[1, 3, 19])
        {
            var bytes = new PayloadEncoder(level).EncodeModel(payload);
            var decoded = PayloadEncoder.DecodeModel(bytes);

            Assert.Equal(canonicalMessagePack, MessagePackSerializer.Serialize(decoded));
            Assert.Equal(applied.Signature, decoded.Signature);
            Assert.Equal(
                applied.Signature,
                ParameterizationSignatureV1.Compute(
                    decoded.ModelSignature,
                    ModelPayloadMapper.ToParameterizationValues(decoded)));
        }
    }

    [Property(MaxTest = 12)]
    public void SignatureIsStableUnderNegativeZeroShuffledOrderAndCompressionLevel(int seed)
    {
        var (project, compiled) = ceria.Value;
        var template = ParameterizationTestSupport.WithSparseExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, compiled),
            compiled,
            seed);
        var applied = new ParameterizationService().Apply(project, compiled, template).Model!;
        var shuffled = new ParameterizationService().Apply(
            project,
            compiled,
            ParameterizationTestSupport.Shuffle(template, seed)).Model!;

        // Shuffling the sparse template maps must not change the applied values or the signature.
        Assert.Equal(applied.Signature, shuffled.Signature);

        // Negative zero is normalized by the canonical projection.
        var values = applied.Values;
        Assert.False(values.PairTables.IsDefaultOrEmpty);
        Assert.False(values.PairTables[0].IsDefaultOrEmpty);
        var negativeZero = new ParameterizationValues(
            values.PairTables.SetItem(0, values.PairTables[0].SetItem(0, -0.0)),
            values.GroupSets,
            values.SiteEnergies,
            values.TransitionRuleFrequencies);
        var positiveZero = negativeZero with
        {
            PairTables = negativeZero.PairTables.SetItem(0, negativeZero.PairTables[0].SetItem(0, 0.0)),
        };
        Assert.Equal(
            ParameterizationSignatureV1.Compute(applied.ModelSignature, positiveZero),
            ParameterizationSignatureV1.Compute(applied.ModelSignature, negativeZero));
        Assert.Equal(
            applied.Signature,
            ParameterizationSignatureV1.Compute(applied.ModelSignature, positiveZero));

        // Zstd level and MessagePack map order must not change the decoded signature.
        var payload = ModelPayloadMapper.Map(applied);
        var signatures = new List<string>();
        foreach (var level in (int[])[1, 3, 19])
        {
            signatures.Add(SignatureOf(PayloadEncoder.DecodeModel(new PayloadEncoder(level).EncodeModel(payload))));
        }

        var reordered = ReorderTopLevelMap(new PayloadEncoder(3).EncodeModel(payload), seed);
        signatures.Add(SignatureOf(PayloadEncoder.DecodeModel(reordered)));

        Assert.All(signatures, signature => Assert.Equal(applied.Signature, signature));
    }

    private static string SignatureOf(ModelPayload payload) =>
        ParameterizationSignatureV1.Compute(
            payload.ModelSignature,
            ModelPayloadMapper.ToParameterizationValues(payload));

    /// <summary>
    /// Decompresses a real payload frame, shuffles its top-level MessagePack map entries, and recompresses it
    /// with the real codec so the decoder sees the same logical map in a different field order.
    /// </summary>
    private static byte[] ReorderTopLevelMap(byte[] frame, int seed)
    {
        if (!ZstandardDecoder.TryGetMaxDecompressedLength(frame, out long bound))
        {
            throw new InvalidDataException("The test payload is not a valid Zstd frame.");
        }

        var messagePack = new byte[bound];
        if (!ZstandardDecoder.TryDecompress(frame, messagePack, out int written))
        {
            throw new InvalidDataException("The test payload could not be decompressed.");
        }

        var sequence = new ReadOnlySequence<byte>(messagePack.AsMemory(0, written));
        var reader = new MessagePackReader(sequence);
        var count = reader.ReadMapHeader();
        var entries = new List<(byte[] Key, byte[] Value)>(count);
        for (var index = 0; index < count; index++)
        {
            entries.Add((reader.ReadRaw().ToArray(), reader.ReadRaw().ToArray()));
        }

        var random = new Random(seed);
        var order = Enumerable.Range(0, entries.Count).OrderBy(_ => random.Next()).ToArray();
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteMapHeader(entries.Count);
        foreach (var index in order)
        {
            writer.WriteRaw(entries[index].Key);
            writer.WriteRaw(entries[index].Value);
        }

        writer.Flush();

        var shuffled = buffer.WrittenSpan.ToArray();
        using var encoder = new ZstandardEncoder(new ZstandardCompressionOptions { Quality = 3 });
        encoder.SetSourceLength(shuffled.Length);
        var destination = new byte[ZstandardEncoder.GetMaxCompressedLength(shuffled.Length)];
        var status = encoder.Compress(
            shuffled,
            destination,
            out _,
            out int compressed,
            isFinalBlock: true);
        Assert.Equal(OperationStatus.Done, status);
        return destination.AsSpan(0, compressed).ToArray();
    }
}

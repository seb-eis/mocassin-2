// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.IO.Compression;
using MessagePack;
using Microsoft.Data.Sqlite;
using Mocassin.Contracts.Packages;
using Mocassin.Results.Packages;
using Mocassin.Results.Packages.Internal;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises the model verifier and verifier engine for the optional cluster code expansion map.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterExpansionVerificationTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine("tests/contracts/packages/step-11-amend-2");

    [Fact]
    public void AValidMapVerifiesAndASupersetCodeIsAccepted()
    {
        var payload = DecodeModel();
        var baseTable = Assert.Single(payload.Energy.ClusterTables);
        var expansions = Assert.IsType<CodeExpansionPayload>(baseTable.CodeExpansions);

        var codes = ImmutableArray.CreateBuilder<ulong>();
        codes.Add(64);
        codes.AddRange(expansions.ExpandedCodes);
        var indices = ImmutableArray.CreateBuilder<uint>();
        indices.Add(0);
        indices.AddRange(expansions.CanonicalIndices);

        var mutated = WithExpansions(payload, new CodeExpansionPayload([.. codes], [.. indices]));
        var errors = new List<string>();
        PackageVerificationChecks.ValidateModel(mutated, errors);

        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(HostileMaps))]
    public void HostileMapsProduceTheExpectedStructuralError(string expected, Func<CodeExpansionPayload, CodeExpansionPayload> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        var payload = DecodeModel();
        var table = Assert.Single(payload.Energy.ClusterTables);
        var expansions = Assert.IsType<CodeExpansionPayload>(table.CodeExpansions);

        var errors = new List<string>();
        PackageVerificationChecks.ValidateModel(
            WithExpansions(payload, mutate(expansions)),
            errors);

        Assert.Contains(errors, error => error.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheVerifierEngineMapsAHostileMapToPkg010()
    {
        using var temp = new TemporaryDirectory();
        var package = Path.Combine(temp.Path, "hostile.msl");
        File.Copy(
            Path.Combine(FixtureDirectory, "step-11-amend-2.msl"),
            package);

        var payload = DecodeModel();
        var table = Assert.Single(payload.Energy.ClusterTables);
        var expansions = Assert.IsType<CodeExpansionPayload>(table.CodeExpansions);
        var codes = expansions.ExpandedCodes.ToArray();
        (codes[2], codes[3]) = (codes[3], codes[2]);
        var hostile = WithExpansions(
            payload,
            new CodeExpansionPayload([.. codes], expansions.CanonicalIndices));
        await ReplaceModelPayloadAsync(package, hostile);

        var report = await new PackageVerifier().VerifyAsync(package, new PackageVerificationOptions());

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG010");
    }

    public static TheoryData<string, Func<CodeExpansionPayload, CodeExpansionPayload>> HostileMaps => new()
    {
        {
            "unequal",
            static expansions => new CodeExpansionPayload(
                expansions.ExpandedCodes,
                expansions.CanonicalIndices[..^1])
        },
        {
            "strictly ascending",
            static expansions =>
            {
                var codes = expansions.ExpandedCodes.ToArray();
                (codes[0], codes[1]) = (codes[1], codes[0]);
                return new CodeExpansionPayload([.. codes], expansions.CanonicalIndices);
            }
        },
        {
            "out of range",
            static expansions =>
            {
                var indices = expansions.CanonicalIndices.ToArray();
                indices[0] = 99;
                return new CodeExpansionPayload(expansions.ExpandedCodes, [.. indices]);
            }
        },
        {
            "bits above",
            static expansions =>
            {
                var codes = expansions.ExpandedCodes.ToArray();
                codes[0] = 1UL << 60;
                return new CodeExpansionPayload([.. codes], expansions.CanonicalIndices);
            }
        },
        {
            "particle outside",
            static expansions =>
            {
                var codes = expansions.ExpandedCodes.Insert(0, 4UL << 6);
                var indices = expansions.CanonicalIndices.Insert(0, 0);
                return new CodeExpansionPayload(codes, indices);
            }
        },
        {
            "canonical self-entry",
            static expansions =>
                new CodeExpansionPayload(
                    expansions.ExpandedCodes[1..],
                    expansions.CanonicalIndices[1..])
        },
    };

    private static ModelPayload WithExpansions(ModelPayload payload, CodeExpansionPayload expansions)
    {
        var table = payload.Energy.ClusterTables[0] with { CodeExpansions = expansions };
        return payload with
        {
            Energy = payload.Energy with { ClusterTables = [table] },
        };
    }

    private static ModelPayload DecodeModel()
    {
        var path = Path.Combine(
            RepositoryPaths.Combine("tests/contracts/packages/step-11-amend-2", "payloads"),
            "model-1.bin");
        return MessagePackSerializer.Deserialize<ModelPayload>(File.ReadAllBytes(path));
    }

    private static async Task ReplaceModelPayloadAsync(string package, ModelPayload payload)
    {
        var raw = MessagePackSerializer.Serialize(payload);
        using var stream = new MemoryStream();
        using (var compressor = new ZstandardStream(stream, CompressionMode.Compress, leaveOpen: true))
        {
            compressor.Write(raw);
        }

        var frame = stream.ToArray();
        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = package,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE models SET payload = $payload;";
        command.Parameters.AddWithValue("$payload", frame);
        await command.ExecuteNonQueryAsync();
    }
}

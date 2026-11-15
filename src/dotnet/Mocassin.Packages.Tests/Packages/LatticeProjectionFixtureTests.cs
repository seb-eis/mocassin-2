// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.Packages;

/// <summary>
/// Generates and checks the checked-in canonical lattice-projection fixtures with the real compiler, supercell
/// generator, and canonical projection.
/// </summary>
/// <remarks>
/// Run with <c>MOCASSIN_UPDATE_GOLDENS=1</c> after an intentional model or lattice change, then update the
/// matching <c>contracts/manifest.json</c> signature vectors and regenerate the contract artifacts.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class LatticeProjectionFixtureTests
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void LatticeProjectionFixturesAreCurrent()
    {
        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections", "lattice");
        Directory.CreateDirectory(directory);

        foreach (var fixture in fixtures)
        {
            var (project, model) = ParameterizationTestSupport.LoadAndCompile(fixture.Project);
            var lattice = Generate(project, model, fixture.SizeA, fixture.SizeB, fixture.SizeC);
            var payload = new LatticePayload(
                PackageFormatConstants.LatticePayloadFormatVersion,
                ImmutableArray.Create(
                    (uint)lattice.SizeA,
                    (uint)lattice.SizeB,
                    (uint)lattice.SizeC),
                (uint)lattice.PositionCount,
                lattice.Particles);
            var projection = LatticeSignatureV1.ComputeProjection(model.ModelSignature, payload);
            var signature = LatticeSignatureV1.Compute(model.ModelSignature, payload);
            var metadata = JsonSerializer.Serialize(
                new
                {
                    fixtureFormat = "canonical-projection-v1",
                    id = fixture.Id,
                    descriptor = "lattice-v1",
                    modelSignature = model.ModelSignature,
                    signatureHex = signature,
                    projectionSha256 = Convert.ToHexStringLower(SHA256.HashData(projection)),
                },
                jsonOptions) + "\n";

            var binaryPath = Path.Combine(directory, fixture.Id + ".bin");
            var metadataPath = Path.Combine(directory, fixture.Id + ".json");
            if (update)
            {
                File.WriteAllBytes(binaryPath, projection);
                File.WriteAllText(metadataPath, metadata);
                continue;
            }

            Assert.True(
                File.Exists(binaryPath),
                $"Missing lattice projection fixture {binaryPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.True(
                File.Exists(metadataPath),
                $"Missing lattice projection metadata {metadataPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.Equal(projection, File.ReadAllBytes(binaryPath));
            Assert.Equal(metadata, File.ReadAllText(metadataPath));
        }
    }

    [Fact]
    public void SignatureVectorEntriesMatchTheProjectionFixtures()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var vectorsPath = RepositoryPaths.Combine("tests", "contracts", "signature-vectors.json");
        using var vectors = JsonDocument.Parse(File.ReadAllText(vectorsPath));
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections", "lattice");
        var entries = vectors.RootElement.GetProperty("vectors")
            .EnumerateArray()
            .Where(static vector => vector.GetProperty("signature").GetString() == "lattice")
            .ToArray();

        Assert.Equal(fixtures.Length, entries.Length);
        foreach (var vector in entries)
        {
            var id = vector.GetProperty("id").GetString();
            var metadataPath = Path.Combine(directory, id + ".json");
            Assert.True(File.Exists(metadataPath), $"Missing fixture metadata {metadataPath}.");
            using var metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = metadata.RootElement;
            Assert.Equal(
                vector.GetProperty("signatureHex").GetString(),
                root.GetProperty("signatureHex").GetString());
            Assert.Equal(
                vector.GetProperty("projectionSha256").GetString(),
                root.GetProperty("projectionSha256").GetString());
            Assert.Equal(
                vector.GetProperty("modelSignature").GetString(),
                root.GetProperty("modelSignature").GetString());
        }
    }

    private static GeneratedLattice Generate(
        Project project,
        CompiledModel model,
        int sizeA,
        int sizeB,
        int sizeC)
    {
        var job = new JobDescriptor(
            1,
            0,
            0,
            0,
            "collection",
            "config",
            SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8a40", CultureInfo.InvariantCulture)),
            0,
            SimulationKind.Kmc,
            UseDualDofCorrection: false,
            new ResolvedJobSettings(
                900.0,
                500,
                7,
                null,
                null,
                ExecutionFlags.Deterministic,
                null,
                0,
                0.0,
                Fractional3.Zero,
                null,
                null,
                0.01,
                1000,
                100,
                200,
                []),
            new LatticeRequest(sizeA, sizeB, sizeC),
            [],
            [],
            [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)]);

        var result = SupercellGenerator.Generate(project, model, job);
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return result.Lattice!;
    }

    private static readonly (string Id, string Project, int SizeA, int SizeB, int SizeC)[] fixtures =
    [
        ("lattice-ceria-1x1x1", ParameterizationTestSupport.Ceria, 1, 1, 1),
        ("lattice-ceria-2x2x2", ParameterizationTestSupport.Ceria, 2, 2, 2),
    ];
}

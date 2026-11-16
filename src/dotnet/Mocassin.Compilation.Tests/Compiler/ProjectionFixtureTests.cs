// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Compilation.Signatures;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Generates and checks the canonical compiled-model projection fixtures.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ProjectionFixtureTests
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task ProjectionFixturesAreCurrent()
    {
        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections");
        Directory.CreateDirectory(directory);

        foreach (var (id, sample, kind) in samples)
        {
            var project = await StageTestSupport.LoadProjectAsync(sample);
            if (kind is not null)
            {
                project = StageTestSupport.WithFirstSimulationTransition(project, kind == "kmc");
            }

            var result = new ModelCompiler().Compile(project);
            Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
            var projection = ModelSignatureProjection.ComputeProjection(result.Model!);
            var signature = result.Model!.ModelSignature;
            var metadata = JsonSerializer.Serialize(
                new
                {
                    fixtureFormat = "canonical-projection-v1",
                    id,
                    descriptor = "model-v1",
                    signatureHex = signature,
                    projectionSha256 = Convert.ToHexStringLower(SHA256.HashData(projection)),
                },
                jsonOptions) + "\n";
            var binaryPath = Path.Combine(directory, id + ".bin");
            var jsonPath = Path.Combine(directory, id + ".json");

            if (update)
            {
                await File.WriteAllBytesAsync(binaryPath, projection);
                await File.WriteAllTextAsync(jsonPath, metadata);
                continue;
            }

            Assert.True(File.Exists(binaryPath), $"Missing projection fixture {binaryPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.True(File.Exists(jsonPath), $"Missing projection metadata {jsonPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.Equal(projection, await File.ReadAllBytesAsync(binaryPath));
            Assert.Equal(metadata, await File.ReadAllTextAsync(jsonPath));
        }
    }


    [Theory]
    [InlineData(5e-13, 1e-12)]
    [InlineData(-4e-13, 0.0)]
    [InlineData(0.30000000000000004, 0.3)]
    [InlineData(2.0, 2.0)]
    public void QuantizationUsesRoundHalfAwayFromZero(double input, double expected) =>
        Assert.Equal(expected, CanonicalProjectionWriter.Quantize(input));

    [Fact]
    public void QuantizationNormalizesNegativeZero()
    {
        var value = CanonicalProjectionWriter.Quantize(-0.0);

        Assert.Equal(0.0, value);
        Assert.False(double.IsNegative(value));
    }

    [Fact]
    public void QuantizationRejectsFiniteValuesAboveTheScalingRange()
    {
        Assert.Throws<InvalidOperationException>(() => CanonicalProjectionWriter.Quantize(1e300));
        Assert.Throws<InvalidOperationException>(() => CanonicalProjectionWriter.Quantize(double.MaxValue));
    }

    private static readonly (string Id, string Sample, string? Kind)[] samples =
    [
        ("model-ceria-kmc", "samples/simulations/ceria-kmc.mocprj", "kmc"),
        ("model-ceria-mmc", "samples/simulations/ceria-mmc.mocprj", "mmc"),
        ("model-vehicle5", "samples/transitions/vehicle5.mocprj", "kmc"),
        ("model-interstitialcy5", "samples/transitions/interstitialcy5.mocprj", "kmc"),
        ("model-barium-zirconate", "samples/barium-zirconate/barium-zirconate.mocprj", null),
    ];
}

// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Signatures;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Pins the deterministic ceria compiled-model signature and inspection JSON.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class GoldenModelTests
{
    [Fact]
    public async Task CeriaKmcSignatureMatchesTheGoldenVector()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess);
        var json = CompiledModelJson.Write(result.Model!);
        Assert.Equal("812a7a77a8b78993cf8c475061fbf071fea27082569f154737ed12eb3d64a314", result.Model!.ModelSignature);
        Assert.Equal("e2b4be280cf91724446b35e91e312765efe249c368919c91ffef45f2d476c162", Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        Assert.Contains("compiled-inspection-v1", CompiledModelJson.Write(result.Model), StringComparison.Ordinal);
    }
}

/// <summary>
/// Pins additional sample signatures and structural counts.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class GoldenSampleTests
{
    [Fact]
    public async Task VehicleCompilesToAStableSignature()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/vehicle5.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Equal("56b5d908194a5a006ba62daa21105edf48b42b529a755e6e694fe35a786eee69", result.Model!.ModelSignature);
        Assert.Equal(0, result.Model.Transitions.InverseRuleCount);
    }

    [Theory]
    [InlineData("samples/ceria/ceria.mocprj", false, "a9e0a45aec33294202b59fdb17c82335121d292ca96f579c479b85898cd0d113")]
    [InlineData("samples/barium-zirconate/barium-zirconate.mocprj", false, "5e62f9dd602110edc5007ad8eb384b0b25047a9205dfe3a5af9797e0f4e38b62")]
    [InlineData("samples/simulations/ceria-mmc.mocprj", true, "bf7fea8429d97cb35ccd3bd1424a1c97c073b98cca2feb458525ffef0e4bb214")]
    public async Task SampleCompilesToAStableSignature(string sample, bool addSimulation, string expectedSignature)
    {
        var project = await StageTestSupport.LoadProjectAsync(sample);
        if (addSimulation)
        {
            project = StageTestSupport.WithFirstSimulationTransition(project, kinetic: false);
        }

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Equal(expectedSignature, result.Model!.ModelSignature);
    }
}

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
        Assert.Equal("a28db57b7ccfd8af29ab2600dd36ab1b84a14f6bc654e3ed226b7cb75b5b357c", result.Model!.ModelSignature);
        Assert.Equal("e20ab855588d2a2d73dcbd10b76af45299a059a0a606da0f0042dd6a01471a01", Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        Assert.Contains("compiled-inspection-v1", CompiledModelJson.Write(result.Model), StringComparison.Ordinal);
    }
}

/// <summary>
/// Pins additional sample signatures and structural counts.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class GoldenSampleTests
{
    [Theory]
    [InlineData("samples/ceria/ceria.mocprj", false, "2e1d56fb6274492c404c8277dccb8d6014b05fd01975bfa162613beb7c009217")]
    [InlineData("samples/barium-zirconate/barium-zirconate.mocprj", false, "fb8462ca13be8dace30eb3eff2f310e42e8e6cbf57b9f71f927d95c3b9426e83")]
    [InlineData("samples/simulations/ceria-mmc.mocprj", true, "766118addc8fc2bc9506c0683ff1243f3f8e6ec9b204711088c8648b0f62f5c5")]
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

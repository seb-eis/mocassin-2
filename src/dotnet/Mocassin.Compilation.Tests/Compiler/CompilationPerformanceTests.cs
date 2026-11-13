// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Guards the local compilation performance target for the ceria sample.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class CompilationPerformanceTests
{
    [Fact]
    public async Task CeriaCompilesWithinTheLocalBudget()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var compiler = new ModelCompiler();
        _ = compiler.Compile(project);
        var started = Stopwatch.GetTimestamp();

        var result = compiler.Compile(project);

        var elapsed = Stopwatch.GetElapsedTime(started);
        Assert.True(result.IsSuccess);
        Assert.True(elapsed < TimeSpan.FromSeconds(2), $"Ceria compilation took {elapsed}.");
    }
}

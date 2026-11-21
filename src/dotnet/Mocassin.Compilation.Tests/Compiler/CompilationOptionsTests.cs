// SPDX-License-Identifier: MIT


namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Pins the documented option defaults and the caller-error ranges.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CompilationOptionsTests
{
    [Fact]
    public void DefaultsMatchTheAcceptedDesign()
    {
        var options = CompilationOptions.Default;

        Assert.Equal(Environment.ProcessorCount, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableStageCache);
        Assert.Equal(256, options.StageCacheCapacity);
        Assert.Equal(1_000_000, options.MaxRuleCount);
        Assert.Equal(100_000, options.AdvisoryRuleCount);
        Assert.Equal(2L * 1024 * 1024 * 1024, options.MaxEnergyTableBytes);
        Assert.Equal(512L * 1024 * 1024, options.AdvisoryEnergyTableBytes);
        Assert.Equal(1_000_000, options.MaxClusterTupleCount);
        Assert.True(options.EmitUnusedTransitionWarnings);
        Assert.Null(options.Cache);
    }

    [Theory]
    [InlineData("parallelism")]
    [InlineData("capacity")]
    [InlineData("maxRules")]
    [InlineData("advisoryRules")]
    [InlineData("maxBytes")]
    [InlineData("advisoryBytes")]
    [InlineData("maxTuples")]
    public void InvalidOptionRangesAreProgrammerErrors(string option)
    {
        var compiler = new ModelCompiler();
        var project = CompilerTestSupport.CreateProject();
        var options = option switch
        {
            "parallelism" => new CompilationOptions { MaxDegreeOfParallelism = 0 },
            "capacity" => new CompilationOptions { StageCacheCapacity = 0 },
            "maxRules" => new CompilationOptions { MaxRuleCount = -1 },
            "advisoryRules" => new CompilationOptions { AdvisoryRuleCount = -1 },
            "maxBytes" => new CompilationOptions { MaxEnergyTableBytes = -1 },
            "advisoryBytes" => new CompilationOptions { AdvisoryEnergyTableBytes = -1 },
            "maxTuples" => new CompilationOptions { MaxClusterTupleCount = -1 },
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, "Unknown option case."),
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => compiler.Compile(project, options));
    }

    [Fact]
    public void TheEnergyTableEstimateIncludesTheClusterExpansionMap()
    {
        var project = ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine(
            "samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj")));

        // Table 0 has 6 canonical occupations and 8 expansion entries: 6 * 8 + 8 * 12 = 144 bytes.
        var (prefix, prefixDiagnostics) = StageTestSupport.Run(
            project,
            new CompilationOptions { MaxEnergyTableBytes = 1_000_000 },
            StageTestSupport.StructureAndPairStages());
        Assert.DoesNotContain(
            prefixDiagnostics,
            static diagnostic => diagnostic.Code == "CMP006");

        var rejected = new Mocassin.Compilation.Energy.ClusterCompilationStage().Execute(
            prefix with { Options = prefix.Options with { MaxEnergyTableBytes = 143 } },
            CancellationToken.None);
        Assert.Contains(rejected.Diagnostics, static diagnostic => diagnostic.Code == "CMP006");

        var accepted = new Mocassin.Compilation.Energy.ClusterCompilationStage().Execute(
            prefix with { Options = prefix.Options with { MaxEnergyTableBytes = 144 } },
            CancellationToken.None);
        Assert.DoesNotContain(accepted.Diagnostics, static diagnostic => diagnostic.Code == "CMP006");
    }

    [Fact]
    public void OptionsAreImmutableValues()
    {
        var original = CompilationOptions.Default;
        var changed = original with { MaxRuleCount = 42 };

        Assert.Equal(1_000_000, original.MaxRuleCount);
        Assert.Equal(42, changed.MaxRuleCount);
        Assert.Equal(original.AdvisoryRuleCount, changed.AdvisoryRuleCount);
    }
}

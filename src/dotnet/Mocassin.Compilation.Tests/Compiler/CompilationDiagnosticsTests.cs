// SPDX-License-Identifier: MIT

using System.Reflection;
using Mocassin.Compilation.Diagnostics;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers the CMP catalog and the report counts, hashes, and advisory warnings.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class CompilationDiagnosticsTests
{
    [Fact]
    public void EveryCompilationCodeHasExactlyOneDescriptor()
    {
        var constants = typeof(CompilationDiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!)
            .ToArray();
        var descriptors = CompilationDiagnosticCatalog.Descriptors.Select(static descriptor => descriptor.Code).ToArray();

        Assert.Equal(12, constants.Length);
        Assert.Equal(constants.Order(StringComparer.Ordinal), descriptors.Order(StringComparer.Ordinal));
        Assert.Equal(descriptors.Length, descriptors.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DefaultCatalogComposesTheCompilationDescriptors()
    {
        Assert.True(DiagnosticCatalog.Default.TryGet(CompilationDiagnosticCodes.Precondition, out var descriptor));
        Assert.Equal("Step 08 — model compilation pipeline", descriptor.Owner);
        Assert.Contains(DiagnosticCatalog.Default.All, static item => item.Code == CompilationDiagnosticCodes.CacheIntegrity);
    }

    [Fact]
    public async Task SuccessfulCompilationReportsCountsAndInputHashes()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess);
        var report = result.Report;
        Assert.Equal(10, report.Stages.Length);
        Assert.True(report.Counts.Particles > 1);
        Assert.True(report.Counts.Positions > 0);
        Assert.True(report.Counts.PairGeometries > 0);
        Assert.True(report.Counts.ClusterTables > 0);
        Assert.Equal(0, report.Counts.Rules);
        Assert.Equal(result.Model!.Transitions.RuleCount, report.Counts.Rules);
        Assert.False(string.IsNullOrWhiteSpace(report.InputHashes.Model));
        Assert.False(string.IsNullOrWhiteSpace(report.InputHashes.Structure));
        Assert.False(string.IsNullOrWhiteSpace(report.InputHashes.Energy));
        Assert.False(string.IsNullOrWhiteSpace(report.InputHashes.Transitions));
        Assert.False(string.IsNullOrWhiteSpace(report.InputHashes.ActiveTransitions));
        Assert.Equal(result.Model.ModelSignature, report.ModelSignature);
    }

    [Fact]
    public async Task KmcCompilationReportsRuleAndInverseCounts()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess);
        Assert.True(result.Report.Counts.Rules > 0);
        Assert.True(result.Report.Counts.InverseRules > 0);
        Assert.Equal(result.Model!.Transitions.RuleCount, result.Report.Counts.Rules);
        Assert.Equal(result.Model.Transitions.InverseRuleCount, result.Report.Counts.InverseRules);
    }

    [Fact]
    public async Task UnusedTransitionsEmitCmp009UnlessSuppressed()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var enabled = new ModelCompiler().Compile(project);
        var disabled = new ModelCompiler().Compile(
            project,
            new CompilationOptions { EmitUnusedTransitionWarnings = false });

        Assert.True(enabled.IsSuccess);
        Assert.True(disabled.IsSuccess);
        Assert.Contains(enabled.Diagnostics, static diagnostic => diagnostic.Code == "CMP009");
        Assert.DoesNotContain(disabled.Diagnostics, static diagnostic => diagnostic.Code == "CMP009");
    }

    [Fact]
    public async Task AdvisoryRuleBudgetEmitsCmp010WithoutFailing()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(
            project,
            new CompilationOptions { AdvisoryRuleCount = 0 });

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "CMP010");
    }
}

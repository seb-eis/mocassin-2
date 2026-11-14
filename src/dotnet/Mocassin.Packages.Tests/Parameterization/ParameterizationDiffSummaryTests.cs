// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for the <c>PRM001</c> diff summary that <see cref="ParameterizationService.Apply"/> returns.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationDiffSummaryTests
{
    [Fact]
    public async Task ChangedGeometryProducesPrm001WithAddedRemovedAndMatchedKeys()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var cell = project.Model.Structure.Cell;
        var edit = new ProjectEditor().SetCellParameters(
            project,
            cell with
            {
                A = new LengthAngstrom(cell.A.Value + 0.05),
                B = new LengthAngstrom(cell.B.Value + 0.05),
                C = new LengthAngstrom(cell.C.Value + 0.05),
            });
        Assert.True(edit.IsSuccess, string.Join(Environment.NewLine, edit.Diagnostics));
        var compilation = new ModelCompiler().Compile(edit.Project);
        Assert.True(
            compilation.IsSuccess,
            string.Join(Environment.NewLine, compilation.Diagnostics.Select(static item => item.ToString())));
        var changedModel = compilation.Model!;
        Assert.NotEqual(model.ModelSignature, changedModel.ModelSignature);

        var result = new ParameterizationService().Apply(edit.Project, changedModel, template);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.ModelSignatureMismatch, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotNull(result.Diff);
        var diff = result.Diff!;
        Assert.Equal(template.ModelSignature, diff.TemplateModelSignature);
        Assert.Equal(changedModel.ModelSignature, diff.ModelSignature);
        Assert.NotEmpty(diff.Added);
        Assert.NotEmpty(diff.Removed);
        Assert.NotEmpty(diff.Matched);
        Assert.NotEmpty(diff.GeometryChanges);
        Assert.All(diff.Added, static entry => Assert.Null(entry.TemplateValue));
        Assert.All(diff.Removed, static entry => Assert.NotNull(entry.TemplateValue));
    }

    [Fact]
    public async Task MismatchDiffIsDeterministic()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var cell = project.Model.Structure.Cell;
        var edit = new ProjectEditor().SetCellParameters(
            project,
            cell with
            {
                A = new LengthAngstrom(cell.A.Value + 0.05),
                B = new LengthAngstrom(cell.B.Value + 0.05),
                C = new LengthAngstrom(cell.C.Value + 0.05),
            });
        Assert.True(edit.IsSuccess, string.Join(Environment.NewLine, edit.Diagnostics));
        var compilation = new ModelCompiler().Compile(edit.Project);
        Assert.True(
            compilation.IsSuccess,
            string.Join(Environment.NewLine, compilation.Diagnostics.Select(static item => item.ToString())));
        var changedModel = compilation.Model!;
        var service = new ParameterizationService();

        var first = service.Apply(edit.Project, changedModel, template).Diff!;
        var second = service.Apply(edit.Project, changedModel, template).Diff!;

        Assert.Equal(first.Added.Select(static entry => entry.Key), second.Added.Select(static entry => entry.Key));
        Assert.Equal(first.Removed.Select(static entry => entry.Key), second.Removed.Select(static entry => entry.Key));
        Assert.Equal(first.Matched.Select(static entry => entry.Key), second.Matched.Select(static entry => entry.Key));
        Assert.Equal(
            first.GeometryChanges.Select(static change => (change.RemovedKey, change.AddedKey)),
            second.GeometryChanges.Select(static change => (change.RemovedKey, change.AddedKey)));
    }

    [Fact]
    public async Task MatchingTemplateHasTheSameSignatureAndEmptyDiff()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);

        var result = new ParameterizationService().Apply(project, model, template);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Diff);
        Assert.Equal(model.ModelSignature, result.Model!.ModelSignature);
    }
}

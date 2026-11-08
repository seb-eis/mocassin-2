// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class DiagnosticBagTests
{
    [Fact]
    public void SortsBySeverityThenCodeThenPath()
    {
        var projectId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var bag = new DiagnosticBag([
            new Diagnostic("PROJ020", DiagnosticSeverity.Info, "info"),
            new Diagnostic("PROJ010", DiagnosticSeverity.Warning, "warning"),
            new Diagnostic("PROJ002", DiagnosticSeverity.Error, "error b", new ModelPath("project", projectId, "b")),
            new Diagnostic("PROJ001", DiagnosticSeverity.Error, "error a", new ModelPath("project", projectId, "a")),
        ]);

        var sorted = bag.Sorted;

        Assert.Equal(["PROJ001", "PROJ002", "PROJ010", "PROJ020"], sorted.Select(static item => item.Code));
        Assert.True(bag.HasErrors);
        Assert.Collection(bag.Errors, static _ => { }, static _ => { });
        Assert.Single(bag.Warnings);
        Assert.Single(bag.Infos);
    }

    [Fact]
    public void KeepsInsertionOrderForEqualSortKeys()
    {
        var bag = new DiagnosticBag();
        bag.Add(new Diagnostic("PROJ001", DiagnosticSeverity.Error, "first"));
        bag.Add(new Diagnostic("PROJ001", DiagnosticSeverity.Error, "second"));

        Assert.Equal(["first", "second"], bag.Sorted.Select(static item => item.Message));
    }

    [Fact]
    public void AddRangeCopiesDiagnostics()
    {
        var bag = new DiagnosticBag([
            new Diagnostic("PROJ001", DiagnosticSeverity.Error, "first"),
            new Diagnostic("PROJ002", DiagnosticSeverity.Warning, "second"),
        ]);

        Assert.Equal(2, bag.Count);
        Assert.True(bag.HasErrors);
    }

    [Fact]
    public void RejectsEmptyCodeAndMessage()
    {
        Assert.Throws<ArgumentException>(() => new Diagnostic("", DiagnosticSeverity.Error, "message"));
        Assert.Throws<ArgumentException>(() => new Diagnostic("PROJ001", DiagnosticSeverity.Error, ""));
    }

    [Fact]
    public void ModelPathFormatsDomainObjectAndProperty()
    {
        var path = new ModelPath("buildTemplate", Guid.Parse("00000000-0000-0000-0000-000000000001"), "jobTemplateId");

        Assert.Equal("buildTemplate/00000000-0000-0000-0000-000000000001/jobTemplateId", path.ToString());
    }
}

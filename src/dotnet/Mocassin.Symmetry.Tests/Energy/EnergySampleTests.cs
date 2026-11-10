// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Energy;

namespace Mocassin.Symmetry.Tests.Energy;

[Trait("Tier", "PR-medium")]
public sealed class EnergySampleTests
{
    [Fact]
    public void CeriaEnergyEnvironmentAndMigrationClusterValidate()
    {
        var project = ReadSample("samples/ceria/ceria.mocprj");

        var diagnostics = Validate(project);

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2.7, project.Model.StableEnvironment.Cutoff.Value);
        var environment = Assert.Single(project.Model.UnstableEnvironments);
        Assert.Equal(
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f07")),
            environment.SiteId);
        var cluster = Assert.Single(project.Model.Clusters);
        Assert.Equal("Oxygen migration edge", cluster.Name);
        Assert.Equal(2, cluster.OffsetVectors.Length);
        Assert.NotEmpty(project.Model.SiteEnergies);
    }

    [Fact]
    public void BariumZirconateEnergySectionsValidate()
    {
        var project = ReadSample("samples/barium-zirconate/barium-zirconate.mocprj");

        var diagnostics = Validate(project);

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2.0, project.Model.StableEnvironment.Cutoff.Value);
        Assert.Empty(project.Model.UnstableEnvironments);
        Assert.Empty(project.Model.Clusters);
        Assert.Equal(2, project.Model.SiteEnergies.Length);
    }

    private static ImmutableArray<Diagnostic> Validate(Project project) =>
    [
        .. ProjectValidator.Validate(project),
        .. new EnergyGeometryValidator().Validate(project.Model),
    ];

    private static Project ReadSample(string relativePath) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine(relativePath.Split('/'))));
}

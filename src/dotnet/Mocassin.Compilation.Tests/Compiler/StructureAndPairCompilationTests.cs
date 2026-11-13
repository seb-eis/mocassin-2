// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Structure;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers the compiled structure, environments, and pair tables against real sample projects.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class StructureAndPairCompilationTests
{
    [Theory]
    [InlineData("samples/ceria/ceria.mocprj")]
    [InlineData("samples/barium-zirconate/barium-zirconate.mocprj")]
    [InlineData("samples/lattice/bzo-doping.mocprj")]
    public async Task StructureAndPairsCompileForRealSamples(string sample)
    {
        var project = await StageTestSupport.LoadProjectAsync(sample);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.StructureAndPairStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var structure = Assert.IsType<CompiledStructureModel>(context.Structure);
        var energy = Assert.IsType<Mocassin.Compilation.Energy.CompiledEnergyModel>(context.Energy);
        Assert.Equal(context.Encoder!.PositionCount, structure.PositionCount);
        Assert.Equal(project.Structure.Sites.Length, structure.SiteCount);
        Assert.Equal(1 + project.Structure.Sites.Count(static site => site.Stability == Core.Model.Structure.SiteStability.Unstable), structure.EnvironmentCount);
        Assert.Equal(project.Particles.Length + 1, context.Particles!.Count);
        Assert.Equal(Core.Ids.ParticleId.Void, context.Particles.Particles[0].Id);
        Assert.All(structure.Positions, position =>
        {
            Assert.InRange(position.EnvironmentIndex, 0, structure.EnvironmentCount - 1);
            Assert.All(position.PairLinks, link =>
            {
                Assert.InRange(link.PairTableIndex, 0, energy.PairTableCount - 1);
                Assert.InRange(link.TargetP, 0, structure.PositionCount - 1);
            });
        });
    }

    [Fact]
    public async Task ParticleIndicesPlaceVoidFirstAndAuthoredParticlesAscending()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.StructureAndPairStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var table = Assert.IsType<CompiledParticleTable>(context.Particles);
        Assert.Equal(Core.Ids.ParticleId.Void, table.Particles[0].Id);
        var authored = project.Particles.OrderBy(static particle => particle.Id.Value).ToArray();
        for (var index = 0; index < authored.Length; index++)
        {
            Assert.Equal(index + 1, table.IndexById[authored[index].Id]);
            Assert.Equal(authored[index].Symbol, table.Particles[index + 1].Symbol);
        }
    }

    [Fact]
    public async Task PairTablesAreSharedPerEnergyIdentityAndCanonical()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.StructureAndPairStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var energy = Assert.IsType<Mocassin.Compilation.Energy.CompiledEnergyModel>(context.Energy);
        Assert.Equal(
            energy.PairGeometries.Select(static geometry => geometry.EnergyId).Distinct(StringComparer.Ordinal).Count(),
            energy.PairTables.Length);
        var ordered = energy.PairGeometries
            .OrderBy(static geometry => geometry.DistanceBucketFm)
            .ThenBy(static geometry => geometry.PartnerP)
            .ThenBy(static geometry => geometry.Offset.A)
            .ThenBy(static geometry => geometry.Offset.B)
            .ThenBy(static geometry => geometry.Offset.C)
            .ThenBy(static geometry => geometry.CenterSiteIndex)
            .ThenBy(static geometry => geometry.PartnerSiteIndex)
            .ThenBy(static geometry => geometry.CenterP)
            .ThenBy(static geometry => geometry.GeometryId, StringComparer.Ordinal);
        Assert.Equal(ordered.Select(static geometry => geometry.GeometryId), energy.PairGeometries.Select(static geometry => geometry.GeometryId));
        Assert.All(energy.PairTables, table =>
        {
            Assert.Equal(context.Particles!.Count * context.Particles.Count, table.Values.Length);
            Assert.All(table.Values, static value => Assert.Equal(0.0, value));
        });
    }
}

/// <summary>
/// Cross-checks the compiled pair tables against the Step 05 advisory preview.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class PairPreviewCrossCheckTests
{
    [Theory]
    [InlineData("samples/ceria/ceria.mocprj")]
    [InlineData("samples/barium-zirconate/barium-zirconate.mocprj")]
    public async Task CompiledPairCountsMatchTheEnergyPreview(string sample)
    {
        var project = await StageTestSupport.LoadProjectAsync(sample);
        var preview = new Mocassin.Symmetry.Energy.EnergyPreviewService()
            .TryCreatePairPreview(project.Model, 1, 1, 1, out var previewDiagnostics);
        Assert.NotNull(preview);
        Assert.Empty(previewDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.StructureAndPairStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var energy = Assert.IsType<Mocassin.Compilation.Energy.CompiledEnergyModel>(context.Energy);
        Assert.Equal(preview!.Value.Length, energy.PairGeometries.Length);
        Assert.Equal(
            preview.Value.Select(static pair => pair.EnergyId).Distinct(StringComparer.Ordinal).Count(),
            energy.PairTables.Length);
    }
}

/// <summary>
/// Covers canonical cluster tables and their bounded pre-check.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterCompilationTests
{
    [Fact]
    public async Task CeriaClusterCompilesToSortedCanonicalOccupations()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.ClusterStages());
        var preview = new Mocassin.Symmetry.Energy.EnergyPreviewService()
            .TryCreatePreview(project.Model, 1, 1, 1, out _);

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var energy = Assert.IsType<Mocassin.Compilation.Energy.CompiledEnergyModel>(context.Energy);
        var table = Assert.Single(energy.ClusterTables);
        Assert.NotEmpty(table.Occupations);
        Assert.Equal(
            preview!.ClusterCosts.Single().UniquePermutationCount,
            table.OccupationCount);
        Assert.Equal(
            table.Occupations.Select(static occupation => occupation.CanonicalCode).Order(),
            table.Occupations.Select(static occupation => occupation.CanonicalCode));
        Assert.All(table.Energies, static value => Assert.Equal(0.0, value));
        Assert.All(table.Occupations, occupation =>
        {
            Assert.Equal(Mocassin.Symmetry.Reduction.ClusterReduction.EncodeOccupation(occupation.CanonicalTuple), occupation.CanonicalCode);
            Assert.NotEmpty(occupation.Expansions);
        });
        Assert.All(
            context.Structure!.Sites[table.CenterSiteIndex].PositionIndices,
            p => Assert.Contains(
                context.Structure.Positions[p].ClusterLinks,
                link => link.ClusterTableIndex == table.Index && link.SiteSlot == 0));
    }

    [Fact]
    public async Task ClusterTupleLimitFailsWithCmp006()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/ceria/ceria.mocprj");

        var (_, diagnostics) = StageTestSupport.Run(
            project,
            new CompilationOptions { MaxClusterTupleCount = 0 },
            StageTestSupport.ClusterStages());

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "CMP006");
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}

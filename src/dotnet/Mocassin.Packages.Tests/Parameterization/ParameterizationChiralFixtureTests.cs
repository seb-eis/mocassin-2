// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Generates and checks the P1/P4mm chiral link-change project fixtures and the shared-value behaviour.
/// </summary>
/// <remarks>
/// The P4mm fixture has one reciprocal chiral pair table; expanding the same two sites in P1 yields four
/// tables. A P4mm template therefore cannot match a P1 chiral table by identity, and the rebase must drop the
/// shared value instead of copying it.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationChiralFixtureTests
{
    private const string p4mmPath = "tests/contracts/project-chiral-p4mm.mocprj";
    private const string p1Path = "tests/contracts/project-chiral-p1.mocprj";
    private const double chiralValue = 0.22;
    private static readonly DateTimeOffset createdUtc =
        DateTimeOffset.Parse("2026-11-14T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    private static readonly Guid templateId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f90");

    [Fact]
    public void ChiralProjectFixturesAreCurrent()
    {
        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);

        var p4mm = CreateProject(spaceGroup: 99);
        var p1 = CreateProject(spaceGroup: 1);
        var p1Model = Compile(p1);
        var p1Template = CreateChiralTemplate(p1, p1Model, "P1 chiral parameters");
        var p1WithTemplate = p1 with { Parameterizations = [p1Template] };
        var p4mmJson = ProjectJson.Serialize(p4mm);
        var p1Json = ProjectJson.Serialize(p1WithTemplate);

        foreach (var (path, json) in new[] { (p4mmPath, p4mmJson), (p1Path, p1Json) })
        {
            var fullPath = RepositoryPaths.Combine(path.Split('/'));
            if (update)
            {
                File.WriteAllText(fullPath, json);
                continue;
            }

            Assert.True(File.Exists(fullPath), $"Missing chiral project fixture {fullPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.Equal(json, File.ReadAllText(fullPath));
        }
    }

    [Fact]
    public void P4mmAndP1FixturesHaveDifferentChiralLinkTables()
    {
        if (IsUpdateMode)
        {
            return;
        }

        var (_, p4mmModel) = LoadAndCompile(p4mmPath);
        var (_, p1Model) = LoadAndCompile(p1Path);

        var p4mmChiral = p4mmModel.Energy.PairGeometries.Where(static geometry => geometry.IsChiral).ToArray();
        var p1Chiral = p1Model.Energy.PairGeometries.Where(static geometry => geometry.IsChiral).ToArray();

        Assert.Equal(2, p4mmChiral.Length);
        Assert.Equal(8, p1Chiral.Length);
        Assert.Single(p4mmChiral.Select(static geometry => geometry.EnergyId).Distinct(StringComparer.Ordinal));
        Assert.Equal(4, p1Chiral.Select(static geometry => geometry.EnergyId).Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(
            p4mmChiral.Select(static geometry => geometry.EnergyId)
                .Intersect(p1Chiral.Select(static geometry => geometry.EnergyId), StringComparer.Ordinal));
    }

    [Fact]
    public void ChiralLinkChangeDropsTheSharedValueAndNeverCopiesIt()
    {
        if (IsUpdateMode)
        {
            return;
        }

        var (_, p4mmModel) = LoadAndCompile(p4mmPath);
        var (p1, p1Model) = LoadAndCompile(p1Path);
        var p4mmTemplate = CreateChiralTemplate(CreateProject(spaceGroup: 99), p4mmModel, "P4mm chiral parameters");
        var service = new ParameterizationService();
        var explicitEntries = p4mmTemplate.StablePairSets.AddRange(p4mmTemplate.UnstablePairSets)
            .SelectMany(static set => set.Entries).ToArray();
        Assert.NotEmpty(explicitEntries);

        var diff = service.Diff(p1, p1Model, p4mmTemplate);
        Assert.NotEmpty(diff.Removed);
        Assert.NotEmpty(diff.Added);
        Assert.All(
            diff.Removed.Where(static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair),
            static entry => Assert.Equal(chiralValue, entry.TemplateValue));

        var rebase = service.Rebase(
            p1,
            p1Model,
            p4mmTemplate,
            new ParameterizationRebaseOptions
            {
                Strategy = ParameterizationRebaseStrategy.NearestGeometry,
                NearestDistanceToleranceFm = int.MaxValue,
            });

        Assert.Contains(rebase.Diagnostics, static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.RebaseReport);
        Assert.Equal(0, rebase.Report.CopiedValues);
        Assert.Equal(0, rebase.Report.DescriptorMatches);
        Assert.Empty(rebase.Report.DescriptorMatchDetails);
        Assert.Equal(diff.Removed.Length, rebase.Report.DroppedValues);
        Assert.All(
            rebase.Template.StablePairSets.AddRange(rebase.Template.UnstablePairSets),
            static set => Assert.Empty(set.Entries));

        var p1Template = Assert.Single(p1.Parameterizations);
        var applied = service.Apply(p1, p1Model, p1Template);
        Assert.True(applied.IsSuccess);
        AssertChiralTablesCarryOneSharedValue(applied.Model!, p1Model);
    }

    [Fact]
    public void GeneratedP1TemplateIsCurrentAndAppliesTheSharedchiralValue()
    {
        if (IsUpdateMode)
        {
            return;
        }

        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);
        var (p1, p1Model) = LoadAndCompile(p1Path);
        var template = Assert.Single(p1.Parameterizations);
        var regenerated = CreateChiralTemplate(CreateProject(spaceGroup: 1), p1Model, template.Name);

        Assert.Equal(template.Id, regenerated.Id);
        Assert.Equal(template.ModelSignature, regenerated.ModelSignature);
        Assert.Equal(template.StablePairSets.Length, regenerated.StablePairSets.Length);
        Assert.All(
            regenerated.StablePairSets.AddRange(regenerated.UnstablePairSets).Where(static set => set.IsChiral),
            static set => Assert.All(set.Entries, static entry => Assert.Equal(chiralValue, entry.Energy.Value)));

        var applied = new ParameterizationService().Apply(p1, p1Model, template);
        Assert.True(applied.IsSuccess);
        AssertChiralTablesCarryOneSharedValue(applied.Model!, p1Model);

        if (update)
        {
            return;
        }

        Assert.NotEmpty(applied.Model!.Signature);
    }

    private static bool IsUpdateMode => string.Equals(
        Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
        "1",
        StringComparison.Ordinal);

    private static void AssertChiralTablesCarryOneSharedValue(ParameterizedModel applied, CompiledModel model)
    {
        var chiralTables = model.Energy.PairGeometries
            .Where(static geometry => geometry.IsChiral)
            .Select(static geometry => geometry.PairTableIndex)
            .Distinct()
            .ToArray();
        Assert.NotEmpty(chiralTables);
        foreach (var tableIndex in chiralTables)
        {
            var table = model.Energy.PairTables[tableIndex];
            var values = applied.Values.PairTables[tableIndex];
            for (var center = 1; center < table.ParticleCount; center++)
            {
                for (var partner = 1; partner < table.ParticleCount; partner++)
                {
                    var value = values[(center * table.ParticleCount) + partner];
                    Assert.Equal(chiralValue, value);
                    Assert.Equal(value, values[(partner * table.ParticleCount) + center]);
                }
            }
        }
    }

    private static ParameterizationTemplate CreateChiralTemplate(
        Project project,
        CompiledModel model,
        string name)
    {
        var template = new ParameterizationFactory(new FixedTimeProvider(createdUtc))
            .Create(
                project,
                model,
                new ParameterizationTemplateOptions
                {
                    Name = name,
                    Id = ParameterizationId.Create(templateId),
                    CreatedUtc = createdUtc,
                })
            .Template;

        var chiralSets = template.StablePairSets.AddRange(template.UnstablePairSets).Where(static set => set.IsChiral).ToArray();
        Assert.NotEmpty(chiralSets);
        foreach (var set in chiralSets)
        {
            var geometry = model.Energy.PairGeometries.Single(
                geometry => geometry.PairTableIndex ==
                            model.Energy.PairTables.Single(table => table.EnergyId == set.EnergyId).Index &&
                            string.Equals(geometry.GeometryId, geometry.EnergyId, StringComparison.Ordinal));
            var centerMask = SiteMask(model, geometry.CenterSiteIndex);
            var partnerMask = SiteMask(model, geometry.PartnerSiteIndex);
            var center = FirstSharedParticle(centerMask, partnerMask);
            var partner = FirstSharedParticle(partnerMask, centerMask);
            var edit = ParameterizationTemplateEditor.SetPairEnergy(
                template,
                set.EnergyId,
                model.Particles.Particles[center].Id,
                model.Particles.Particles[partner].Id,
                new EnergyEv(chiralValue));
            Assert.Empty(edit.Diagnostics);
            template = edit.Template!;
        }

        Assert.Equal(chiralSets.Length, template.StablePairSets.AddRange(template.UnstablePairSets)
            .Where(static set => set.IsChiral)
            .Sum(static set => set.Entries.Length));
        return template;
    }

    private static int FirstSharedParticle(ulong mask, ulong otherMask)
    {
        for (var index = 1; index < 64; index++)
        {
            var bit = 1UL << index;
            if ((mask & bit) != 0 && (otherMask & bit) != 0)
            {
                return index;
            }
        }

        throw new InvalidOperationException("The fixture has no shared particle in the reciprocal masks.");
    }

    private static ulong SiteMask(CompiledModel model, int siteIndex)
    {
        ulong mask = 0;
        foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
        {
            mask |= model.Structure.Positions[positionIndex].AllowedParticleMask;
        }

        return mask;
    }

    private static (Project Project, CompiledModel Model) LoadAndCompile(string path)
    {
        var project = ParameterizationTestSupport.LoadProject(path);
        return (project, Compile(project));
    }

    private static CompiledModel Compile(Project project)
    {
        var result = new ModelCompiler().Compile(project);
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return result.Model!;
    }

    private static Project CreateProject(int spaceGroup)
    {
        var particleId = ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-3a2b3c4d8f20"));
        var particleSetId = ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-3a2b3c4d8f30"));
        var firstSiteId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-3a2b3c4d8f10"));
        var secondSiteId = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-3a2b3c4d8f11"));
        var particle = new Particle(particleId, "P0", "P0", new ChargeElementary(0.0), false);
        var particleSet = new ParticleSet(particleSetId, "Set", [particle.Id]);
        var model = new ModelDefinition
        {
            Particles = [particle],
            ParticleSets = [particleSet],
            Structure = new StructureModel(
                "P1/P4mm chiral-link fixture",
                new SpaceGroupRef(spaceGroup, AxisSetting.None, OriginChoice.None),
                new CellParameters(
                    new LengthAngstrom(5.0),
                    new LengthAngstrom(5.0),
                    new LengthAngstrom(spaceGroup == 1 ? 10.1 : 10.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                [
                    new CellSite(firstSiteId, "A", new Fractional3(0.0, 0.0, 0.0), particleSet.Id, SiteStability.Stable),
                    new CellSite(secondSiteId, "B", new Fractional3(0.5, 0.5, 0.2), particleSet.Id, SiteStability.Stable),
                ],
                []),
            StableEnvironment = new StableEnvironment(new LengthAngstrom(4.5), []),
        };

        return new Project(
            "https://mocassin.dev/schemas/project-v1.json",
            1,
            new ProjectMetadata(
                Guid.Parse(spaceGroup == 1
                    ? "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f41"
                    : "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f40"),
                new string('b', 64),
                spaceGroup == 1 ? "P1 chiral-link fixture" : "P4mm chiral-link fixture",
                "Step 09 generated chiral link-change fixture.",
                createdUtc,
                createdUtc),
            model,
            [],
            [],
            [],
            ViewSettings.Default);
    }
}

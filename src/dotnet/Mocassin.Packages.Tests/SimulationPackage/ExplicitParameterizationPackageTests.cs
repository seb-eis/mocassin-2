// SPDX-License-Identifier: MIT

using System.Numerics;
using Microsoft.Data.Sqlite;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Jobs;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;
using Mocassin.Packages.Tests.SimulationPackage.TestSupport;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Builds real packages from explicit parameterizations and proves that the stored model row holds the
/// applied values and signature rather than the compiled defaults.
/// </summary>
/// <remarks>
/// No checked-in sample has energy pair/group tables, transition rules, and a job template at the same time.
/// The pair/group/site half uses <c>samples/parameterizations/ceria-parameters.mocprj</c>; the absolute
/// rule-frequency half uses <c>samples/simulations/ceria-kmc.mocprj</c>. Both build the package with the real
/// writer and a hand-constructed job descriptor, which is the same seam Step 12 will drive from the CLI.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class ExplicitParameterizationPackageTests
{
    private const string energySample = "samples/parameterizations/ceria-parameters.mocprj";
    private const string transitionSample = "samples/simulations/ceria-kmc.mocprj";
    private const double authoredPairEnergyEv = 0.37;
    private const double authoredGroupEnergyEv = 0.21;
    private const double authoredSiteEnergyEv = 0.13;
    private const double authoredFrequencyHz = 3.5e12;

    [Fact]
    public async Task ExplicitParameterizationPackageStoresAppliedPairGroupAndSiteValues()
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(energySample);
        var template = ParameterizationTestSupport.WithExplicitDefaults(
            Assert.Single(project.Parameterizations),
            compiled);

        var pairSet = template.StablePairSets.First(static set => set.Entries.Length > 0);
        var pairEntry = pairSet.Entries[0];
        template = Edit(ParameterizationTemplateEditor.SetPairEnergy(
            template,
            pairSet.EnergyId,
            pairEntry.CenterParticleId,
            pairEntry.PartnerParticleId,
            new EnergyEv(authoredPairEnergyEv)));

        var groupSet = template.GroupSets.First(static set => set.Entries.Length > 0);
        var groupEntry = groupSet.Entries[0];
        template = Edit(ParameterizationTemplateEditor.SetGroupEnergy(
            template,
            groupSet.ClusterEnergyId,
            groupEntry.OccupationParticleIds,
            new EnergyEv(authoredGroupEnergyEv)));

        var site = compiled.Structure.Sites[0];
        var mask = ParameterizationTestSupport.SiteMask(compiled, site.Index);
        Assert.NotEqual(0UL, mask);
        var particleId = compiled.Particles.Particles[BitOperations.TrailingZeroCount(mask)].Id;
        template = Edit(ParameterizationTemplateEditor.SetSiteEnergyOverride(
            template,
            site.Id,
            particleId,
            new EnergyEv(authoredSiteEnergyEv)));

        var (applied, implicitModel, storedValues, storedSignature) =
            await ApplyAndReadAsync(project, compiled, template);

        Assert.Equal(applied.Signature, storedSignature);
        Assert.Equal(applied.Values.PairTables.Length, storedValues.PairTables.Length);
        for (var index = 0; index < applied.Values.PairTables.Length; index++)
        {
            Assert.Equal(applied.Values.PairTables[index].ToArray(), storedValues.PairTables[index].ToArray());
        }

        for (var index = 0; index < applied.Values.GroupSets.Length; index++)
        {
            Assert.Equal(applied.Values.GroupSets[index].ToArray(), storedValues.GroupSets[index].ToArray());
        }

        Assert.Equal(applied.Values.SiteEnergies.ToArray(), storedValues.SiteEnergies.ToArray());
        Assert.Equal(
            applied.Values.TransitionRuleFrequencies.ToArray(),
            storedValues.TransitionRuleFrequencies.ToArray());
        Assert.Contains(authoredPairEnergyEv, storedValues.PairTables.SelectMany(static table => table));
        Assert.Contains(authoredGroupEnergyEv, storedValues.GroupSets.SelectMany(static table => table));
        Assert.Contains(authoredSiteEnergyEv, storedValues.SiteEnergies);
        Assert.DoesNotContain(authoredPairEnergyEv, implicitModel.Values.PairTables.SelectMany(static table => table));
        Assert.DoesNotContain(authoredGroupEnergyEv, implicitModel.Values.GroupSets.SelectMany(static table => table));
        Assert.DoesNotContain(authoredSiteEnergyEv, implicitModel.Values.SiteEnergies);
    }

    [Fact]
    public async Task ExplicitParameterizationPackageStoresAppliedAbsoluteRuleFrequencies()
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(transitionSample);
        var template = ParameterizationTestSupport.WithExplicitDefaults(
            ParameterizationTestSupport.CreateTemplate(project, compiled),
            compiled);

        var ruleSet = template.TransitionRuleSets.First(static set => set.Rules.Length > 0);
        var ruleEntry = ruleSet.Rules[0];
        template = Edit(ParameterizationTemplateEditor.SetRuleFrequency(
            template,
            ruleSet.TransitionKind,
            ruleSet.TransitionId,
            ruleEntry.RuleGeometryId,
            new FrequencyHz(authoredFrequencyHz)));

        var (applied, implicitModel, storedValues, storedSignature) =
            await ApplyAndReadAsync(project, compiled, template);

        Assert.Equal(applied.Signature, storedSignature);
        Assert.Equal(
            applied.Values.TransitionRuleFrequencies.ToArray(),
            storedValues.TransitionRuleFrequencies.ToArray());
        Assert.Contains(authoredFrequencyHz, storedValues.TransitionRuleFrequencies);
        Assert.DoesNotContain(authoredFrequencyHz, implicitModel.Values.TransitionRuleFrequencies);
    }

    private static async Task<(
        ParameterizedModel Applied,
        ParameterizedModel ImplicitModel,
        ParameterizationValues StoredValues,
        string StoredSignature)> ApplyAndReadAsync(
        Project project,
        CompiledModel compiled,
        ParameterizationTemplate template)
    {
        var applied = new ParameterizationService().Apply(project, compiled, template).Model!;
        var implicitModel = new ParameterizationService().Apply(project, compiled, template: null).Model!;
        Assert.NotEqual(applied.Signature, implicitModel.Signature);
        var (storedSignature, storedValues) = await BuildAndReadAsync(project, compiled, applied);
        Assert.Equal(applied.Signature, storedSignature);
        return (applied, implicitModel, storedValues, storedSignature);
    }

    private static ParameterizationTemplate Edit(ParameterizationTemplateEditResult result)
    {
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return result.Template;
    }

    private static async Task<(string Signature, ParameterizationValues Values)> BuildAndReadAsync(
        Project project,
        CompiledModel compiled,
        ParameterizedModel applied)
    {
        using var temp = new TemporaryDirectory();
        var path = temp.Combine("explicit.msl");
        var descriptor = new JobDescriptor(
            1,
            0,
            0,
            0,
            "explicit",
            "explicit",
            SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f70")),
            0,
            SimulationKind.Kmc,
            UseDualDofCorrection: false,
            new ResolvedJobSettings(
                900.0,
                500,
                7,
                null,
                null,
                ExecutionFlags.Deterministic,
                "explicit-parameterization",
                0,
                0.0,
                Fractional3.Zero,
                null,
                null,
                0.01,
                1000,
                100,
                200,
                []),
            new LatticeRequest(2, 2, 2),
            [],
            [],
            [.. Enumerable.Repeat((byte)0x5a, 32)],
            []);
        var generated = SupercellGenerator.Generate(project, compiled, descriptor);
        Assert.True(
            generated.IsSuccess,
            string.Join(Environment.NewLine, generated.Diagnostics.Select(static diagnostic => diagnostic.ToString())));

        var writer = SimulationPackageWriter.Create(
            SimulationPackageTestSupport.Request(path),
            new PackageBuildOptions());
        await using (writer)
        {
            await writer.WriteModelAsync(applied);
            await writer.WriteJobAsync(new JobPackage(descriptor, generated.Lattice!));
            await writer.CompleteAsync();
        }

        await using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
            }.ToString());
        await connection.OpenAsync();
        var signature = await QueryStringAsync(connection, "SELECT signature FROM models LIMIT 1;");
        var payload = await QueryBlobAsync(connection, "SELECT payload FROM models LIMIT 1;");
        var decoded = PayloadEncoder.DecodeModel(payload);
        Assert.Equal(signature, decoded.Signature);
        return (signature, ModelPayloadMapper.ToParameterizationValues(decoded));
    }

    private static async Task<string> QueryStringAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    private static async Task<byte[]> QueryBlobAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (byte[])(await command.ExecuteScalarAsync())!;
    }
}

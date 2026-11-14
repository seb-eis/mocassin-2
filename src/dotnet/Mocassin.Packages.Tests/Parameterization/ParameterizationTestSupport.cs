// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Numerics;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Core.Editing;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.Projects;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Loads real sample projects, compiles them with the real compiler, and builds deterministic test variants.
/// </summary>
internal static class ParameterizationTestSupport
{
    public const string Ceria = "samples/ceria/ceria.mocprj";
    public const string CeriaKmc = "samples/simulations/ceria-kmc.mocprj";
    public const string CeriaMmc = "samples/simulations/ceria-mmc.mocprj";
    public const string BariumZirconate = "samples/barium-zirconate/barium-zirconate.mocprj";
    public const string Vehicle5 = "samples/transitions/vehicle5.mocprj";
    public const string Interstitialcy5 = "samples/transitions/interstitialcy5.mocprj";
    public const string CeriaParameters = "samples/parameterizations/ceria-parameters.mocprj";
    public const string BariumZirconateParameters = "samples/parameterizations/barium-zirconate-parameters.mocprj";

    public static Project LoadProject(string relativePath) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine(relativePath)));

    public static (Project Project, CompiledModel Model) LoadAndCompile(string relativePath)
    {
        var project = LoadProject(relativePath);
        var result = new ModelCompiler().Compile(project);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return (project, result.Model!);
    }

    public static async Task<Project> LoadProjectAsync(string relativePath)
    {
        var store = new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(RepositoryPaths.Combine(relativePath));
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return result.Project!;
    }

    public static async Task<(Project Project, CompiledModel Model)> LoadAndCompileAsync(string relativePath)
    {
        var project = await LoadProjectAsync(relativePath);
        var result = new ModelCompiler().Compile(project);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return (project, result.Model!);
    }

    public static ulong SiteMask(CompiledModel model, int siteIndex)
    {
        ulong mask = 0;
        foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
        {
            mask |= model.Structure.Positions[positionIndex].AllowedParticleMask;
        }

        return mask;
    }

    public static int PopCount(ulong mask) => BitOperations.PopCount(mask);

    public static int ChiralSlotCount(ulong centerMask, ulong partnerMask)
    {
        var pairs = new HashSet<(int Center, int Partner)>();
        for (var center = 0; center < 64; center++)
        {
            if ((centerMask & (1UL << center)) == 0)
            {
                continue;
            }

            for (var partner = 0; partner < 64; partner++)
            {
                if ((partnerMask & (1UL << partner)) == 0)
                {
                    continue;
                }

                pairs.Add(center <= partner ? (center, partner) : (partner, center));
            }
        }

        return pairs.Count;
    }

    public static CompiledPairGeometry Representative(CompiledModel model, CompiledPairTable table) =>
        model.Energy.PairGeometries.FirstOrDefault(
            geometry => geometry.PairTableIndex == table.Index &&
                        string.Equals(geometry.GeometryId, table.EnergyId, StringComparison.Ordinal)) ??
        model.Energy.PairGeometries.First(geometry => geometry.PairTableIndex == table.Index);

    /// <summary>
    /// Creates a header-only template with a fixed clock.
    /// </summary>
    public static ParameterizationTemplate CreateTemplate(
        Project project,
        CompiledModel model,
        string name = "Test parameters") =>
        new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch))
            .Create(project, model, new ParameterizationTemplateOptions { Name = name })
            .Template;

    /// <summary>
    /// Recompiles an edited project with the real compiler.
    /// </summary>
    public static CompiledModel Compile(Project project)
    {
        var result = new ModelCompiler().Compile(project);
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        return result.Model!;
    }

    /// <summary>
    /// Changes the three cell lengths by a fixed amount through the real project editor.
    /// </summary>
    public static Project ChangeCellParameters(Project project, double delta)
    {
        var cell = project.Model.Structure.Cell;
        var edit = new ProjectEditor().SetCellParameters(
            project,
            cell with
            {
                A = new LengthAngstrom(cell.A.Value + delta),
                B = new LengthAngstrom(cell.B.Value + delta),
                C = new LengthAngstrom(cell.C.Value + delta),
            });
        Assert.True(edit.IsSuccess, string.Join(Environment.NewLine, edit.Diagnostics));
        return edit.Project;
    }

    /// <summary>
    /// Adds an explicit value for every required pair, group, and rule slot with deterministic non-default values.
    /// </summary>
    public static ParameterizationTemplate WithAllExplicitValues(
        ParameterizationTemplate template,
        CompiledModel model,
        double pairValue = 0.11,
        double groupValue = 0.13,
        double frequencyHz = 2.0e13)
    {
        var filled = WithExplicitDefaults(template, model);
        return filled with
        {
            StablePairSets =
            [
                .. filled.StablePairSets.Select(
                    set => set with
                    {
                        Entries = [.. set.Entries.Select(
                            entry => entry with { Energy = new EnergyEv(pairValue) })],
                    }),
            ],
            UnstablePairSets =
            [
                .. filled.UnstablePairSets.Select(
                    set => set with
                    {
                        Entries = [.. set.Entries.Select(
                            entry => entry with { Energy = new EnergyEv(pairValue) })],
                    }),
            ],
            GroupSets =
            [
                .. filled.GroupSets.Select(
                    set => set with
                    {
                        Entries = [.. set.Entries.Select(
                            entry => entry with { Energy = new EnergyEv(groupValue) })],
                    }),
            ],
            TransitionRuleSets =
            [
                .. filled.TransitionRuleSets.Select(
                    set => set with
                    {
                        Rules = [.. set.Rules.Select(
                            entry => entry with { Frequency = new FrequencyHz(frequencyHz) })],
                    }),
            ],
        };
    }

    /// <summary>
    /// Adds a deterministic random subset of explicit pair, group, and rule values.
    /// </summary>
    public static ParameterizationTemplate WithSparseExplicitValues(
        ParameterizationTemplate template,
        CompiledModel model,
        int seed)
    {
        var random = new Random(seed);
        var filled = WithExplicitDefaults(template, model);
        return filled with
        {
            StablePairSets = [.. filled.StablePairSets.Select(
                set => set with { Entries = SparsePairs(set.Entries, random) })],
            UnstablePairSets = [.. filled.UnstablePairSets.Select(
                set => set with { Entries = SparsePairs(set.Entries, random) })],
            GroupSets = [.. filled.GroupSets.Select(
                set => set with { Entries = SparseGroups(set.Entries, random) })],
            TransitionRuleSets = [.. filled.TransitionRuleSets.Select(
                set => set with { Rules = SparseRules(set.Rules, random) })],
        };

        static ImmutableArray<PairEnergyEntry> SparsePairs(
            ImmutableArray<PairEnergyEntry> entries,
            Random random) =>
            [
                .. entries
                    .Where(_ => random.Next(2) == 0)
                    .Select(entry => entry with
                    {
                        Energy = new EnergyEv(Math.Round((random.NextDouble() * 0.4) - 0.2, 6)),
                    }),
            ];

        static ImmutableArray<GroupEnergyEntry> SparseGroups(
            ImmutableArray<GroupEnergyEntry> entries,
            Random random) =>
            [
                .. entries
                    .Where(_ => random.Next(2) == 0)
                    .Select(entry => entry with
                    {
                        Energy = new EnergyEv(Math.Round(random.NextDouble() * 0.6, 6)),
                    }),
            ];

        static ImmutableArray<RuleFrequencyEntry> SparseRules(
            ImmutableArray<RuleFrequencyEntry> entries,
            Random random) =>
            [
                .. entries
                    .Where(_ => random.Next(2) == 0)
                    .Select(entry => entry with
                    {
                        Frequency = new FrequencyHz(
                            1.0e12 * (1.0 + Math.Round(random.NextDouble() * 39.0, 3))),
                    }),
            ];
    }

    /// <summary>
    /// Adds explicit default values for every required pair, group, and rule slot.
    /// </summary>
    public static ParameterizationTemplate WithExplicitDefaults(
        ParameterizationTemplate template,
        CompiledModel model)
    {
        var siteIndex = model.Structure.Sites
            .Select(static (site, index) => (site.Id, index))
            .ToDictionary(static item => item.Id, static item => item.index);
        var stable = template.StablePairSets
            .Select(set => set with
            {
                Entries = BuildPairEntries(model, set, siteIndex[set.CenterSiteId], siteIndex[set.PartnerSiteId]),
            })
            .ToImmutableArray();
        var unstable = template.UnstablePairSets
            .Select(set => set with
            {
                Entries = BuildPairEntries(model, set, siteIndex[set.CenterSiteId], siteIndex[set.PartnerSiteId]),
            })
            .ToImmutableArray();
        var groups = template.GroupSets
            .Select(set =>
            {
                var table = model.Energy.ClusterTables.Single(
                    candidate => string.Equals(candidate.ClusterEnergyId, set.ClusterEnergyId, StringComparison.Ordinal));
                var entries = table.Occupations
                    .Select(occupation => new GroupEnergyEntry(
                        [.. occupation.CanonicalTuple.Select(index => model.Particles.Particles[index].Id)],
                        new EnergyEv(0.0)))
                    .ToImmutableArray();
                return set with { Entries = entries };
            })
            .ToImmutableArray();
        var rules = template.TransitionRuleSets
            .Select(set =>
            {
                var collection = model.Transitions.Collections.Single(
                    candidate => candidate.Transition.Kind == set.TransitionKind &&
                                 candidate.Transition.Id == set.TransitionId);
                var entries = collection.RuleIndices
                    .Select(index =>
                    {
                        var rule = model.Transitions.Rules[index];
                        return new RuleFrequencyEntry(rule.RuleGeometryId, new FrequencyHz(rule.FrequencyHz));
                    })
                    .ToImmutableArray();
                return set with { Rules = entries };
            })
            .ToImmutableArray();
        return template with
        {
            StablePairSets = stable,
            UnstablePairSets = unstable,
            GroupSets = groups,
            TransitionRuleSets = rules,
        };
    }

    /// <summary>
    /// Shuffles every template array and entry deterministically to exercise order independence.
    /// </summary>
    public static ParameterizationTemplate Shuffle(ParameterizationTemplate template, int seed)
    {
        var random = new Random(seed);
        return template with
        {
            StablePairSets = ShuffleSets(template.StablePairSets, random),
            UnstablePairSets = ShuffleSets(template.UnstablePairSets, random),
            GroupSets = [.. template.GroupSets.OrderBy(_ => random.Next())],
            SiteEnergyOverrides = [.. template.SiteEnergyOverrides.OrderBy(_ => random.Next())],
            TransitionRuleSets = [.. template.TransitionRuleSets.OrderBy(_ => random.Next())],
        };
    }

    private static ImmutableArray<PairEnergySet> ShuffleSets(
        ImmutableArray<PairEnergySet> sets,
        Random random) =>
        [.. sets.Select(set => set with { Entries = [.. set.Entries.OrderBy(_ => random.Next())] })
            .OrderBy(_ => random.Next())];

    private static ImmutableArray<PairEnergyEntry> BuildPairEntries(
        CompiledModel model,
        PairEnergySet set,
        int centerSiteIndex,
        int partnerSiteIndex)
    {
        var centerMask = SiteMask(model, centerSiteIndex);
        var partnerMask = SiteMask(model, partnerSiteIndex);
        var entries = ImmutableArray.CreateBuilder<PairEnergyEntry>();
        for (var center = 1; center < model.Particles.Count; center++)
        {
            if ((centerMask & (1UL << center)) == 0)
            {
                continue;
            }

            for (var partner = 1; partner < model.Particles.Count; partner++)
            {
                if ((partnerMask & (1UL << partner)) == 0)
                {
                    continue;
                }

                if (set.IsChiral && center > partner)
                {
                    var mirrored = (partnerMask & (1UL << center)) != 0 &&
                                   (centerMask & (1UL << partner)) != 0;
                    if (mirrored)
                    {
                        continue;
                    }
                }

                entries.Add(new PairEnergyEntry(
                    model.Particles.Particles[center].Id,
                    model.Particles.Particles[partner].Id,
                    new EnergyEv(0.0)));
            }
        }

        return entries.ToImmutable();
    }
}

/// <summary>
/// A fixed clock for template-creation tests.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

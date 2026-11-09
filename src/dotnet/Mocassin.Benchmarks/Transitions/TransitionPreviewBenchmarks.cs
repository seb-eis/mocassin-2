// SPDX-License-Identifier: MIT

using BenchmarkDotNet.Attributes;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Benchmarks.Transitions;

/// <summary>
/// Transition-preview benchmark for a compact ceria-like P1 migration fixture.
/// </summary>
/// <remarks>
/// Each invocation creates a fresh preview service so the measurement covers the cache-miss path rather than a
/// warm cache lookup.
/// </remarks>
[MemoryDiagnoser]
public class TransitionPreviewBenchmarks
{
    private Project project = null!;

    /// <summary>
    /// Builds and validates the benchmark fixture.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        project = CreateProject();
        var service = new TransitionPreviewService();
        var transition = project.Model.KineticTransitions[0];
        var preview = service.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);
        if (preview is null)
        {
            throw new InvalidOperationException(
                $"The transition-preview fixture is invalid: {string.Join("; ", diagnostics.Select(static diagnostic => diagnostic.ToString()))}");
        }

        if (preview.StateChangeMatrices.Length != 3 || preview.ExpandedPaths.Length != 1)
        {
            throw new InvalidOperationException("The transition-preview fixture has an unexpected shape.");
        }
    }

    /// <summary>
    /// Builds the ceria-like transition preview.
    /// </summary>
    /// <returns>A checksum that prevents elimination.</returns>
    [Benchmark]
    public int TransitionPreviewCeria()
    {
        var service = new TransitionPreviewService();
        var preview = service.TryCreatePreview(
            project.Model.KineticTransitions[0],
            project,
            1,
            1,
            1,
            out _);
        return preview is null ? 0 : preview.StateChangeMatrices.Length + preview.ExpandedPaths.Length;
    }

    private static Project CreateProject()
    {
        var oxygen = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f10")),
            "O2-",
            "O",
            new ChargeElementary(-2.0),
            IsVacancy: false);
        var vacancy = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f11")),
            "Vacancy",
            "Vc",
            new ChargeElementary(0.0),
            IsVacancy: true);
        var oxygenSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f13")),
            "Oxygen site",
            [oxygen.Id, vacancy.Id]);
        var stableSite = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f15"));
        var unstableSite = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f16"));
        var endSite = CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f17"));
        var stateChange = new StateChange(
            StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f21")),
            "O -> vacancy",
            oxygen.Id,
            vacancy.Id);
        var unstableChange = new StateChange(
            StateChangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f22")),
            "O -> void",
            oxygen.Id,
            ParticleId.Void);
        var stableGroup = new StateChangeGroup(
            StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f31")),
            "Stable",
            [stateChange.Id]);
        var unstableGroup = new StateChangeGroup(
            StateChangeGroupId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f32")),
            "Unstable",
            [unstableChange.Id]);
        var chain = new TransitionChain(
            TransitionChainId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f41")),
            "Migration3",
            TransitionPattern.Migration3,
            [stableGroup.Id, unstableGroup.Id, stableGroup.Id],
            [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
            GeneratesReverse: true);
        var transition = new KineticTransition(
            KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f51")),
            "O migration",
            chain.Id,
            [
                new Fractional3(0.25, 0.25, 0.25),
                new Fractional3(0.5, 0.25, 0.25),
                new Fractional3(0.75, 0.25, 0.25),
            ],
            ReverseTransitionId: null);
        var model = new ModelDefinition
        {
            Particles = [oxygen, vacancy],
            ParticleSets = [oxygenSet],
            Structure = new StructureModel(
                "Ceria-like P1 preview",
                new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                new CellParameters(
                    new LengthAngstrom(5.41),
                    new LengthAngstrom(5.41),
                    new LengthAngstrom(5.41),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0),
                    new AngleDegrees(90.0)),
                [
                    new CellSite(stableSite, "O", new Fractional3(0.25, 0.25, 0.25), oxygenSet.Id, SiteStability.Stable),
                    new CellSite(unstableSite, "U", new Fractional3(0.5, 0.25, 0.25), oxygenSet.Id, SiteStability.Unstable),
                    new CellSite(endSite, "O end", new Fractional3(0.75, 0.25, 0.25), oxygenSet.Id, SiteStability.Stable),
                ],
                []),
            StateChanges = [stateChange, unstableChange],
            StateChangeGroups = [stableGroup, unstableGroup],
            Chains = [chain],
            KineticTransitions = [transition],
        };

        return new Project(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d7f00"),
                new string('a', 64),
                "Transition preview benchmark",
                null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            model,
            [],
            [],
            [],
            ViewSettings.Default);
    }
}

// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Creates cached lattice and job-count previews over the shared Step 10 generator.
/// </summary>
/// <remarks>
/// The service never compiles and never writes a package; it takes the current project and compiled model. It
/// delegates lattice generation to <see cref="SupercellGenerator"/>, so the preview and the build share one doping,
/// truncation, and compensation implementation. Invalid user data returns <see langword="null"/> with diagnostics
/// and is never cached. The preview cache is bounded to eight entries and 256 MiB of estimated payload with LRU
/// eviction.
/// </remarks>
public sealed class LatticePreviewService : ILatticePreviewService
{
    private const int maximumJobCount = 1_000;
    private const int minimumJobCount = 1;

    private static readonly ImmutableArray<byte> previewSeed =
        [.. new byte[LatticeSeedDeriver.SeedByteLength]];

    private readonly LatticePreviewCache cache;

    /// <summary>
    /// Initializes a preview service with the production cache bounds.
    /// </summary>
    public LatticePreviewService()
        : this(LatticePreviewCache.DefaultEntryCapacity, LatticePreviewCache.DefaultByteCapacity)
    {
    }

    /// <summary>
    /// Initializes a preview service with explicit cache bounds.
    /// </summary>
    /// <param name="entryCapacity">The maximum number of cached previews.</param>
    /// <param name="byteCapacity">The maximum estimated cached payload in bytes.</param>
    internal LatticePreviewService(int entryCapacity, long byteCapacity)
    {
        cache = new LatticePreviewCache(entryCapacity, byteCapacity);
    }

    /// <summary>
    /// Gets the number of cached previews. Intended for diagnostics and tests.
    /// </summary>
    internal int CachedEntryCount => cache.Count;

    /// <summary>
    /// Gets the estimated cached payload in bytes. Intended for diagnostics and tests.
    /// </summary>
    internal long CachedBytes => cache.SizeBytes;

    /// <inheritdoc />
    public LatticePreview? TryCreatePreview(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);

        var requestedDopings = dopings.IsDefault ? ImmutableArray<DopingValue>.Empty : dopings;
        if (!LatticeRequestHasher.TryResolveDopings(project, requestedDopings, out var resolvedDopings, out var unknown))
        {
            diagnostics = [Error(
                JobDiagnosticCodes.UnknownDoping,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The lattice preview references unknown doping '{unknown.GetValueOrDefault().Value:D}'."),
                PreviewPath(project, "dopings"))];
            return null;
        }

        var key = CreateCacheKey(project, model, request, requestedDopings, resolvedDopings);
        if (cache.TryGet(key, out var cached) && cached is not null)
        {
            diagnostics = cached.Diagnostics;
            return cached.Preview;
        }

        var result = SupercellGenerator.Generate(
            project,
            model,
            request,
            requestedDopings,
            previewSeed.AsSpan(),
            PreviewPath(project, "lattice"),
            PreviewPath(project, "dopings"));
        diagnostics = result.Diagnostics;
        if (result.Lattice is null)
        {
            return null;
        }

        var preview = new LatticePreview(
            request,
            requestedDopings,
            result.Lattice.Report,
            CreatePopulations(project, model, result.Lattice, requestedDopings));
        cache.Add(new LatticePreviewCacheEntry(key, preview, result.Diagnostics, EstimatePayloadBytes(preview)));
        return preview;
    }

    /// <inheritdoc />
    public JobCountPreview? TryCreateJobCountPreview(
        Project project,
        JobTemplate jobTemplate,
        SimulationId simulationId,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(jobTemplate);

        var bag = new DiagnosticBag();
        var simulations = IndexSimulations(project);
        if (!simulations.TryGetValue(simulationId, out var selected))
        {
            bag.Add(Error(
                JobDiagnosticCodes.UnknownSimulation,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The job-count preview references unknown simulation '{simulationId.Value:D}'."),
                new ModelPath("jobTemplate", jobTemplate.Id.Value, "collections")));
            diagnostics = bag.Sorted;
            return null;
        }

        if (!IsValidJobCount(selected.JobCount))
        {
            bag.Add(Error(
                JobDiagnosticCodes.InvalidJobCount,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The simulation job count must be within {minimumJobCount}..{maximumJobCount}; it is {selected.JobCount}."),
                new ModelPath("simulation", simulationId.Value, "jobCount")));
        }

        var collections = jobTemplate.Collections is { IsDefault: false } authoredCollections
            ? authoredCollections
            : ImmutableArray<JobCollection>.Empty;
        long effectiveJobs = 0;
        long totalJobs = 0;
        foreach (var collection in collections)
        {
            if (collection is null)
            {
                continue;
            }

            if (!simulations.TryGetValue(collection.SimulationId, out var simulation))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.UnknownSimulation,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Collection references unknown simulation '{collection.SimulationId.Value:D}'."),
                    new ModelPath("jobCollection", collection.Id.Value, "simulationId")));
                continue;
            }

            var jobCount = collection.JobCountPerConfig ?? simulation.JobCount;
            if (!IsValidJobCount(jobCount))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidJobCount,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The effective job count per configuration must be within {minimumJobCount}..{maximumJobCount}; " +
                        $"it is {jobCount}."),
                    new ModelPath("jobCollection", collection.Id.Value, "jobCountPerConfig")));
                continue;
            }

            var configCount = collection.Configs is { IsDefault: false } configs ? configs.Length : 0;
            var jobs = (long)jobCount * configCount;
            totalJobs += jobs;
            if (collection.SimulationId == simulationId)
            {
                effectiveJobs += jobs;
            }
        }

        if (effectiveJobs > int.MaxValue)
        {
            bag.Add(Error(
                JobDiagnosticCodes.InvalidJobCount,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The effective job total {effectiveJobs} exceeds the supported {int.MaxValue}."),
                new ModelPath("jobTemplate", jobTemplate.Id.Value, "collections")));
        }

        if (bag.HasErrors)
        {
            diagnostics = bag.Sorted;
            return null;
        }

        diagnostics = bag.Sorted;
        return new JobCountPreview(
            simulationId,
            selected.JobCount,
            (int)effectiveJobs,
            totalJobs);
    }

    /// <summary>
    /// Estimates the payload of one preview for the bounded cache. Intended for tests.
    /// </summary>
    /// <param name="preview">The preview to estimate.</param>
    /// <returns>The estimated payload in bytes.</returns>
    internal static long EstimatePayloadBytes(LatticePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var bytes = 160L;
        bytes += 32L + (preview.Dopings.Length * 32L);
        bytes += 64L + (preview.Report.Applications.Length * 64L);
        foreach (var site in preview.RemainingPopulations)
        {
            bytes += 32L + (site.Particles.Length * 16L);
        }

        return bytes;
    }

    /// <summary>
    /// Determines whether a key is cached. Intended for diagnostics and tests.
    /// </summary>
    /// <param name="project">The project that supplies the authored model and dopings.</param>
    /// <param name="model">The compiled model that supplies the symmetry provenance.</param>
    /// <param name="request">The requested supercell size.</param>
    /// <param name="dopings">The requested doping values.</param>
    /// <returns><see langword="true"/> when the exact key is cached.</returns>
    internal bool IsCached(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);

        var requestedDopings = dopings.IsDefault ? ImmutableArray<DopingValue>.Empty : dopings;
        if (!LatticeRequestHasher.TryResolveDopings(project, requestedDopings, out var resolvedDopings, out _))
        {
            return false;
        }

        return cache.Contains(CreateCacheKey(project, model, request, requestedDopings, resolvedDopings));
    }

    private static string CreateCacheKey(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings,
        ImmutableArray<LatticeDoping> resolvedDopings)
    {
        var builder = new StringBuilder("mocassin/lattice-preview/v1");
        AppendField(builder, ProjectHasher.Compute(project));
        AppendField(builder, LatticeRequestHasher.Compute(project, model, request, dopings));
        AppendField(
            builder,
            string.Create(CultureInfo.InvariantCulture, $"{resolvedDopings.Length}"));
        foreach (var doping in resolvedDopings)
        {
            AppendField(builder, doping.DopingId.Value.ToString("D", CultureInfo.InvariantCulture));
            AppendField(builder, doping.Priority.ToString(CultureInfo.InvariantCulture));
            AppendField(builder, doping.Fraction.ToString("R", CultureInfo.InvariantCulture));
        }

        AppendField(builder, model.SpaceGroupDataVersion);
        AppendField(builder, model.SpaceGroupDataSha256);
        AppendField(builder, model.SymmetryContractVersion);
        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string value)
    {
        builder.Append('|').Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value);
    }

    private static ImmutableArray<SitePopulation> CreatePopulations(
        Project project,
        CompiledModel model,
        GeneratedLattice lattice,
        ImmutableArray<DopingValue> dopings)
    {
        var positions = model.Structure.Positions;
        var counts = new Dictionary<CellSiteId, Dictionary<int, int>>();
        for (var offset = 0; offset < lattice.Particles.Length; offset += lattice.PositionCount)
        {
            for (var index = 0; index < positions.Length; index++)
            {
                var particle = lattice.Particles[offset + index];
                if (particle == CompiledParticleTable.VoidIndex)
                {
                    continue;
                }

                var siteId = positions[index].SiteId;
                if (!counts.TryGetValue(siteId, out var particles))
                {
                    particles = [];
                    counts.Add(siteId, particles);
                }

                particles[particle] = particles.TryGetValue(particle, out var count) ? count + 1 : 1;
            }
        }

        foreach (var siteId in ResolveAffectedSites(project, dopings))
        {
            counts.TryAdd(siteId, []);
        }

        var populations = ImmutableArray.CreateBuilder<SitePopulation>(counts.Count);
        foreach (var entry in counts.OrderBy(static pair => pair.Key.Value))
        {
            var particles = ImmutableArray.CreateBuilder<ParticlePopulation>(entry.Value.Count);
            foreach (var particle in entry.Value.OrderBy(static pair => pair.Key))
            {
                particles.Add(new ParticlePopulation(
                    model.Particles.Particles[particle.Key].Id,
                    particle.Value));
            }

            populations.Add(new SitePopulation(entry.Key, particles.ToImmutable()));
        }

        return populations.ToImmutable();
    }

    private static HashSet<CellSiteId> ResolveAffectedSites(
        Project project,
        ImmutableArray<DopingValue> dopings)
    {
        var sites = new HashSet<CellSiteId>();
        foreach (var value in dopings)
        {
            if (value is null)
            {
                continue;
            }

            var doping = project.Find(value.DopingId);
            if (doping is null)
            {
                continue;
            }

            if (project.Find(doping.PrimaryExchangeId) is { } primary)
            {
                sites.Add(primary.SiteId);
            }

            if (doping.DependentExchangeId is { } dependentId &&
                project.Find(dependentId) is { } dependent)
            {
                sites.Add(dependent.SiteId);
            }
        }

        return sites;
    }

    private static Dictionary<SimulationId, Simulation> IndexSimulations(Project project)
    {
        var simulations = project.Model.Simulations.IsDefault
            ? ImmutableArray<Simulation>.Empty
            : project.Model.Simulations;
        var lookup = new Dictionary<SimulationId, Simulation>(simulations.Length);
        foreach (var simulation in simulations)
        {
            if (simulation is not null)
            {
                lookup.TryAdd(simulation.Id, simulation);
            }
        }

        return lookup;
    }

    private static bool IsValidJobCount(int jobCount) => jobCount is >= minimumJobCount and <= maximumJobCount;

    private static ModelPath PreviewPath(Project project, string property) =>
        new("project", project.Metadata.ProjectId, string.Create(CultureInfo.InvariantCulture, $"latticePreview/{property}"));

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));
}

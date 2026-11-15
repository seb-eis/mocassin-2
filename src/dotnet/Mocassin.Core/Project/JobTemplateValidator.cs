// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Project;

/// <summary>
/// Validates the structural job-template rules that do not need a compiled model.
/// </summary>
/// <remarks>
/// The validator owns <c>JOB001</c>–<c>JOB003</c>, <c>JOB005</c>–<c>JOB008</c>, the reference half of
/// <c>JOB009</c>, <c>JOB015</c>, <c>JOB017</c>, <c>JOB018</c>, <c>JOB020</c>, and the cell-range part of
/// <c>JOB004</c>. Total-position, doping-count, compensation, neutrality, and removes-all checks require a
/// compiled model and belong to the Packages build stage.
/// </remarks>
internal static class JobTemplateValidator
{
    private const int templatesPerProject = 100;
    private const int collectionsPerTemplate = 100;
    private const int configsPerCollection = 1000;
    private const int dopingsPerConfig = 1000;
    private const int optimizersPerCollection = 1000;
    private const int jobCountMinimum = 1;
    private const int jobCountMaximum = 1000;
    private const long jobWarningThreshold = 10_000;
    private const long jobHardLimit = 99_999;
    private const int minimumCellCount = 1;
    private const int maximumCellCount = 256;

    /// <summary>
    /// Validates all job templates of a project.
    /// </summary>
    /// <param name="project">The project that owns the templates.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var bag = new DiagnosticBag();
        var templates = project.JobTemplates.IsDefault
            ? ImmutableArray<JobTemplate>.Empty
            : project.JobTemplates;

        if (templates.Length > templatesPerProject)
        {
            bag.Add(Error(
                JobDiagnosticCodes.LimitExceeded,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"A project may contain at most {templatesPerProject} job templates; it contains {templates.Length}."),
                new ModelPath("project", project.Metadata.ProjectId, "jobTemplates")));
        }

        var model = project.Model;
        var simulations = model.Simulations.IsDefault
            ? ImmutableArray<Simulation>.Empty
            : model.Simulations;
        var simulationsById = new Dictionary<SimulationId, Simulation>();
        foreach (var simulation in simulations)
        {
            if (simulation is not null)
            {
                simulationsById.TryAdd(simulation.Id, simulation);
            }
        }

        var dopingsById = new Dictionary<DopingId, Model.Lattice.Doping>();
        foreach (var doping in model.Dopings.IsDefault ? ImmutableArray<Model.Lattice.Doping>.Empty : model.Dopings)
        {
            if (doping is not null)
            {
                dopingsById.TryAdd(doping.Id, doping);
            }
        }

        var sitesById = new Dictionary<CellSiteId, CellSite>();
        foreach (var site in model.Structure.Sites.IsDefault
                     ? ImmutableArray<CellSite>.Empty
                     : model.Structure.Sites)
        {
            if (site is not null)
            {
                sitesById.TryAdd(site.Id, site);
            }
        }

        var particlesById = new Dictionary<ParticleId, Particle>();
        foreach (var particle in model.Particles.IsDefault
                     ? ImmutableArray<Particle>.Empty
                     : model.Particles)
        {
            if (particle is not null)
            {
                particlesById.TryAdd(particle.Id, particle);
            }
        }

        var particleSetsById = new Dictionary<ParticleSetId, ParticleSet>();
        foreach (var particleSet in model.ParticleSets.IsDefault
                     ? ImmutableArray<ParticleSet>.Empty
                     : model.ParticleSets)
        {
            if (particleSet is not null)
            {
                particleSetsById.TryAdd(particleSet.Id, particleSet);
            }
        }

        foreach (var template in templates)
        {
            if (template is not null)
            {
                ValidateTemplate(
                    template,
                    simulationsById,
                    dopingsById,
                    sitesById,
                    particlesById,
                    particleSetsById,
                    bag);
            }
        }

        return bag.Sorted;
    }

    private static void ValidateTemplate(
        JobTemplate template,
        Dictionary<SimulationId, Simulation> simulations,
        Dictionary<DopingId, Model.Lattice.Doping> dopings,
        Dictionary<CellSiteId, CellSite> sites,
        Dictionary<ParticleId, Particle> particles,
        Dictionary<ParticleSetId, ParticleSet> particleSets,
        DiagnosticBag bag)
    {
        var collections = template.Collections is { IsDefault: false } value
            ? value
            : ImmutableArray<JobCollection>.Empty;
        if (collections.Length == 0)
        {
            bag.Add(Error(
                JobDiagnosticCodes.NoCollections,
                "Job template must contain at least one collection.",
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
            return;
        }

        if (collections.Length > collectionsPerTemplate)
        {
            bag.Add(Error(
                JobDiagnosticCodes.LimitExceeded,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"A job template may contain at most {collectionsPerTemplate} collections; it contains {collections.Length}."),
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
        }

        var collectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalJobs = 0;
        foreach (var collection in collections)
        {
            if (collection is null)
            {
                continue;
            }

            ValidateCollectionName(collection, collectionNames, bag);
            simulations.TryGetValue(collection.SimulationId, out var simulation);
            if (simulation is null)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.UnknownSimulation,
                    $"Collection references unknown simulation '{collection.SimulationId.Value:D}'.",
                    new ModelPath("jobCollection", collection.Id.Value, "simulationId")));
            }

            if (collection.BaseSettings is not null)
            {
                bag.AddRange(JobSettingsValidator.Validate(
                    collection.BaseSettings,
                    simulation?.Kind,
                    new ModelPath("jobCollection", collection.Id.Value, "baseSettings")));
            }

            if (collection.JobCountPerConfig is { } jobCount &&
                jobCount is < jobCountMinimum or > jobCountMaximum)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidJobCount,
                    string.Create(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $"Job count per configuration must be within {jobCountMinimum}..{jobCountMaximum}."),
                    new ModelPath("jobCollection", collection.Id.Value, "jobCountPerConfig")));
            }

            var optimizers = collection.Optimizers is { IsDefault: false } optimizerValue
                ? optimizerValue
                : ImmutableArray<SelectionOptimizer>.Empty;
            ValidateOptimizers(collection, optimizers, sites, particles, particleSets, bag);

            var configs = collection.Configs is { IsDefault: false } configValue
                ? configValue
                : ImmutableArray<JobConfig>.Empty;
            if (configs.Length == 0)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.NoConfigs,
                    "Job collection must contain at least one configuration.",
                    new ModelPath("jobCollection", collection.Id.Value, "configs")));
                continue;
            }

            if (configs.Length > configsPerCollection)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.LimitExceeded,
                    string.Create(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $"A job collection may contain at most {configsPerCollection} configurations; it contains {configs.Length}."),
                    new ModelPath("jobCollection", collection.Id.Value, "configs")));
            }

            var configNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var effectiveJobCount = collection.JobCountPerConfig ?? simulation?.JobCount ?? 0;
            var jobsPerConfig = Math.Max(effectiveJobCount, 0);
            totalJobs += (long)jobsPerConfig * configs.Length;
            foreach (var config in configs)
            {
                if (config is not null)
                {
                    ValidateConfig(config, configNames, simulation, dopings, bag);
                }
            }
        }

        if (totalJobs > jobHardLimit)
        {
            bag.Add(Error(
                JobDiagnosticCodes.JobLimitExceeded,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"The template expands to {totalJobs} jobs; the hard limit is {jobHardLimit}."),
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
        }
        else if (totalJobs > jobWarningThreshold)
        {
            bag.Add(Warning(
                JobDiagnosticCodes.JobCountWarning,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"The template expands to {totalJobs} jobs; the warning threshold is {jobWarningThreshold}."),
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
        }
    }

    private static void ValidateCollectionName(
        JobCollection collection,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(collection.Name))
        {
            bag.Add(Error(
                JobDiagnosticCodes.DuplicateName,
                "Collection name must not be empty or whitespace-only.",
                new ModelPath("jobCollection", collection.Id.Value, "name")));
            return;
        }

        if (!names.Add(collection.Name))
        {
            bag.Add(Error(
                JobDiagnosticCodes.DuplicateName,
                $"Collection name '{collection.Name}' is used more than once in the template.",
                new ModelPath("jobCollection", collection.Id.Value, "name")));
        }
    }

    private static void ValidateConfig(
        JobConfig config,
        HashSet<string> names,
        Simulation? simulation,
        Dictionary<DopingId, Model.Lattice.Doping> dopings,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            bag.Add(Error(
                JobDiagnosticCodes.DuplicateName,
                "Configuration name must not be empty or whitespace-only.",
                new ModelPath("jobConfig", config.Id.Value, "name")));
        }
        else if (!names.Add(config.Name))
        {
            bag.Add(Error(
                JobDiagnosticCodes.DuplicateName,
                $"Configuration name '{config.Name}' is used more than once in the collection.",
                new ModelPath("jobConfig", config.Id.Value, "name")));
        }

        if (config.Lattice is null ||
            config.Lattice.A is < minimumCellCount or > maximumCellCount ||
            config.Lattice.B is < minimumCellCount or > maximumCellCount ||
            config.Lattice.C is < minimumCellCount or > maximumCellCount)
        {
            bag.Add(Error(
                JobDiagnosticCodes.InvalidCellSize,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"Cell counts must each be within {minimumCellCount}..{maximumCellCount}."),
                new ModelPath("jobConfig", config.Id.Value, "lattice")));
        }

        if (config.Overrides is not null)
        {
            bag.AddRange(JobSettingsValidator.Validate(
                config.Overrides,
                simulation?.Kind,
                new ModelPath("jobConfig", config.Id.Value, "overrides")));
        }

        var dopingValues = config.Dopings is { IsDefault: false } value
            ? value
            : ImmutableArray<DopingValue>.Empty;
        if (dopingValues.Length > dopingsPerConfig)
        {
            bag.Add(Error(
                JobDiagnosticCodes.LimitExceeded,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"A configuration may contain at most {dopingsPerConfig} dopings; it contains {dopingValues.Length}."),
                new ModelPath("jobConfig", config.Id.Value, "dopings")));
        }

        var seenDopings = new HashSet<DopingId>();
        foreach (var dopingValue in dopingValues)
        {
            if (dopingValue is null)
            {
                continue;
            }

            if (!dopings.ContainsKey(dopingValue.DopingId))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.UnknownDoping,
                    $"Doping reference '{dopingValue.DopingId.Value:D}' does not exist.",
                    new ModelPath("jobConfig", config.Id.Value, "dopings")));
            }

            if (!seenDopings.Add(dopingValue.DopingId))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.DuplicateReference,
                    $"Doping reference '{dopingValue.DopingId.Value:D}' is used more than once in the configuration.",
                    new ModelPath("jobConfig", config.Id.Value, "dopings")));
            }

            if (!double.IsFinite(dopingValue.Fraction) ||
                dopingValue.Fraction is < 0.0 or > 1.0)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidDopingFraction,
                    "Doping fraction must be finite and within 0..1.",
                    new ModelPath("jobConfig", config.Id.Value, "dopings")));
            }
        }
    }

    private static void ValidateOptimizers(
        JobCollection collection,
        ImmutableArray<SelectionOptimizer> optimizers,
        Dictionary<CellSiteId, CellSite> sites,
        Dictionary<ParticleId, Particle> particles,
        Dictionary<ParticleSetId, ParticleSet> particleSets,
        DiagnosticBag bag)
    {
        if (optimizers.Length > optimizersPerCollection)
        {
            bag.Add(Error(
                JobDiagnosticCodes.LimitExceeded,
                string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"A collection may contain at most {optimizersPerCollection} optimizers; it contains {optimizers.Length}."),
                new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
        }

        var seen = new HashSet<(CellSiteId SiteId, ParticleId ParticleId)>();
        foreach (var optimizer in optimizers)
        {
            if (optimizer is null)
            {
                continue;
            }

            if (!seen.Add((optimizer.SiteId, optimizer.ParticleId)))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.DuplicateReference,
                    $"Optimizer ({optimizer.SiteId.Value:D}, {optimizer.ParticleId.Value:D}) is defined more than once.",
                    new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
                continue;
            }

            if (!sites.TryGetValue(optimizer.SiteId, out var site))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unknown site '{optimizer.SiteId.Value:D}'.",
                    new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
                continue;
            }

            if (site.Stability != SiteStability.Stable)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unstable site '{site.Name}'.",
                    new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
                continue;
            }

            if (!particles.ContainsKey(optimizer.ParticleId))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unknown particle '{optimizer.ParticleId.Value:D}'.",
                    new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
                continue;
            }

            if (!IsParticleAllowed(site, optimizer.ParticleId, particleSets))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Particle '{optimizer.ParticleId.Value:D}' is not allowed at site '{site.Name}'.",
                    new ModelPath("jobCollection", collection.Id.Value, "selectionOptimizers")));
            }
        }
    }

    private static bool IsParticleAllowed(
        CellSite site,
        ParticleId particleId,
        Dictionary<ParticleSetId, ParticleSet> particleSets)
    {
        if (site.ParticleSetId == default || site.ParticleSetId == ParticleSetId.Empty)
        {
            return false;
        }

        return particleSets.TryGetValue(site.ParticleSetId, out var particleSet) &&
               particleSet.ParticleIds.Contains(particleId);
    }

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));

    private static Diagnostic Warning(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Warning, message, path, JobDiagnosticCodes.HelpFor(code));
}

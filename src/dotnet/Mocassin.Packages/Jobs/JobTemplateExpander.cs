// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Expands one job template into the deterministic, build-time job descriptors.
/// </summary>
/// <remarks>
/// Collections are expanded in authored order, configurations in authored order, and duplicate jobs in
/// ascending <see cref="JobDescriptor.JobIndex"/>. <see cref="JobDescriptor.JobId"/> is 1-based across the
/// complete template. The effective job count is the collection override or the compiled simulation default.
/// Every configuration is resolved and validated before any descriptor is emitted, so invalid user data
/// returns diagnostics and an empty job list rather than a partial expansion. Job counts above
/// <see cref="MaximumExpandedJobs"/> are rejected.
/// </remarks>
public static class JobTemplateExpander
{
    /// <summary>
    /// The maximum number of jobs that can be expanded from one template.
    /// </summary>
    public const int MaximumExpandedJobs = 99_999;

    /// <summary>
    /// Expands the supplied job template.
    /// </summary>
    /// <param name="project">The project that owns the template and supplies the fallback master seed.</param>
    /// <param name="model">The compiled model that supplies simulations and receives the compiled indices.</param>
    /// <param name="template">The template to expand.</param>
    /// <returns>The descriptors and diagnostics. The descriptor list is empty when any error is reported.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static JobExpansionResult Expand(Project project, CompiledModel model, JobTemplate template)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);

        var diagnostics = new DiagnosticBag();
        if (!ProjectMetadata.IsValidMasterSeed(project.Metadata.MasterSeed))
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidMetadata,
                "The project master seed must be 64 lowercase hexadecimal characters.",
                new ModelPath("project", project.Metadata.ProjectId, "masterSeed")));
            return JobExpansionResult.Failed(diagnostics.Sorted);
        }

        var collections = template.Collections is { IsDefault: false } authoredCollections
            ? authoredCollections
            : ImmutableArray<JobCollection>.Empty;
        if (collections.Length == 0)
        {
            diagnostics.Add(Error(
                JobDiagnosticCodes.NoCollections,
                "Job template must contain at least one collection.",
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
            return JobExpansionResult.Failed(diagnostics.Sorted);
        }

        var simulations = BuildSimulationLookup(model);
        var plans = new List<CollectionPlan>();
        long totalJobs = 0;
        for (var collectionIndex = 0; collectionIndex < collections.Length; collectionIndex++)
        {
            var collection = collections[collectionIndex];
            if (collection is null)
            {
                continue;
            }

            if (!simulations.TryGetValue(collection.SimulationId, out var compiled))
            {
                diagnostics.Add(Error(
                    JobDiagnosticCodes.UnknownSimulation,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Collection references unknown simulation '{collection.SimulationId.Value:D}'."),
                    new ModelPath("jobCollection", collection.Id.Value, "simulationId")));
                continue;
            }

            var configs = collection.Configs is { IsDefault: false } authoredConfigs
                ? authoredConfigs
                : ImmutableArray<JobConfig>.Empty;
            if (configs.Length == 0)
            {
                diagnostics.Add(Error(
                    JobDiagnosticCodes.NoConfigs,
                    "Job collection must contain at least one configuration.",
                    new ModelPath("jobCollection", collection.Id.Value, "configs")));
                continue;
            }

            var jobCount = collection.JobCountPerConfig ?? compiled.Simulation.Settings.JobCount;
            if (jobCount is < 1 or > 1_000)
            {
                diagnostics.Add(Error(
                    JobDiagnosticCodes.InvalidJobCount,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The effective job count per configuration must be within 1..1000; it is {jobCount}."),
                    new ModelPath("jobCollection", collection.Id.Value, "jobCountPerConfig")));
                continue;
            }

            var configPlans = new List<ConfigPlan>(configs.Length);
            for (var configIndex = 0; configIndex < configs.Length; configIndex++)
            {
                var config = configs[configIndex];
                if (config is null)
                {
                    continue;
                }

                var settings = JobSettingsResolver.Resolve(
                    collection,
                    config,
                    compiled.Simulation,
                    out var settingsDiagnostics);
                diagnostics.AddRange(settingsDiagnostics);
                if (settings is null)
                {
                    continue;
                }

                configPlans.Add(new ConfigPlan(config, settings));
            }

            totalJobs += (long)jobCount * configPlans.Count;
            plans.Add(new CollectionPlan(
                collectionIndex,
                collection,
                compiled,
                jobCount,
                configPlans));
        }

        if (totalJobs > MaximumExpandedJobs)
        {
            diagnostics.Add(Error(
                JobDiagnosticCodes.JobLimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The template expands to {totalJobs} jobs; the maximum is {MaximumExpandedJobs}."),
                new ModelPath("jobTemplate", template.Id.Value, "collections")));
        }

        if (diagnostics.HasErrors)
        {
            return JobExpansionResult.Failed(diagnostics.Sorted);
        }

        var descriptors = ImmutableArray.CreateBuilder<JobDescriptor>((int)totalJobs);
        var jobId = 1;
        foreach (var plan in plans)
        {
            var seedText = JobSeedDeriver.ResolveSeedText(
                plan.Collection.RngSeed,
                plan.Compiled.Simulation.Settings.RngSeed,
                project.Metadata.MasterSeed);
            var masterSeed = JobSeedDeriver.DeriveMasterSeed(seedText!);
            var optimizers = plan.Collection.Optimizers is { IsDefault: false } authoredOptimizers
                ? authoredOptimizers
                : ImmutableArray<SelectionOptimizer>.Empty;

            for (var configIndex = 0; configIndex < plan.Configs.Count; configIndex++)
            {
                var configPlan = plan.Configs[configIndex];
                var config = configPlan.Config;
                var dopings = config.Dopings is { IsDefault: false } authoredDopings
                    ? authoredDopings
                    : ImmutableArray<DopingValue>.Empty;
                for (var jobIndex = 0; jobIndex < plan.JobCount; jobIndex++)
                {
                    descriptors.Add(new JobDescriptor(
                        jobId,
                        plan.CollectionIndex,
                        configIndex,
                        jobIndex,
                        plan.Collection.Name,
                        config.Name,
                        plan.Compiled.Simulation.Id,
                        plan.Compiled.Index,
                        plan.Compiled.Simulation.Kind,
                        plan.Compiled.Simulation.Intent.UseDualDofCorrection,
                        configPlan.Settings,
                        config.Lattice,
                        dopings,
                        optimizers,
                        JobSeedDeriver.DeriveJobSeed(
                            masterSeed.AsSpan(),
                            plan.CollectionIndex,
                            configIndex,
                            jobIndex)));
                    jobId++;
                }
            }
        }

        return new JobExpansionResult(descriptors.ToImmutable(), diagnostics.Sorted);
    }

    /// <summary>
    /// Resolves and expands one authored template by identifier.
    /// </summary>
    /// <param name="project">The project that owns the template and supplies the fallback master seed.</param>
    /// <param name="model">The compiled model that supplies simulations and receives the compiled indices.</param>
    /// <param name="templateId">The stable identifier of the template to expand.</param>
    /// <returns>
    /// The descriptors and diagnostics, or an unknown-reference diagnostic when the project does not contain
    /// the template.
    /// </returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static JobExpansionResult Expand(Project project, CompiledModel model, JobTemplateId templateId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);

        var templates = project.JobTemplates.IsDefault
            ? ImmutableArray<JobTemplate>.Empty
            : project.JobTemplates;
        foreach (var template in templates)
        {
            if (template is not null && template.Id == templateId)
            {
                return Expand(project, model, template);
            }
        }

        var diagnostic = Error(
            ProjectDiagnosticCodes.UnknownReference,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The project does not contain the job template '{templateId.Value:D}'."),
            new ModelPath("project", project.Metadata.ProjectId, "jobTemplates"));
        return JobExpansionResult.Failed([diagnostic]);
    }

    private static Dictionary<SimulationId, CompiledSimulationEntry> BuildSimulationLookup(CompiledModel model)
    {
        var simulations = model.Simulations.IsDefault
            ? ImmutableArray<CompiledSimulation>.Empty
            : model.Simulations;
        var lookup = new Dictionary<SimulationId, CompiledSimulationEntry>(simulations.Length);
        for (var index = 0; index < simulations.Length; index++)
        {
            var simulation = simulations[index];
            if (simulation is not null)
            {
                lookup.TryAdd(simulation.Id, new CompiledSimulationEntry(simulation, index));
            }
        }

        return lookup;
    }

    private static Diagnostic Error(string code, string message, ModelPath? path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));

    private sealed record CompiledSimulationEntry(CompiledSimulation Simulation, int Index);

    private sealed record ConfigPlan(JobConfig Config, ResolvedJobSettings Settings);

    private sealed record CollectionPlan(
        int CollectionIndex,
        JobCollection Collection,
        CompiledSimulationEntry Compiled,
        int JobCount,
        List<ConfigPlan> Configs);
}

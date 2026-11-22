// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// The loaded spike projects: the real samples plus the generated breadth value, loaded once per test process.
/// </summary>
/// <param name="Ceria">The ceria sample (particles, sets, structure).</param>
/// <param name="CeriaKmc">The ceria KMC transition sample; the editable transition document.</param>
/// <param name="CeriaMmc">The ceria MMC transition sample.</param>
/// <param name="Interstitialcy5">The interstitialcy-5 pattern-breadth sample.</param>
/// <param name="Vehicle5">The vehicle-5 pattern-breadth sample.</param>
/// <param name="PolaronHop">The polaron-hop Metropolis sample.</param>
/// <param name="Toy">The generated 1,024-site breadth value.</param>
internal sealed record SpikeProjects(
    ProjectModel Ceria,
    ProjectModel CeriaKmc,
    ProjectModel CeriaMmc,
    ProjectModel Interstitialcy5,
    ProjectModel Vehicle5,
    ProjectModel PolaronHop,
    ProjectModel Toy);

/// <summary>
/// Loads the spike's representative data through the production project store.
/// </summary>
internal static class SpikeFixtures
{
    private static readonly Lazy<Task<SpikeProjects>> projects = new(LoadAsync);

    /// <summary>
    /// Gets the loaded spike projects.
    /// </summary>
    /// <returns>The projects, loaded once per process.</returns>
    public static Task<SpikeProjects> GetAsync() => projects.Value;

    private static async Task<SpikeProjects> LoadAsync()
    {
        var ceria = await LoadAsync("samples", "ceria", "ceria.mocprj").ConfigureAwait(false);
        var kmc = await LoadAsync("samples", "transitions", "ceria-kmc.mocprj").ConfigureAwait(false);
        var mmc = await LoadAsync("samples", "transitions", "ceria-mmc.mocprj").ConfigureAwait(false);
        var interstitialcy = await LoadAsync("samples", "transitions", "interstitialcy5.mocprj")
            .ConfigureAwait(false);
        var vehicle = await LoadAsync("samples", "transitions", "vehicle5.mocprj").ConfigureAwait(false);
        var polaron = await LoadAsync("samples", "transitions", "polaron-hop.mocprj").ConfigureAwait(false);
        return new SpikeProjects(
            ceria,
            kmc,
            mmc,
            interstitialcy,
            vehicle,
            polaron,
            SpikeToyProject.Create(ceria));
    }

    private static async Task<ProjectModel> LoadAsync(params string[] parts)
    {
        var path = RepositoryPaths.Combine(parts);
        var store = new ProjectStore(
            new ManualTimeProvider(),
            new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path, CancellationToken.None).ConfigureAwait(false);
        return result.Project
            ?? throw new InvalidOperationException($"The spike fixture '{path}' did not load: {result.FailureKind}.");
    }
}

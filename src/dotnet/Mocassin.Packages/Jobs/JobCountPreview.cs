// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The simulation-scoped job-count preview of one job template.
/// </summary>
/// <remarks>
/// The preview never expands or generates a lattice. <see cref="DefaultJobCount"/> is the referenced simulation's
/// authored job count, <see cref="EffectiveJobCount"/> is the total number of jobs produced by the template
/// collections bound to that simulation after collection overrides, and <see cref="TotalJobs"/> is the whole
/// template total across every simulation.
/// </remarks>
/// <param name="SimulationId">The referenced simulation identifier.</param>
/// <param name="DefaultJobCount">The authored job count of the simulation.</param>
/// <param name="EffectiveJobCount">The job total of the collections bound to the simulation.</param>
/// <param name="TotalJobs">The job total of the whole template across all simulations.</param>
public sealed record JobCountPreview(
    SimulationId SimulationId,
    int DefaultJobCount,
    int EffectiveJobCount,
    long TotalJobs);

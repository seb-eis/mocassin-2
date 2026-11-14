// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// One named group of configurations that share a simulation and base settings.
/// </summary>
/// <param name="Id">The stable collection identifier.</param>
/// <param name="Name">The user-visible collection name.</param>
/// <param name="SimulationId">The referenced simulation.</param>
/// <param name="BaseSettings">The collection-level setting overrides, or <see langword="null"/> to inherit.</param>
/// <param name="JobCountPerConfig">The duplicate count per configuration, or <see langword="null"/> to inherit.</param>
/// <param name="RngSeed">The optional collection seed text.</param>
/// <param name="Optimizers">The KMC start-option optimizers in authored order.</param>
/// <param name="Configs">The configurations in authored order.</param>
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "JobCollection is the reviewed Step 10 contract name; 'collection' is a domain noun here.",
    Scope = "type",
    Target = "~T:Mocassin.Core.Model.Jobs.JobCollection")]
public sealed record JobCollection(
    JobCollectionId Id,
    string Name,
    SimulationId SimulationId,
    JobSettings? BaseSettings = null,
    int? JobCountPerConfig = null,
    string? RngSeed = null,
    [property: JsonPropertyName("selectionOptimizers")] ImmutableArray<SelectionOptimizer>? Optimizers = null,
    ImmutableArray<JobConfig>? Configs = null);

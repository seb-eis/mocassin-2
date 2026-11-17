// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Packages.Build;

/// <summary>
/// The complete description of one build request.
/// </summary>
/// <remarks>
/// The CLI and Step 26's view models construct the same request; the pipeline repeats the selection resolution
/// defensively and never reads a file, template, or option that is not on this record. <see cref="TargetPath"/>
/// is required for a full build and ignored by a preview. <see cref="CreatedUtc"/> defaults to the request clock.
/// </remarks>
public sealed record BuildRequest
{
    /// <summary>Gets the <c>.mocprj</c> project path.</summary>
    public required string ProjectPath { get; init; }

    /// <summary>Gets the final <c>.msl</c> path; ignored by a preview.</summary>
    public string? TargetPath { get; init; }

    /// <summary>Gets the explicit build template, or <see langword="null"/> for automatic selection.</summary>
    public BuildTemplateId? BuildTemplateId { get; init; }

    /// <summary>Gets the explicit job template override, or <see langword="null"/>.</summary>
    public JobTemplateId? JobTemplateId { get; init; }

    /// <summary>Gets the explicit parameterization override, or <see langword="null"/>.</summary>
    public ParameterizationId? ParameterizationId { get; init; }

    /// <summary>
    /// Gets the 1-based full-expansion job selection expression, for example <c>all</c>, <c>1-100,250</c>, or
    /// <c>2,5</c>.
    /// </summary>
    public string JobSelection { get; init; } = "all";

    /// <summary>Gets a value indicating whether the run is a no-write preview.</summary>
    public bool Preview { get; init; }

    /// <summary>Gets a value indicating whether an existing package may be replaced atomically.</summary>
    public bool Overwrite { get; init; }

    /// <summary>Gets a value indicating whether the compact package preset is requested.</summary>
    public bool Compact { get; init; }

    /// <summary>Gets the maximum compile and lattice-generation concurrency (1..256).</summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>Gets a value indicating whether validation and parameterization warnings fail the build.</summary>
    public bool WarningsAsErrors { get; init; }

    /// <summary>Gets the build timestamp; <see langword="null"/> uses the request clock.</summary>
    public DateTimeOffset? CreatedUtc { get; init; }

    /// <summary>Gets the injected clock, progress interval, and event-channel capacity.</summary>
    public BuildOptions Options { get; init; } = new();
}

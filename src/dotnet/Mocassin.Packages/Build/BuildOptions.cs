// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Build;

/// <summary>
/// Injection points for one build run.
/// </summary>
/// <remarks>
/// The options are reached only through <see cref="BuildRequest.Options"/>; the pipeline never reads ambient or
/// static configuration and has no options constructor. Tests supply a fixed clock, a longer progress interval,
/// or a lower event-channel capacity through the request.
/// </remarks>
public sealed record BuildOptions
{
    /// <summary>
    /// Gets the minimum interval between two progress events for the same stage.
    /// </summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the clock used for timings, the build timestamp, and progress throttling.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Gets the capacity of the bounded build-event channel.
    /// </summary>
    public int EventChannelCapacity { get; init; } = 256;
}

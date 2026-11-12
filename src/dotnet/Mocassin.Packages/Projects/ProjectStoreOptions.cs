// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects;

/// <summary>
/// Configuration for <see cref="ProjectStore"/>.
/// </summary>
public sealed record ProjectStoreOptions
{
    /// <summary>
    /// Gets the age after which an unresponsive lock file may be reclaimed.
    /// </summary>
    public TimeSpan StaleLockTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets the suggested autosave interval for the document layer.
    /// </summary>
    public TimeSpan AutosaveInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets a value indicating whether a <c>.bak</c> copy is created before replacing an existing file.
    /// </summary>
    public bool CreateBackup { get; init; }

    /// <summary>
    /// Gets the maximum accepted project document size in bytes.
    /// </summary>
    public long MaxDocumentBytes { get; init; } = 64L * 1024L * 1024L;

    /// <summary>
    /// Gets a value indicating whether the composed model validator runs after a successful load.
    /// </summary>
    /// <remarks>
    /// Store callers that own a higher-level validation pass, such as the validation pipeline, set this to
    /// <see langword="false"/> so the model is validated exactly once.
    /// </remarks>
    public bool ValidateOnLoad { get; init; } = true;
}

// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Recovery;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Creates, opens, saves, reloads, and closes project sessions. Services never prompt.
/// </summary>
internal interface IProjectSessionService
{
    /// <summary>
    /// Gets the open sessions.
    /// </summary>
    public IReadOnlyList<ProjectSession> Sessions { get; }

    /// <summary>
    /// Gets the active session.
    /// </summary>
    public ProjectSession? ActiveSession { get; }

    /// <summary>
    /// Creates a new project from a template in a target directory.
    /// </summary>
    /// <param name="kind">The template kind.</param>
    /// <param name="name">The project name.</param>
    /// <param name="directory">The target directory.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> CreateAsync(
        ProjectTemplateKind kind,
        string name,
        string directory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens or activates a project.
    /// </summary>
    /// <param name="path">The project path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> OpenAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an already-open project.
    /// </summary>
    /// <param name="path">The project path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> ActivateAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> SaveAsync(ProjectSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a session to a new path.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="path">The new project path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> SaveAsAsync(
        ProjectSession session,
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads a session from disk.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="discardChanges">Whether dirty changes may be discarded without a prompt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> ReloadAsync(
        ProjectSession session,
        bool discardChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a session and releases its lock.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="discardChanges">Whether dirty changes may be discarded without a prompt.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> CloseAsync(
        ProjectSession session,
        bool discardChanges = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies one recovery candidate to a session.
    /// </summary>
    /// <param name="session">The target session.</param>
    /// <param name="candidate">The candidate to recover.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> RecoverAsync(
        ProjectSession session,
        RecoveryCandidate candidate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards one recovery candidate.
    /// </summary>
    /// <param name="session">The target session.</param>
    /// <param name="candidate">The candidate to discard.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<ProjectSessionResult> DiscardRecoveryAsync(
        ProjectSession session,
        RecoveryCandidate candidate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the valid recovery candidates in the application-local recovery directory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The candidates, newest first.</returns>
    public Task<IReadOnlyList<RecoveryCandidate>> ListRecoveryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sweeps recovery files older than the retention.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted candidates.</returns>
    public Task<int> SweepRecoveryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the current session and clean-exit state in the settings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the settings write attempt.</returns>
    public Task MarkCleanExitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the project file changed outside the session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <returns>The external-change classification.</returns>
    public ProjectExternalChange CheckExternalChange(ProjectSession session);
}

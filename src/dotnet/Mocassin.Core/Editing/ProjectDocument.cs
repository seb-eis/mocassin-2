// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// I/O-free editing session over an immutable project, with undo/redo and dirty tracking.
/// </summary>
/// <remarks>
/// Persistence and locking live in <c>Mocassin.Packages.Projects</c>. The application or CLI calls
/// <see cref="MarkSaved(ProjectModel)"/> after a successful store save.
/// </remarks>
public sealed class ProjectDocument
{
    private readonly ProjectEditor editor;
    private readonly List<ProjectChange> undoStack = [];
    private readonly List<ProjectChange> redoStack = [];
    private int maxUndoDepth;
    private DateTimeOffset? lastAutosaveUtc;

    /// <summary>
    /// Initializes a document from a loaded or newly created project.
    /// </summary>
    /// <param name="project">The current project value, which is also treated as the last saved value.</param>
    /// <param name="editor">Optional custom editor.</param>
    public ProjectDocument(ProjectModel project, ProjectEditor? editor = null)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        LastSavedProject = project;
        this.editor = editor ?? new ProjectEditor();
    }

    /// <summary>
    /// Gets the current project value.
    /// </summary>
    public ProjectModel Project { get; private set; }

    /// <summary>
    /// Gets the last persisted project value.
    /// </summary>
    public ProjectModel LastSavedProject { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current project differs from the last saved project.
    /// </summary>
    public bool IsDirty =>
        !string.Equals(
            ProjectHasher.ComputeDocument(Project),
            ProjectHasher.ComputeDocument(LastSavedProject),
            StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether undo is available.
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether redo is available.
    /// </summary>
    public bool CanRedo => redoStack.Count > 0;

    /// <summary>
    /// Gets or initializes the maximum number of retained undo entries; <c>0</c> means unbounded.
    /// </summary>
    /// <remarks>
    /// The GUI initializes documents with a finite bound (200); the library default keeps Step 01's unbounded
    /// behaviour. Overflow drops the oldest entries first. Redo entries are bounded by the same value.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxUndoDepth
    {
        get => maxUndoDepth;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "The maximum undo depth must be zero (unbounded) or positive.");
            }

            maxUndoDepth = value;
        }
    }

    /// <summary>
    /// Gets the current undo stack in oldest-first order.
    /// </summary>
    public ImmutableArray<ProjectChange> UndoStack => [.. undoStack];

    /// <summary>
    /// Gets the current redo stack in oldest-first order.
    /// </summary>
    public ImmutableArray<ProjectChange> RedoStack => [.. redoStack];

    /// <summary>
    /// Gets the UTC time of the last successful autosave, if any.
    /// </summary>
    public DateTimeOffset? LastAutosaveUtc => lastAutosaveUtc;

    /// <summary>
    /// Applies a validated mutation, records one undoable change, and clears the redo stack on success.
    /// </summary>
    /// <param name="description">The user-visible change description.</param>
    /// <param name="mutation">The pure mutation that returns the candidate project.</param>
    /// <returns>The edit result. Failed edits leave the document unchanged.</returns>
    public ProjectEditResult Apply(string description, Func<ProjectModel, ProjectModel> mutation)
    {
        var result = editor.Apply(Project, description, mutation);
        if (result.Change is null)
        {
            return result;
        }

        undoStack.Add(result.Change);
        TrimUndoStack();
        redoStack.Clear();
        Project = result.Project;
        return result;
    }

    /// <summary>
    /// Undoes the most recent change.
    /// </summary>
    /// <returns><see langword="true"/> when a change was undone.</returns>
    public bool Undo()
    {
        if (undoStack.Count == 0)
        {
            return false;
        }

        var change = undoStack[^1];
        undoStack.RemoveAt(undoStack.Count - 1);
        redoStack.Add(change);
        Project = change.Before;
        return true;
    }

    /// <summary>
    /// Redoes the most recently undone change.
    /// </summary>
    /// <returns><see langword="true"/> when a change was redone.</returns>
    public bool Redo()
    {
        if (redoStack.Count == 0)
        {
            return false;
        }

        var change = redoStack[^1];
        redoStack.RemoveAt(redoStack.Count - 1);
        undoStack.Add(change);
        TrimUndoStack();
        Project = change.After;
        return true;
    }

    private void TrimUndoStack()
    {
        if (maxUndoDepth == 0)
        {
            return;
        }

        var overflow = undoStack.Count - maxUndoDepth;
        if (overflow > 0)
        {
            undoStack.RemoveRange(0, overflow);
        }
    }

    /// <summary>
    /// Marks a persisted project value as the last saved value.
    /// </summary>
    /// <param name="savedProject">The project value returned by the store, including its updated timestamp.</param>
    public void MarkSaved(ProjectModel savedProject)
    {
        ArgumentNullException.ThrowIfNull(savedProject);
        Project = savedProject;
        LastSavedProject = savedProject;
    }

    /// <summary>
    /// Marks the current project value as saved.
    /// </summary>
    public void MarkSaved() => LastSavedProject = Project;

    /// <summary>
    /// Determines whether an autosave should run now.
    /// </summary>
    /// <param name="now">The current UTC time.</param>
    /// <param name="interval">The configured autosave interval.</param>
    /// <returns><see langword="true"/> when the document is dirty and the interval has elapsed.</returns>
    public bool IsAutosaveDue(DateTimeOffset now, TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Autosave interval must be positive.");
        }

        return IsDirty && (lastAutosaveUtc is null || now - lastAutosaveUtc.Value >= interval);
    }

    /// <summary>
    /// Records a successful autosave timestamp.
    /// </summary>
    /// <param name="now">The UTC time of the autosave.</param>
    public void MarkAutosaved(DateTimeOffset now) => lastAutosaveUtc = now;
}

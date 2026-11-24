// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// Folds several typed <see cref="ProjectEditor"/> operations into the one delegate the Core
/// <see cref="ProjectDocument.Apply(string, Func{ProjectEditor, ProjectModel, ProjectEditResult})"/> overload accepts.
/// </summary>
/// <remarks>
/// <para>
/// All-or-nothing (D-13): the fold validates each step over a candidate project. A mid-batch rejection returns the
/// **original** document project, a null change, and the accumulated diagnostics, so the document, undo stack, redo
/// stack, save point, and dirty state stay untouched. The failing step's diagnostics name the object it failed on.
/// </para>
/// <para>
/// The fold never constructs a <see cref="ProjectChange"/>. It returns the last successful typed result; the Core
/// overload is the permitted place that records the canonical change from the document value to the final value.
/// A later no-op step is content-equal to the last change's result, so the last non-null change is the right
/// anchor.
/// </para>
/// </remarks>
internal static class BulkEditComposer
{
    /// <summary>
    /// Composes typed operations into one all-or-nothing delegate.
    /// </summary>
    /// <param name="steps">The typed operations, in order.</param>
    /// <returns>The composed delegate.</returns>
    public static Func<ProjectEditor, ProjectModel, ProjectEditResult> Compose(
        IEnumerable<Func<ProjectEditor, ProjectModel, ProjectEditResult>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var operations = steps.ToArray();
        if (operations.Length == 0)
        {
            throw new ArgumentException("A bulk edit needs at least one operation.", nameof(steps));
        }

        return (editor, project) =>
        {
            var candidate = project;
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            ProjectChange? lastChange = null;

            foreach (var operation in operations)
            {
                var result = operation(editor, candidate);
                if (!result.Diagnostics.IsDefault)
                {
                    diagnostics.AddRange(result.Diagnostics);
                }

                if (!result.IsSuccess)
                {
                    return new ProjectEditResult(project, null, diagnostics.ToImmutable());
                }

                candidate = result.Project;
                if (result.Change is not null)
                {
                    lastChange = result.Change;
                }
            }

            return lastChange is null
                ? new ProjectEditResult(project, null, diagnostics.ToImmutable())
                : new ProjectEditResult(lastChange.After, lastChange, diagnostics.ToImmutable());
        };
    }
}

// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The stable metadata for one model-editor domain: its model-path domain, document kind, title, empty-state text,
/// and object noun.
/// </summary>
/// <param name="Domain">The model-path domain the editor owns.</param>
/// <param name="DocumentKind">The stable document kind.</param>
/// <param name="Title">The tab title.</param>
/// <param name="EmptyMessage">The empty-state sentence that names the next action.</param>
/// <param name="ObjectNoun">The singular object noun used in blocked-delete and usage text.</param>
internal sealed record ModelEditorDescriptor(
    string Domain,
    string DocumentKind,
    string Title,
    string EmptyMessage,
    string ObjectNoun)
{
    /// <summary>
    /// Creates the document key for a project scope.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The document key.</returns>
    public DocumentKey KeyFor(Guid projectId) => new(DocumentKind, projectId);

    /// <summary>
    /// Creates the activation path for a project.
    /// </summary>
    /// <param name="projectId">The project id.</param>
    /// <returns>The model path.</returns>
    public ModelPath ActivationPath(Guid projectId) => new(Domain, projectId);
}

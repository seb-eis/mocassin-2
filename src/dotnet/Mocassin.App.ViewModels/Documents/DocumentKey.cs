// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Documents;

/// <summary>
/// Stable identity of a document tab. The scope id is the project id for project-scoped documents.
/// </summary>
/// <param name="Kind">The stable document kind.</param>
/// <param name="ScopeId">The optional project scope.</param>
internal readonly record struct DocumentKey(string Kind, Guid? ScopeId = null);

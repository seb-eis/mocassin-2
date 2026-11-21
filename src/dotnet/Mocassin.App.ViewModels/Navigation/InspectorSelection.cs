// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// The shell's typed selection passed to inspector contributors.
/// </summary>
/// <param name="DocumentKey">The active document key.</param>
/// <param name="ProjectId">The active project id, if any.</param>
/// <param name="Paths">The selected model paths.</param>
/// <param name="Domain">The optional selected domain.</param>
internal sealed record InspectorSelection(
    DocumentKey DocumentKey,
    Guid? ProjectId,
    IReadOnlyList<ModelPath> Paths,
    string? Domain);

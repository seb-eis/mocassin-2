// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// One entry in the navigation history.
/// </summary>
/// <param name="ProjectId">The optional project scope.</param>
/// <param name="Key">The document key.</param>
/// <param name="Path">The optional model path used to recreate the document.</param>
/// <param name="Intent">The navigation intent.</param>
internal readonly record struct NavigationEntry(
    Guid? ProjectId,
    DocumentKey Key,
    ModelPath? Path,
    NavigationIntent Intent);

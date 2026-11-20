// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// The intent of a navigation request.
/// </summary>
internal enum NavigationIntent
{
    /// <summary>
    /// Open or activate the document.
    /// </summary>
    Activate,

    /// <summary>
    /// Select an object in the target document.
    /// </summary>
    SelectObject,

    /// <summary>
    /// Focus a property in the target document.
    /// </summary>
    FocusProperty,
}

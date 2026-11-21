// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// One related path in a diagnostic's context menu.
/// </summary>
internal sealed class DiagnosticRelatedItemViewModel
{
    /// <summary>
    /// Initializes a related-path entry.
    /// </summary>
    /// <param name="path">The related path.</param>
    /// <param name="navigation">The navigation service.</param>
    public DiagnosticRelatedItemViewModel(ModelPath path, INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(navigation);
        Display = path.ToString();
        NavigateCommand = new AsyncRelayCommand(
            () => navigation.NavigateToAsync(path, NavigationIntent.SelectObject));
    }

    /// <summary>
    /// Gets the display text.
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// Gets the navigate command.
    /// </summary>
    public AsyncRelayCommand NavigateCommand { get; }
}

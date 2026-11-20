// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Applies the theme, density, font scale, contrast, and motion settings. The implementation lives in Views.
/// </summary>
internal interface IThemeApplier
{
    /// <summary>
    /// Applies settings to the live application.
    /// </summary>
    /// <param name="settings">The current settings.</param>
    public void Apply(AppSettings settings);
}

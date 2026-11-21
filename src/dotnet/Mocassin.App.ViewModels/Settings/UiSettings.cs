// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// UI behaviour settings.
/// </summary>
internal sealed record UiSettings
{
    /// <summary>
    /// Gets a value indicating whether an external project change prompts before reloading.
    /// </summary>
    public bool ConfirmOnExternalChange { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the welcome document opens on a fresh start.
    /// </summary>
    public bool ShowWelcomeOnStart { get; init; } = true;
}

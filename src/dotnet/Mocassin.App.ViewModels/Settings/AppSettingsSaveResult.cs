// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Result of writing the application settings.
/// </summary>
/// <param name="IsSuccess">Whether the file was replaced.</param>
/// <param name="Message">The failure message, if any.</param>
internal sealed record AppSettingsSaveResult(bool IsSuccess, string? Message = null)
{
    /// <summary>
    /// Gets a successful result.
    /// </summary>
    public static AppSettingsSaveResult Success { get; } = new(true);
}

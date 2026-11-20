// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Pure settings migrations keyed by schema version.
/// </summary>
internal static class SettingsMigration
{
    /// <summary>
    /// Migrates an older document to the current major. Version 1 is the first version, so the migration is the
    /// identity function and is idempotent.
    /// </summary>
    /// <param name="settings">The loaded settings.</param>
    /// <returns>The settings at the current schema version.</returns>
    public static AppSettings Migrate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.SchemaVersion < SettingsDefaults.SchemaVersion
            ? settings with { SchemaVersion = SettingsDefaults.SchemaVersion }
            : settings;
    }
}

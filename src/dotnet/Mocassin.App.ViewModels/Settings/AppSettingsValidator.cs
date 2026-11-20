// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Normalizes programmatic settings, clamping values that the dialog or a migration can produce.
/// </summary>
internal static class AppSettingsValidator
{
    /// <summary>
    /// Normalizes settings and reports repairs.
    /// </summary>
    /// <param name="settings">The candidate settings.</param>
    /// <returns>The normalized settings and warnings.</returns>
    public static (AppSettings Settings, IReadOnlyList<string> Warnings) Normalize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var warnings = new List<string>();
        var fontScale = settings.FontScale;
        if (!double.IsFinite(fontScale)
            || fontScale < SettingsDefaults.MinFontScale
            || fontScale > SettingsDefaults.MaxFontScale)
        {
            warnings.Add("The font scale is outside 0.85–1.5; the default was used.");
            fontScale = SettingsDefaults.DefaultFontScale;
        }

        var accent = settings.Accent;
        if (accent is not null && !AccentPalette.IsKnown(accent))
        {
            warnings.Add($"The accent '{accent}' is not in the fixed palette; the default accent was used.");
            accent = null;
        }

        var layout = settings.Layout;
        var normalizedLayout = new LayoutSettings
        {
            LeftWidth = InRange(
                layout.LeftWidth,
                SettingsDefaults.MinLeftWidth,
                SettingsDefaults.MaxLeftWidth,
                SettingsDefaults.DefaultLeftWidth,
                "left width",
                warnings),
            RightWidth = InRange(
                layout.RightWidth,
                SettingsDefaults.MinRightWidth,
                SettingsDefaults.MaxRightWidth,
                SettingsDefaults.DefaultRightWidth,
                "right width",
                warnings),
            ConsoleHeight = InRange(
                layout.ConsoleHeight,
                SettingsDefaults.MinConsoleHeight,
                SettingsDefaults.MaxConsoleHeight,
                SettingsDefaults.DefaultConsoleHeight,
                "console height",
                warnings),
            LeftVisible = layout.LeftVisible,
            RightVisible = layout.RightVisible,
            ConsoleVisible = layout.ConsoleVisible,
            ActiveConsoleChannel = string.IsNullOrWhiteSpace(layout.ActiveConsoleChannel)
                ? SettingsDefaults.DefaultConsoleChannel
                : layout.ActiveConsoleChannel,
        };

        var recent = NormalizeRecentFiles(settings.RecentFiles, warnings);
        var normalized = settings with
        {
            Format = SettingsDefaults.Format,
            SchemaVersion = SettingsDefaults.SchemaVersion,
            Accent = accent,
            FontScale = fontScale,
            Culture = string.IsNullOrWhiteSpace(settings.Culture) ? "en" : settings.Culture,
            Layout = normalizedLayout,
            RecentFiles = recent,
        };
        return (normalized, warnings);
    }

    private static int InRange(int value, int minimum, int maximum, int fallback, string name, List<string> warnings)
    {
        if (value < minimum || value > maximum)
        {
            warnings.Add($"The {name} is outside {minimum}–{maximum}; the default was used.");
            return fallback;
        }

        return value;
    }

    private static ImmutableArray<RecentFile> NormalizeRecentFiles(
        ImmutableArray<RecentFile> files,
        List<string> warnings)
    {
        if (files.IsDefaultOrEmpty)
        {
            return [];
        }

        var comparer = StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);
        var builder = ImmutableArray.CreateBuilder<RecentFile>();
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.Path) || !seen.Add(file.Path))
            {
                continue;
            }

            builder.Add(file);
            if (builder.Count == SettingsDefaults.MaxRecentFiles)
            {
                break;
            }
        }

        if (files.Length > SettingsDefaults.MaxRecentFiles)
        {
            warnings.Add("The recent-file list was capped at 10 entries.");
        }

        return builder.ToImmutable();
    }
}

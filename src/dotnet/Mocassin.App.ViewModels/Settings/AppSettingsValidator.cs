// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

using Mocassin.App.ViewModels.Settings.Blocks;

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
    /// <param name="ignoreCasePaths">Whether recent paths are de-duplicated case-insensitively.</param>
    /// <returns>The normalized settings and warnings.</returns>
    public static (AppSettings Settings, IReadOnlyList<string> Warnings) Normalize(
        AppSettings settings,
        bool ignoreCasePaths = false)
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

        var layout = settings.Layout;
        var leftFraction = Fraction(
            layout.LeftFraction,
            SettingsDefaults.MinSideFraction,
            SettingsDefaults.MaxSideFraction,
            SettingsDefaults.DefaultLeftFraction,
            "left fraction",
            warnings);
        var rightFraction = Fraction(
            layout.RightFraction,
            SettingsDefaults.MinSideFraction,
            SettingsDefaults.MaxSideFraction,
            SettingsDefaults.DefaultRightFraction,
            "right fraction",
            warnings);
        if (leftFraction + rightFraction >= 1d)
        {
            warnings.Add("The left and right panel fractions leave no centre share; their defaults were used.");
            leftFraction = SettingsDefaults.DefaultLeftFraction;
            rightFraction = SettingsDefaults.DefaultRightFraction;
        }

        var normalizedLayout = new LayoutSettings
        {
            LeftFraction = leftFraction,
            RightFraction = rightFraction,
            ConsoleFraction = Fraction(
                layout.ConsoleFraction,
                SettingsDefaults.MinConsoleFraction,
                SettingsDefaults.MaxConsoleFraction,
                SettingsDefaults.DefaultConsoleFraction,
                "console fraction",
                warnings),
            EditorFraction = EditorFraction(layout.EditorFraction, warnings),
            LeftVisible = layout.LeftVisible,
            RightVisible = layout.RightVisible,
            ConsoleVisible = layout.ConsoleVisible,
            ActiveConsoleChannel = string.IsNullOrWhiteSpace(layout.ActiveConsoleChannel)
                ? SettingsDefaults.DefaultConsoleChannel
                : layout.ActiveConsoleChannel,
            DefaultsRevision = layout.DefaultsRevision < SettingsDefaults.LegacyDefaultsRevision
                ? SettingsDefaults.DefaultsRevision
                : layout.DefaultsRevision,
        };

        var recent = NormalizeRecentFiles(settings.RecentFiles, warnings, ignoreCasePaths);
        var autosave = NormalizeAutosave(settings.Autosave, warnings);
        var session = NormalizeSession(settings.Session, warnings, ignoreCasePaths);
        var normalized = settings with
        {
            Format = SettingsDefaults.Format,
            SchemaVersion = SettingsDefaults.SchemaVersion,
            FontScale = fontScale,
            Culture = string.IsNullOrWhiteSpace(settings.Culture) ? "en" : settings.Culture,
            Layout = normalizedLayout,
            RecentFiles = recent,
            Autosave = autosave,
            Session = session,
        };
        return (normalized, warnings);
    }

    private static AutosaveSettings NormalizeAutosave(AutosaveSettings autosave, List<string> warnings)
    {
        var interval = autosave.IntervalMinutes;
        if (interval < SettingsDefaults.MinAutosaveIntervalMinutes
            || interval > SettingsDefaults.MaxAutosaveIntervalMinutes)
        {
            warnings.Add(
                $"The autosave interval is outside {SettingsDefaults.MinAutosaveIntervalMinutes}–{SettingsDefaults.MaxAutosaveIntervalMinutes}; the default was used.");
            interval = SettingsDefaults.DefaultAutosaveIntervalMinutes;
        }

        return new AutosaveSettings { Enabled = autosave.Enabled, IntervalMinutes = interval };
    }

    private static SessionSettings NormalizeSession(
        SessionSettings session,
        List<string> warnings,
        bool ignoreCasePaths)
    {
        var comparer = ignoreCasePaths ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var builder = ImmutableArray.CreateBuilder<string>();
        var source = session.OpenProjects.IsDefault ? ImmutableArray<string>.Empty : session.OpenProjects;
        foreach (var path in source)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                warnings.Add("A session open-project path was empty and was dropped.");
                continue;
            }

            if (builder.Any(existing => comparer.Equals(existing, path)))
            {
                continue;
            }

            if (builder.Count >= SettingsDefaults.MaxOpenProjects)
            {
                warnings.Add($"session.openProjects was capped at {SettingsDefaults.MaxOpenProjects} entries.");
                break;
            }

            builder.Add(path);
        }

        var active = session.ActiveProject;
        if (active is not null && !builder.Any(path => comparer.Equals(path, active)))
        {
            warnings.Add("session.activeProject was not one of the open projects; it was cleared.");
            active = null;
        }

        return new SessionSettings
        {
            OpenProjects = builder.ToImmutable(),
            ActiveProject = active,
            CleanExit = session.CleanExit,
            SavedUtc = session.SavedUtc,
        };
    }

    private static double EditorFraction(double value, List<string> warnings)
    {
        if (!double.IsFinite(value))
        {
            warnings.Add("The editor fraction is not finite; the default was used.");
            return SettingsDefaults.DefaultEditorFraction;
        }

        var clamped = Math.Clamp(
            value,
            SettingsDefaults.MinEditorFraction,
            SettingsDefaults.MaxEditorFraction);
        if (clamped != value)
        {
            warnings.Add(
                $"The editor fraction was clamped to {SettingsDefaults.MinEditorFraction}–{SettingsDefaults.MaxEditorFraction}.");
        }

        return clamped;
    }

    private static double Fraction(
        double value,
        double minimum,
        double maximum,
        double fallback,
        string name,
        List<string> warnings)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            warnings.Add($"The {name} is outside {minimum}–{maximum}; the default was used.");
            return fallback;
        }

        return value;
    }

    private static ImmutableArray<RecentFile> NormalizeRecentFiles(
        ImmutableArray<RecentFile> files,
        List<string> warnings,
        bool ignoreCasePaths)
    {
        if (files.IsDefaultOrEmpty)
        {
            return [];
        }

        var comparer = ignoreCasePaths ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
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

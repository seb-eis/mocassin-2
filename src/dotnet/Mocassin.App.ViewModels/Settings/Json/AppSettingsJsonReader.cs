// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

using Mocassin.App.ViewModels.Settings.Blocks;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Settings.Json;

/// <summary>
/// Tolerant reader for the application-local settings document. Unknown properties are ignored. A bad value
/// falls back to that field's default with one warning; a wrong format or future major rejects the document.
/// </summary>
internal static class AppSettingsJsonReader
{
    /// <summary>
    /// Reads settings from UTF-8 JSON.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 JSON bytes.</param>
    /// <param name="ignoreCasePaths">Whether recent-file paths are de-duplicated case-insensitively.</param>
    /// <returns>The load result.</returns>
    public static AppSettingsLoadResult Read(ReadOnlySpan<byte> utf8Json, bool ignoreCasePaths = false)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8Json.ToArray());
            return Read(document.RootElement, ignoreCasePaths);
        }
        catch (JsonException exception)
        {
            return AppSettingsLoadResult.Fatal(
                string.Create(CultureInfo.InvariantCulture, $"The settings document is malformed: {exception.Message}"));
        }
    }

    /// <summary>
    /// Reads settings from JSON text.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <param name="ignoreCasePaths">Whether recent-file paths are de-duplicated case-insensitively.</param>
    /// <returns>The load result.</returns>
    public static AppSettingsLoadResult Read(string json, bool ignoreCasePaths = false) =>
        Read(System.Text.Encoding.UTF8.GetBytes(json), ignoreCasePaths);

    private static AppSettingsLoadResult Read(JsonElement root, bool ignoreCasePaths)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return AppSettingsLoadResult.Fatal("The settings document root must be an object.");
        }

        if (!root.TryGetProperty("format", out var formatElement)
            || formatElement.ValueKind != JsonValueKind.String
            || !string.Equals(formatElement.GetString(), SettingsDefaults.Format, StringComparison.Ordinal))
        {
            return AppSettingsLoadResult.Fatal(
                $"The settings format must be '{SettingsDefaults.Format}'; the file was not loaded.");
        }

        var warnings = new List<string>();
        var schemaVersion = ReadInt(root, "schemaVersion", SettingsDefaults.SchemaVersion, warnings);
        if (schemaVersion > SettingsDefaults.SchemaVersion)
        {
            return AppSettingsLoadResult.Fatal(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Settings schema version {schemaVersion} is newer than this application supports."));
        }

        if (schemaVersion < 1)
        {
            warnings.Add("The settings schema version was below 1; defaults were used for the base document.");
            schemaVersion = SettingsDefaults.SchemaVersion;
        }

        var loggingElement = ReadObject(root, "logging", warnings);
        var uiElement = ReadObject(root, "ui", warnings);
        var pathsElement = ReadObject(root, "paths", warnings);
        var layoutElement = ReadObject(root, "layout", warnings);
        var autosaveElement = ReadObject(root, "autosave", warnings);
        var sessionElement = ReadObject(root, "session", warnings);

        var settings = new AppSettings
        {
            SchemaVersion = schemaVersion,
            Theme = ReadTheme(root, warnings),
            Accent = ReadAccent(root, warnings),
            Density = ReadDensity(root, warnings),
            FontScale = ReadFontScale(root, warnings),
            HighContrast = ReadBool(root, "highContrast", false, warnings),
            ReduceMotion = ReadBool(root, "reduceMotion", false, warnings),
            Culture = ReadString(root, "culture", "en", warnings, allowNull: false) ?? "en",
            Logging = new LoggingSettings { Level = ReadLogLevel(loggingElement, warnings) },
            Ui = new UiSettings
            {
                ConfirmOnExternalChange = ReadBool(uiElement, "confirmOnExternalChange", true, warnings),
                ShowWelcomeOnStart = ReadBool(uiElement, "showWelcomeOnStart", true, warnings),
            },
            Paths = new PathSettings
            {
                LastOpenDirectory = ReadString(pathsElement, "lastOpenDirectory", null, warnings, allowNull: true),
                LastSaveDirectory = ReadString(pathsElement, "lastSaveDirectory", null, warnings, allowNull: true),
                LastFolderDirectory = ReadString(pathsElement, "lastFolderDirectory", null, warnings, allowNull: true),
            },
            RecentFiles = ReadRecentFiles(root, warnings, ignoreCasePaths),
            Layout = ReadLayout(layoutElement, warnings),
            Autosave = ReadAutosave(autosaveElement, warnings),
            Session = ReadSession(sessionElement, warnings, ignoreCasePaths),
        };

        return new AppSettingsLoadResult(settings, warnings, IsFatal: false, LoadedFromFile: true, FatalMessage: null);
    }

    private static LayoutSettings ReadLayout(JsonElement element, List<string> warnings)
    {
        var hasFractions = element.ValueKind == JsonValueKind.Object
            && (element.TryGetProperty("leftFraction", out _)
                || element.TryGetProperty("rightFraction", out _)
                || element.TryGetProperty("consoleFraction", out _));
        var leftFraction = hasFractions
            ? ReadFraction(
                element,
                "leftFraction",
                SettingsDefaults.DefaultLeftFraction,
                SettingsDefaults.MinSideFraction,
                SettingsDefaults.MaxSideFraction,
                warnings)
            : ReadLegacyFraction(
                element,
                "leftWidth",
                SettingsDefaults.LegacyDesignWidth,
                SettingsDefaults.MinSideFraction,
                SettingsDefaults.MaxSideFraction,
                SettingsDefaults.DefaultLeftFraction,
                warnings);
        var rightFraction = hasFractions
            ? ReadFraction(
                element,
                "rightFraction",
                SettingsDefaults.DefaultRightFraction,
                SettingsDefaults.MinSideFraction,
                SettingsDefaults.MaxSideFraction,
                warnings)
            : ReadLegacyFraction(
                element,
                "rightWidth",
                SettingsDefaults.LegacyDesignWidth,
                SettingsDefaults.MinSideFraction,
                SettingsDefaults.MaxSideFraction,
                SettingsDefaults.DefaultRightFraction,
                warnings);
        if (leftFraction + rightFraction >= 1d)
        {
            warnings.Add("The left and right panel fractions leave no centre share; their defaults were used.");
            leftFraction = SettingsDefaults.DefaultLeftFraction;
            rightFraction = SettingsDefaults.DefaultRightFraction;
        }

        var consoleFraction = hasFractions
            ? ReadFraction(
                element,
                "consoleFraction",
                SettingsDefaults.DefaultConsoleFraction,
                SettingsDefaults.MinConsoleFraction,
                SettingsDefaults.MaxConsoleFraction,
                warnings)
            : ReadLegacyFraction(
                element,
                "consoleHeight",
                SettingsDefaults.LegacyDesignHeight,
                SettingsDefaults.MinConsoleFraction,
                SettingsDefaults.MaxConsoleFraction,
                SettingsDefaults.DefaultConsoleFraction,
                warnings);

        return new LayoutSettings
        {
            LeftFraction = leftFraction,
            RightFraction = rightFraction,
            ConsoleFraction = consoleFraction,
            LeftVisible = ReadBool(element, "leftVisible", true, warnings),
            RightVisible = ReadBool(element, "rightVisible", true, warnings),
            ConsoleVisible = ReadBool(element, "consoleVisible", true, warnings),
            ActiveConsoleChannel = ReadString(
                    element,
                    "activeConsoleChannel",
                    SettingsDefaults.DefaultConsoleChannel,
                    warnings,
                    allowNull: false)
                ?? SettingsDefaults.DefaultConsoleChannel,
        };
    }

    private static double ReadFraction(
        JsonElement parent,
        string name,
        double fallback,
        double minimum,
        double maximum,
        List<string> warnings)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var value)
            || !double.IsFinite(value)
            || value < minimum
            || value > maximum)
        {
            warnings.Add($"'{name}' is outside {minimum}–{maximum}; the default was used.");
            return fallback;
        }

        return value;
    }

    private static double ReadLegacyFraction(
        JsonElement parent,
        string name,
        double designExtent,
        double minimum,
        double maximum,
        double fallback,
        List<string> warnings)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var pixels)
            || !double.IsFinite(pixels))
        {
            warnings.Add($"'{name}' is not a finite number; the default was used.");
            return fallback;
        }

        return ShellLayoutFractions.FromLegacyPixels(pixels, designExtent, minimum, maximum, fallback);
    }

    private static AutosaveSettings ReadAutosave(JsonElement element, List<string> warnings) =>
        new()
        {
            Enabled = ReadBool(element, "enabled", true, warnings),
            IntervalMinutes = ReadRange(
                element,
                "intervalMinutes",
                SettingsDefaults.DefaultAutosaveIntervalMinutes,
                SettingsDefaults.MinAutosaveIntervalMinutes,
                SettingsDefaults.MaxAutosaveIntervalMinutes,
                warnings),
        };

    private static SessionSettings ReadSession(
        JsonElement element,
        List<string> warnings,
        bool ignoreCasePaths)
    {
        var comparer = ignoreCasePaths ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var projects = ImmutableArray.CreateBuilder<string>();
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("openProjects", out var projectsElement))
        {
            if (projectsElement.ValueKind != JsonValueKind.Array)
            {
                warnings.Add("session.openProjects was not an array; it was cleared.");
            }
            else
            {
                foreach (var item in projectsElement.EnumerateArray())
                {
                    if (projects.Count >= SettingsDefaults.MaxOpenProjects)
                    {
                        warnings.Add($"session.openProjects was capped at {SettingsDefaults.MaxOpenProjects} entries.");
                        break;
                    }

                    if (item.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        warnings.Add("A session.openProjects entry was ignored because it is empty or not a string.");
                        continue;
                    }

                    var path = item.GetString()!;
                    if (projects.Any(existing => comparer.Equals(existing, path)))
                    {
                        continue;
                    }

                    projects.Add(path);
                }
            }
        }

        var activeProject = ReadString(element, "activeProject", null, warnings, allowNull: true);
        if (activeProject is not null
            && !projects.Any(path => comparer.Equals(path, activeProject)))
        {
            warnings.Add("session.activeProject was not one of the open projects; it was cleared.");
            activeProject = null;
        }

        DateTimeOffset? savedUtc = null;
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("savedUtc", out var savedElement))
        {
            if (savedElement.ValueKind == JsonValueKind.Null)
            {
                savedUtc = null;
            }
            else if (savedElement.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(
                    savedElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                savedUtc = parsed.ToUniversalTime();
            }
            else
            {
                warnings.Add("session.savedUtc is not a valid ISO-8601 UTC time; it was cleared.");
            }
        }

        return new SessionSettings
        {
            OpenProjects = projects.ToImmutable(),
            ActiveProject = activeProject,
            CleanExit = ReadBool(element, "cleanExit", true, warnings),
            SavedUtc = savedUtc,
        };
    }

    private static ImmutableArray<RecentFile> ReadRecentFiles(
        JsonElement root,
        List<string> warnings,
        bool ignoreCasePaths)
    {
        if (!root.TryGetProperty("recentFiles", out var element))
        {
            return [];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            warnings.Add("recentFiles was not an array; the recent list was cleared.");
            return [];
        }

        var comparer = ignoreCasePaths ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var files = ImmutableArray.CreateBuilder<RecentFile>();
        foreach (var item in element.EnumerateArray())
        {
            if (files.Count >= SettingsDefaults.MaxRecentFiles)
            {
                break;
            }

            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("path", out var pathElement)
                || pathElement.ValueKind != JsonValueKind.String)
            {
                warnings.Add("A recentFiles entry was ignored because it has no path.");
                continue;
            }

            var path = pathElement.GetString();
            if (string.IsNullOrWhiteSpace(path))
            {
                warnings.Add("A recentFiles entry was ignored because its path is empty.");
                continue;
            }

            if (files.Any(file => comparer.Equals(file.Path, path)))
            {
                continue;
            }

            if (!item.TryGetProperty("lastOpenedUtc", out var timeElement)
                || timeElement.ValueKind != JsonValueKind.String
                || !DateTimeOffset.TryParse(
                    timeElement.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var openedAt))
            {
                warnings.Add("A recentFiles entry was ignored because its timestamp is invalid.");
                continue;
            }

            files.Add(new RecentFile(path, openedAt.ToUniversalTime()));
        }

        return files.ToImmutable();
    }

    private static JsonElement ReadObject(JsonElement root, string name, List<string> warnings)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return default;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"'{name}' was not an object; its defaults were used.");
            return default;
        }

        return element;
    }

    private static ThemeSelection ReadTheme(JsonElement root, List<string> warnings)
    {
        var value = ReadString(root, "theme", "system", warnings, allowNull: false);
        return value?.ToUpperInvariant() switch
        {
            "SYSTEM" => ThemeSelection.System,
            "LIGHT" => ThemeSelection.Light,
            "DARK" => ThemeSelection.Dark,
            _ => Unknown("theme", value, ThemeSelection.System, warnings),
        };
    }

    private static Density ReadDensity(JsonElement root, List<string> warnings)
    {
        var value = ReadString(root, "density", "comfortable", warnings, allowNull: false);
        return value?.ToUpperInvariant() switch
        {
            "COMFORTABLE" => Density.Comfortable,
            "COMPACT" => Density.Compact,
            _ => Unknown("density", value, Density.Comfortable, warnings),
        };
    }

    private static SettingsLogLevel ReadLogLevel(JsonElement parent, List<string> warnings)
    {
        var value = ReadString(parent, "level", "information", warnings, allowNull: false);
        return value?.ToUpperInvariant() switch
        {
            "TRACE" => SettingsLogLevel.Trace,
            "DEBUG" => SettingsLogLevel.Debug,
            "INFORMATION" => SettingsLogLevel.Information,
            "WARNING" => SettingsLogLevel.Warning,
            "ERROR" => SettingsLogLevel.Error,
            "FATAL" or "CRITICAL" => SettingsLogLevel.Fatal,
            _ => Unknown("logging.level", value, SettingsLogLevel.Information, warnings),
        };
    }

    private static T Unknown<T>(string field, string? value, T fallback, List<string> warnings)
        where T : struct, Enum
    {
        warnings.Add($"'{field}' has the unknown value '{value}'; the default was used.");
        return fallback;
    }

    private static string? ReadAccent(JsonElement root, List<string> warnings)
    {
        var value = ReadString(root, "accent", null, warnings, allowNull: true);
        if (value is null || AccentPalette.IsKnown(value))
        {
            return value;
        }

        warnings.Add($"'accent' has the unknown value '{value}'; the default accent was used.");
        return null;
    }

    private static double ReadFontScale(JsonElement root, List<string> warnings)
    {
        if (!root.TryGetProperty("fontScale", out var element))
        {
            return SettingsDefaults.DefaultFontScale;
        }

        if (element.ValueKind != JsonValueKind.Number
            || !element.TryGetDouble(out var value)
            || !double.IsFinite(value)
            || value < SettingsDefaults.MinFontScale
            || value > SettingsDefaults.MaxFontScale)
        {
            warnings.Add("'fontScale' is outside 0.85–1.5; the default was used.");
            return SettingsDefaults.DefaultFontScale;
        }

        return value;
    }

    private static int ReadRange(
        JsonElement parent,
        string name,
        int fallback,
        int minimum,
        int maximum,
        List<string> warnings)
    {
        var value = ReadInt(parent, name, fallback, warnings);
        if (value < minimum || value > maximum)
        {
            warnings.Add($"'{name}' is outside {minimum}–{maximum}; the default was used.");
            return fallback;
        }

        return value;
    }

    private static int ReadInt(JsonElement parent, string name, int fallback, List<string> warnings)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            warnings.Add($"'{name}' is not an integer; the default was used.");
            return fallback;
        }

        return value;
    }

    private static bool ReadBool(JsonElement parent, string name, bool fallback, List<string> warnings)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            warnings.Add($"'{name}' is not a boolean; the default was used.");
            return fallback;
        }

        return element.GetBoolean();
    }

    private static string? ReadString(
        JsonElement parent,
        string name,
        string? fallback,
        List<string> warnings,
        bool allowNull)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out var element))
        {
            return fallback;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            if (allowNull)
            {
                return null;
            }

            warnings.Add($"'{name}' is null; the default was used.");
            return fallback;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            warnings.Add($"'{name}' is not a string; the default was used.");
            return fallback;
        }

        var value = element.GetString();
        if (!allowNull && string.IsNullOrWhiteSpace(value))
        {
            warnings.Add($"'{name}' is empty; the default was used.");
            return fallback;
        }

        return value;
    }
}

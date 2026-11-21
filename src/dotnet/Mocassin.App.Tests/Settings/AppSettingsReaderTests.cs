// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Blocks;
using Mocassin.App.ViewModels.Settings.Json;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the tolerant settings reader, source-generated writer, and validator.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AppSettingsReaderTests
{
    [Fact]
    public void TheDefaultsLoadFromAFormatOnlyDocument()
    {
        var result = AppSettingsJsonReader.Read("""{"format":"mocassin.settings"}""");

        Assert.False(result.IsFatal);
        Assert.Empty(result.Warnings);
        Assert.Equal(SettingsDefaults.SchemaVersion, result.Settings.SchemaVersion);
        Assert.Equal(ThemeSelection.System, result.Settings.Theme);
        Assert.Equal(Density.Comfortable, result.Settings.Density);
        Assert.Equal(SettingsDefaults.DefaultFontScale, result.Settings.FontScale);
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction);
        Assert.Equal(SettingsDefaults.DefaultConsoleChannel, result.Settings.Layout.ActiveConsoleChannel);
        Assert.True(result.Settings.Autosave.Enabled);
        Assert.Equal(SettingsDefaults.DefaultAutosaveIntervalMinutes, result.Settings.Autosave.IntervalMinutes);
        Assert.Empty(result.Settings.Session.OpenProjects);
        Assert.Null(result.Settings.Session.ActiveProject);
        Assert.True(result.Settings.Session.CleanExit);
        Assert.Null(result.Settings.Session.SavedUtc);
    }

    [Fact]
    public void EveryFieldRoundTripsThroughTheSourceGeneratedContext()
    {
        var settings = new AppSettings
        {
            Theme = ThemeSelection.Dark,
            Accent = "teal",
            Density = Density.Compact,
            FontScale = 1.25,
            HighContrast = true,
            ReduceMotion = true,
            Culture = "de-DE",
            Logging = new LoggingSettings { Level = SettingsLogLevel.Warning },
            Ui = new UiSettings { ConfirmOnExternalChange = false, ShowWelcomeOnStart = false },
            Paths = new PathSettings
            {
                LastOpenDirectory = "/tmp/open",
                LastSaveDirectory = "/tmp/save",
                LastFolderDirectory = "/tmp/folder",
            },
            RecentFiles = [new RecentFile("/tmp/ceria.mocprj", new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero))],
            Layout = new LayoutSettings
            {
                LeftFraction = 0.25,
                RightFraction = 0.30,
                ConsoleFraction = 0.28,
                LeftVisible = false,
                RightVisible = false,
                ConsoleVisible = false,
                ActiveConsoleChannel = "problems",
            },
            Autosave = new AutosaveSettings { Enabled = false, IntervalMinutes = 7 },
            Session = new SessionSettings
            {
                OpenProjects = ["/tmp/a.mocprj", "/tmp/b.mocprj"],
                ActiveProject = "/tmp/b.mocprj",
                CleanExit = false,
                SavedUtc = new DateTimeOffset(2026, 11, 21, 10, 0, 0, TimeSpan.Zero),
            },
        };
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);

        var result = AppSettingsJsonReader.Read(json);

        Assert.False(result.IsFatal);
        Assert.Contains("\"format\": \"mocassin.settings\"", json, StringComparison.Ordinal);
        Assert.Contains("\"theme\": \"dark\"", json, StringComparison.Ordinal);
        Assert.Contains("\"density\": \"compact\"", json, StringComparison.Ordinal);
        Assert.Contains("\"level\": \"warning\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Format\"", json, StringComparison.Ordinal);
        Assert.Equal(settings.Theme, result.Settings.Theme);
        Assert.Equal(settings.Accent, result.Settings.Accent);
        Assert.Equal(settings.FontScale, result.Settings.FontScale);
        Assert.Equal(settings.Logging.Level, result.Settings.Logging.Level);
        Assert.Equal(settings.Ui.ConfirmOnExternalChange, result.Settings.Ui.ConfirmOnExternalChange);
        Assert.Equal(settings.Paths.LastOpenDirectory, result.Settings.Paths.LastOpenDirectory);
        Assert.Equal(settings.Layout.LeftFraction, result.Settings.Layout.LeftFraction);
        Assert.Equal(settings.Layout.RightFraction, result.Settings.Layout.RightFraction);
        Assert.Equal(settings.Layout.ConsoleFraction, result.Settings.Layout.ConsoleFraction);
        Assert.Equal(settings.Layout.ActiveConsoleChannel, result.Settings.Layout.ActiveConsoleChannel);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
        var recent = Assert.Single(result.Settings.RecentFiles);
        Assert.Equal(settings.RecentFiles[0].Path, recent.Path);
        Assert.Equal(settings.RecentFiles[0].LastOpenedUtc, recent.LastOpenedUtc);
        Assert.False(result.Settings.Autosave.Enabled);
        Assert.Equal(7, result.Settings.Autosave.IntervalMinutes);
        Assert.Equal(settings.Session.OpenProjects.ToArray(), result.Settings.Session.OpenProjects.ToArray());
        Assert.Equal("/tmp/b.mocprj", result.Settings.Session.ActiveProject);
        Assert.False(result.Settings.Session.CleanExit);
        Assert.Equal(settings.Session.SavedUtc, result.Settings.Session.SavedUtc);
    }

    [Fact]
    public void AutosaveAndSessionValuesNormalizeWithWarnings()
    {
        var projects = string.Join(
            ',',
            Enumerable.Range(0, 25).Select(index => $"\"/tmp/p{index % 21}.mocprj\""));
        var result = AppSettingsJsonReader.Read(
            $$"""
            {
              "format": "mocassin.settings",
              "autosave": { "enabled": "no", "intervalMinutes": 99 },
              "session": {
                "openProjects": [{{projects}}, "", "/tmp/outside.mocprj"],
                "activeProject": "/tmp/outside.mocprj",
                "cleanExit": "yes",
                "savedUtc": "not-a-time"
              }
            }
            """);

        Assert.False(result.IsFatal);
        Assert.True(result.Settings.Autosave.Enabled);
        Assert.Equal(SettingsDefaults.DefaultAutosaveIntervalMinutes, result.Settings.Autosave.IntervalMinutes);
        Assert.Equal(SettingsDefaults.MaxOpenProjects, result.Settings.Session.OpenProjects.Length);
        Assert.Null(result.Settings.Session.ActiveProject);
        Assert.True(result.Settings.Session.CleanExit);
        Assert.Null(result.Settings.Session.SavedUtc);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void UnknownMembersAreIgnored()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","unknown":true,"nested":{"value":1},"theme":"dark"}""");

        Assert.False(result.IsFatal);
        Assert.Equal(ThemeSelection.Dark, result.Settings.Theme);
    }

    [Fact]
    public void BadValuesFallBackPerFieldWithWarnings()
    {
        var result = AppSettingsJsonReader.Read(
            """
            {
              "format": "mocassin.settings",
              "theme": "purple",
              "density": "wide",
              "fontScale": 9,
              "accent": "orange",
              "logging": { "level": "verbose" },
              "layout": {
                "leftFraction": 0.9,
                "rightFraction": 0.5,
                "consoleFraction": "tall",
                "leftVisible": "yes"
              }
            }
            """);

        Assert.False(result.IsFatal);
        Assert.Equal(ThemeSelection.System, result.Settings.Theme);
        Assert.Equal(Density.Comfortable, result.Settings.Density);
        Assert.Equal(SettingsDefaults.DefaultFontScale, result.Settings.FontScale);
        Assert.Null(result.Settings.Accent);
        Assert.Equal(SettingsLogLevel.Information, result.Settings.Logging.Level);
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction);
        Assert.True(result.Settings.Layout.LeftVisible);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void AFutureMajorIsRejectedWithoutChangingTheDocument()
    {
        var result = AppSettingsJsonReader.Read("""{"format":"mocassin.settings","schemaVersion":2}""");

        Assert.True(result.IsFatal);
        Assert.Equal(SettingsDefaults.SchemaVersion, result.Settings.SchemaVersion);
        Assert.Contains("newer", result.FatalMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingOrWrongFormatIsRejected()
    {
        Assert.True(AppSettingsJsonReader.Read("""{"schemaVersion":1}""").IsFatal);
        Assert.True(AppSettingsJsonReader.Read("""{"format":"other","schemaVersion":1}""").IsFatal);
    }

    [Fact]
    public void RecentFilesAreCappedAndDeduplicated()
    {
        var entries = string.Join(
            ',',
            Enumerable.Range(0, 12).Select(index =>
                $$"""{"path":"/tmp/p{{index % 10}}.mocprj","lastOpenedUtc":"2026-11-20T12:00:00Z"}"""));
        var result = AppSettingsJsonReader.Read(
            $$"""{"format":"mocassin.settings","recentFiles":[{{entries}}]}""");

        Assert.Equal(SettingsDefaults.MaxRecentFiles, result.Settings.RecentFiles.Length);
        Assert.Equal(
            result.Settings.RecentFiles.Length,
            result.Settings.RecentFiles.Select(file => file.Path).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ARecentFileWithoutATimestampIsIgnored()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","recentFiles":[{"path":"/tmp/a.mocprj"},{"path":"/tmp/b.mocprj","lastOpenedUtc":"2026-11-20T12:00:00Z"}]}""");

        var file = Assert.Single(result.Settings.RecentFiles);
        Assert.Equal("/tmp/b.mocprj", file.Path);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void ALegacyPixelLayoutIsMigratedOnceAndRewrittenAsFractions()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftWidth":300,"rightWidth":400,"consoleHeight":240}}""");

        Assert.False(result.IsFatal);
        Assert.Equal(300d / 1200d, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(400d / 1200d, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(240d / 800d, result.Settings.Layout.ConsoleFraction, 10);
        var json = JsonSerializer.Serialize(result.Settings, AppSettingsJsonContext.Default.AppSettings);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"rightWidth\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"consoleHeight\"", json, StringComparison.Ordinal);

        var again = AppSettingsJsonReader.Read(json);
        Assert.Equal(result.Settings.Layout, again.Settings.Layout);
    }

    [Fact]
    public void LegacyPixelValuesAreClampedToThePersistedFractionRange()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftWidth":10,"rightWidth":9999,"consoleHeight":2000}}""");

        Assert.Equal(SettingsDefaults.MinSideFraction, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(SettingsDefaults.MaxSideFraction, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(SettingsDefaults.MaxConsoleFraction, result.Settings.Layout.ConsoleFraction, 10);
    }

    [Fact]
    public void AMissingLayoutUsesTheRefreshedDefaultsAndTheCurrentRevision()
    {
        var result = AppSettingsJsonReader.Read("""{"format":"mocassin.settings"}""");

        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);
    }

    [Fact]
    public void ANeverDraggedLayoutIsUpgradedOnceToTheRefreshedDefaults()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftFraction":0.21666666666666667,"rightFraction":0.26666666666666666,"consoleFraction":0.225}}""");

        Assert.False(result.IsFatal);
        Assert.Empty(result.Warnings);
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);

        var json = JsonSerializer.Serialize(result.Settings, AppSettingsJsonContext.Default.AppSettings);
        Assert.Contains("\"defaultsRevision\": 1", json, StringComparison.Ordinal);
        var again = AppSettingsJsonReader.Read(json);
        Assert.Equal(result.Settings.Layout, again.Settings.Layout);
    }

    [Fact]
    public void ACustomLayoutWithoutARevisionIsPreservedAndMarked()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftFraction":0.25,"rightFraction":0.30,"consoleFraction":0.28}}""");

        Assert.Equal(0.25, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(0.30, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(0.28, result.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);
    }

    [Fact]
    public void ARevisionOneLayoutIsNeverAdjustedAgain()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"defaultsRevision":1,"leftFraction":0.21666666666666667,"rightFraction":0.26666666666666666,"consoleFraction":0.225}}""");

        Assert.Equal(260d / 1200d, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(320d / 1200d, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(180d / 800d, result.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);

        var custom = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"defaultsRevision":1,"leftFraction":0.30,"rightFraction":0.10,"consoleFraction":0.40}}""");
        Assert.Equal(0.30, custom.Settings.Layout.LeftFraction, 10);
        Assert.Equal(0.10, custom.Settings.Layout.RightFraction, 10);
        Assert.Equal(0.40, custom.Settings.Layout.ConsoleFraction, 10);
    }

    [Fact]
    public void TheLegacyPixelDefaultsUpgradeToTheRefreshedDefaultsAfterThePixelMigration()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftWidth":260,"rightWidth":320,"consoleHeight":180}}""");

        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction, 10);
        Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);

        var json = JsonSerializer.Serialize(result.Settings, AppSettingsJsonContext.Default.AppSettings);
        Assert.DoesNotContain("\"leftWidth\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"rightWidth\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"consoleHeight\"", json, StringComparison.Ordinal);
        Assert.Equal(result.Settings.Layout, AppSettingsJsonReader.Read(json).Settings.Layout);
    }

    [Fact]
    public void AnInvalidRevisionWarnsAndRunsTheDefaultsMigration()
    {
        foreach (var value in new[] { "-2", "\"one\"", "1.5" })
        {
            var json = """{"format":"mocassin.settings","layout":{"defaultsRevision":__VALUE__,"leftFraction":0.21666666666666667,"rightFraction":0.26666666666666666,"consoleFraction":0.225}}"""
                .Replace("__VALUE__", value, StringComparison.Ordinal);
            var result = AppSettingsJsonReader.Read(json);

            Assert.False(result.IsFatal);
            Assert.Contains(result.Warnings, warning => warning.Contains("defaultsRevision", StringComparison.Ordinal));
            Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction, 10);
            Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction, 10);
            Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction, 10);
            Assert.Equal(SettingsDefaults.DefaultsRevision, result.Settings.Layout.DefaultsRevision);
        }
    }

    [Fact]
    public void InvalidFractionsFallBackWithWarnings()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftFraction":0,"rightFraction":2,"consoleFraction":"tall"}}""");

        Assert.Equal(SettingsDefaults.DefaultLeftFraction, result.Settings.Layout.LeftFraction);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, result.Settings.Layout.RightFraction);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, result.Settings.Layout.ConsoleFraction);
        Assert.NotEmpty(result.Warnings);

        var noCentre = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftFraction":0.7,"rightFraction":0.5}}""");

        Assert.Equal(SettingsDefaults.DefaultLeftFraction, noCentre.Settings.Layout.LeftFraction);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, noCentre.Settings.Layout.RightFraction);
        Assert.NotEmpty(noCentre.Warnings);
    }

    [Fact]
    public void MigrationIsIdempotent()
    {
        var migrated = SettingsMigration.Migrate(new AppSettings { SchemaVersion = 0 });
        var again = SettingsMigration.Migrate(migrated);

        Assert.Equal(SettingsDefaults.SchemaVersion, migrated.SchemaVersion);
        Assert.Equal(migrated, again);
    }

    [Fact]
    public void TheValidatorClampsOutOfRangeValues()
    {
        var settings = new AppSettings
        {
            FontScale = 4,
            Accent = "orange",
            Culture = " ",
            Layout = new LayoutSettings
            {
                LeftFraction = 0.01,
                RightFraction = 0.99,
                ConsoleFraction = 0.01,
                ActiveConsoleChannel = " ",
            },
            RecentFiles =
            [
                .. Enumerable.Range(0, 12).Select(index => new RecentFile($"/tmp/{index}.mocprj", DateTimeOffset.UnixEpoch)),
            ],
            Autosave = new AutosaveSettings { Enabled = false, IntervalMinutes = 0 },
            Session = new SessionSettings
            {
                OpenProjects = ["/tmp/a.mocprj", "/tmp/a.mocprj", "", .. Enumerable.Range(0, 25).Select(index => $"/tmp/{index}.mocprj")],
                ActiveProject = "/tmp/missing.mocprj",
            },
        };

        var (normalized, warnings) = AppSettingsValidator.Normalize(settings);

        Assert.Equal(SettingsDefaults.DefaultFontScale, normalized.FontScale);
        Assert.Null(normalized.Accent);
        Assert.Equal("en", normalized.Culture);
        Assert.Equal(SettingsDefaults.DefaultLeftFraction, normalized.Layout.LeftFraction);
        Assert.Equal(SettingsDefaults.DefaultRightFraction, normalized.Layout.RightFraction);
        Assert.Equal(SettingsDefaults.DefaultConsoleFraction, normalized.Layout.ConsoleFraction);
        Assert.Equal(SettingsDefaults.DefaultConsoleChannel, normalized.Layout.ActiveConsoleChannel);
        Assert.Equal(SettingsDefaults.MaxRecentFiles, normalized.RecentFiles.Length);
        Assert.False(normalized.Autosave.Enabled);
        Assert.Equal(SettingsDefaults.DefaultAutosaveIntervalMinutes, normalized.Autosave.IntervalMinutes);
        Assert.Equal(SettingsDefaults.MaxOpenProjects, normalized.Session.OpenProjects.Length);
        Assert.Null(normalized.Session.ActiveProject);
        Assert.NotEmpty(warnings);
    }
}

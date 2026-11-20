// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Json;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the tolerant settings reader, source-generated writer, and validator.
/// </summary>
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
        Assert.Equal(SettingsDefaults.DefaultLeftWidth, result.Settings.Layout.LeftWidth);
        Assert.Equal(SettingsDefaults.DefaultConsoleChannel, result.Settings.Layout.ActiveConsoleChannel);
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
                LeftWidth = 300,
                RightWidth = 400,
                ConsoleHeight = 240,
                LeftVisible = false,
                RightVisible = false,
                ConsoleVisible = false,
                ActiveConsoleChannel = "problems",
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
        Assert.Equal(settings.Layout.ActiveConsoleChannel, result.Settings.Layout.ActiveConsoleChannel);
        var recent = Assert.Single(result.Settings.RecentFiles);
        Assert.Equal(settings.RecentFiles[0].Path, recent.Path);
        Assert.Equal(settings.RecentFiles[0].LastOpenedUtc, recent.LastOpenedUtc);
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
              "layout": { "leftWidth": 9999, "consoleHeight": "tall", "leftVisible": "yes" }
            }
            """);

        Assert.False(result.IsFatal);
        Assert.Equal(ThemeSelection.System, result.Settings.Theme);
        Assert.Equal(Density.Comfortable, result.Settings.Density);
        Assert.Equal(SettingsDefaults.DefaultFontScale, result.Settings.FontScale);
        Assert.Null(result.Settings.Accent);
        Assert.Equal(SettingsLogLevel.Information, result.Settings.Logging.Level);
        Assert.Equal(SettingsDefaults.DefaultLeftWidth, result.Settings.Layout.LeftWidth);
        Assert.Equal(SettingsDefaults.DefaultConsoleHeight, result.Settings.Layout.ConsoleHeight);
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
                LeftWidth = 10,
                RightWidth = 9999,
                ConsoleHeight = 1,
                ActiveConsoleChannel = " ",
            },
            RecentFiles =
            [
                .. Enumerable.Range(0, 12).Select(index => new RecentFile($"/tmp/{index}.mocprj", DateTimeOffset.UnixEpoch)),
            ],
        };

        var (normalized, warnings) = AppSettingsValidator.Normalize(settings);

        Assert.Equal(SettingsDefaults.DefaultFontScale, normalized.FontScale);
        Assert.Null(normalized.Accent);
        Assert.Equal("en", normalized.Culture);
        Assert.Equal(SettingsDefaults.DefaultLeftWidth, normalized.Layout.LeftWidth);
        Assert.Equal(SettingsDefaults.DefaultRightWidth, normalized.Layout.RightWidth);
        Assert.Equal(SettingsDefaults.DefaultConsoleHeight, normalized.Layout.ConsoleHeight);
        Assert.Equal(SettingsDefaults.DefaultConsoleChannel, normalized.Layout.ActiveConsoleChannel);
        Assert.Equal(SettingsDefaults.MaxRecentFiles, normalized.RecentFiles.Length);
        Assert.NotEmpty(warnings);
    }
}

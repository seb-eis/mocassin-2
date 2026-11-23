// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Json;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Pins the additive <c>layout.editorFraction</c> setting: absent reads the default, an out-of-range value falls
/// back, the validator clamps, and the source-generated writer emits the field.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorFractionSettingsTests
{
    [Fact]
    public void AnAbsentEditorFractionReadsTheDefault()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"leftFraction":0.2,"rightFraction":0.2,"consoleFraction":0.3}}""");

        Assert.False(result.IsFatal);
        Assert.Equal(SettingsDefaults.DefaultEditorFraction, result.Settings.Layout.EditorFraction);
    }

    [Fact]
    public void AValidEditorFractionIsPreserved()
    {
        var result = AppSettingsJsonReader.Read(
            """{"format":"mocassin.settings","layout":{"editorFraction":0.5}}""");

        Assert.False(result.IsFatal);
        Assert.Equal(0.5d, result.Settings.Layout.EditorFraction);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.9)]
    [InlineData(-1)]
    public void AnOutOfRangeEditorFractionFallsBackWithAWarning(double value)
    {
        var json = "{\"format\":\"mocassin.settings\",\"layout\":{\"editorFraction\":"
            + value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "}}";

        var result = AppSettingsJsonReader.Read(json);

        Assert.False(result.IsFatal);
        Assert.Equal(SettingsDefaults.DefaultEditorFraction, result.Settings.Layout.EditorFraction);
        Assert.Contains(result.Warnings, warning => warning.Contains("editorFraction", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0.1, SettingsDefaults.MinEditorFraction)]
    [InlineData(0.9, SettingsDefaults.MaxEditorFraction)]
    [InlineData(double.NaN, SettingsDefaults.DefaultEditorFraction)]
    public void TheValidatorClampsTheEditorFraction(double value, double expected)
    {
        var settings = new AppSettings
        {
            Layout = new LayoutSettings { EditorFraction = value },
        };

        var (normalized, _) = AppSettingsValidator.Normalize(settings);

        Assert.Equal(expected, normalized.Layout.EditorFraction);
    }

    [Fact]
    public void TheSourceGeneratedWriterEmitsTheEditorFraction()
    {
        var settings = new AppSettings
        {
            Layout = new LayoutSettings { EditorFraction = 0.55d },
        };

        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);

        Assert.Contains("\"editorFraction\"", json, StringComparison.Ordinal);
        Assert.Contains("0.55", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TheShellLayoutStateClampsRuntimeDrags()
    {
        var regions = new ShellRegionsViewModel();

        regions.EditorFraction = 0.1d;
        Assert.Equal(SettingsDefaults.MinEditorFraction, regions.EditorFraction);

        regions.EditorFraction = 0.99d;
        Assert.Equal(SettingsDefaults.MaxEditorFraction, regions.EditorFraction);

        regions.EditorFraction = double.NaN;
        Assert.Equal(SettingsDefaults.DefaultEditorFraction, regions.EditorFraction);

        regions.EditorFraction = 0.5d;
        Assert.Equal(0.5d, regions.EditorFraction);
        Assert.IsAssignableFrom<IEditorPaneLayout>(regions);
    }
}

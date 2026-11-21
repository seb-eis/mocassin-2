// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Dialog;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Settings;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Verifies the view-locator convention and the settings dialog combo boxes it feeds.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ViewLocatorTests
{
    [Fact]
    public void SettingsEnumsAreNotClaimed()
    {
        var locator = new ViewLocator();

        Assert.False(locator.Match(ThemeSelection.Dark));
        Assert.False(locator.Match(Density.Compact));
    }

    [Fact]
    public void SettingsModelsAndStringsAreNotClaimed()
    {
        var locator = new ViewLocator();

        Assert.False(locator.Match(AppSettings.Default));
        Assert.False(locator.Match("text"));
        Assert.False(locator.Match(null));
    }

    [Fact]
    public Task ARealViewModelMatchesAndBuildsItsView() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var locator = new ViewLocator();
                var welcome = new WelcomeDocumentViewModel(new RelayCommand(() => { }));
                var dialog = new ConfirmDialogViewModel(new ConfirmDialogRequest("Delete", "Delete it?"));

                Assert.True(locator.Match(welcome));
                Assert.True(locator.Match(dialog));
                Assert.IsType<WelcomeDocumentView>(locator.Build(welcome));
                Assert.IsType<ConfirmDialogView>(locator.Build(dialog));
            });

    [Fact]
    public Task TheSettingsComboBoxesRenderTheirItemTextWithoutAMissingViewPanel() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var settings = new SettingsViewModel(
                    new SettingsService(new InMemorySettingsStore(), AppSettingsLoadResult.Missing()),
                    new RecordingSettingsEnvironment())
                {
                    Theme = ThemeSelection.Dark,
                    Density = Density.Compact,
                    LogLevel = SettingsLogLevel.Warning,
                };

                var window = new Window
                {
                    Width = 760,
                    Height = 900,
                    Content = new SettingsDialogView { DataContext = settings },
                };
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var comboBoxes = window.GetVisualDescendants().OfType<ComboBox>().ToList();
                    AssertComboBoxShowsText(comboBoxes, "Theme", "Dark");
                    AssertComboBoxShowsText(comboBoxes, "Density", "Compact");
                    AssertComboBoxShowsText(comboBoxes, "Log level", "Warning");

                    var texts = window.GetVisualDescendants()
                        .OfType<TextBlock>()
                        .Select(block => block.Text ?? string.Empty)
                        .ToList();
                    Assert.DoesNotContain(
                        texts,
                        text => text.Contains("View not found", StringComparison.Ordinal));
                }
                finally
                {
                    window.Close();
                }
            });

    private static void AssertComboBoxShowsText(
        IReadOnlyList<ComboBox> comboBoxes,
        string automationName,
        string expected)
    {
        var comboBox = comboBoxes.Single(
            control => AutomationProperties.GetName(control) == automationName);
        Assert.Contains(
            comboBox.GetVisualDescendants().OfType<TextBlock>(),
            block => block.Text == expected);
    }
}

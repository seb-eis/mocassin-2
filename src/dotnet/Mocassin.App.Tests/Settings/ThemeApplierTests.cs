// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies theme, density, scale, contrast, and motion propagation to Avalonia resources.
/// </summary>
public sealed class ThemeApplierTests
{
    [Fact]
    public Task TheDarkThemeAndFontScaleAreApplied() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                applier.Apply(
                    AppSettings.Default with
                    {
                        Theme = ThemeSelection.Dark,
                        Density = Density.Compact,
                        FontScale = 1.25,
                        HighContrast = true,
                        ReduceMotion = true,
                        Accent = "teal",
                    });

                Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
                Assert.Equal(1.25, Assert.IsType<double>(Application.Current.Resources["Mocassin.FontScale"]));
                Assert.Equal(17.5d, Assert.IsType<double>(Application.Current.Resources["Mocassin.BodyFontSize"]));
                Assert.Equal("compact", Assert.IsType<string>(Application.Current.Resources["Mocassin.Density"]));
                Assert.True(Assert.IsType<bool>(Application.Current.Resources["Mocassin.HighContrast"]));
                Assert.True(Assert.IsType<bool>(Application.Current.Resources["Mocassin.ReduceMotion"]));
                Assert.Equal("teal", Assert.IsType<string>(Application.Current.Resources["Mocassin.Accent"]));
                Assert.IsType<Color>(Application.Current.Resources["SystemAccentColor"]);
            });

    [Fact]
    public Task TheLightThemeAndDefaultAccentAreApplied() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                applier.Apply(AppSettings.Default with { Theme = ThemeSelection.Light, Accent = null });

                Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
                Assert.Equal("comfortable", Assert.IsType<string>(Application.Current.Resources["Mocassin.Density"]));
                Assert.False(Assert.IsType<bool>(Application.Current.Resources["Mocassin.HighContrast"]));
                Assert.False(Application.Current.Resources.ContainsKey("SystemAccentColor"));
            });
}

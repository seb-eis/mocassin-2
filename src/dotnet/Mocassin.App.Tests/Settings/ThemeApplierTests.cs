// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies theme, density, scale, contrast, and motion propagation to Avalonia resources.
/// </summary>
[Trait("Tier", "PR-fast")]
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
                Assert.Equal(new Thickness(4), Application.Current.Resources["Mocassin.ControlPadding"]);
                Assert.IsType<Color>(Application.Current.Resources["SystemAccentColor"]);
                Assert.Empty(Assert.IsType<Transitions>(Application.Current.Resources["Mocassin.Transitions"]));
                Assert.True(
                    Application.Current.Resources.TryGetResource(
                        "SystemControlBackgroundAltHighBrush",
                        null,
                        out var highContrastBackground));
                Assert.NotNull(highContrastBackground);
            });

    [Fact]
    public Task TheLightThemeAndDefaultAccentAreApplied() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                applier.Apply(AppSettings.Default with { Theme = ThemeSelection.Light, Accent = null });

                Assert.Equal(ThemeVariant.Light, Application.Current!.RequestedThemeVariant);
                Assert.Equal(Color.Parse("#007ACC"), Assert.IsType<Color>(Application.Current.Resources["SystemAccentColor"]));
                Assert.False(Application.Current.Resources.ContainsKey("Mocassin.AccentBrush"));
                Assert.Equal(new Thickness(8), Application.Current.Resources["Mocassin.ControlPadding"]);
                Assert.NotEmpty(Assert.IsType<Transitions>(Application.Current.Resources["Mocassin.Transitions"]));
            });
}

// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Resolves the visual-contract palette in both themes and verifies its WCAG contrast floor.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ThemeContrastTests
{
    private static readonly IReadOnlyDictionary<string, (string Dark, string Light)> roles =
        new Dictionary<string, (string Dark, string Light)>(StringComparer.Ordinal)
        {
            ["Mocassin.ChromeBackground"] = ("#1E1E1E", "#F3F3F3"),
            ["Mocassin.WorkspaceBackground"] = ("#252526", "#FFFFFF"),
            ["Mocassin.CardBackground"] = ("#2D2D30", "#F3F3F3"),
            ["Mocassin.BorderBrush"] = ("#3C3C3C", "#E0E0E0"),
            ["Mocassin.InputBackground"] = ("#3C3C3C", "#FFFFFF"),
            ["Mocassin.InputBorderBrush"] = ("#3C3C3C", "#CECECE"),
            ["Mocassin.MenuBackground"] = ("#1E1E1E", "#F3F3F3"),
            ["Mocassin.TabStripBackground"] = ("#1E1E1E", "#F3F3F3"),
            ["Mocassin.TabActiveBackground"] = ("#252526", "#FFFFFF"),
            ["Mocassin.TabInactiveBackground"] = ("#1E1E1E", "#F3F3F3"),
            ["Mocassin.OverlayScrim"] = ("#66000000", "#66000000"),
            ["Mocassin.ForegroundBrush"] = ("#D4D4D4", "#333333"),
            ["Mocassin.ForegroundStrongBrush"] = ("#FFFFFF", "#1F1F1F"),
            ["Mocassin.ForegroundMutedBrush"] = ("#9D9D9D", "#6F6F6F"),
            ["Mocassin.DisabledForegroundBrush"] = ("#6B6B6B", "#9A9A9A"),
            ["Mocassin.DisabledBackgroundBrush"] = ("#2A2A2A", "#EDEDED"),
            ["Mocassin.AccentBrush"] = ("#007ACC", "#007ACC"),
            ["Mocassin.AccentTextBrush"] = ("#4DA6FF", "#005A9E"),
            ["Mocassin.OnAccentBrush"] = ("#FFFFFF", "#FFFFFF"),
            ["Mocassin.TabActiveForeground"] = ("#FFFFFF", "#1F1F1F"),
            ["Mocassin.TabInactiveForeground"] = ("#9D9D9D", "#6F6F6F"),
            ["Mocassin.HoverBackground"] = ("#37373D", "#E8E8E8"),
            ["Mocassin.PressedBackground"] = ("#2A2D2E", "#DCDCDC"),
            ["Mocassin.SelectedBackground"] = ("#094771", "#CCE5FF"),
            ["Mocassin.SelectedForeground"] = ("#FFFFFF", "#1F1F1F"),
            ["Mocassin.ErrorForeground"] = ("#F48771", "#A1260D"),
            ["Mocassin.WarningForeground"] = ("#CCA700", "#B26A00"),
            ["Mocassin.SuccessForeground"] = ("#89D185", "#1B5E20"),
            ["Mocassin.InfoForeground"] = ("#75BEFF", "#0B4F8A"),
            ["Mocassin.ErrorBackground"] = ("#3A1D1D", "#FDECEA"),
            ["Mocassin.WarningBackground"] = ("#3A2F12", "#FFF4CE"),
            ["Mocassin.SuccessBackground"] = ("#1E3A24", "#E8F5E9"),
            ["Mocassin.ErrorBorderBrush"] = ("#F48771", "#C62828"),
            ["Mocassin.WarningBorderBrush"] = ("#CCA700", "#B26A00"),
            ["Mocassin.SuccessBorderBrush"] = ("#89D185", "#2E7D32"),
            ["Mocassin.StatusBarBackground"] = ("#007ACC", "#007ACC"),
            ["Mocassin.StatusBarForeground"] = ("#FFFFFF", "#FFFFFF"),
        };

    private static readonly string[] optionalAccents = ["blue", "teal", "green", "amber", "violet", "rose"];
    private static readonly ThemeSelection[] bothThemes = [ThemeSelection.Light, ThemeSelection.Dark];
    private static readonly string[] darkSurfaces = ["#1E1E1E", "#252526", "#2D2D30"];
    private static readonly string[] lightSurfaces = ["#F3F3F3", "#FFFFFF", "#F3F3F3"];
    private static readonly string[] focusSurfaces = ["#1E1E1E", "#252526", "#2D2D30", "#FFFFFF", "#F3F3F3"];

    private static readonly string[] fluentKeys =
    [
        "SystemControlForegroundBaseHighBrush",
        "SystemControlForegroundBaseMediumBrush",
        "SystemControlForegroundBaseMediumLowBrush",
        "SystemControlForegroundBaseLowBrush",
        "SystemControlForegroundChromeWhiteBrush",
        "SystemControlBackgroundAltHighBrush",
        "SystemControlBackgroundChromeMediumLowBrush",
        "SystemControlTransientBackgroundBrush",
        "SystemControlTransientBorderBrush",
        "SystemControlDisabledBaseMediumLowBrush",
        "SystemControlDisabledBaseLowBrush",
        "SystemControlDisabledChromeDisabledLowBrush",
        "SystemControlHighlightAccentBrush",
        "SystemControlHighlightAltBaseHighBrush",
        "SystemControlHighlightListLowBrush",
        "SystemControlHighlightListMediumBrush",
        "SystemControlHighlightListAccentLowBrush",
        "SystemControlHighlightListAccentMediumBrush",
        "SystemControlHighlightListAccentHighBrush",
        "ButtonBackground",
        "ButtonBackgroundPointerOver",
        "ButtonBackgroundPressed",
        "ButtonBackgroundDisabled",
        "ButtonForeground",
        "ButtonForegroundPointerOver",
        "ButtonForegroundPressed",
        "ButtonForegroundDisabled",
        "ButtonBorderBrush",
        "ButtonBorderBrushPointerOver",
        "ButtonBorderBrushPressed",
        "ButtonBorderBrushDisabled",
        "ButtonBorderThemeThickness",
        "AccentButtonBackground",
        "AccentButtonBackgroundPointerOver",
        "AccentButtonBackgroundPressed",
        "AccentButtonBackgroundDisabled",
        "AccentButtonForeground",
        "AccentButtonForegroundPointerOver",
        "AccentButtonForegroundPressed",
        "AccentButtonForegroundDisabled",
        "AccentButtonBorderBrush",
        "AccentButtonBorderBrushPointerOver",
        "AccentButtonBorderBrushPressed",
        "AccentButtonBorderBrushDisabled",
        "TextControlForeground",
        "TextControlForegroundPointerOver",
        "TextControlForegroundFocused",
        "TextControlForegroundDisabled",
        "TextControlBackground",
        "TextControlBackgroundPointerOver",
        "TextControlBackgroundFocused",
        "TextControlBackgroundDisabled",
        "TextControlBorderBrush",
        "TextControlBorderBrushPointerOver",
        "TextControlBorderBrushFocused",
        "TextControlBorderBrushDisabled",
        "TextControlPlaceholderForeground",
        "TextControlPlaceholderForegroundPointerOver",
        "TextControlPlaceholderForegroundFocused",
        "TextControlPlaceholderForegroundDisabled",
        "TextControlSelectionHighlightColor",
        "ComboBoxBackground",
        "ComboBoxBackgroundPointerOver",
        "ComboBoxBackgroundPressed",
        "ComboBoxBackgroundDisabled",
        "ComboBoxForeground",
        "ComboBoxForegroundFocused",
        "ComboBoxForegroundDisabled",
        "ComboBoxBorderBrush",
        "ComboBoxBorderBrushPointerOver",
        "ComboBoxBorderBrushPressed",
        "ComboBoxBorderBrushDisabled",
        "ComboBoxDropDownBackground",
        "ComboBoxDropDownBorderBrush",
        "ComboBoxDropDownGlyphForeground",
        "ComboBoxDropDownGlyphForegroundFocused",
        "ComboBoxDropDownGlyphForegroundFocusedPressed",
        "ComboBoxDropDownGlyphForegroundDisabled",
        "ComboBoxItemBackground",
        "ComboBoxItemBackgroundPointerOver",
        "ComboBoxItemBackgroundPressed",
        "ComboBoxItemBackgroundDisabled",
        "ComboBoxItemBackgroundSelected",
        "ComboBoxItemBackgroundSelectedPointerOver",
        "ComboBoxItemBackgroundSelectedPressed",
        "ComboBoxItemBackgroundSelectedDisabled",
        "ComboBoxItemForeground",
        "ComboBoxItemForegroundPointerOver",
        "ComboBoxItemForegroundPressed",
        "ComboBoxItemForegroundDisabled",
        "ComboBoxItemForegroundSelected",
        "ComboBoxItemForegroundSelectedPointerOver",
        "ComboBoxItemForegroundSelectedPressed",
        "ComboBoxItemForegroundSelectedDisabled",
        "TabItemHeaderBackgroundUnselected",
        "TabItemHeaderBackgroundUnselectedPointerOver",
        "TabItemHeaderBackgroundUnselectedPressed",
        "TabItemHeaderBackgroundSelected",
        "TabItemHeaderBackgroundSelectedPointerOver",
        "TabItemHeaderBackgroundSelectedPressed",
        "TabItemHeaderBackgroundDisabled",
        "TabItemHeaderForegroundUnselected",
        "TabItemHeaderForegroundUnselectedPointerOver",
        "TabItemHeaderForegroundUnselectedPressed",
        "TabItemHeaderForegroundSelected",
        "TabItemHeaderForegroundSelectedPointerOver",
        "TabItemHeaderForegroundSelectedPressed",
        "TabItemHeaderForegroundDisabled",
        "TabItemHeaderSelectedPipeFill",
        "TabItemMinHeight",
        "TabItemHeaderMargin",
        "MenuFlyoutPresenterBackground",
        "MenuFlyoutPresenterBorderBrush",
        "MenuFlyoutItemBackgroundPointerOver",
        "MenuFlyoutItemBackgroundPressed",
        "MenuFlyoutItemForeground",
        "MenuFlyoutItemForegroundPointerOver",
        "MenuFlyoutItemForegroundPressed",
        "MenuFlyoutItemForegroundDisabled",
        "MenuFlyoutItemKeyboardAcceleratorTextForeground",
        "MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver",
        "MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed",
        "MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled",
    ];

    [Fact]
    public Task EveryRoleResolvesToTheContractValueInbothThemesAndTheDefaultFallback() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                foreach (var (selection, variant, index) in new[]
                {
                    (ThemeSelection.Light, ThemeVariant.Light, 1),
                    (ThemeSelection.Dark, ThemeVariant.Dark, 0),
                    (ThemeSelection.System, ThemeVariant.Default, 1),
                })
                {
                    applier.Apply(AppSettings.Default with { Theme = selection });
                    foreach (var (key, values) in roles)
                    {
                        var actual = ResolveColor(key, variant);
                        var expected = index == 0 ? values.Dark : values.Light;
                        Assert.True(
                            Color.Parse(expected) == actual,
                            $"{key} under {selection}: expected {expected}, got #{actual.A:X2}{actual.R:X2}{actual.G:X2}{actual.B:X2}");
                    }
                }
            });

    [Fact]
    public Task EveryFluentKeyResolvesInbothThemesAndTheDefaultFallback() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                foreach (var (selection, variant) in new[]
                {
                    (ThemeSelection.Light, ThemeVariant.Light),
                    (ThemeSelection.Dark, ThemeVariant.Dark),
                    (ThemeSelection.System, ThemeVariant.Default),
                })
                {
                    applier.Apply(AppSettings.Default with { Theme = selection });
                    Assert.Equal(variant, Application.Current!.RequestedThemeVariant);
                    foreach (var key in fluentKeys)
                    {
                        Assert.True(
                            Application.Current!.Resources.TryGetResource(key, variant, out var value),
                            $"{key} did not resolve under {selection}.");
                        Assert.NotNull(value);
                    }
                }
            });

    [Fact]
    public Task HighContrastOverridesEveryRoleAndFluentKeyAndKeepsTwoPixelBorders() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                new AvaloniaThemeApplier().Apply(
                    AppSettings.Default with
                    {
                        Theme = ThemeSelection.Dark,
                        HighContrast = true,
                        Density = Density.Compact,
                        Accent = "teal",
                    });

                Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
                Assert.Equal("#000000", Hex(ResolveColor("Mocassin.ChromeBackground", ThemeVariant.Dark)));
                Assert.Equal("#FFD700", Hex(ResolveColor("Mocassin.BorderBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFFFFF", Hex(ResolveColor("Mocassin.ForegroundBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("Mocassin.OnAccentBrush", ThemeVariant.Dark)));
                Assert.Equal(new Thickness(2), Application.Current.Resources["Mocassin.BorderThickness"]);
                Assert.Equal(new Thickness(4), Application.Current.Resources["Mocassin.ControlPadding"]);
                Assert.Equal(26d, Application.Current.Resources["Mocassin.ControlMinHeight"]);
                foreach (var key in fluentKeys)
                {
                    Assert.True(
                        Application.Current.Resources.TryGetResource(key, ThemeVariant.Dark, out var value),
                        $"{key} did not resolve in high contrast.");
                    Assert.NotNull(value);
                }
            });

    [Fact]
    public Task ApplierDirectResourcesDoNotMaskAPaletteRole() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                new AvaloniaThemeApplier().Apply(AppSettings.Default with { Theme = ThemeSelection.Light });
                Assert.False(Application.Current!.Resources.ContainsKey("Mocassin.ChromeBackground"));
                Assert.False(Application.Current.Resources.ContainsKey("Mocassin.AccentBrush"));
                Assert.False(Application.Current.Resources.ContainsKey("Mocassin.OnAccentBrush"));
            });

    [Fact]
    public Task EveryOptionalAccentPublishesAContrastSafeOnAccentBrush() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                foreach (var accent in optionalAccents)
                {
                    foreach (var theme in bothThemes)
                    {
                        applier.Apply(AppSettings.Default with { Theme = theme, Accent = accent });
                        var accentColor = ResolveColor("Mocassin.AccentBrush", ThemeVariant.Default);
                        var onAccent = ResolveColor("Mocassin.OnAccentBrush", ThemeVariant.Default);
                        Assert.True(
                            Contrast(onAccent, accentColor) >= 4.5,
                            $"{accent}/{theme} on-accent contrast was {Contrast(onAccent, accentColor):F2}.");
                    }
                }

                applier.Apply(AppSettings.Default with { Accent = "blue" });
                Assert.Equal("#007ACC", Hex(ResolveColor("Mocassin.AccentBrush", ThemeVariant.Default)));
            });

    [Fact]
    public void TextPairsMeetTheContractFloor()
    {
        for (var i = 0; i < darkSurfaces.Length; i++)
        {
            AssertAtLeast("#D4D4D4", darkSurfaces[i], 4.5);
            AssertAtLeast("#333333", lightSurfaces[i], 4.5);
            AssertAtLeast("#9D9D9D", darkSurfaces[i], 4.5);
            AssertAtLeast("#6F6F6F", lightSurfaces[i], 4.5);
        }

        AssertAtLeast("#FFFFFF", "#2D2D30", 4.5);
        AssertAtLeast("#1F1F1F", "#F3F3F3", 4.5);
        AssertAtLeast("#4DA6FF", "#2D2D30", 4.5);
        AssertAtLeast("#005A9E", "#F3F3F3", 4.5);
        AssertAtLeast("#FFFFFF", "#007ACC", 4.5);
        AssertAtLeast("#FFFFFF", "#094771", 4.5);
        AssertAtLeast("#1F1F1F", "#CCE5FF", 4.5);
        AssertAtLeast("#F48771", "#2D2D30", 4.5);
        AssertAtLeast("#A1260D", "#F3F3F3", 4.5);
        AssertAtLeast("#89D185", "#2D2D30", 4.5);
        AssertAtLeast("#1B5E20", "#F3F3F3", 4.5);
        AssertAtLeast("#75BEFF", "#2D2D30", 4.5);
        AssertAtLeast("#0B4F8A", "#F3F3F3", 4.5);

        // The approved light WarningForeground (#B26A00) is a glyph and border role: it is 3.82:1 on the light
        // card and 3.85:1 on the warning fill, above the 3:1 non-text floor. Warning banner body text uses the
        // strong foreground, which meets the normal-text floor. This is recorded in the maintenance evidence.
        AssertAtLeast("#CCA700", "#2D2D30", 4.5);
        AssertAtLeast("#B26A00", "#F3F3F3", 3.0);
        AssertAtLeast("#FFFFFF", "#000000", 21.0);
        AssertAtLeast("#000000", "#FFD700", 4.5);
    }

    [Fact]
    public void FocusRingMeetsTheNonTextFloorOnEverySurface()
    {
        foreach (var surface in focusSurfaces)
        {
            AssertAtLeast("#007ACC", surface, 3.0);
        }
    }

    private static Color ResolveColor(string key, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.Resources.TryGetResource(key, variant, out var value),
            $"{key} did not resolve under {variant}.");
        return Assert.IsAssignableFrom<ISolidColorBrush>(value).Color;
    }

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static void AssertAtLeast(string foreground, string background, double minimum) =>
        Assert.True(
            Contrast(Color.Parse(foreground), Color.Parse(background)) >= minimum,
            $"{foreground} on {background} was {Contrast(Color.Parse(foreground), Color.Parse(background)):F2}, expected {minimum}.");

    private static double Contrast(Color first, Color second)
    {
        var light = Math.Max(Luminance(first), Luminance(second));
        var dark = Math.Min(Luminance(first), Luminance(second));
        return (light + 0.05) / (dark + 0.05);
    }

    private static double Luminance(Color color) =>
        (0.2126 * Linear(color.R)) + (0.7152 * Linear(color.G)) + (0.0722 * Linear(color.B));

    private static double Linear(byte channel)
    {
        var value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}

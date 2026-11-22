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
            ["Mocassin.FocusBrush"] = ("#248DD3", "#007ACC"),
            ["Mocassin.TabActiveForeground"] = ("#FFFFFF", "#1F1F1F"),
            ["Mocassin.TabInactiveForeground"] = ("#9D9D9D", "#6F6F6F"),
            ["Mocassin.HoverBackground"] = ("#37373D", "#E8E8E8"),
            ["Mocassin.PressedBackground"] = ("#2A2D2E", "#DCDCDC"),
            ["Mocassin.SelectedBackground"] = ("#094771", "#CCE5FF"),
            ["Mocassin.SelectedForeground"] = ("#FFFFFF", "#1F1F1F"),
            ["Mocassin.ErrorForeground"] = ("#F48771", "#A1260D"),
            ["Mocassin.WarningForeground"] = ("#CCA700", "#A15C00"),
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

    private static readonly string[] darkSurfaces = ["#1E1E1E", "#252526", "#2D2D30"];
    private static readonly string[] lightSurfaces = ["#F3F3F3", "#FFFFFF", "#F3F3F3"];
    private static readonly string[] focusSurfaces = ["#1E1E1E", "#252526", "#2D2D30", "#FFFFFF", "#F3F3F3"];
    private static readonly string[] darkFocusSurfaces = ["#1E1E1E", "#252526", "#2D2D30", "#3C3C3C", "#37373D"];
    private static readonly string[] lightFocusSurfaces = ["#F3F3F3", "#FFFFFF"];

    private static readonly string[] derivedAccentKeys =
    [
        "SystemAccentColorLight1",
        "SystemAccentColorLight2",
        "SystemAccentColorLight3",
        "SystemAccentColorDark1",
        "SystemAccentColorDark2",
        "SystemAccentColorDark3",
    ];

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
        "SystemControlForegroundAccentBrush",
        "SystemControlHighlightAccent2RevealBackgroundBrush",
        "SystemControlHighlightAccent3RevealBackgroundBrush",
        "SystemControlHighlightAltChromeWhiteBrush",
        "SystemControlHighlightChromeWhiteBrush",
        "SystemControlFocusVisualPrimaryBrush",
        "SystemControlFocusVisualSecondaryBrush",
        "CheckBoxCheckBackgroundFillChecked",
        "CheckBoxCheckBackgroundFillIndeterminate",
        "CheckBoxCheckBackgroundStrokeIndeterminate",
        "CheckBoxCheckGlyphForegroundChecked",
        "CheckBoxCheckGlyphForegroundCheckedPointerOver",
        "CheckBoxCheckGlyphForegroundCheckedPressed",
        "CheckBoxCheckGlyphForegroundIndeterminate",
        "CheckBoxCheckGlyphForegroundIndeterminatePointerOver",
        "CheckBoxCheckGlyphForegroundIndeterminatePressed",
        "CheckBoxForegroundUncheckedPressed",
        "CheckBoxCheckBackgroundFillUncheckedPressed",
        "CheckBoxCheckBackgroundFillCheckedDisabled",
        "CheckBoxCheckBackgroundFillIndeterminateDisabled",
        "CheckBoxCheckGlyphForegroundCheckedDisabled",
        "CheckBoxCheckGlyphForegroundIndeterminateDisabled",
        "SliderThumbBackground",
        "SliderTrackValueFill",
        "SliderTrackValueFillPointerOver",
        "SliderTrackValueFillPressed",
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
                    });

                Assert.Equal(ThemeVariant.Dark, Application.Current!.RequestedThemeVariant);
                Assert.Equal("#000000", Hex(ResolveColor("Mocassin.ChromeBackground", ThemeVariant.Dark)));
                Assert.Equal("#FFD700", Hex(ResolveColor("Mocassin.BorderBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFFFFF", Hex(ResolveColor("Mocassin.ForegroundBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("Mocassin.OnAccentBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFD700", Hex(ResolveColor("Mocassin.FocusBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFD700", Hex(ResolveColor("SystemControlFocusVisualPrimaryBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("SystemControlFocusVisualSecondaryBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("SystemControlForegroundChromeWhiteBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("SystemControlHighlightAltChromeWhiteBrush", ThemeVariant.Dark)));
                Assert.Equal("#000000", Hex(ResolveColor("SystemControlHighlightChromeWhiteBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFFFFF", Hex(ResolveColor("CheckBoxForegroundUncheckedPressed", ThemeVariant.Dark)));
                Assert.Equal("#FFD700", Hex(ResolveColor("CheckBoxCheckBackgroundFillUncheckedPressed", ThemeVariant.Dark)));
                Assert.Equal("#A0A0A0", Hex(ResolveColor("CheckBoxForegroundCheckedDisabled", ThemeVariant.Dark)));
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

                foreach (var key in derivedAccentKeys)
                {
                    Assert.Equal(
                        "#FFD700",
                        Hex(Assert.IsType<Color>(ResolveResource(key, ThemeVariant.Dark))));
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
    public Task TheSixDerivedAccentKeysAreColorsInEveryThemeAndHighContrast() =>
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
                    foreach (var key in derivedAccentKeys)
                    {
                        Assert.IsType<Color>(ResolveResource(key, variant));
                    }
                }

                applier.Apply(AppSettings.Default with { Theme = ThemeSelection.Dark, HighContrast = true });
                foreach (var key in derivedAccentKeys)
                {
                    Assert.Equal("#FFD700", Hex(Assert.IsType<Color>(ResolveResource(key, ThemeVariant.Dark))));
                }
            });

    [Fact]
    public Task TheFixedFamilyUsesTheContractValuesInBothThemes() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                var applier = new AvaloniaThemeApplier();
                applier.Apply(AppSettings.Default with { Theme = ThemeSelection.Dark });
                Assert.Equal("#007ACC", Hex(ResolveColor("Mocassin.AccentBrush", ThemeVariant.Dark)));
                Assert.Equal("#FFFFFF", Hex(ResolveColor("Mocassin.OnAccentBrush", ThemeVariant.Dark)));
                Assert.Equal("#248DD3", Hex(ResolveColor("Mocassin.FocusBrush", ThemeVariant.Dark)));
                Assert.Equal("#4DA6FF", Hex(ResolveColor("Mocassin.AccentTextBrush", ThemeVariant.Dark)));
                Assert.Equal("#094771", Hex(ResolveColor("Mocassin.SelectedBackground", ThemeVariant.Dark)));
                Assert.Equal("#FFFFFF", Hex(ResolveColor("Mocassin.SelectedForeground", ThemeVariant.Dark)));
                Assert.Equal("#248DD3", Hex(ResolveColor("SystemControlHighlightAccentBrush", ThemeVariant.Dark)));
                Assert.Equal("#248DD3", Hex(ResolveColor("TabItemHeaderSelectedPipeFill", ThemeVariant.Dark)));
                Assert.Equal("#248DD3", Hex(ResolveColor("TextControlBorderBrushFocused", ThemeVariant.Dark)));
                Assert.Equal("#3A9CDD", Hex(Assert.IsType<Color>(ResolveResource("SystemAccentColorLight1", ThemeVariant.Dark))));
                Assert.Equal("#248CD1", Hex(Assert.IsType<Color>(ResolveResource("SystemAccentColorDark1", ThemeVariant.Dark))));

                applier.Apply(AppSettings.Default with { Theme = ThemeSelection.Light });
                Assert.Equal("#007ACC", Hex(ResolveColor("Mocassin.AccentBrush", ThemeVariant.Light)));
                Assert.Equal("#007ACC", Hex(ResolveColor("Mocassin.FocusBrush", ThemeVariant.Light)));
                Assert.Equal("#005A9E", Hex(ResolveColor("Mocassin.AccentTextBrush", ThemeVariant.Light)));
                Assert.Equal("#CCE5FF", Hex(ResolveColor("Mocassin.SelectedBackground", ThemeVariant.Light)));
                Assert.Equal("#1F1F1F", Hex(ResolveColor("Mocassin.SelectedForeground", ThemeVariant.Light)));
                Assert.Equal("#007ACC", Hex(ResolveColor("SystemControlHighlightAccentBrush", ThemeVariant.Light)));
                Assert.Equal("#0090F0", Hex(Assert.IsType<Color>(ResolveResource("SystemAccentColorLight1", ThemeVariant.Light))));
                Assert.Equal("#005994", Hex(Assert.IsType<Color>(ResolveResource("SystemAccentColorDark1", ThemeVariant.Light))));
            });

    [Fact]
    public void FixedFamilyPairsMeetTheContrastFloor()
    {
        // Text roles on the surfaces they are used on.
        foreach (var surface in darkSurfaces)
        {
            AssertAtLeast("#4DA6FF", surface, 4.5);
        }

        foreach (var surface in lightSurfaces)
        {
            AssertAtLeast("#005A9E", surface, 4.5);
        }

        AssertAtLeast("#FFFFFF", "#007ACC", 4.5);
        AssertAtLeast("#FFFFFF", "#094771", 4.5);
        AssertAtLeast("#1F1F1F", "#CCE5FF", 4.5);
        AssertAtLeast("#FFFFFF", "#007ACC", 3.0);

        // Paired focus/derived values on the surfaces they are rendered against.
        foreach (var surface in darkFocusSurfaces)
        {
            AssertAtLeast("#248DD3", surface, 3.0);
            AssertAtLeast("#3A9CDD", surface, 3.0);
            AssertAtLeast("#248CD1", surface, 3.0);
        }

        foreach (var surface in lightFocusSurfaces)
        {
            AssertAtLeast("#007ACC", surface, 3.0);
            AssertAtLeast("#0090F0", surface, 3.0);
            AssertAtLeast("#005994", surface, 3.0);
        }

        // Text selection pair.
        AssertAtLeast("#FFFFFF", "#007ACC", 4.5);
        AssertAtLeast("#000000", "#FFD700", 4.5);

        // High contrast.
        AssertAtLeast("#FFFFFF", "#000000", 21.0);
        AssertAtLeast("#000000", "#FFD700", 4.5);
        AssertAtLeast("#FFD700", "#000000", 3.0);
    }

    [Fact]
    public void TheNeutralFocusPairMeetsTheLayeredFloor()
    {
        // Light: primary black against the light surfaces, inner secondary white against the primary.
        foreach (var surface in lightFocusSurfaces)
        {
            AssertAtLeast("#000000", surface, 3.0);
        }

        AssertAtLeast("#FFFFFF", "#000000", 3.0);

        // Dark: primary white against the dark surfaces, inner secondary black against the primary.
        foreach (var surface in darkFocusSurfaces)
        {
            AssertAtLeast("#FFFFFF", surface, 3.0);
        }

        AssertAtLeast("#000000", "#FFFFFF", 3.0);

        // High contrast: gold primary on black, black secondary on gold.
        AssertAtLeast("#FFD700", "#000000", 3.0);
        AssertAtLeast("#000000", "#FFD700", 3.0);
    }

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

        AssertAtLeast("#CCA700", "#2D2D30", 4.5);
        AssertAtLeast("#A15C00", "#F3F3F3", 4.5);
        AssertAtLeast("#A15C00", "#FFF4CE", 4.5);
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
        return Assert.IsAssignableFrom<ISolidColorBrush>(ResolveResource(key, variant)).Color;
    }

    private static object ResolveResource(string key, ThemeVariant variant)
    {
        Assert.True(
            Application.Current!.Resources.TryGetResource(key, variant, out var value),
            $"{key} did not resolve under {variant}.");
        Assert.NotNull(value);
        return value;
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

// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Views.Theming;

/// <summary>
/// Applies the settings to Avalonia application resources and the requested theme variant.
/// </summary>
internal sealed class AvaloniaThemeApplier : IThemeApplier
{
    private static readonly Uri highContrastUri = new("avares://Mocassin.App.Views/Themes/HighContrast.axaml");
    private static readonly Uri compactDensityUri = new("avares://Mocassin.App.Views/Themes/Density.axaml");
    private static readonly Uri resourceBaseUri = new("avares://Mocassin.App.Views/");
    private static readonly Color defaultAccent = Color.Parse("#007ACC");
    private static readonly Color highContrastAccent = Color.Parse("#FFD700");

    private static ResourceDictionary? highContrast;
    private static ResourceDictionary? compactDensity;

    /// <inheritdoc />
    public void Apply(AppSettings settings)
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        application.RequestedThemeVariant = settings.Theme switch
        {
            ThemeSelection.Light => ThemeVariant.Light,
            ThemeSelection.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        SetDictionary(application, highContrastUri, ref highContrast, settings.HighContrast);
        SetDictionary(application, compactDensityUri, ref compactDensity, settings.Density == Density.Compact);

        var compact = settings.Density == Density.Compact;
        ApplyTypography(application, settings.FontScale);
        ApplySpacing(application, compact);
        ApplyShape(application, settings.HighContrast);
        application.Resources["Mocassin.Transitions"] = CreateTransitions(settings.ReduceMotion);
        ApplyAccent(application, settings);
    }

    private static void ApplyTypography(Application application, double scale)
    {
        application.Resources["Mocassin.FontScale"] = scale;
        application.Resources["Mocassin.DisplayFontSize"] = 28d * scale;
        application.Resources["Mocassin.TitleFontSize"] = 20d * scale;
        application.Resources["Mocassin.SubtitleFontSize"] = 16d * scale;
        application.Resources["Mocassin.BodyFontSize"] = 14d * scale;
        application.Resources["Mocassin.CaptionFontSize"] = 12d * scale;
        application.Resources["Mocassin.DisplayLineHeight"] = 36d * scale;
        application.Resources["Mocassin.TitleLineHeight"] = 26d * scale;
        application.Resources["Mocassin.SubtitleLineHeight"] = 22d * scale;
        application.Resources["Mocassin.BodyLineHeight"] = 20d * scale;
        application.Resources["Mocassin.CaptionLineHeight"] = 16d * scale;
    }

    private static void ApplySpacing(Application application, bool compact)
    {
        application.Resources["Mocassin.RegionGap"] = new Thickness(compact ? 4 : 6);
        application.Resources["Mocassin.RegionPadding"] = new Thickness(compact ? 8 : 12);
        application.Resources["Mocassin.RegionHeaderPadding"] = compact
            ? new Thickness(8, 6)
            : new Thickness(12, 8);
        application.Resources["Mocassin.CardPadding"] = new Thickness(compact ? 12 : 16);
        application.Resources["Mocassin.ControlPadding"] = new Thickness(compact ? 4 : 8);
        application.Resources["Mocassin.ItemPadding"] = compact
            ? new Thickness(2, 1)
            : new Thickness(4, 2);
        application.Resources["Mocassin.CompactButtonPadding"] = compact
            ? new Thickness(6, 2)
            : new Thickness(8, 2);
        application.Resources["Mocassin.ControlMinHeight"] = compact ? 26d : 34d;
        application.Resources["Mocassin.CompactButtonMinHeight"] = compact ? 22d : 26d;
        application.Resources["Mocassin.TabHeight"] = compact ? 28d : 32d;
        application.Resources["Mocassin.MenuHeight"] = compact ? 28d : 32d;
        application.Resources["Mocassin.StatusBarHeight"] = compact ? 22d : 24d;
        application.Resources["TabItemMinHeight"] = compact ? 28d : 32d;
        application.Resources["TabItemHeaderMargin"] = compact
            ? new Thickness(6, 2)
            : new Thickness(8, 3);
    }

    private static void ApplyShape(Application application, bool highContrast)
    {
        application.Resources["Mocassin.BorderThickness"] = new Thickness(highContrast ? 2 : 1);
        application.Resources["Mocassin.ControlBorderThickness"] = new Thickness(highContrast ? 2 : 1);
        application.Resources["Mocassin.FocusThickness"] = new Thickness(2);
    }

    private static void ApplyAccent(Application application, AppSettings settings)
    {
        // The accent is a fixed family owned by the palette theme dictionaries. Only the Fluent base colour is a
        // direct runtime resource; the fixed roles and aliases must stay in a theme dictionary so a Light/Dark
        // switch can supply them.
        application.Resources["SystemAccentColor"] = settings.HighContrast ? highContrastAccent : defaultAccent;
        application.Resources.Remove("Mocassin.AccentBrush");
        application.Resources.Remove("Mocassin.OnAccentBrush");
    }

    private static void SetDictionary(
        Application application,
        Uri uri,
        ref ResourceDictionary? slot,
        bool enabled)
    {
        if (enabled)
        {
            if (slot is null || !application.Resources.MergedDictionaries.Contains(slot))
            {
                if (AvaloniaXamlLoader.Load(uri, resourceBaseUri) is ResourceDictionary dictionary)
                {
                    application.Resources.MergedDictionaries.Add(dictionary);
                    slot = dictionary;
                }
            }
        }
        else if (slot is not null)
        {
            application.Resources.MergedDictionaries.Remove(slot);
            slot = null;
        }
    }

    private static Transitions CreateTransitions(bool reduceMotion)
    {
        var transitions = new Transitions();
        if (!reduceMotion)
        {
            transitions.Add(
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(120),
                });
        }

        return transitions;
    }
}

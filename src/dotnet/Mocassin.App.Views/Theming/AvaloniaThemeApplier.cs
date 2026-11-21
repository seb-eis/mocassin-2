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

    private ResourceDictionary? highContrast;
    private ResourceDictionary? compactDensity;

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

        application.Resources["Mocassin.FontScale"] = settings.FontScale;
        application.Resources["Mocassin.BodyFontSize"] = 14d * settings.FontScale;
        application.Resources["Mocassin.Density"] = settings.Density == Density.Compact ? "compact" : "comfortable";
        var compact = settings.Density == Density.Compact;
        application.Resources["Mocassin.ControlPadding"] = new Thickness(compact ? 4 : 8);
        application.Resources["Mocassin.ItemPadding"] = compact
            ? new Thickness(2, 1)
            : new Thickness(4, 2);
        application.Resources["Mocassin.ControlMinHeight"] = compact ? 26d : 34d;
        application.Resources["Mocassin.BorderThickness"] = new Thickness(settings.HighContrast ? 2 : 1);
        application.Resources["Mocassin.HighContrast"] = settings.HighContrast;
        application.Resources["Mocassin.ReduceMotion"] = settings.ReduceMotion;
        application.Resources["Mocassin.Accent"] = settings.Accent ?? "default";
        application.Resources["Mocassin.Transitions"] = CreateTransitions(settings.ReduceMotion);

        if (AccentColor(settings.Accent) is { } color)
        {
            application.Resources["SystemAccentColor"] = color;
        }
        else
        {
            application.Resources.Remove("SystemAccentColor");
        }
    }

    private static void SetDictionary(
        Application application,
        Uri uri,
        ref ResourceDictionary? slot,
        bool enabled)
    {
        if (enabled && slot is null)
        {
            if (AvaloniaXamlLoader.Load(uri, resourceBaseUri) is ResourceDictionary dictionary)
            {
                application.Resources.MergedDictionaries.Add(dictionary);
                slot = dictionary;
            }
        }
        else if (!enabled && slot is not null)
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

    private static Color? AccentColor(string? accent) =>
        accent switch
        {
            "blue" => Color.Parse("#0078D4"),
            "teal" => Color.Parse("#008B8B"),
            "green" => Color.Parse("#2E7D32"),
            "amber" => Color.Parse("#B26A00"),
            "violet" => Color.Parse("#6A4C93"),
            "rose" => Color.Parse("#B0004F"),
            _ => null,
        };
}

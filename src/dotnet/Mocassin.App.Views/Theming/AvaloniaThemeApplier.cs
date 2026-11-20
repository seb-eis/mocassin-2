// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Views.Theming;

/// <summary>
/// Applies the settings to Avalonia application resources and the requested theme variant.
/// </summary>
internal sealed class AvaloniaThemeApplier : IThemeApplier
{
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

        application.Resources["Mocassin.FontScale"] = settings.FontScale;
        application.Resources["Mocassin.BodyFontSize"] = 14d * settings.FontScale;
        application.Resources["Mocassin.Density"] = settings.Density == Density.Compact ? "compact" : "comfortable";
        application.Resources["Mocassin.HighContrast"] = settings.HighContrast;
        application.Resources["Mocassin.ReduceMotion"] = settings.ReduceMotion;
        application.Resources["Mocassin.Accent"] = settings.Accent ?? "default";

        if (AccentColor(settings.Accent) is { } color)
        {
            application.Resources["SystemAccentColor"] = color;
        }
        else
        {
            application.Resources.Remove("SystemAccentColor");
        }
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

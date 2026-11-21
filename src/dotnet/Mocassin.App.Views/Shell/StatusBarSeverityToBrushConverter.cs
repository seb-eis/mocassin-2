// SPDX-License-Identifier: MIT

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Maps a <see cref="StatusBarSeverity"/> to a readable foreground brush.
/// </summary>
internal sealed class StatusBarSeverityToBrushConverter : IValueConverter
{
    private static readonly IBrush errorBrush = new SolidColorBrush(Color.Parse("#D32F2F"));
    private static readonly IBrush warningBrush = new SolidColorBrush(Color.Parse("#F57C00"));
    private static readonly IBrush successBrush = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush informationBrush = new SolidColorBrush(Color.Parse("#1565C0"));
    private static readonly IBrush normalBrush = new SolidColorBrush(Color.Parse("#616161"));

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            StatusBarSeverity.Error => errorBrush,
            StatusBarSeverity.Warning => warningBrush,
            StatusBarSeverity.Success => successBrush,
            StatusBarSeverity.Information => informationBrush,
            _ => normalBrush,
        };

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Status-bar severity is display-only.");
}

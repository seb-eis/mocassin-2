// SPDX-License-Identifier: MIT

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mocassin.App.ViewModels.Icons;

namespace Mocassin.App.Views.Icons;

/// <summary>
/// Resolves an <see cref="AppIcon"/> key to its committed <see cref="StreamGeometry"/> resource.
/// </summary>
/// <remarks>
/// The lookup is local to the application resources; it never dereferences a URL. An unresolved key returns the
/// <c>Mocassin.Icon.Missing</c> marker so a wiring defect renders visibly instead of disappearing.
/// </remarks>
internal sealed class IconGeometryConverter : IValueConverter
{
    /// <summary>
    /// Gets the shared converter instance used by the implicit icon template.
    /// </summary>
    public static IconGeometryConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key)
        {
            return null;
        }

        var resourceKey = IconKeys.ResourceKeyFor(key) ?? IconKeys.MissingResource;
        return Application.Current is { } application
            && application.TryFindResource(resourceKey, out var resource)
            && resource is Geometry geometry
                ? geometry
                : null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Icon geometry is a one-way presentation mapping.");
}

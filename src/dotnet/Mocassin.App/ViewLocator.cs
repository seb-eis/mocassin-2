// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.Logging;
using Mocassin.App.ViewModels;
using Mocassin.App.Views;

namespace Mocassin.App;

/// <summary>
/// Maps an application view-model type to its view by convention. A missing view is logged and rendered as an
/// explicit panel instead of throwing.
/// </summary>
internal sealed partial class ViewLocator : IDataTemplate
{
    private readonly Microsoft.Extensions.Logging.ILogger<ViewLocator>? logger;

    /// <summary>
    /// Initializes a new locator.
    /// </summary>
    /// <param name="logger">The optional logger used for missing views.</param>
    public ViewLocator(Microsoft.Extensions.Logging.ILogger<ViewLocator>? logger = null)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var viewModelType = param.GetType();
        if (viewModelType.Assembly != typeof(AppViewModelsModule).Assembly)
        {
            return null;
        }

        var fullName = viewModelType.FullName;
        if (fullName is null || !fullName.Contains(".ViewModels.", StringComparison.Ordinal))
        {
            return NotFound(viewModelType.Name);
        }

        var viewName = fullName.Replace(".ViewModels.", ".Views.", StringComparison.Ordinal);
        if (viewName.EndsWith("ViewModel", StringComparison.Ordinal))
        {
            viewName = viewName[..^"ViewModel".Length] + "View";
        }

        var viewType = typeof(AppViewsModule).Assembly.GetType(viewName, throwOnError: false);
        if (viewType is null || !typeof(Control).IsAssignableFrom(viewType))
        {
            if (logger is not null)
            {
                LogMissingView(logger, viewModelType, viewName);
            }
            return NotFound(viewName);
        }

        return Activator.CreateInstance(viewType, nonPublic: true) as Control ?? NotFound(viewName);
    }

    /// <inheritdoc />
    public bool Match(object? data) =>
        data is not null && data.GetType().Assembly == typeof(AppViewModelsModule).Assembly;

    [LoggerMessage(EventId = 30, Level = LogLevel.Error, Message = "No view was found for {ViewModelType}; expected {ViewType}.")]
    private static partial void LogMissingView(ILogger logger, Type viewModelType, string viewType);

    private static Border NotFound(string typeName) =>
        new Border
        {
            Padding = new Avalonia.Thickness(12),
            Child = new TextBlock
            {
                Text = $"View not found: {typeName}",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            },
        };
}

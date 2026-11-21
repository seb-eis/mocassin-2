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
        if (!IsViewModelType(viewModelType))
        {
            return null;
        }

        var fullName = viewModelType.FullName!;
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
    public bool Match(object? data) => data is not null && IsViewModelType(data.GetType());

    /// <summary>
    /// Determines whether a type is a view model that follows the naming convention this locator maps. Only
    /// classes named <c>*ViewModel</c> in the application view-model assembly qualify, so enums, settings
    /// records, plain strings, and other model objects are never claimed.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <returns><see langword="true"/> when the type is a view-model type.</returns>
    private static bool IsViewModelType(Type type) =>
        type.Assembly == typeof(AppViewModelsModule).Assembly
        && type.IsClass
        && type.Name.EndsWith("ViewModel", StringComparison.Ordinal)
        && type.FullName?.Contains(".ViewModels.", StringComparison.Ordinal) == true;

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

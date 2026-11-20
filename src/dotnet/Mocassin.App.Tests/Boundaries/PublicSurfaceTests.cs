// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels;
using Mocassin.App.Views;

namespace Mocassin.App.Tests.Boundaries;

/// <summary>
/// Verifies the public-surface rule: only App is a new public application type.
/// </summary>
public sealed class PublicSurfaceTests
{
    private static readonly string[] viewModelTypes = ["Mocassin.App.ViewModels.AppViewModelsModule"];
    private static readonly string[] viewTypes = ["Mocassin.App.Views.AppViewsModule"];
    private static readonly string[] appTypes = ["Mocassin.App.App", "Mocassin.App.Program"];

    [Fact]
    public void TheApplicationAssembliesExposeOnlyTheDocumentedPublicTypes()
    {
        var viewModels = typeof(AppViewModelsModule).Assembly.GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        // The Avalonia XAML compiler emits public types in the CompiledAvaloniaXaml namespace.
        var views = typeof(AppViewsModule).Assembly.GetExportedTypes()
            .Where(type => !string.Equals(type.Namespace, "CompiledAvaloniaXaml", StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var app = typeof(Mocassin.App.App).Assembly.GetExportedTypes()
            .Where(type => !string.Equals(type.Namespace, "CompiledAvaloniaXaml", StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(viewModelTypes, viewModels);
        Assert.Equal(viewTypes, views);
        Assert.Equal(appTypes, app);
    }
}

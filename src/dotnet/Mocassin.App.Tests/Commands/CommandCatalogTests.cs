// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Commands;

namespace Mocassin.App.Tests.Commands;

/// <summary>
/// Verifies the command catalog, gestures, palette, and menus.
/// </summary>
public sealed class CommandCatalogTests
{
    [Fact]
    public void DuplicateCommandIdsAreRejected()
    {
        var catalog = new CommandCatalog(new GestureFormatter(GesturePlatform.Linux));
        catalog.Add("test.run", "Run", "Test", MenuKind.View, null, new RelayCommand(() => { }));

        Assert.Throws<InvalidOperationException>(
            () => catalog.Add("test.run", "Again", "Test", null, null, new RelayCommand(() => { })));
    }

    [Fact]
    public void GesturesUseThePlatformPrimaryModifier()
    {
        var gesture = new KeyGestureDescriptor("P", GestureModifier.Primary | GestureModifier.Shift);

        Assert.Equal("Ctrl+Shift+P", new GestureFormatter(GesturePlatform.Linux).Format(gesture));
        Assert.Equal("Ctrl+Shift+P", new GestureFormatter(GesturePlatform.Windows).Format(gesture));
        Assert.Equal("Cmd+Shift+P", new GestureFormatter(GesturePlatform.MacOS).Format(gesture));
    }

    [Fact]
    public void ThePaletteFiltersByTitleIdCategoryAndGesture()
    {
        var catalog = new CommandCatalog(new GestureFormatter(GesturePlatform.Linux));
        var executed = false;
        catalog.Add(
            "view.palette",
            "Command Palette",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("P", GestureModifier.Primary | GestureModifier.Shift),
            new RelayCommand(() => executed = true));
        catalog.Add("shell.exit", "Exit", "File", MenuKind.File, null, new RelayCommand(() => { }));
        var closed = false;
        var palette = new CommandPaletteViewModel(catalog, () => closed = true);

        palette.SearchText = "palette";
        Assert.Single(palette.Results);

        palette.SearchText = "Ctrl+Shift+P";
        Assert.Single(palette.Results);

        palette.SearchText = "file";
        Assert.Single(palette.Results);

        palette.SearchText = string.Empty;
        Assert.Equal(2, palette.Results.Count);

        palette.SelectedCommand = catalog.Find("view.palette");
        palette.ExecuteSelectedCommand.Execute(null);

        Assert.True(executed);
        Assert.True(closed);
    }

    [Fact]
    public void TheMenuProviderReturnsCommandsByMenu()
    {
        var catalog = new CommandCatalog(new GestureFormatter(GesturePlatform.Windows));
        catalog.Add("file.exit", "Exit", "File", MenuKind.File, null, new RelayCommand(() => { }));
        catalog.Add("view.projects", "Projects", "View", MenuKind.View, null, new RelayCommand(() => { }));
        var provider = new ShellMenuProvider(catalog);

        Assert.Single(provider.CreateMenuItems(MenuKind.File));
        Assert.Single(provider.CreateMenuItems(MenuKind.View));
        Assert.Empty(provider.CreateMenuItems(MenuKind.Help));
    }

    [Fact]
    public void TheCheatSheetGroupsCommandsWithGestures()
    {
        var catalog = new CommandCatalog(new GestureFormatter(GesturePlatform.Linux));
        catalog.Add(
            "view.palette",
            "Command Palette",
            "View",
            MenuKind.View,
            new KeyGestureDescriptor("P", GestureModifier.Primary),
            new RelayCommand(() => { }));
        catalog.Add("shell.exit", "Exit", "File", MenuKind.File, null, new RelayCommand(() => { }));

        var cheatSheet = new ShortcutCheatSheetViewModel(catalog, () => { });

        var group = Assert.Single(cheatSheet.Groups);
        Assert.Equal("View", group.Title);
        Assert.Single(group.Commands);
    }
}

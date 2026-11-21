// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Project switcher and explorer region.
/// </summary>
internal sealed partial class ProjectsRegionView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ProjectsRegionView() => InitializeComponent();

    private void OnExplorerDoubleTapped(object? sender, TappedEventArgs e) => OpenSelected();

    private void OnExplorerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space))
        {
            return;
        }

        e.Handled = true;
        OpenSelected();
    }

    private void OpenSelected()
    {
        if (DataContext is ShellViewModel shell && shell.Explorer.OpenSelectedCommand.CanExecute(null))
        {
            shell.Explorer.OpenSelectedCommand.Execute(null);
        }
    }
}

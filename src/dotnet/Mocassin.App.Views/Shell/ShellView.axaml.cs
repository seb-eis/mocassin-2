// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Shell layout control.
/// </summary>
internal sealed partial class ShellView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            MenuBuilder.Apply(MainMenu, shell.Menus, shell.Platform);
        }
    }
}

// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mocassin.App.ViewModels.Documents;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Document tab region. Tabs are added by the workspace feature.
/// </summary>
internal sealed partial class DocumentsRegionView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public DocumentsRegionView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            return;
        }

        var tab = (e.Source as Visual)?.FindAncestorOfType<TabItem>();
        if (tab?.DataContext is DocumentViewModel document && document.CloseCommand?.CanExecute(null) == true)
        {
            e.Handled = true;
            document.CloseCommand.Execute(null);
        }
    }
}

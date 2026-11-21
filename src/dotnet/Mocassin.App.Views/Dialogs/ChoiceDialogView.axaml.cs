// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mocassin.App.ViewModels.Dialogs;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// Choice dialog view.
/// </summary>
internal sealed partial class ChoiceDialogView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ChoiceDialogView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter
            || DataContext is not ChoiceDialogViewModel dialog
            || dialog.Result is not null)
        {
            return;
        }

        // A focused button owns its own Enter action; only the list path needs the explicit confirm.
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Button)
        {
            return;
        }

        if (dialog.ConfirmCommand.CanExecute(null))
        {
            e.Handled = true;
            dialog.ConfirmCommand.Execute(null);
        }
    }
}

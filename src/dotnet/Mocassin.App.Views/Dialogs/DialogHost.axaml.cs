// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mocassin.App.ViewModels.Dialogs;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// In-shell modal overlay. It hosts one dialog at a time and restores focus when the dialog closes.
/// </summary>
internal sealed partial class DialogHost : UserControl, IDialogHost
{
    private readonly Grid root;
    private readonly ContentControl dialogContent;
    private DialogViewModel? current;
    private TaskCompletionSource<DialogResult>? completion;
    private CancellationTokenRegistration cancellationRegistration;
    private IInputElement? previousFocus;

    /// <summary>
    /// Initializes the overlay control.
    /// </summary>
    public DialogHost()
    {
        InitializeComponent();
        root = this.FindControl<Grid>("Root")
            ?? throw new InvalidOperationException("The dialog overlay root is missing.");
        dialogContent = this.FindControl<ContentControl>("DialogContent")
            ?? throw new InvalidOperationException("The dialog overlay content host is missing.");
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc />
    public async Task<DialogResult> ShowAsync(DialogViewModel dialog, CancellationToken cancellationToken = default)
    {
        var source = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ShowCore(dialog, source);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() => Close(dialog, DialogResult.Canceled));
        }

        return await source.Task;
    }

    /// <inheritdoc />
    public void Show(DialogViewModel dialog) => ShowCore(dialog, source: null);

    /// <inheritdoc />
    public void Close(DialogViewModel dialog, DialogResult result)
    {
        if (current != dialog)
        {
            return;
        }

        dialog.Close(result);
    }

    private void ShowCore(DialogViewModel dialog, TaskCompletionSource<DialogResult>? source)
    {
        if (current is not null)
        {
            throw new InvalidOperationException("Only one modal dialog may be open at a time.");
        }

        current = dialog;
        completion = source;
        previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        dialog.Closed += OnDialogClosed;
        dialogContent.Content = dialog;
        root.IsVisible = true;

        Dispatcher.UIThread.Post(
            () =>
            {
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                if (focusManager is null)
                {
                    return;
                }

                var target = dialogContent.GetVisualDescendants()
                    .OfType<IInputElement>()
                    .FirstOrDefault(element => element.Focusable && element.IsEffectivelyVisible)
                    ?? dialogContent;
                focusManager.Focus(target, NavigationMethod.Tab, KeyModifiers.None);
            });
    }

    private void OnDialogClosed(object? sender, DialogViewModel dialog)
    {
        if (current != dialog)
        {
            return;
        }

        var result = dialog.Result ?? DialogResult.Dismissed;
        dialog.Closed -= OnDialogClosed;
        cancellationRegistration.Dispose();
        cancellationRegistration = default;
        dialogContent.Content = null;
        root.IsVisible = false;
        current = null;
        previousFocus?.Focus(NavigationMethod.Tab, KeyModifiers.None);
        previousFocus = null;

        var source = completion;
        completion = null;
        source?.TrySetResult(result);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || current is null || !current.CanCancel)
        {
            return;
        }

        e.Handled = true;
        current.Close(DialogResult.Canceled);
    }
}

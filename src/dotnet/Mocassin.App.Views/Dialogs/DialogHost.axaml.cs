// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// In-shell modal overlay. It hosts one dialog at a time, declares its initial focus, and restores focus when the
/// dialog closes.
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

    /// <summary>
    /// Occurs after the previous focus has been captured and the dialog is visible, before the initial focus is
    /// posted.
    /// </summary>
    internal event EventHandler? DialogOpened;

    /// <summary>
    /// Occurs after the dialog is hidden and before the previous focus is restored.
    /// </summary>
    internal event EventHandler? DialogClosed;

    /// <summary>
    /// Gets a value indicating whether a dialog is open.
    /// </summary>
    internal bool IsDialogOpen => current is not null;

    /// <summary>
    /// Gets the active dialog title, or <see langword="null"/>.
    /// </summary>
    internal string? CurrentDialogTitle => current?.Title;

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

    /// <summary>
    /// Cancels the active dialog when it is dismissible and consumes the request otherwise.
    /// </summary>
    /// <returns><see langword="true"/> when a dialog was open.</returns>
    internal bool TryCancel()
    {
        if (current is null)
        {
            return false;
        }

        if (current.CanCancel)
        {
            current.Close(DialogResult.Canceled);
        }

        return true;
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
        AutomationProperties.SetName(dialogContent, dialog.Title);
        root.IsVisible = true;
        DialogOpened?.Invoke(this, EventArgs.Empty);

        Dispatcher.UIThread.Post(FocusInitialTarget, DispatcherPriority.Loaded);
    }

    private void FocusInitialTarget()
    {
        if (current is null)
        {
            return;
        }

        var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
        if (focusManager is null)
        {
            return;
        }

        focusManager.Focus(ResolveInitialTarget(), NavigationMethod.Tab, KeyModifiers.None);
    }

    private IInputElement ResolveInitialTarget()
    {
        var controls = dialogContent.GetVisualDescendants().OfType<Control>().ToList();
        var marked = controls.FirstOrDefault(
            control => DialogFocus.GetInitialFocus(control) == DialogFocusTarget.True);
        if (marked is not null && marked.Focusable && marked.IsEffectivelyVisible)
        {
            return marked;
        }

        if (controls.Any(control => DialogFocus.GetInitialFocus(control) == DialogFocusTarget.Container))
        {
            return dialogContent;
        }

        return controls.FirstOrDefault(
                control => control.Focusable && control.IsEffectivelyVisible && control.IsEffectivelyEnabled)
            ?? dialogContent;
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
        AutomationProperties.SetName(dialogContent, null);
        current = null;
        DialogClosed?.Invoke(this, EventArgs.Empty);
        ShellFocusRestore.TryFocus(TopLevel.GetTopLevel(this), previousFocus);
        previousFocus = null;

        var source = completion;
        completion = null;
        source?.TrySetResult(result);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || current is null)
        {
            return;
        }

        e.Handled = true;
        if (current.CanCancel)
        {
            current.Close(DialogResult.Canceled);
        }
    }
}

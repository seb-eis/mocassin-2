// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.Views.Editors;

/// <summary>
/// Shared document-level editor gestures. The interaction contract's <c>Enter</c> rule ("applies the focused field
/// or form") is one implementation so the multi-field forms cannot drift.
/// </summary>
internal static class EditorKeyGestures
{
    /// <summary>
    /// Applies the form on <c>Enter</c> while focus is inside a single-line field or a combo box.
    /// </summary>
    /// <param name="owner">The control whose subtree receives the routed key event.</param>
    /// <param name="resolveCommand">Resolves the form's Apply command at key time.</param>
    /// <remarks>
    /// Buttons, check boxes, multiline text, and list rows keep their own <c>Enter</c> behaviour: only a focused
    /// single-line <see cref="TextBox"/> or a <see cref="ComboBox"/> is intercepted.
    /// </remarks>
    public static void ApplyOnEnter(Control owner, Func<IRelayCommand?> resolveCommand)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(resolveCommand);
        owner.AddHandler(
            InputElement.KeyDownEvent,
            (_, args) =>
            {
                if (args.Key != Key.Enter
                    || args.KeyModifiers != KeyModifiers.None
                    || args.Handled)
                {
                    return;
                }

                var focused = TopLevel.GetTopLevel(owner)?.FocusManager?.GetFocusedElement();
                if (focused is TextBox { AcceptsReturn: true } || focused is not (TextBox or ComboBox))
                {
                    return;
                }

                var command = resolveCommand();
                if (command is null || !command.CanExecute(null))
                {
                    return;
                }

                args.Handled = true;
                command.Execute(null);
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }
}

// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// The explicit initial-focus choice for a dialog view.
/// </summary>
internal enum DialogFocusTarget
{
    /// <summary>
    /// No marker; the host falls back to the first focusable control.
    /// </summary>
    None,

    /// <summary>
    /// The marked control receives the initial focus.
    /// </summary>
    True,

    /// <summary>
    /// The dialog container receives the initial focus because the view has no dismiss action.
    /// </summary>
    Container,
}

/// <summary>
/// Attached marker used by <see cref="DialogHost"/> to select a dialog's initial focus.
/// </summary>
internal static class DialogFocus
{
    /// <summary>
    /// Identifies the initial-focus marker.
    /// </summary>
    public static readonly AttachedProperty<DialogFocusTarget> InitialFocusProperty =
        AvaloniaProperty.RegisterAttached<Control, DialogFocusTarget>(
            "InitialFocus",
            typeof(DialogFocus),
            defaultValue: DialogFocusTarget.None);

    /// <summary>
    /// Sets the initial-focus marker.
    /// </summary>
    /// <param name="element">The marked control.</param>
    /// <param name="value">The marker value.</param>
    public static void SetInitialFocus(Control element, DialogFocusTarget value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(InitialFocusProperty, value);
    }

    /// <summary>
    /// Gets the initial-focus marker.
    /// </summary>
    /// <param name="element">The marked control.</param>
    /// <returns>The marker value.</returns>
    public static DialogFocusTarget GetInitialFocus(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(InitialFocusProperty);
    }
}

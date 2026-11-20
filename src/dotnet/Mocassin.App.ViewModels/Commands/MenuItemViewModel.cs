// SPDX-License-Identifier: MIT

using System.Windows.Input;

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// One menu item in the shell menu bar.
/// </summary>
/// <param name="Header">The visible header.</param>
/// <param name="Command">The optional command.</param>
/// <param name="Gesture">The optional gesture.</param>
/// <param name="GestureText">The formatted gesture.</param>
internal sealed record MenuItemViewModel(
    string Header,
    ICommand? Command,
    KeyGestureDescriptor? Gesture = null,
    string GestureText = "");

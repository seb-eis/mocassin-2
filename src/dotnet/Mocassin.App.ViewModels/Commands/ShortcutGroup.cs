// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// A category of commands shown in the shortcut cheat sheet.
/// </summary>
/// <param name="Title">The category title.</param>
/// <param name="Commands">The commands with gestures.</param>
internal sealed record ShortcutGroup(string Title, IReadOnlyList<ShellCommand> Commands);

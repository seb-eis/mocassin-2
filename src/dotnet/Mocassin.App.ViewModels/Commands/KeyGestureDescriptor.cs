// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Framework-neutral key gesture.
/// </summary>
/// <param name="Key">The key name, for example <c>P</c>, <c>Left</c>, or <c>F1</c>.</param>
/// <param name="Modifiers">The modifiers.</param>
internal readonly record struct KeyGestureDescriptor(string Key, GestureModifier Modifiers = GestureModifier.None);

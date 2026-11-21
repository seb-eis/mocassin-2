// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// One top-level shell menu.
/// </summary>
/// <param name="Header">The visible header.</param>
/// <param name="Items">The menu items.</param>
internal sealed record MenuViewModel(string Header, IReadOnlyList<MenuItemViewModel> Items);

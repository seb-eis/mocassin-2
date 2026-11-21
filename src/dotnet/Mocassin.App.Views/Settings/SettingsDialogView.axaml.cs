// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Views.Settings;

/// <summary>
/// Settings dialog overlay.
/// </summary>
internal sealed partial class SettingsDialogView : UserControl
{
    /// <summary>
    /// Initializes the view.
    /// </summary>
    public SettingsDialogView() => InitializeComponent();

    /// <summary>
    /// Focuses the theme combo box, the first editable control. Save is never the initial focus.
    /// </summary>
    public void FocusInitial() => ThemeBox.Focus();
}

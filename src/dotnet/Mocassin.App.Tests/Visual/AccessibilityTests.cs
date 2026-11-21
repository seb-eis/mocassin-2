// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Sweeps the primary interactive shell controls for an accessible name.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class AccessibilityTests
{
    [Fact]
    public Task EveryPrimaryInteractiveControlHasAnAccessibleName() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var controls = window.GetVisualDescendants()
                        .OfType<Control>()
                        .Where(control => control is Button or TextBox or ComboBox or CheckBox or Slider
                            or ListBox or TreeView or Menu or TabControl);
                    foreach (var control in controls)
                    {
                        var name = AutomationProperties.GetName(control);
                        var fallback = control switch
                        {
                            Button button => button.Content as string,
                            TextBox textBox => textBox.PlaceholderText,
                            _ => null,
                        };
                        Assert.False(
                            string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(fallback),
                            $"A {control.GetType().Name} has no accessible name.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
}

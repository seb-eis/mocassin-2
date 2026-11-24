// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mocassin.App.ViewModels.Editors.Particles;

namespace Mocassin.App.Views.Editors.Particles;

/// <summary>
/// The particle detail view: one field per undo entry on <c>Enter</c> or focus loss, plus the explicit Apply/Revert
/// form actions and the inline error/warning surface.
/// </summary>
internal sealed partial class ParticleDetailView : UserControl
{
    private ParticleDetailViewModel? viewModel;

    /// <summary>
    /// Initializes the particle detail view.
    /// </summary>
    public ParticleDetailView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => viewModel = DataContext as ParticleDetailViewModel;
    }

    private void OnNameLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitNameCommand.Execute(null);

    private void OnSymbolLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitSymbolCommand.Execute(null);

    private void OnChargeLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitChargeCommand.Execute(null);

    private void OnVacancyClick(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitVacancyCommand.Execute(null);

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        var command = (sender as Control)?.Name switch
        {
            "name" => viewModel?.CommitNameCommand,
            "symbol" => viewModel?.CommitSymbolCommand,
            "charge" => viewModel?.CommitChargeCommand,
            _ => null,
        };
        command?.Execute(null);
    }
}

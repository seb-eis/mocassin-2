// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mocassin.App.ViewModels.Editors.Sets;
using Mocassin.Core.Ids;

namespace Mocassin.App.Views.Editors.Sets;

/// <summary>
/// The particle-set detail view. The "Project particles" list is the drag source; the ordered member list is the
/// drop target, and every drag action has a button equivalent.
/// </summary>
internal sealed partial class ParticleSetDetailView : UserControl
{
    private static readonly DataFormat<string> particleFormat =
        DataFormat.CreateInProcessFormat<string>("mocassin.particle.set-member");

    private ParticleSetDetailViewModel? viewModel;

    /// <summary>
    /// Initializes the particle-set detail view.
    /// </summary>
    public ParticleSetDetailView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => viewModel = DataContext as ParticleSetDetailViewModel;
    }

    private void OnNameLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.ApplyCommand.Execute(null);

    private void OnNameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        viewModel?.ApplyCommand.Execute(null);
    }

    private async void OnSourcePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (viewModel is null
            || !e.GetCurrentPoint(ProjectParticles).Properties.IsLeftButtonPressed
            || (e.Source as Control)?.DataContext is not ProjectParticleChoiceViewModel choice
            || choice.IsMember)
        {
            return;
        }

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.Create(particleFormat, choice.Id.Value.ToString("D")));
        try
        {
            await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Copy);
        }
        catch (InvalidOperationException)
        {
            // The platform refused the drag gesture; the keyboard Add button remains the fallback.
        }
    }

    private void OnMembersDragOver(object? sender, DragEventArgs e)
    {
        if (viewModel is not null && e.DataTransfer.Contains(particleFormat))
        {
            e.DragEffects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnMembersDrop(object? sender, DragEventArgs e)
    {
        if (viewModel is null)
        {
            return;
        }

        var text = e.DataTransfer.TryGetValue(particleFormat);
        if (text is null || !Guid.TryParse(text, out var id))
        {
            return;
        }

        var index = InsertionIndex(e.GetPosition(Members));
        viewModel.TryDropParticle(ParticleId.Create(id), index);
        e.Handled = true;
    }

    private int InsertionIndex(Point position)
    {
        if (viewModel is null)
        {
            return 0;
        }

        foreach (var item in Members.GetVisualDescendants().OfType<ListBoxItem>())
        {
            var top = item.TranslatePoint(new Point(0, 0), Members)?.Y ?? 0d;
            if (position.Y < top + (item.Bounds.Height / 2d))
            {
                var index = Members.Items.IndexOf(item.DataContext);
                return index < 0 ? viewModel.MemberCount : index;
            }
        }

        return viewModel.MemberCount;
    }
}

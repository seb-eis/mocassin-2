// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mocassin.App.ViewModels.Editors;

namespace Mocassin.App.Views.Editors;

/// <summary>
/// The master-detail model-editor document view: the table selects, the splitter resizes, and the detail pane
/// edits.
/// </summary>
internal sealed partial class ModelEditorDocumentView : UserControl
{
    private ModelEditorDocumentViewModel? viewModel;

    /// <summary>
    /// Initializes the document view.
    /// </summary>
    public ModelEditorDocumentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachModel();
        TableSurface.DetailPane = DetailHost;
        TableSurface.TabToDetailRequested += (_, _) =>
        {
            var pending = viewModel?.Editor.ConsumePendingFocus();
            if (pending is not null)
            {
                TableSurface.FocusDetailField(pending);
            }
            else
            {
                TableSurface.FocusDetailFirstField();
            }
        };
        EditorSplitter.AddHandler(PointerReleasedEvent, OnSplitterReleased, RoutingStrategies.Bubble);
        AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(FocusInitial);
    }

    /// <summary>
    /// Gets the table surface, used by the headless tests.
    /// </summary>
    internal ModelTableSurface Table => TableSurface;

    /// <summary>
    /// Gets the detail host, used by the headless tests.
    /// </summary>
    internal ContentControl Detail => DetailHost;

    private void AttachModel()
    {
        if (viewModel is not null)
        {
            viewModel.Editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        viewModel = DataContext as ModelEditorDocumentViewModel;
        if (viewModel is not null)
        {
            viewModel.Editor.PropertyChanged += OnEditorPropertyChanged;
        }

        TableSurface.DetailPane = DetailHost;
        ApplyEditorFraction();
        ApplyPendingFocus();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(ModelEditorViewModel.EditorFraction):
                ApplyEditorFraction();
                break;
            case nameof(ModelEditorViewModel.IsReadOnly):
                ApplyEditorFraction();
                break;
        }
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs args)
    {
        if (viewModel is null)
        {
            return;
        }

        if (args.Key == Key.Escape && viewModel.Editor.Detail is not null)
        {
            args.Handled = true;
            viewModel.Editor.Detail.RevertCommand.Execute(null);
            return;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        if (focused is TextBox)
        {
            return;
        }

        if (args.Key == Key.Delete && viewModel.Editor.RemoveCommand.CanExecute(null))
        {
            args.Handled = true;
            viewModel.Editor.RemoveCommand.Execute(null);
            return;
        }

        if (args.Key == Key.D
            && (args.KeyModifiers & KeyModifiers.Control) != 0
            && viewModel.Editor.DuplicateCommand.CanExecute(null))
        {
            args.Handled = true;
            viewModel.Editor.DuplicateCommand.Execute(null);
        }
    }

    private void OnSplitterReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (viewModel is null)
        {
            return;
        }

        var tableWidth = MasterDetailGrid.ColumnDefinitions[0].ActualWidth;
        var detailWidth = MasterDetailGrid.ColumnDefinitions[2].ActualWidth;
        var total = tableWidth + detailWidth;
        if (total > 0d && double.IsFinite(total))
        {
            viewModel.Editor.PaneLayout.EditorFraction = tableWidth / total;
        }
    }

    private void ApplyEditorFraction()
    {
        var fraction = viewModel?.Editor.EditorFraction ?? 0.42d;
        MasterDetailGrid.ColumnDefinitions[0].Width = new GridLength(fraction, GridUnitType.Star);
        MasterDetailGrid.ColumnDefinitions[2].Width = new GridLength(1d - fraction, GridUnitType.Star);
    }

    private void ApplyPendingFocus()
    {
        if (viewModel is null)
        {
            return;
        }

        var pending = viewModel.Editor.PendingFocus;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (!string.IsNullOrWhiteSpace(pending))
                {
                    if (TableSurface.FocusDetailField(pending))
                    {
                        viewModel.Editor.ConsumePendingFocus();
                    }

                    return;
                }

            });
    }

    private void FocusInitial() => TableSurface.Rows.Focus();
}

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
/// edits. <c>Enter</c> applies the focused field/form, <c>Escape</c> reverts it, <c>Delete</c> asks for confirmation
/// before removing, and <c>Ctrl+D</c> duplicates.
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
        EditorKeyGestures.ApplyOnEnter(DetailHost, () => viewModel?.Editor.Detail?.ApplyCommand);
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

    /// <summary>
    /// Gets the resizable master-detail grid, used by the headless splitter test.
    /// </summary>
    internal Grid MasterDetail => MasterDetailGrid;

    /// <summary>
    /// Gets the editor splitter, used by the headless accessibility test.
    /// </summary>
    internal GridSplitter Splitter => EditorSplitter;

    /// <summary>
    /// Commits the current splitter ratio to the shell-owned layout state. The pointer-release handler calls it; the
    /// headless test calls it after realizing a layout so the drag commit path is exercised without a real pointer.
    /// </summary>
    internal void CommitSplitterFraction()
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
            viewModel.Editor.RequestRemove();
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

    private void OnSplitterReleased(object? sender, PointerReleasedEventArgs args) => CommitSplitterFraction();

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
                if (!string.IsNullOrWhiteSpace(pending) && TableSurface.FocusDetailField(pending))
                {
                    viewModel.Editor.ConsumePendingFocus();
                }
            });
    }

    private void FocusInitial() => TableSurface.Rows.Focus();
}

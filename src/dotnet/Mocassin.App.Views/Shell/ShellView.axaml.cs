// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Shell layout control. It applies the proportional star weights from the view model and re-captures the
/// fractions after a completed splitter gesture.
/// </summary>
internal sealed partial class ShellView : UserControl
{
    private const double projectsMinimum = 160d;
    private const double centreMinimum = 360d;
    private const double documentsMinimum = 200d;
    private const double consoleMinimum = 80d;

    private ShellRegionsViewModel? regions;

    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ShellView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        foreach (var splitter in new[] { ProjectsSplitter, ConsoleSplitter, InspectorSplitter })
        {
            splitter.DragCompleted += OnSplitterDragCompleted;
            splitter.AddHandler(KeyUpEvent, OnSplitterKeyUp, RoutingStrategies.Bubble, handledEventsToo: true);
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (regions is not null)
        {
            regions.PropertyChanged -= OnRegionsChanged;
        }

        regions = (DataContext as ShellViewModel)?.Regions;
        if (regions is not null)
        {
            regions.PropertyChanged += OnRegionsChanged;
        }

        ApplyLayout();
        if (DataContext is ShellViewModel shell)
        {
            MenuBuilder.Apply(MainMenu, shell.Menus, shell.Platform);
        }
    }

    private void OnRegionsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellRegionsViewModel.IsProjectsVisible)
            or nameof(ShellRegionsViewModel.IsInspectorVisible)
            or nameof(ShellRegionsViewModel.IsConsoleVisible)
            or nameof(ShellRegionsViewModel.LeftFraction)
            or nameof(ShellRegionsViewModel.RightFraction)
            or nameof(ShellRegionsViewModel.ConsoleFraction))
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        if (regions is null)
        {
            return;
        }

        var projectsVisible = regions.IsProjectsVisible;
        var inspectorVisible = regions.IsInspectorVisible;
        var consoleVisible = regions.IsConsoleVisible;
        SetColumn(0, projectsVisible ? regions.LeftFraction : 0d, projectsVisible ? projectsMinimum : 0d);
        SetColumn(2, regions.CentreFraction, centreMinimum);
        SetColumn(4, inspectorVisible ? regions.RightFraction : 0d, inspectorVisible ? projectsMinimum : 0d);
        SetRow(0, consoleVisible ? regions.DocumentsFraction : 1d, consoleVisible ? documentsMinimum : 0d);
        SetRow(2, consoleVisible ? regions.ConsoleFraction : 0d, consoleVisible ? consoleMinimum : 0d);
    }

    private void SetColumn(int index, double fraction, double minimum)
    {
        if (index >= WorkspaceGrid.ColumnDefinitions.Count)
        {
            return;
        }

        var definition = WorkspaceGrid.ColumnDefinitions[index];
        definition.Width = new GridLength(Math.Max(0d, fraction), GridUnitType.Star);
        definition.MinWidth = Math.Max(0d, minimum);
    }

    private void SetRow(int index, double fraction, double minimum)
    {
        if (index >= CentreGrid.RowDefinitions.Count)
        {
            return;
        }

        var definition = CentreGrid.RowDefinitions[index];
        definition.Height = new GridLength(Math.Max(0d, fraction), GridUnitType.Star);
        definition.MinHeight = Math.Max(0d, minimum);
    }

    private void OnSplitterDragCompleted(object? sender, VectorEventArgs e) => PostCaptureFractions();

    private void OnSplitterKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            PostCaptureFractions();
        }
    }

    private void PostCaptureFractions() =>
        Dispatcher.UIThread.Post(CaptureFractions, DispatcherPriority.Loaded);

    private void CaptureFractions()
    {
        if (regions is null)
        {
            return;
        }

        var left = WorkspaceGrid.ColumnDefinitions[0].ActualWidth;
        var centre = WorkspaceGrid.ColumnDefinitions[2].ActualWidth;
        var right = WorkspaceGrid.ColumnDefinitions[4].ActualWidth;
        if (regions.IsProjectsVisible && regions.IsInspectorVisible)
        {
            var total = left + centre + right;
            regions.LeftFraction = ShellLayoutFractions.Share(left, total, regions.LeftFraction);
            regions.RightFraction = ShellLayoutFractions.Share(right, total, regions.RightFraction);
        }
        else if (regions.IsProjectsVisible)
        {
            regions.LeftFraction = ShellLayoutFractions.Share(left, left + centre, regions.LeftFraction);
        }
        else if (regions.IsInspectorVisible)
        {
            regions.RightFraction = ShellLayoutFractions.Share(right, centre + right, regions.RightFraction);
        }

        if (!regions.IsConsoleVisible)
        {
            return;
        }

        var documents = CentreGrid.RowDefinitions[0].ActualHeight;
        var console = CentreGrid.RowDefinitions[2].ActualHeight;
        regions.ConsoleFraction = ShellLayoutFractions.Share(console, documents + console, regions.ConsoleFraction);
    }
}

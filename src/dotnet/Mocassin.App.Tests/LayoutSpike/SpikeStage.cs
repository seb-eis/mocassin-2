// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Mocassin.App.Tests.LayoutSpike.Options;

using Mocassin.App.Tests.LayoutSpike.Editing;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// The composition shared by every option: a spike notice, the search/filter/sort toolbar, the option's selection
/// surface, and a glyph-plus-text status bar.
/// </summary>
/// <remarks>
/// The toolbar is deliberately shared. It is view-model wiring that every layout must repeat, and keeping it out of
/// the option code makes the recorded size difference the cost of the selection surface itself.
/// </remarks>
internal sealed class SpikeStage
{
    private readonly ContentControl toolbarHost = new();
    private readonly ContentControl surfaceHost = new();
    private readonly TextBlock statusText;
    private SpikeDomainViewModel? observedDomain;
    private SpikeEditorViewModel? observedEditor;

    /// <summary>
    /// Initializes the stage.
    /// </summary>
    /// <param name="option">The option to build.</param>
    /// <param name="workspace">The shared workspace.</param>
    public SpikeStage(SpikeOption option, SpikeWorkspaceViewModel workspace)
    {
        Option = option;
        Workspace = workspace;
        statusText = new TextBlock
        {
            Foreground = SpikeTheme.Brush("Mocassin.StatusBarForeground"),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
        };
        AutomationProperties.SetName(statusText, "Spike status");
        Root = BuildRoot();
        Root.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Workspace.PropertyChanged += OnWorkspaceChanged;
        ObserveActiveDomain();
        RebuildToolbar();
        surfaceHost.Content = CreateSurface().Build();
        RefreshStatus();
    }

    /// <summary>Gets the option.</summary>
    public SpikeOption Option { get; }

    /// <summary>Gets the shared workspace.</summary>
    public SpikeWorkspaceViewModel Workspace { get; }

    /// <summary>Gets the stage root.</summary>
    public Grid Root { get; }

    /// <summary>Gets the window title.</summary>
    public string Title => $"Step 24 layout spike — {SpikeOptionText.Display(Option)}";

    private Grid BuildRoot()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            Background = SpikeTheme.Brush("Mocassin.ChromeBackground"),
        };

        var title = new TextBlock
        {
            Text = SpikeOptionText.Display(Option),
            FontSize = SpikeTheme.Double("Mocassin.TitleFontSize", 20),
            FontWeight = FontWeight.SemiBold,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundStrongBrush"),
        };
        var notice = new TextBlock
        {
            Text = "Step 24 layout-spike POC · spike code, discarded after the ADR 0003 pick · "
                + "real ceria samples plus generated 1,024-row breadth",
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
        };
        var header = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.ChromeBackground"),
            BorderBrush = SpikeTheme.Brush("Mocassin.BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 8),
            Child = new StackPanel { Spacing = 2, Children = { title, notice } },
        };
        AutomationProperties.SetName(header, "Spike notice");
        Grid.SetRow(header, 0);
        grid.Children.Add(header);

        Grid.SetRow(toolbarHost, 1);
        grid.Children.Add(toolbarHost);

        Grid.SetRow(surfaceHost, 2);
        grid.Children.Add(surfaceHost);

        var status = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.StatusBarBackground"),
            Padding = new Thickness(10, 3),
            MinHeight = SpikeTheme.Double("Mocassin.StatusBarHeight", 24),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "◆",
                        Foreground = SpikeTheme.Brush("Mocassin.StatusBarForeground"),
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
                    },
                    statusText,
                },
            },
        };
        AutomationProperties.SetName(status, "Spike status bar");
        Grid.SetRow(status, 3);
        grid.Children.Add(status);
        return grid;
    }

    private SpikeOptionSurface CreateSurface() => Option switch
    {
        SpikeOption.ListDetail => new ListDetailOption(Workspace),
        SpikeOption.TreeDetail => new TreeDetailOption(Workspace),
        SpikeOption.Tabs => new TabsOption(Workspace),
        SpikeOption.SplitPane => new SplitPaneOption(Workspace),
        SpikeOption.CustomTable => new CustomTableOption(Workspace),
        _ => throw new ArgumentOutOfRangeException(nameof(Option), Option, "Unknown spike option."),
    };

    private void RebuildToolbar()
    {
        var domain = Workspace.ActiveDomain;
        var panel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 6),
        };
        panel.Children.Add(Label("Domain"));
        var domainCombo = new ComboBox
        {
            ItemsSource = Workspace.Domains,
            SelectedItem = domain,
            MinWidth = 150,
            Margin = new Thickness(0, 0, 10, 4),
            ItemTemplate = new FuncDataTemplate<SpikeDomainViewModel>(
                (value, _) => new TextBlock { Text = value.Title }),
        };
        AutomationProperties.SetName(domainCombo, "Domain");
        domainCombo.SelectionChanged += (_, _) =>
        {
            if (domainCombo.SelectedItem is SpikeDomainViewModel selected)
            {
                Workspace.ActiveDomain = selected;
            }
        };
        panel.Children.Add(domainCombo);

        panel.Children.Add(Label("Search"));
        var search = new TextBox
        {
            Text = domain.SearchText,
            PlaceholderText = "Search rows",
            Width = 170,
            Margin = new Thickness(0, 0, 10, 4),
        };
        AutomationProperties.SetName(search, "Search rows");
        search.TextChanged += (_, _) => domain.SearchText = search.Text ?? string.Empty;
        panel.Children.Add(search);

        panel.Children.Add(Label("Filter"));
        var filter = new ComboBox
        {
            ItemsSource = domain.Filters,
            SelectedItem = domain.Filter,
            MinWidth = 140,
            Margin = new Thickness(0, 0, 10, 4),
            ItemTemplate = new FuncDataTemplate<SpikeFilterOption>(
                (value, _) => new TextBlock { Text = value.Label }),
        };
        AutomationProperties.SetName(filter, "Row filter");
        filter.SelectionChanged += (_, _) =>
        {
            if (filter.SelectedItem is SpikeFilterOption selected)
            {
                domain.Filter = selected;
            }
        };
        panel.Children.Add(filter);

        panel.Children.Add(Label("Sort"));
        var sort = new ComboBox
        {
            ItemsSource = domain.SortOptions,
            SelectedItem = domain.SortHeader,
            MinWidth = 120,
            Margin = new Thickness(0, 0, 6, 4),
        };
        AutomationProperties.SetName(sort, "Sort rows");
        sort.SelectionChanged += (_, _) =>
        {
            if (sort.SelectedItem is string selected)
            {
                domain.SortHeader = selected;
            }
        };
        panel.Children.Add(sort);
        var descending = new CheckBox
        {
            Content = "Descending",
            IsChecked = domain.SortDescending,
            Margin = new Thickness(0, 0, 10, 4),
        };
        AutomationProperties.SetName(descending, "Descending sort");
        descending.IsCheckedChanged += (_, _) => domain.SortDescending = descending.IsChecked == true;
        panel.Children.Add(descending);

        var breadth = new CheckBox
        {
            Content = "Generated breadth (1,024 sites)",
            IsChecked = Workspace.GeneratedBreadth,
            IsEnabled = Workspace.HasGeneratedBreadth,
            Margin = new Thickness(0, 0, 10, 4),
        };
        AutomationProperties.SetName(breadth, "Generated breadth (1,024 sites)");
        AutomationProperties.SetHelpText(
            breadth,
            "Replaces the particle and site rows with the generated 1,024-row breadth value.");
        breadth.IsCheckedChanged += (_, _) => Workspace.GeneratedBreadth = breadth.IsChecked == true;
        panel.Children.Add(breadth);

        var remove = new Button
        {
            Content = "Remove selected",
            Command = domain.RemoveSelectedCommand,
            Margin = new Thickness(0, 0, 6, 4),
        };
        AutomationProperties.SetName(remove, "Remove selected");
        AutomationProperties.SetHelpText(remove, "Bulk delete is offered only for particles, sets, and sites.");
        panel.Children.Add(remove);

        var clear = new Button
        {
            Content = "Clear filters",
            Margin = new Thickness(0, 0, 0, 4),
        };
        AutomationProperties.SetName(clear, "Clear filters");
        clear.Click += (_, _) =>
        {
            domain.ClearFilters();
            RebuildToolbar();
        };
        panel.Children.Add(clear);

        toolbarHost.Content = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.WorkspaceBackground"),
            BorderBrush = SpikeTheme.Brush("Mocassin.BorderBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = panel,
        };
    }

    private void OnWorkspaceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SpikeWorkspaceViewModel.ActiveDomain)
            or nameof(SpikeWorkspaceViewModel.GeneratedBreadth))
        {
            ObserveActiveDomain();
            RebuildToolbar();
            RefreshStatus();
        }
    }

    private void ObserveActiveDomain()
    {
        if (observedDomain is not null)
        {
            observedDomain.PropertyChanged -= OnObservedChanged;
        }

        if (observedEditor is not null)
        {
            observedEditor.PropertyChanged -= OnObservedChanged;
        }

        observedDomain = Workspace.ActiveDomain;
        observedEditor = Workspace.Editor;
        observedDomain.PropertyChanged += OnObservedChanged;
        observedEditor.PropertyChanged += OnObservedChanged;
    }

    private void OnObservedChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SpikeDomainViewModel.StatusText)
            or nameof(SpikeEditorViewModel.StatusText)
            or nameof(SpikeEditorViewModel.ErrorText))
        {
            RefreshStatus();
        }
    }

    private void RefreshStatus() => statusText.Text = Workspace.StatusText;

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        var focused = TopLevel.GetTopLevel(this.Root)?.FocusManager?.GetFocusedElement();
        var editor = Workspace.Editor;
        Console.WriteLine($"KEY {args.Key} mods={args.KeyModifiers} focused={focused?.GetType().Name}");
        switch (args.Key)
        {
            case Key.Enter when focused is not ComboBox:
                editor.ApplyCommand.Execute(null);
                args.Handled = true;
                break;
            case Key.Escape when focused is not ComboBox:
                editor.RevertCommand.Execute(null);
                args.Handled = true;
                break;
        }
    }

    private static TextBlock Label(string text) =>
        new()
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 4),
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
        };
}

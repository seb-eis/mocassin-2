// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;

using Mocassin.App.Tests.LayoutSpike.Data;
using Mocassin.App.Tests.LayoutSpike.Editing;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 2: a hierarchy of model domains with dense table rows inside the tree and a detail editor.
/// </summary>
/// <remarks>
/// The roots are real navigation (particles, structure, transitions); the children are the same dense rows as the
/// table. Avalonia's TreeView realizes the expanded children eagerly, which is exactly the virtualization weakness
/// this option must demonstrate.
/// </remarks>
internal sealed class TreeDetailOption : SpikeOptionSurface
{
    private readonly ContentControl headerHost = new();
    private readonly TreeView tree = new();
    private readonly ContentControl editorHost = new();
    private bool rebuilding;
    private bool syncing;
    private string? pendingKey;

    /// <summary>
    /// Initializes the option.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    public TreeDetailOption(SpikeWorkspaceViewModel workspace)
        : base(workspace)
    {
    }

    /// <inheritdoc />
    public override Control Build()
    {
        tree.ItemsSource = Workspace.Domains;
        tree.Loaded += (_, _) => RefreshTree();
        tree.DataTemplates.Add(
            new FuncTreeDataTemplate<SpikeDomainViewModel>(
                (domain, _) => DomainHeader(domain),
                domain => domain.Items));
        tree.DataTemplates.Add(
            new FuncTreeDataTemplate<SpikeRow>(
                (row, _) => SpikeColumnLayout.BuildRow(row, SpikeColumns.For(row.Domain)),
                _ => Array.Empty<SpikeRow>()));
        AutomationProperties.SetName(tree, "Model tree");
        AutomationProperties.SetAutomationId(tree, "SpikeModelTree");
        AutomationProperties.SetHelpText(
            tree,
            "Domain roots expand to dense rows; arrow keys navigate the expanded hierarchy.");
        tree.Classes.Add("scroll-gutter-right");
        tree.SelectionChanged += OnTreeSelectionChanged;
        foreach (var domain in Workspace.Domains)
        {
            domain.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SpikeDomainViewModel.StatusText)
                    && ReferenceEquals(domain, Workspace.ActiveDomain))
                {
                    RefreshTree();
                }
            };
        }

        Workspace.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(SpikeWorkspaceViewModel.ActiveDomain)
                or nameof(SpikeWorkspaceViewModel.GeneratedBreadth))
            {
                RefreshTree();
            }
        };

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,8,380"),
        };
        var treeHost = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        treeHost.Children.Add(headerHost);
        Grid.SetRow(tree, 1);
        treeHost.Children.Add(tree);
        body.Children.Add(treeHost);
        Grid.SetColumn(editorHost, 2);
        body.Children.Add(editorHost);

        var root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Margin = new Thickness(8),
        };
        root.Children.Add(body);
        RefreshTree();
        editorHost.Content = new SpikeEditorView { DataContext = Workspace.Editor };
        return root;
    }

    private void RefreshTree()
    {
        if (rebuilding)
        {
            return;
        }

        rebuilding = true;
        try
        {
            var domain = Workspace.ActiveDomain;
            headerHost.Content = SpikeColumnLayout.BuildHeader(domain.Columns, sort: null);
            foreach (var candidate in Workspace.Domains)
            {
                if (Container(candidate) is { } container)
                {
                    container.IsExpanded = ReferenceEquals(candidate, domain);
                }
            }

            if (pendingKey is not null)
            {
                var row = domain.Items.FirstOrDefault(candidate => candidate.Key == pendingKey);
                if (row is not null && Container(row) is { } item)
                {
                    tree.SelectedItem = row;
                    item.BringIntoView();
                }
                else
                {
                    tree.SelectedItem = null;
                }
            }

            if (tree.SelectedItem is SpikeRow selectedRow)
            {
                domain.SetSelection([selectedRow]);
            }

            editorHost.Content = new SpikeEditorView { DataContext = Workspace.Editor };
        }
        finally
        {
            rebuilding = false;
        }
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (rebuilding || syncing)
        {
            return;
        }

        switch (tree.SelectedItem)
        {
            case SpikeRow row:
                pendingKey = row.Key;
                var domain = Workspace.Domains.First(candidate => candidate.Domain == row.Domain);
                syncing = true;
                try
                {
                    if (!ReferenceEquals(Workspace.ActiveDomain, domain))
                    {
                        Workspace.ActiveDomain = domain;
                        RefreshTree();
                    }

                    domain.SetSelection([row]);
                }
                finally
                {
                    syncing = false;
                }

                break;
            case SpikeDomainViewModel domainRoot:
                Workspace.ActiveDomain = domainRoot;
                break;
        }
    }

    private TreeViewItem? Container(object item) =>
        tree.GetVisualDescendants()
            .OfType<TreeViewItem>()
            .FirstOrDefault(container => ReferenceEquals(container.DataContext, item));

    private static TextBlock DomainHeader(SpikeDomainViewModel domain) =>
        new()
        {
            Text = $"{domain.Title} ({domain.Items.Count})",
            FontWeight = FontWeight.SemiBold,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundStrongBrush"),
        };
}

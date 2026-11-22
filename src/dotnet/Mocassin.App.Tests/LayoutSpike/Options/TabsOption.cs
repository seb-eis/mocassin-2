// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Mocassin.App.Tests.LayoutSpike.Editing;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Options;

/// <summary>
/// Option 3: one tab per domain, each tab containing its own list and detail editor.
/// </summary>
/// <remarks>
/// The outer tab strip emulates the shell's document tabs. The spike cannot add a non-production view type to the
/// shell's <c>ViewLocator</c>, so the two-level tab cost is measured in a stand-in strip with the same nesting.
/// </remarks>
internal sealed class TabsOption : SpikeOptionSurface
{
    private readonly TabControl documentTabs = new();
    private readonly TabControl domainTabs = new();
    private bool syncing;

    /// <summary>
    /// Initializes the option.
    /// </summary>
    /// <param name="workspace">The shared workspace.</param>
    public TabsOption(SpikeWorkspaceViewModel workspace)
        : base(workspace)
    {
    }

    /// <inheritdoc />
    public override Control Build()
    {
        domainTabs.Items.Clear();
        foreach (var domain in Workspace.Domains)
        {
            var tab = new TabItem
            {
                Header = domain.Title,
                Content = BuildDomainContent(domain),
                Tag = domain,
            };
            AutomationProperties.SetName(tab, $"{domain.Title} tab");
            domainTabs.Items.Add(tab);
        }

        AutomationProperties.SetName(domainTabs, "Domain tabs (nested)");
        AutomationProperties.SetHelpText(
            domainTabs,
            "Six domain tabs nested below the emulated document tab strip.");
        domainTabs.SelectedItem = domainTabs.Items
            .OfType<TabItem>()
            .FirstOrDefault(tab => ReferenceEquals(tab.Tag, Workspace.ActiveDomain));
        domainTabs.SelectionChanged += OnDomainTabChanged;

        documentTabs.Items.Clear();
        documentTabs.Items.Add(new TabItem
        {
            Header = "Ceria model",
            Content = domainTabs,
        });
        documentTabs.Items.Add(new TabItem
        {
            Header = "Overview",
            Content = new TextBlock
            {
                Text = "The overview document is a placeholder that makes the emulated document tab strip visible.",
                Margin = new Thickness(16),
                TextWrapping = TextWrapping.Wrap,
                Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            },
        });
        documentTabs.SelectedIndex = 0;
        AutomationProperties.SetName(documentTabs, "Document tabs (emulated)");
        AutomationProperties.SetHelpText(
            documentTabs,
            "Emulates the shell's document tabs so the nested domain tab level is present in the capture.");

        Workspace.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SpikeWorkspaceViewModel.ActiveDomain)
                && domainTabs.SelectedItem is TabItem selected
                && !ReferenceEquals(selected.Tag, Workspace.ActiveDomain))
            {
                syncing = true;
                try
                {
                    domainTabs.SelectedItem = domainTabs.Items
                        .OfType<TabItem>()
                        .FirstOrDefault(tab => ReferenceEquals(tab.Tag, Workspace.ActiveDomain));
                }
                finally
                {
                    syncing = false;
                }
            }
        };

        var root = new Grid { Margin = new Thickness(8) };
        root.Children.Add(documentTabs);
        return root;
    }

    private static Grid BuildDomainContent(SpikeDomainViewModel domain)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,8,340") };
        var list = new SpikeListSurface(domain, headerSort: false);
        var editor = new SpikeEditorView { DataContext = domain.Editor };
        Grid.SetColumn(editor, 2);
        grid.Children.Add(list);
        grid.Children.Add(editor);
        return grid;
    }

    private void OnDomainTabChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (syncing)
        {
            return;
        }

        if (domainTabs.SelectedItem is TabItem { Tag: SpikeDomainViewModel domain })
        {
            Workspace.ActiveDomain = domain;
        }
    }
}

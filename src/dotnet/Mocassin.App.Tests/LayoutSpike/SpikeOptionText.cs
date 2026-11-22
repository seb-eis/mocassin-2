// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// Human-readable option labels used by the stage heading, the capture names, and the report.
/// </summary>
internal static class SpikeOptionText
{
    /// <summary>
    /// Gets the option label.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <returns>The label.</returns>
    public static string Display(SpikeOption option) => option switch
    {
        SpikeOption.ListDetail => "1 · ListBox table + detail",
        SpikeOption.TreeDetail => "2 · TreeView + detail",
        SpikeOption.Tabs => "3 · TabControl + list + detail",
        SpikeOption.SplitPane => "4 · GridSplitter master-detail",
        SpikeOption.CustomTable => "5 · Custom TableView control",
        _ => option.ToString(),
    };

    /// <summary>
    /// Gets the short slug used in capture file names.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <returns>The slug.</returns>
    public static string Slug(SpikeOption option) => option switch
    {
        SpikeOption.ListDetail => "01-list-detail",
        SpikeOption.TreeDetail => "02-tree-detail",
        SpikeOption.Tabs => "03-tabs",
        SpikeOption.SplitPane => "04-split-pane",
        SpikeOption.CustomTable => "05-custom-table",
        _ => option.ToString().ToLowerInvariant(),
    };
}

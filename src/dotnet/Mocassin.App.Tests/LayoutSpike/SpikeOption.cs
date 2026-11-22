// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// The five layout options the Step 24 spike builds. Option 6 (the custom matrix control) is a data-shape control
/// inside these layouts, not a sixth option.
/// </summary>
public enum SpikeOption
{
    /// <summary>ListBox table plus detail editor (the ADR 0003 baseline).</summary>
    ListDetail,

    /// <summary>TreeView hierarchy plus detail editor.</summary>
    TreeDetail,

    /// <summary>TabControl per domain, each with a list and detail editor.</summary>
    Tabs,

    /// <summary>Resizable GridSplitter master-detail.</summary>
    SplitPane,

    /// <summary>Custom TemplatedControl table plus detail editor.</summary>
    CustomTable,
}

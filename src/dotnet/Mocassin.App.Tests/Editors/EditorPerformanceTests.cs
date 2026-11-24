// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Sites;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Symmetry;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Functional performance evidence only (no absolute duration): realized row containers are bounded and do not grow
/// with the row count, every row is present and the last row is selectable, rapid edits coalesce into one
/// preview with a fake clock, and no row is dropped after a rebuild.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorPerformanceTests
{
    [Fact]
    public async Task RealizedRowContainersAreBoundedAndDoNotGrowWithTheRowCount()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var small = BuildEditor(entry, 100, out var smallWindow, out var smallView);
                try
                {
                    HeadlessSession.Pump(smallWindow);
                    var realizedSmall = RealizedRows(smallView);
                    Assert.Equal(100, small.Table.TotalCount);
                    Assert.Equal(100, small.Table.Rows.Count);
                    Assert.True(realizedSmall < 100, $"realized={realizedSmall} for 100 rows");
                    Assert.True(small.RequestSelection(small.Table.Rows[^1].ObjectId));

                    var large = BuildEditor(entry, 1_000, out var largeWindow, out var largeView);
                    try
                    {
                        HeadlessSession.Pump(largeWindow);
                        var realizedLarge = RealizedRows(largeView);
                        Assert.Equal(1_000, large.Table.TotalCount);
                        Assert.Equal(1_000, large.Table.Rows.Count);
                        Assert.True(realizedLarge < 1_000, $"realized={realizedLarge} for 1,000 rows");
                        Assert.True(
                            Math.Abs(realizedLarge - realizedSmall) <= 5,
                            $"100 rows realized {realizedSmall}, 1,000 rows realized {realizedLarge}");
                        Assert.True(large.RequestSelection(large.Table.Rows[^1].ObjectId));
                        HeadlessSession.Pump(largeWindow);
                        largeView.Table.Rows.ScrollIntoView(large.Table.SelectedRow!);
                        HeadlessSession.Pump(largeWindow);
                        Assert.Equal(1_000, large.Table.TotalCount);
                        Assert.Equal(large.Table.Rows[^1].ObjectId, large.Table.SelectedRow?.ObjectId);
                    }
                    finally
                    {
                        largeWindow.Close();
                        large.Dispose();
                    }
                }
                finally
                {
                    smallWindow.Close();
                    small.Dispose();
                }
            });
    }

    [Fact]
    public async Task RapidEditsCoalesceIntoOnePreviewBeforeTheDebounce()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var clock = new ManualTimeProvider();
        var dispatcher = new RecordingUiDispatcher();
        using var editor = new SitesEditorViewModel(
            entry.Session,
            new ShellRegionsViewModel(),
            new StructurePreviewService(),
            new StructureValidator(),
            clock,
            dispatcher);
        var site = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O");
        Assert.True(editor.RequestSelection(site.Id.Value));
        var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);

        detail.FieldA = "0.11";
        detail.FieldB = "0.12";
        detail.FieldC = "0.13";
        clock.Advance(TimeSpan.FromMilliseconds(200));
        Assert.Equal(0, dispatcher.InvokeCount);

        clock.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(1, dispatcher.InvokeCount);
        Assert.Equal("0.13", detail.FieldC);
    }

    private static SitesEditorViewModel BuildEditor(
        ProjectWorkspaceEntry entry,
        int siteCount,
        out Window window,
        out ModelEditorDocumentView view)
    {
        var p1 = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None);
        var cell = new Mocassin.Core.Model.CellParameters(
            new Mocassin.Core.Units.LengthAngstrom(5d),
            new Mocassin.Core.Units.LengthAngstrom(5d),
            new Mocassin.Core.Units.LengthAngstrom(5d),
            new Mocassin.Core.Units.AngleDegrees(90d),
            new Mocassin.Core.Units.AngleDegrees(90d),
            new Mocassin.Core.Units.AngleDegrees(90d));
        var set = entry.Project.Model.ParticleSets[0];
        var sites = Enumerable.Range(0, siteCount)
            .Select(index => new CellSite(
                CellSiteId.New(),
                $"S{index}",
                new Mocassin.Core.Geometry.Fractional3((index + 0.5d) / siteCount, 0.13, 0.27),
                set.Id,
                SiteStability.Stable))
            .ToImmutableArray();
        var structure = entry.Project.Structure with
        {
            SpaceGroup = p1,
            Cell = cell,
            Sites = sites,
            DummyPositions = [],
        };
        var project = entry.Project with { Model = entry.Project.Model with { Structure = structure } };
        var session = ProjectSession.CreateStandalone(project, $"performance-{siteCount}.mocprj");
        var editor = new SitesEditorViewModel(
            session,
            new ShellRegionsViewModel(),
            new StructurePreviewService(),
            new StructureValidator());
        var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Sites, editor);
        view = new ModelEditorDocumentView { DataContext = document };
        window = new Window { Width = 900, Height = 260, Content = view };
        window.Show();
        return editor;
    }

    private static int RealizedRows(ModelEditorDocumentView view) =>
        view.Table.Rows.GetVisualDescendants().OfType<ListBoxItem>().Count();
}

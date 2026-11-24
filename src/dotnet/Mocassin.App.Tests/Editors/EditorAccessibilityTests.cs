// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Sites;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the editor accessibility contract: every interactive control has a name, the table exposes a list role,
/// severity is announced as text, and the splitter is named, focusable, keyboard-resizable, and persisted.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorAccessibilityTests
{
    [Fact]
    public async Task EveryInteractiveControlHasAnAccessibleName()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var layout = new ShellRegionsViewModel();
                using var particles = new ParticlesEditorViewModel(entry.Session, layout);
                var particleWindow = ShowDocument(ModelEditorDomains.Particles, particles);
                HeadlessSession.Pump(particleWindow);
                try
                {
                    AssertAllControlsNamed(particleWindow);
                }
                finally
                {
                    particleWindow.Close();
                }

                using var sites = new SitesEditorViewModel(
                    entry.Session,
                    layout,
                    new StructurePreviewService(),
                    new StructureValidator());
                Assert.True(sites.RequestSelection(entry.Project.Structure.Sites[0].Id.Value));
                var siteWindow = ShowDocument(ModelEditorDomains.Sites, sites);
                HeadlessSession.Pump(siteWindow);
                try
                {
                    AssertAllControlsNamed(siteWindow);
                }
                finally
                {
                    siteWindow.Close();
                }

                using var structure = new StructureEditorViewModel(
                    entry.Session,
                    layout,
                    new StructurePreviewService(),
                    new StructureValidator());
                var document = new StructureEditorDocumentViewModel(
                    ModelEditorDomains.StructureEditor,
                    structure);
                var structureView = new StructureEditorDocumentView { DataContext = document };
                var structureWindow = new Window { Width = 1000, Height = 900, Content = structureView };
                structureWindow.Show();
                HeadlessSession.Pump(structureWindow);
                try
                {
                    AssertAllControlsNamed(structureWindow);
                }
                finally
                {
                    structureWindow.Close();
                }
            });
    }

    [Fact]
    public async Task TheTableExposesAListRole()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var layout = new ShellRegionsViewModel();
                using var editor = new ParticlesEditorViewModel(entry.Session, layout);
                var window = ShowDocument(ModelEditorDomains.Particles, editor);
                HeadlessSession.Pump(window);
                try
                {
                    var list = window.GetVisualDescendants().OfType<ListBox>()
                        .Single(box => AutomationProperties.GetName(box) == "Model table rows");
                    var peer = ControlAutomationPeer.CreatePeerForElement(list);
                    Assert.NotNull(peer);
                    Assert.Equal(AutomationControlType.List, peer!.GetAutomationControlType());
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task TheEditorSplitterIsNamedFocusableAndKeyboardResizable()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var layout = new ShellRegionsViewModel();
                using var editor = new ParticlesEditorViewModel(entry.Session, layout);
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Particles, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    var splitter = view.Splitter;
                    Assert.Equal("Resize editor panes", AutomationProperties.GetName(splitter));
                    Assert.Contains("arrow keys", AutomationProperties.GetHelpText(splitter), StringComparison.Ordinal);
                    Assert.True(splitter.Focusable);

                    splitter.Focus();
                    HeadlessSession.Pump(window);
                    var before = view.MasterDetail.ColumnDefinitions[0].ActualWidth;
                    window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
                    HeadlessSession.Pump(window);

                    Assert.NotEqual(before, view.MasterDetail.ColumnDefinitions[0].ActualWidth);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task TheSplitterCommitPersistsThePaneFraction()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var layout = new ShellRegionsViewModel();
                using var editor = new ParticlesEditorViewModel(entry.Session, layout);
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Particles, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    view.MasterDetail.ColumnDefinitions[0].Width = new GridLength(0.62d, GridUnitType.Star);
                    view.MasterDetail.ColumnDefinitions[2].Width = new GridLength(0.38d, GridUnitType.Star);
                    HeadlessSession.Pump(window);

                    view.CommitSplitterFraction();

                    Assert.Equal(0.62d, layout.EditorFraction, 2);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public void SeverityIsTextNotColour()
    {
        var row = new ModelTableRow(Guid.NewGuid(), [], ModelRowSeverity.Error, "Error");

        Assert.Equal("Error", row.SeverityText);
        Assert.True(row.HasError);
        Assert.True(row.HasWarning);
    }

    private static Window ShowDocument(ModelEditorDescriptor descriptor, ModelEditorViewModel editor)
    {
        var document = new ModelEditorDocumentViewModel(descriptor, editor);
        var view = new ModelEditorDocumentView { DataContext = document };
        var window = new Window { Width = 1000, Height = 700, Content = view };
        window.Show();
        return window;
    }

    private static void AssertAllControlsNamed(Avalonia.Visual root)
    {
        var interactive = root.GetVisualDescendants()
            .OfType<Control>()
            .Where(static control => control.TemplatedParent is null)
            .Where(static control => control is Button or TextBox or ComboBox or CheckBox or ListBox or GridSplitter)
            .ToArray();
        Assert.NotEmpty(interactive);
        foreach (var control in interactive)
        {
            var name = AutomationProperties.GetName(control);
            Assert.False(
                string.IsNullOrWhiteSpace(name),
                $"{control.GetType().Name} '{control.Name}' has no accessible name.");
        }
    }
}

// SPDX-License-Identifier: MIT

using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Sites;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors;
using Mocassin.App.Views.Editors.Structure;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the document-level editor gestures: <c>Escape</c> reverts, <c>Delete</c> asks for confirmation,
/// <c>Enter</c> applies the focused form, and the selection surface remains the one tab stop.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorKeyboardTests
{
    [Fact]
    public async Task EscapeRevertsTheOpenForm()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Particles, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    var particle = entry.Project.Particles.First(candidate => candidate.Name == "O2-");
                    Assert.True(editor.RequestSelection(particle.Id.Value));
                    HeadlessSession.Pump(window);
                    var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);
                    var committed = detail.Name;
                    detail.Name = "Draft name";
                    view.Table.Rows.Focus();
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);

                    Assert.Equal(committed, detail.Name);
                    Assert.False(detail.HasUncommittedChanges);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task DeleteAsksForConfirmationBeforeRemoving()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new ParticlesEditorViewModel(entry.Session, new ShellRegionsViewModel());
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Particles, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    editor.AddCommand.Execute(null);
                    var originalCount = entry.Session.Project.Particles.Length;
                    HeadlessSession.Pump(window);
                    view.Table.Rows.Focus();
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, null);
                    HeadlessSession.Pump(window);

                    Assert.True(editor.HasPendingRemove);
                    Assert.Equal(originalCount, entry.Session.Project.Particles.Length);

                    var cancel = view.GetVisualDescendants().OfType<Button>()
                        .Single(button => AutomationProperties.GetName(button) == "Cancel removal");
                    cancel.Command?.Execute(null);
                    HeadlessSession.Pump(window);
                    Assert.False(editor.HasPendingRemove);
                    Assert.Equal(originalCount, entry.Session.Project.Particles.Length);

                    window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, null);
                    HeadlessSession.Pump(window);
                    Assert.True(editor.HasPendingRemove);
                    var confirm = view.GetVisualDescendants().OfType<Button>()
                        .Single(button => AutomationProperties.GetName(button) == "Confirm removal");
                    confirm.Command?.Execute(null);
                    HeadlessSession.Pump(window);

                    Assert.False(editor.HasPendingRemove);
                    Assert.Equal(originalCount - 1, entry.Session.Project.Particles.Length);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task EnterAppliesTheFocusedSiteForm()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new SitesEditorViewModel(
                    entry.Session,
                    new ShellRegionsViewModel(),
                    new StructurePreviewService(),
                    new StructureValidator());
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Sites, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    var site = entry.Project.Structure.Sites.First(candidate => candidate.Name == "O");
                    Assert.True(editor.RequestSelection(site.Id.Value));
                    HeadlessSession.Pump(window);
                    var detail = Assert.IsType<CellSiteDetailViewModel>(editor.Detail);
                    var undoDepth = entry.Session.Document.UndoStack.Length;
                    detail.Name = "O renamed";

                    var name = view.GetVisualDescendants().OfType<TextBox>()
                        .Single(box => AutomationProperties.GetName(box) == "Site name");
                    name.Focus();
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);

                    Assert.Equal("O renamed", entry.Session.Project.Find(site.Id)?.Name);
                    Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task EnterAppliesTheCellParameterForm()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var original = entry.Session.Project.Structure.Cell.A.Value;
        var updated = (original + 0.01d).ToString(CultureInfo.InvariantCulture);
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var form = new CellParameterEditorViewModel(entry.Session, new StructurePreviewService());
                var view = new CellParametersView { DataContext = form };
                var window = new Window { Width = 900, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    var undoDepth = entry.Session.Document.UndoStack.Length;
                    form.Fields[0].Text = updated;
                    var field = view.GetVisualDescendants().OfType<TextBox>()
                        .Single(box => AutomationProperties.GetName(box) == "a");
                    field.Focus();
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);

                    Assert.Equal(double.Parse(updated, CultureInfo.InvariantCulture),
                        entry.Session.Project.Structure.Cell.A.Value, 6);
                    Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
                }
                finally
                {
                    window.Close();
                }
            });
    }
}

// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the read-only contract: the 23B banner is rendered, every edit path is disabled, the coordinator refuses a
/// forced write, and no undo/dirty/rejection side effect appears.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorReadOnlyTests
{
    [Fact]
    public async Task AReadOnlySessionShowsTheBannerAndDisablesEveryEditPath()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new ParticlesEditorViewModel(session, new ShellRegionsViewModel());
                var document = new ModelEditorDocumentViewModel(ModelEditorDomains.Particles, editor);
                var view = new ModelEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 700, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    Assert.Equal(ModelEditorState.ReadOnly, editor.State);
                    Assert.True(editor.IsReadOnly);
                    Assert.False(editor.AddCommand.CanExecute(null));
                    Assert.False(editor.RemoveCommand.CanExecute(null));
                    Assert.False(editor.DuplicateCommand.CanExecute(null));
                    Assert.False(editor.CanMutateSelection);
                    var banner = view.GetVisualDescendants().OfType<TextBlock>()
                        .SingleOrDefault(text => AutomationProperties.GetName(text)
                            == "Read-only project banner");
                    Assert.NotNull(banner);
                    Assert.True(banner!.IsEffectivelyVisible);

                    Assert.True(editor.RequestSelection(entry.Project.Particles[0].Id.Value));
                    var detail = Assert.IsType<ParticleDetailViewModel>(editor.Detail);
                    Assert.False(detail.CanEditForm);
                    Assert.False(detail.ApplyCommand.CanExecute(null));

                    var rejected = 0;
                    session.EditCoordinator.EditRejected += (_, _) => rejected++;
                    var undoDepth = session.Document.UndoStack.Length;
                    var result = session.EditCoordinator.Apply(
                        "Add particle",
                        (core, project) => core.AddParticle(
                            project,
                            new Particle(
                                ParticleId.New(),
                                "Forbidden",
                                "X",
                                new ChargeElementary(0d),
                                false)));

                    Assert.Equal(EditApplyOutcome.ReadOnly, result.Outcome);
                    Assert.Equal(0, rejected);
                    Assert.False(session.Document.IsDirty);
                    Assert.Equal(undoDepth, session.Document.UndoStack.Length);
                }
                finally
                {
                    window.Close();
                }
            });
    }

    [Fact]
    public async Task TheStructureFormShowsTheBannerAndDisablesBothForms()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        await HeadlessSession.DispatchAsync(
            () =>
            {
                using var editor = new StructureEditorViewModel(
                    session,
                    new ShellRegionsViewModel(),
                    new StructurePreviewService(),
                    new StructureValidator());
                var document = new StructureEditorDocumentViewModel(
                    ModelEditorDomains.StructureEditor,
                    editor);
                var view = new StructureEditorDocumentView { DataContext = document };
                var window = new Window { Width = 1000, Height = 800, Content = view };
                window.Show();
                HeadlessSession.Pump(window);
                try
                {
                    Assert.True(editor.IsReadOnly);
                    Assert.False(editor.SpaceGroupEditor.ApplyCommand.CanExecute(null));
                    Assert.False(editor.CellParameters.ApplyCommand.CanExecute(null));
                    var banner = view.GetVisualDescendants().OfType<TextBlock>()
                        .SingleOrDefault(text => AutomationProperties.GetName(text)
                            == "Read-only project banner");
                    Assert.NotNull(banner);
                    Assert.True(banner!.IsEffectivelyVisible);
                }
                finally
                {
                    window.Close();
                }
            });
    }
}

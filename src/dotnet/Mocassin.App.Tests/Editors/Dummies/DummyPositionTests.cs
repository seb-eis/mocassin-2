// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Dummies;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.Editors.Dummies;

/// <summary>
/// Pins the dummy-positions document: distinct visual-only marker, add/edit/remove/undo, read-only, and a real
/// save/load round-trip through the project store.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class DummyPositionTests
{
    [Fact]
    public async Task AddEditRemoveAndTheDistinctMarkerWork()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new DummiesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        var original = entry.Session.Project.Structure.DummyPositions.Length;
        var undoDepth = entry.Session.Document.UndoStack.Length;

        editor.AddCommand.Execute(null);

        Assert.Equal(original + 1, entry.Session.Project.Structure.DummyPositions.Length);
        Assert.Equal(undoDepth + 1, entry.Session.Document.UndoStack.Length);
        var detail = Assert.IsType<DummyPositionDetailViewModel>(editor.Detail);
        var row = Assert.Single(editor.Table.Rows, candidate => candidate.ObjectId == detail.ObjectId);
        Assert.Equal(DummiesEditorViewModel.MarkerGlyph, row.Cells[0].Text);
        Assert.Equal(DummiesEditorViewModel.MarkerStatus, row.Cells[5].Text);
        Assert.StartsWith("Dummy ", row.Cells[1].Text, StringComparison.Ordinal);

        Assert.Equal(DummiesEditorViewModel.MarkerGlyph, detail.MarkerGlyph);
        Assert.Contains("Visual-only", detail.Explanation, StringComparison.Ordinal);
        detail.Name = "Marker A";
        detail.FieldA = "0.125";
        detail.FieldB = "0.25";
        detail.FieldC = "0.375";
        Assert.True(detail.CanApply);
        detail.ApplyCommand.Execute(null);

        var authored = entry.Session.Project.Structure.DummyPositions
            .Single(dummy => dummy.Id.Value == detail.ObjectId);
        Assert.Equal("Marker A", authored.Name);
        Assert.Equal(0.125d, authored.Fractional.A, 10);
        Assert.Equal(0.375d, authored.Fractional.C, 10);

        editor.RemoveCommand.Execute(null);
        Assert.Equal(original, entry.Session.Project.Structure.DummyPositions.Length);
        Assert.True(entry.Session.EditCoordinator.Undo());
        Assert.Equal(original + 1, entry.Session.Project.Structure.DummyPositions.Length);
    }

    [Fact]
    public async Task ADummyRoundTripsThroughSaveAndLoad()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        using var editor = new DummiesEditorViewModel(entry.Session, new ShellRegionsViewModel());
        editor.AddCommand.Execute(null);
        var detail = Assert.IsType<DummyPositionDetailViewModel>(editor.Detail);
        detail.FieldA = "0.1";
        detail.FieldB = "0.2";
        detail.FieldC = "0.3";
        detail.ApplyCommand.Execute(null);

        using var workspace = new TempWorkspace();
        var path = Path.Combine(workspace.Path, "dummy-roundtrip.mocprj");
        var time = new ManualTimeProvider();
        var store = new ProjectStore(time, new ProjectStoreOptions { ValidateOnLoad = false });
        var saved = await store.SaveAsync(entry.Session.Project, path);
        Assert.True(saved.IsSuccess, string.Join("; ", saved.Diagnostics.Select(static diagnostic => diagnostic.Message)));

        var loaded = await store.LoadAsync(path);
        Assert.NotNull(loaded.Project);
        var dummy = loaded.Project!.Structure.DummyPositions.Single(candidate => candidate.Name == "Dummy 1");
        Assert.Equal("Dummy 1", dummy.Name);
        Assert.Equal(0.1d, dummy.Fractional.A, 10);
        Assert.Equal(0.2d, dummy.Fractional.B, 10);
        Assert.Equal(0.3d, dummy.Fractional.C, 10);
    }

    [Fact]
    public async Task ReadOnlyDisablesTheDummyDocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var session = ProjectSession.CreateStandalone(entry.Project, entry.Path, isReadOnly: true);
        using var editor = new DummiesEditorViewModel(session, new ShellRegionsViewModel());
        Assert.False(editor.AddCommand.CanExecute(null));

        var dummy = new CellDummyPosition(CellDummyPositionId.New(), "Read only", new Mocassin.Core.Geometry.Fractional3(0.1, 0.1, 0.1));
        var detail = new DummyPositionDetailViewModel(dummy);
        detail.Attach(session.EditCoordinator);
        Assert.False(detail.CanApply);
        Assert.False(detail.ApplyCommand.CanExecute(null));
    }
}

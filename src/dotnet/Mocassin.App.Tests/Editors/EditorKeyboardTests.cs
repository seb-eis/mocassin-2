// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the document-level editor keys: <c>Escape</c> reverts the open form and the selection surface remains the
/// one tab stop (covered with the table's keyboard tests).
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
}

// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Sets;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Editors.Particles;
using Mocassin.App.Views.Editors.Sets;
using Mocassin.Core.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Registers and renders the particle/set editors: the Model menu and navigation open real documents, and the two
/// detail views realize their fields/lists.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EditorIntegrationTests
{
    [Fact]
    public Task TheModelMenuAndNavigationOpenTheParticleAndSetDocuments() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<Mocassin.App.Views.Shell.ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    HeadlessSession.Pump(window);

                    var particleCommand = shell.Commands.Find("model.open.particle");
                    var setCommand = shell.Commands.Find("model.open.particleSet");
                    Assert.NotNull(particleCommand);
                    Assert.NotNull(setCommand);
                    Assert.True(particleCommand!.Command.CanExecute(null));
                    Assert.True(setCommand!.Command.CanExecute(null));

                    Assert.True(await shell.Navigation.NavigateToAsync(
                        new ModelPath(ModelEditorDomains.Particle, Guid.Empty),
                        NavigationIntent.Activate));
                    var particleDocument = Assert.IsType<ModelEditorDocumentViewModel>(shell.Documents.ActiveDocument);
                    Assert.IsType<ParticlesEditorViewModel>(particleDocument.Editor);
                    HeadlessSession.Pump(window);
                    Assert.NotNull(window.GetVisualDescendants().OfType<TextBox>()
                        .FirstOrDefault(box => Avalonia.Automation.AutomationProperties.GetName(box) == "Bulk action value"));

                    Assert.True(await shell.Navigation.NavigateToAsync(
                        new ModelPath(ModelEditorDomains.ParticleSet, Guid.Empty),
                        NavigationIntent.Activate));
                    var setDocument = Assert.IsType<ModelEditorDocumentViewModel>(shell.Documents.ActiveDocument);
                    Assert.IsType<ParticleSetsEditorViewModel>(setDocument.Editor);
                    HeadlessSession.Pump(window);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public async Task TheParticleAndSetDetailViewsRealizeTheirControls()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        await HeadlessSession.DispatchAsync(
            () =>
            {
                var layout = new ShellRegionsViewModel();
                using var particles = new ParticlesEditorViewModel(entry.Session, layout);
                Assert.True(particles.RequestSelection(entry.Project.Particles[0].Id.Value));
                var particleDetail = Assert.IsType<ParticleDetailViewModel>(particles.Detail);
                var particleView = new ParticleDetailView { DataContext = particleDetail };
                var particleWindow = new Window { Width = 800, Height = 600, Content = particleView };
                particleWindow.Show();
                HeadlessSession.Pump(particleWindow);
                try
                {
                    Assert.NotNull(particleView.GetVisualDescendants().OfType<TextBox>()
                        .Single(box => box.Name == "name"));
                    Assert.NotNull(particleView.GetVisualDescendants().OfType<TextBox>()
                        .Single(box => box.Name == "symbol"));
                    Assert.NotNull(particleView.GetVisualDescendants().OfType<TextBox>()
                        .Single(box => box.Name == "charge"));
                    Assert.NotNull(particleView.GetVisualDescendants().OfType<CheckBox>()
                        .Single(box => box.Name == "isVacancy"));
                }
                finally
                {
                    particleWindow.Close();
                }

                using var sets = new ParticleSetsEditorViewModel(entry.Session, layout);
                var set = entry.Project.Model.ParticleSets[0];
                Assert.True(sets.RequestSelection(set.Id.Value));
                var setDetail = Assert.IsType<ParticleSetDetailViewModel>(sets.Detail);
                var setView = new ParticleSetDetailView { DataContext = setDetail };
                var setWindow = new Window { Width = 1100, Height = 760, Content = setView };
                setWindow.Show();
                HeadlessSession.Pump(setWindow);
                try
                {
                    var source = setView.GetVisualDescendants().OfType<ListBox>()
                        .Single(list => list.Name == "ProjectParticles");
                    var target = setView.GetVisualDescendants().OfType<ListBox>()
                        .Single(list => list.Name == "Members");
                    Assert.Equal(setDetail.ProjectParticles.Count, source.ItemCount);
                    Assert.Equal(setDetail.Members.Count, target.ItemCount);
                    Assert.True(DragDrop.GetAllowDrop(target));
                    Assert.Equal(setDetail.OccupationChips.Count, setDetail.Members.Count);
                }
                finally
                {
                    setWindow.Close();
                }
            });
    }
}

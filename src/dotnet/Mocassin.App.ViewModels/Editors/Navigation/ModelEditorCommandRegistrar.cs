// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.ViewModels.Editors.Navigation;

/// <summary>
/// Registers the Model-menu entries that open the five 24A editor documents. The gestures that act on the focused
/// editor stay document-local (D-7); these commands only open and activate.
/// </summary>
internal sealed class ModelEditorCommandRegistrar
{
    private readonly ICommandCatalog catalog;
    private readonly INavigationService navigation;
    private readonly IProjectWorkspace workspace;
    private readonly List<AsyncRelayCommand> commands = [];
    private bool registered;

    /// <summary>
    /// Initializes the registrar.
    /// </summary>
    /// <param name="catalog">The shell command catalog.</param>
    /// <param name="navigation">The navigation service.</param>
    /// <param name="workspace">The project workspace.</param>
    public ModelEditorCommandRegistrar(
        ICommandCatalog catalog,
        INavigationService navigation,
        IProjectWorkspace workspace)
    {
        this.catalog = catalog;
        this.navigation = navigation;
        this.workspace = workspace;
    }

    /// <summary>
    /// Registers the commands once.
    /// </summary>
    public void Register()
    {
        if (registered)
        {
            return;
        }

        foreach (var descriptor in ModelEditorDomains.All)
        {
            var captured = descriptor;
            var command = new AsyncRelayCommand(
                () => OpenAsync(captured),
                () => workspace.ActiveProject is not null);
            commands.Add(command);
            catalog.Add(
                $"model.open.{descriptor.Domain}",
                descriptor.Title,
                "Model",
                MenuKind.Model,
                gesture: null,
                command,
                descriptor.EmptyMessage);
        }

        workspace.ActiveProjectChanged += (_, _) =>
        {
            foreach (var command in commands)
            {
                command.NotifyCanExecuteChanged();
            }
        };
        registered = true;
    }

    private async Task OpenAsync(ModelEditorDescriptor descriptor)
    {
        if (workspace.ActiveProject is not { } project)
        {
            return;
        }

        await navigation.NavigateToAsync(descriptor.ActivationPath(project.Id), NavigationIntent.Activate);
    }
}

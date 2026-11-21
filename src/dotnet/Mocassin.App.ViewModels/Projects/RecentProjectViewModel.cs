// SPDX-License-Identifier: MIT

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// One recent-project row with open and remove actions.
/// </summary>
internal sealed class RecentProjectViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a recent-project row.
    /// </summary>
    /// <param name="file">The stored recent file.</param>
    /// <param name="isMissing">Whether the file is missing.</param>
    /// <param name="open">The open action.</param>
    /// <param name="remove">The remove action.</param>
    public RecentProjectViewModel(
        RecentFile file,
        bool isMissing,
        Func<Task> open,
        Func<Task> remove)
    {
        Source = file;
        IsMissing = isMissing;
        OpenCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(open, () => !IsMissing);
        RemoveCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(remove);
    }

    /// <summary>
    /// Gets the stored recent file.
    /// </summary>
    public RecentFile Source { get; }

    /// <summary>
    /// Gets the project path.
    /// </summary>
    public string Path => Source.Path;

    /// <summary>
    /// Gets the missing-file state.
    /// </summary>
    public bool IsMissing { get; }

    /// <summary>
    /// Gets the open command.
    /// </summary>
    public ICommand OpenCommand { get; }

    /// <summary>
    /// Gets the remove command.
    /// </summary>
    public ICommand RemoveCommand { get; }
}

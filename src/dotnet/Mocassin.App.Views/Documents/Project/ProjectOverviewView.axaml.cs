// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mocassin.App.ViewModels.Documents.Project;

namespace Mocassin.App.Views.Documents.Project;

/// <summary>
/// Project overview view with the editable metadata form and focus requests.
/// </summary>
internal sealed partial class ProjectOverviewView : UserControl
{
    private ProjectOverviewViewModel? viewModel;

    /// <summary>
    /// Initializes the view.
    /// </summary>
    public ProjectOverviewView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.FocusRequested -= OnFocusRequested;
        }

        viewModel = DataContext as ProjectOverviewViewModel;
        if (viewModel is not null)
        {
            viewModel.FocusRequested += OnFocusRequested;
        }
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (viewModel?.ConsumePendingFocus() is { } property)
        {
            FocusField(property);
        }
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (viewModel is not null)
        {
            viewModel.FocusRequested -= OnFocusRequested;
        }
    }

    private void OnFocusRequested(object? sender, string property)
    {
        viewModel?.ConsumePendingFocus();
        FocusField(property);
    }

    private void FocusField(string? property)
    {
        var control = property switch
        {
            "name" => NameField,
            "description" => DescriptionField,
            "author" => AuthorField,
            "notes" => NotesField,
            _ => null,
        };
        control?.Focus();
    }

    private void OnNameLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitNameCommand.Execute(null);

    private void OnDescriptionLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitDescriptionCommand.Execute(null);

    private void OnAuthorLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitAuthorCommand.Execute(null);

    private void OnNotesLostFocus(object? sender, RoutedEventArgs e) =>
        viewModel?.CommitNotesCommand.Execute(null);

    private void OnFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || viewModel is null)
        {
            return;
        }

        e.Handled = true;
        if (ReferenceEquals(sender, NameField))
        {
            viewModel.CommitNameCommand.Execute(null);
        }
        else if (ReferenceEquals(sender, DescriptionField))
        {
            viewModel.CommitDescriptionCommand.Execute(null);
        }
        else if (ReferenceEquals(sender, AuthorField))
        {
            viewModel.CommitAuthorCommand.Execute(null);
        }
        else if (ReferenceEquals(sender, NotesField))
        {
            viewModel.CommitNotesCommand.Execute(null);
        }
    }
}

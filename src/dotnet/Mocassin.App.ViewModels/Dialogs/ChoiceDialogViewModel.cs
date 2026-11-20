// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Selects one of a fixed set of choices.
/// </summary>
internal sealed partial class ChoiceDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private DialogChoice? selectedChoice;

    /// <summary>
    /// Initializes a new choice dialog.
    /// </summary>
    /// <param name="request">The choice request.</param>
    public ChoiceDialogViewModel(ChoiceDialogRequest request)
        : base(request.Title, request.Message)
    {
        Choices = new ObservableCollection<DialogChoice>(request.Choices);
        SelectedChoice = Choices.FirstOrDefault(choice => choice.IsDefault) ?? Choices.FirstOrDefault();
    }

    /// <summary>
    /// Gets the available choices.
    /// </summary>
    public ObservableCollection<DialogChoice> Choices { get; }

    /// <summary>
    /// Confirms the selected choice.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        if (SelectedChoice is null)
        {
            return;
        }

        Close(new DialogResult(DialogResultKind.ChoiceSelected, ChoiceId: SelectedChoice.Id));
    }
}

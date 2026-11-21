// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Collects a single line of text.
/// </summary>
internal sealed partial class TextPromptDialogViewModel : DialogViewModel
{
    [ObservableProperty]
    private string text;

    /// <summary>
    /// Initializes a new text prompt.
    /// </summary>
    /// <param name="request">The prompt request.</param>
    public TextPromptDialogViewModel(TextPromptRequest request)
        : base(request.Title, request.Message)
    {
        text = request.Value ?? string.Empty;
        Watermark = request.Watermark;
    }

    /// <summary>
    /// Gets the optional watermark.
    /// </summary>
    public string? Watermark { get; }

    /// <summary>
    /// Confirms the entered text.
    /// </summary>
    [RelayCommand]
    private void Confirm() => Close(new DialogResult(DialogResultKind.TextEntered, Text));
}

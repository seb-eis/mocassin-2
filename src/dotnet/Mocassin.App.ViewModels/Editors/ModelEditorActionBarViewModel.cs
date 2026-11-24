// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// One optional document-level action bar. A domain editor exposes a single text value plus an Apply command for a
/// bulk action (the particle editor's bulk charge); the shared document view renders it without knowing the domain.
/// </summary>
internal sealed partial class ModelEditorActionBarViewModel : ObservableObject
{
    private readonly Action<string> apply;
    private string valueText = string.Empty;
    private string? errorText;

    /// <summary>
    /// Initializes an action bar.
    /// </summary>
    /// <param name="label">The action label.</param>
    /// <param name="placeholder">The value field's placeholder.</param>
    /// <param name="apply">The action invoked with the current text value.</param>
    /// <param name="canApply">The optional can-execute predicate.</param>
    public ModelEditorActionBarViewModel(
        string label,
        string placeholder,
        Action<string> apply,
        Func<bool>? canApply = null)
    {
        Label = label;
        Placeholder = placeholder;
        this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
        ApplyCommand = new RelayCommand(() => this.apply(ValueText), canApply ?? (() => true));
    }

    /// <summary>
    /// Gets the action label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the value field's placeholder.
    /// </summary>
    public string Placeholder { get; }

    /// <summary>
    /// Gets or sets the value text.
    /// </summary>
    public string ValueText
    {
        get => valueText;
        set => SetProperty(ref valueText, value);
    }

    /// <summary>
    /// Gets the inline error from the last action.
    /// </summary>
    public string? ErrorText
    {
        get => errorText;
        private set
        {
            if (SetProperty(ref errorText, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the action bar has an inline error.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    /// <summary>
    /// Gets the action command.
    /// </summary>
    public IRelayCommand ApplyCommand { get; }

    /// <summary>
    /// Sets or clears the inline error.
    /// </summary>
    /// <param name="error">The message, or <see langword="null"/>.</param>
    public void SetError(string? error) => ErrorText = error;
}

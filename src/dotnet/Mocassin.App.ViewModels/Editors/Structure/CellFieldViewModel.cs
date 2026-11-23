// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// One cell-parameter field with its label, unit, expression text, lock state, and inline error.
/// </summary>
internal sealed partial class CellFieldViewModel : ObservableObject
{
    private readonly Action<CellFieldViewModel> onChanged;
    private string text = string.Empty;
    private bool isLocked;
    private bool isEditable = true;
    private string? lockReason;
    private string? error;
    private bool suppressChange;

    /// <summary>
    /// Initializes a cell field.
    /// </summary>
    /// <param name="kind">The field kind.</param>
    /// <param name="label">The visible label.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="onChanged">The change callback.</param>
    public CellFieldViewModel(CellFieldKind kind, string label, string unit, Action<CellFieldViewModel> onChanged)
    {
        Kind = kind;
        Label = label;
        Unit = unit;
        this.onChanged = onChanged;
    }

    /// <summary>
    /// Gets the field kind.
    /// </summary>
    public CellFieldKind Kind { get; }

    /// <summary>
    /// Gets the visible label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets the unit.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets or sets the expression text.
    /// </summary>
    public string Text
    {
        get => text;
        set
        {
            if (SetProperty(ref text, value) && !suppressChange)
            {
                onChanged(this);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the field is locked by the crystal system.
    /// </summary>
    public bool IsLocked
    {
        get => isLocked;
        internal set
        {
            if (SetProperty(ref isLocked, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the field accepts input in this session.
    /// </summary>
    public bool IsEditable
    {
        get => isEditable;
        internal set
        {
            if (SetProperty(ref isEditable, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the field accepts input: editable and not locked.
    /// </summary>
    public bool IsInputEnabled => isEditable && !isLocked;

    /// <summary>
    /// Gets the lock explanation.
    /// </summary>
    public string? LockReason
    {
        get => lockReason;
        internal set => SetProperty(ref lockReason, value);
    }

    /// <summary>
    /// Gets the field's inline parse error.
    /// </summary>
    public string? Error
    {
        get => error;
        internal set
        {
            if (SetProperty(ref error, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the field has an error.
    /// </summary>
    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    /// <summary>
    /// Sets the text without invoking the change callback (lock mirroring and refresh).
    /// </summary>
    /// <param name="value">The text.</param>
    public void SetTextSilently(string value)
    {
        suppressChange = true;
        try
        {
            Text = value;
        }
        finally
        {
            suppressChange = false;
        }
    }

    /// <summary>
    /// Sets the value without invoking the change callback.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    public void SetValueSilently(double value) => SetTextSilently(CellParameterLocks.Format(value));
}

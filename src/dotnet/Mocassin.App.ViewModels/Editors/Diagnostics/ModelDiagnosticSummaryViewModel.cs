// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Editors.Diagnostics;

/// <summary>
/// The inline validation summary of one model editor. It filters the session's existing validation state to the
/// editor's domains and never runs the validation pipeline itself.
/// </summary>
internal sealed partial class ModelDiagnosticSummaryViewModel : ObservableObject
{
    private ImmutableArray<Diagnostic> diagnostics = [];
    private int errorCount;
    private int warningCount;

    /// <summary>
    /// Initializes a summary with the action that activates the existing Problems channel.
    /// </summary>
    /// <param name="openProblems">The Problems-channel activation, or <see langword="null"/> in a headless test.</param>
    public ModelDiagnosticSummaryViewModel(Action? openProblems = null)
    {
        OpenProblemsCommand = new RelayCommand(() => openProblems?.Invoke(), () => openProblems is not null);
    }

    /// <summary>
    /// Gets the filtered diagnostics.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => diagnostics;

    /// <summary>
    /// Gets the error count.
    /// </summary>
    public int ErrorCount => errorCount;

    /// <summary>
    /// Gets the warning count.
    /// </summary>
    public int WarningCount => warningCount;

    /// <summary>
    /// Gets a value indicating whether the editor has any diagnostic.
    /// </summary>
    public bool HasDiagnostics => !diagnostics.IsDefaultOrEmpty;

    /// <summary>
    /// Gets the summary text.
    /// </summary>
    public string SummaryText => (errorCount, warningCount) switch
    {
        (0, 0) => "No problems in this editor",
        (0, _) => $"{warningCount} warning(s)",
        (_, 0) => $"{errorCount} error(s)",
        _ => $"{errorCount} error(s), {warningCount} warning(s)",
    };

    /// <summary>
    /// Gets the command that activates the Problems channel.
    /// </summary>
    public IRelayCommand OpenProblemsCommand { get; }

    /// <summary>
    /// Replaces the diagnostics and refreshes the counts.
    /// </summary>
    /// <param name="value">The filtered diagnostics.</param>
    public void SetDiagnostics(IEnumerable<Diagnostic> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        diagnostics = [.. value];
        errorCount = diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        warningCount = diagnostics.Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(HasDiagnostics));
        OnPropertyChanged(nameof(SummaryText));
    }
}

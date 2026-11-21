// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// One diagnostic row in the Problems channel.
/// </summary>
internal sealed class DiagnosticItemViewModel
{
    private readonly INavigationService navigation;
    private readonly IShellLauncher launcher;

    /// <summary>
    /// Initializes a diagnostic row.
    /// </summary>
    /// <param name="diagnostic">The immutable diagnostic.</param>
    /// <param name="source">The source category, <c>validation</c> or <c>edit</c>.</param>
    /// <param name="navigation">The navigation service.</param>
    /// <param name="launcher">The help-link launcher.</param>
    public DiagnosticItemViewModel(
        Diagnostic diagnostic,
        string source,
        INavigationService navigation,
        IShellLauncher launcher)
    {
        Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        Source = source;
        this.navigation = navigation;
        this.launcher = launcher;
        HelpUri = DiagnosticHelpLinks.Resolve(diagnostic.Code);
        NavigateCommand = new AsyncRelayCommand(NavigateAsync, () => Diagnostic.Path is not null);
        OpenHelpCommand = new AsyncRelayCommand(OpenHelpAsync, () => HelpUri is not null);
        RelatedItems = diagnostic.Related.IsDefault
            ? []
            : diagnostic.Related
                .Select(path => new DiagnosticRelatedItemViewModel(path, navigation))
                .ToImmutableArray();
    }

    /// <summary>
    /// Gets the immutable diagnostic.
    /// </summary>
    public Diagnostic Diagnostic { get; }

    /// <summary>
    /// Gets the source category.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the display source.
    /// </summary>
    public string SourceText => string.Equals(Source, "edit", StringComparison.Ordinal) ? "Edit" : "Validation";

    /// <summary>
    /// Gets the severity.
    /// </summary>
    public DiagnosticSeverity Severity => Diagnostic.Severity;

    /// <summary>
    /// Gets the severity glyph.
    /// </summary>
    public string SeverityGlyph => Severity switch
    {
        DiagnosticSeverity.Error => "✖",
        DiagnosticSeverity.Warning => "⚠",
        _ => "ℹ",
    };

    /// <summary>
    /// Gets the stable code.
    /// </summary>
    public string Code => Diagnostic.Code;

    /// <summary>
    /// Gets the message.
    /// </summary>
    public string Message => Diagnostic.Message;

    /// <summary>
    /// Gets the path text, or an empty string.
    /// </summary>
    public string PathText => Diagnostic.Path?.ToString() ?? string.Empty;

    /// <summary>
    /// Gets the optional hint.
    /// </summary>
    public string Hint => Diagnostic.Hint ?? string.Empty;

    /// <summary>
    /// Gets the optional help URI.
    /// </summary>
    public string? HelpUri { get; }

    /// <summary>
    /// Gets a value indicating whether the row has a help URI.
    /// </summary>
    public bool HasHelp => HelpUri is not null;

    /// <summary>
    /// Gets a value indicating whether the row has a navigable path.
    /// </summary>
    public bool CanNavigate => Diagnostic.Path is not null;

    /// <summary>
    /// Gets the related paths.
    /// </summary>
    public ImmutableArray<DiagnosticRelatedItemViewModel> RelatedItems { get; }

    /// <summary>
    /// Gets a value indicating whether the row has related paths.
    /// </summary>
    public bool HasRelated => RelatedItems.Length > 0;

    /// <summary>
    /// Gets the navigate command.
    /// </summary>
    public AsyncRelayCommand NavigateCommand { get; }

    /// <summary>
    /// Gets the help command.
    /// </summary>
    public AsyncRelayCommand OpenHelpCommand { get; }

    /// <summary>
    /// Returns a sort key for the path.
    /// </summary>
    /// <returns>The path text or an empty string.</returns>
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Code} {Message}");

    private async Task NavigateAsync()
    {
        if (Diagnostic.Path is { } path)
        {
            await navigation.NavigateToAsync(path, NavigationIntent.SelectObject);
        }
    }

    private async Task OpenHelpAsync()
    {
        if (HelpUri is { } uri)
        {
            await launcher.OpenUriAsync(uri);
        }
    }
}

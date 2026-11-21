// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// Contributes the active project's validation state to the status bar.
/// </summary>
internal sealed class ValidationStatusItemProvider : IStatusBarItemProvider
{
    private readonly IProjectWorkspace workspace;
    private readonly StatusBarItemViewModel item = new(
        "Not validated",
        "Project validation status",
        severity: StatusBarSeverity.Normal,
        glyph: "○");
    private ProjectSession? session;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    public ValidationStatusItemProvider(IProjectWorkspace workspace)
    {
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        workspace.ActiveProjectChanged += OnActiveProjectChanged;
        Attach(workspace.ActiveProject?.Session);
    }

    /// <inheritdoc />
    public IEnumerable<StatusBarItemViewModel> CreateItems() => [item];

    private void OnActiveProjectChanged(object? sender, EventArgs e) =>
        Attach(workspace.ActiveProject?.Session);

    private void Attach(ProjectSession? value)
    {
        if (ReferenceEquals(session, value))
        {
            return;
        }

        if (session is not null)
        {
            session.ValidationCoordinator.Changed -= OnValidationChanged;
        }

        session = value;
        if (session is not null)
        {
            session.ValidationCoordinator.Changed += OnValidationChanged;
        }

        Update();
    }

    private void OnValidationChanged(object? sender, EventArgs e) => Update();

    private void Update()
    {
        if (session is null)
        {
            item.Text = "Not validated";
            item.Glyph = "○";
            item.Severity = StatusBarSeverity.Normal;
            return;
        }

        var validation = session.ValidationCoordinator;
        item.Text = validation.StatusText;
        item.Glyph = validation.StatusGlyph;
        item.Severity = validation.State switch
        {
            ValidationState.Valid => StatusBarSeverity.Success,
            ValidationState.Validating => StatusBarSeverity.Information,
            ValidationState.Invalid when validation.Summary.Errors > 0 => StatusBarSeverity.Error,
            ValidationState.Invalid => StatusBarSeverity.Warning,
            _ => StatusBarSeverity.Normal,
        };
    }
}

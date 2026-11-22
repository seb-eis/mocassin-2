// SPDX-License-Identifier: MIT

using System.Text.RegularExpressions;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mocassin.App.Tests.TestSupport;

using Mocassin.App.Tests.LayoutSpike.Data;
using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Interaction;

/// <summary>
/// The §0.2 editor evidence: a type-specific editor opens for every domain, validation rejects invalid edits without
/// touching the project, reference pickers are searchable name-plus-details combos, and every accepted edit goes
/// through the real <c>ProjectEditCoordinator</c> and its undo stack.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class SpikeEditorTests
{
    private static readonly Regex guidPattern =
        new("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

    [Fact]
    public Task TheParticleEditorRejectsInvalidEditsAndUndoesValidOnes() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(SpikeOption.ListDetail);
                var domain = host.Workspace.ActiveDomain;
                SpikeInteractions.SelectRow(host, "Ce4+");
                var editor = host.Workspace.Editor;
                Assert.Equal(["Name", "Symbol", "Charge", "Vacancy"], editor.Fields.Select(field => field.Label));

                // The real validator owns the structural rule and the project stays the same object.
                var before = editor.Coordinator!.Document.Project;
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text = string.Empty;
                editor.Apply();
                Assert.Same(before, editor.Coordinator.Document.Project);
                Assert.False(editor.Coordinator.CanUndo);

                // A value that cannot be represented is a local field error, still without an apply.
                var charge = SpikeInteractions.EditorControl<TextBox>(host, "Charge field");
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text = "Ce4+";
                charge.Text = "abc";
                editor.Apply();
                Assert.Contains("Charge must be a finite number", editor.ErrorText);
                Assert.Same(before, editor.Coordinator.Document.Project);

                // Enter is a real key: it applies the field values through the coordinator.
                charge.Text = "+4";
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text = "Ce4+ (POC)";
                editor = host.Workspace.Editor;
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Focus();
                SpikeInteractions.Press(host, Avalonia.Input.Key.Enter);
                Assert.Equal("Ce4+ (POC)", domain.SelectedRow?.Title);
                Assert.Empty(host.Workspace.Editor.ErrorText);
                Assert.True(host.Workspace.Editor.Coordinator!.CanUndo);

                // Undo runs over the document's own stack through the editor command surface.
                SpikeInteractions.EditorControl<Button>(host, "Undo edit").RaiseEvent(
                    new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("Ce4+", domain.SelectedRow?.Title);
                Assert.Equal("Ce4+", host.Workspace.Editor.Coordinator!.Document.Project.Particles[0].Name);

                // Escape reverts unapplied field text without touching the document.
                var name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Focus();
                host.Pump();
                name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                name.Text = "Discarded";
                SpikeInteractions.Press(host, Avalonia.Input.Key.Escape);
                name = SpikeInteractions.EditorControl<TextBox>(host, "Name field");
                Assert.Equal("Ce4+", name.Text);
                Assert.Equal("Ce4+", host.Workspace.Editor.Coordinator!.Document.Project.Particles[0].Name);
            });

    [Fact]
    public Task TheSiteEditorUsesASearchableSetPickerAndRejectsAnOutOfRangePosition() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(SpikeOption.ListDetail);
                host.Workspace.ActiveDomain = host.Workspace.Domains[2];
                host.Pump();
                SpikeInteractions.SelectRow(host, "O");
                var editor = host.Workspace.Editor;
                Assert.Contains("Particle set", editor.Fields.Select(field => field.Label));

                // The picker shows names and details, never raw identifiers, and its search filters the choices.
                var setField = editor.Fields.Single(field => field.Key == "particleSet");
                Assert.Contains(setField.Options, option => option.Display.Contains("Oxygen site"));
                Assert.DoesNotContain(setField.Options, option => guidPattern.IsMatch(option.Display));
                var search = SpikeInteractions.EditorControl<TextBox>(host, "Particle set search");
                search.Text = "transition";
                host.Pump();
                Assert.Single(setField.VisibleOptions);
                Assert.Contains("Oxygen transition site", setField.VisibleOptions[0].Display);

                // An out-of-range fractional position is rejected by the real structure validator.
                var before = editor.Coordinator!.Document.Project;
                SpikeInteractions.EditorControl<TextBox>(host, "A field").Text = "1.5";
                editor.Apply();
                Assert.False(string.IsNullOrWhiteSpace(host.Workspace.Editor.ErrorText));
                Assert.Same(before, host.Workspace.Editor.Coordinator!.Document.Project);

                // A picker change is a real, undoable edit.
                SpikeInteractions.EditorControl<TextBox>(host, "A field").Text = "0.25";
                setField = host.Workspace.Editor.Fields.Single(field => field.Key == "particleSet");
                setField.SelectedOption = setField.Options.Single(
                    option => option.Display.Contains("Oxygen transition site"));
                host.Workspace.Editor.Apply();
                var site = host.Workspace.Editor.Coordinator!.Document.Project.Structure.Sites
                    .Single(candidate => candidate.Name == "O");
                Assert.Contains("Oxygen transition site", SpikeModelLookup.FindSet(
                    host.Workspace.Editor.Coordinator.Document.Project,
                    site.ParticleSetId.Value)!.Name);
                Assert.True(host.Workspace.Editor.Coordinator.CanUndo);
                host.Workspace.Editor.Undo();
                Assert.Equal(
                    "Oxygen site",
                    SpikeModelLookup.FindSet(
                        host.Workspace.Editor.Coordinator!.Document.Project,
                        host.Workspace.Editor.Coordinator.Document.Project.Structure.Sites
                            .Single(candidate => candidate.Name == "O").ParticleSetId.Value)!.Name);
            });

    [Fact]
    public Task TheStateChangeChainAndTransitionEditorsUseSearchablePickers() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(SpikeOption.ListDetail);
                foreach (var (domainIndex, title, pickerKey, expectedDetail) in new[]
                {
                    (3, "O -> vacancy", "donor", "O2-"),
                    (4, "3-site migration", "groups", "positions"),
                    (5, "O migration", "chain", "Migration3"),
                })
                {
                    host.Workspace.ActiveDomain = host.Workspace.Domains[domainIndex];
                    host.Pump();
                    SpikeInteractions.SelectRow(host, title);
                    var editor = host.Workspace.Editor;
                    Assert.True(editor.Fields.Count >= 2, $"The {title} editor has no fields.");
                    if (pickerKey is "groups")
                    {
                        Assert.Contains(editor.Fields, field => field.Label == "Pattern");
                        continue;
                    }

                    var picker = editor.Fields.Single(field => field.Key == pickerKey);
                    Assert.NotEmpty(picker.Options);
                    Assert.Contains(picker.Options, option => option.Display.Contains(expectedDetail));
                    Assert.DoesNotContain(picker.Options, option => guidPattern.IsMatch(option.Display));
                }

                // Rename the chain through the real coordinator and undo it.
                host.Workspace.ActiveDomain = host.Workspace.Domains[4];
                host.Pump();
                SpikeInteractions.SelectRow(host, "3-site migration");
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text = "3-site migration (POC)";
                host.Workspace.Editor.Apply();
                Assert.Equal("3-site migration (POC)", host.Workspace.ActiveDomain.SelectedRow?.Title);
                host.Workspace.Editor.Undo();
                Assert.Equal("3-site migration", host.Workspace.ActiveDomain.SelectedRow?.Title);

                // A transition with an empty name is rejected with the transition diagnostic code.
                host.Workspace.ActiveDomain = host.Workspace.Domains[5];
                host.Pump();
                SpikeInteractions.SelectRow(host, "O migration");
                SpikeInteractions.EditorControl<TextBox>(host, "Name field").Text = string.Empty;
                host.Workspace.Editor.Apply();
                Assert.StartsWith("TRN", host.Workspace.Editor.ErrorText);
            });

    [Fact]
    public Task TheVoidRowAndBreadthRowsExplainWhyTheyAreReadOnly() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(SpikeOption.ListDetail);
                SpikeInteractions.SelectRow(host, "Void");
                var editor = host.Workspace.Editor;
                Assert.True(editor.HasSelection);
                Assert.False(editor.HasEditableRow);
                Assert.Contains("implicit", editor.ReadOnlyMessage);
                Assert.Empty(editor.Fields);
                Assert.False(editor.ApplyCommand.CanExecute(null));

                host.Workspace.ActiveDomain = host.Workspace.Domains[2];
                host.Workspace.GeneratedBreadth = true;
                host.Pump();
                SpikeInteractions.SelectRow(host, "O0003");
                editor = host.Workspace.Editor;
                Assert.False(editor.HasEditableRow);
                Assert.Contains("Generated breadth row", editor.ReadOnlyMessage);
                Assert.False(editor.ApplyCommand.CanExecute(null));
            });

    [Fact]
    public Task TheMatrixControlComposesInsideTheDetailPane() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                using var host = await SpikeTestHost.OpenAsync(SpikeOption.SplitPane);
                host.Workspace.ActiveDomain = host.Workspace.Domains[5];
                host.Pump();
                SpikeInteractions.SelectRow(host, "O migration");
                var matrix = host.Workspace.Editor.Matrix;
                Assert.NotNull(matrix);
                Assert.Contains("Occupation matrix", matrix.Title);
                Assert.Equal(["O vacancy", "O unstable"], matrix.ColumnLabels.ToArray());
                Assert.Equal(["S0", "S1", "S2"], matrix.Rows.Select(row => row.Label).ToArray());

                var control = host.Window.GetVisualDescendants().OfType<SpikeMatrixControl>().Single();
                Assert.Contains("Occupation matrix", AutomationProperties.GetName(control) ?? string.Empty);
                Assert.Equal(
                    AutomationControlType.Table,
                    ControlAutomationPeer.CreatePeerForElement(control)!.GetAutomationControlType());
            });
}

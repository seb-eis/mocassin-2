// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

using Mocassin.App.Tests.LayoutSpike.Visuals;

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// The type-specific editor view. It renders the field list supplied by <see cref="SpikeEditorViewModel"/> and is
/// hosted unchanged by every layout option.
/// </summary>
internal sealed class SpikeEditorView : UserControl
{
    private readonly StackPanel fieldsPanel;
    private readonly TextBlock heading;
    private readonly Border readOnlyNote;
    private readonly TextBlock readOnlyText;
    private readonly Border errorNote;
    private readonly TextBlock errorText;
    private readonly TextBlock statusGlyph;
    private readonly TextBlock statusText;
    private readonly TextBlock undoText;
    private readonly TextBlock redoText;
    private readonly Button applyButton;
    private readonly Button revertButton;
    private readonly Button undoButton;
    private readonly Button redoButton;
    private readonly StackPanel matrixHost;
    private SpikeEditorViewModel? model;

    /// <summary>
    /// Initializes the editor view.
    /// </summary>
    public SpikeEditorView()
    {
        AutomationProperties.SetName(this, "Type editor");
        heading = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            FontSize = SpikeTheme.Double("Mocassin.SubtitleFontSize", 16),
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundStrongBrush"),
            TextWrapping = TextWrapping.Wrap,
        };
        readOnlyText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundBrush"),
        };
        readOnlyNote = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.WarningBackground"),
            BorderBrush = SpikeTheme.Brush("Mocassin.WarningBorderBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            IsVisible = false,
            Child = readOnlyText,
        };
        AutomationProperties.SetName(readOnlyNote, "Read-only row explanation");

        fieldsPanel = new StackPanel { Spacing = 6 };
        matrixHost = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };

        statusGlyph = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        statusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
        };
        AutomationProperties.SetName(statusText, "Editor status");

        errorText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = SpikeTheme.Brush("Mocassin.ErrorForeground"),
        };
        errorNote = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.ErrorBackground"),
            BorderBrush = SpikeTheme.Brush("Mocassin.ErrorBorderBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 6),
            CornerRadius = new CornerRadius(4),
            IsVisible = false,
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Children =
                {
                    new TextBlock
                    {
                        Text = "✕",
                        Margin = new Thickness(0, 0, 6, 0),
                        Foreground = SpikeTheme.Brush("Mocassin.ErrorForeground"),
                    },
                    errorText,
                },
            },
        };
        Grid.SetColumn(errorText, 1);
        AutomationProperties.SetName(errorNote, "Inline validation error");

        undoText = new TextBlock
        {
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
        };
        redoText = new TextBlock
        {
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
        };

        applyButton = Button("Apply changes", (_, _) => model?.ApplyCommand.Execute(null));
        revertButton = Button("Revert changes", (_, _) => model?.RevertCommand.Execute(null));
        undoButton = Button("Undo edit", (_, _) => model?.UndoCommand.Execute(null));
        redoButton = Button("Redo edit", (_, _) => model?.RedoCommand.Execute(null));

        var buttons = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0),
        };
        foreach (var button in new[] { applyButton, revertButton, undoButton, redoButton })
        {
            button.Margin = new Thickness(0, 0, 6, 4);
            buttons.Children.Add(button);
        }
        var undoRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children = { undoText, redoText },
        };
        var statusRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Children = { statusGlyph, statusText },
        };
        statusGlyph.Margin = new Thickness(0, 0, 6, 0);
        Grid.SetColumn(statusText, 1);

        var body = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                heading,
                readOnlyNote,
                fieldsPanel,
                matrixHost,
                buttons,
                undoRow,
                errorNote,
                statusRow,
            },
        };
        Content = new Border
        {
            Background = SpikeTheme.Brush("Mocassin.WorkspaceBackground"),
            BorderBrush = SpikeTheme.Brush("Mocassin.BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Child = new ScrollViewer
            {
                Content = body,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            },
        };
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (model is not null)
        {
            model.PropertyChanged -= OnModelChanged;
            model.FieldsChanged -= OnFieldsChanged;
        }

        model = DataContext as SpikeEditorViewModel;
        if (model is not null)
        {
            model.PropertyChanged += OnModelChanged;
            model.FieldsChanged += OnFieldsChanged;
        }

        Rebuild();
    }

    private void OnFieldsChanged(object? sender, EventArgs e) => Rebuild();

    private void OnModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SpikeEditorViewModel.ErrorText)
            or nameof(SpikeEditorViewModel.HasError)
            or nameof(SpikeEditorViewModel.StatusText)
            or nameof(SpikeEditorViewModel.UndoText)
            or nameof(SpikeEditorViewModel.RedoText)
            or nameof(SpikeEditorViewModel.UndoCommand)
            or nameof(SpikeEditorViewModel.RedoCommand)
            or nameof(SpikeEditorViewModel.HeaderText)
            or nameof(SpikeEditorViewModel.HasSelection)
            or nameof(SpikeEditorViewModel.Matrix))
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        if (model is null)
        {
            heading.Text = "No selection";
            fieldsPanel.Children.Clear();
            matrixHost.Children.Clear();
            readOnlyNote.IsVisible = false;
            errorNote.IsVisible = false;
            statusText.Text = "Select a row to edit.";
            undoText.Text = string.Empty;
            redoText.Text = string.Empty;
            return;
        }

        heading.Text = model.HeaderText;
        readOnlyNote.IsVisible = model.ReadOnlyMessage.Length > 0;
        readOnlyText.Text = model.ReadOnlyMessage;
        errorNote.IsVisible = model.HasError;
        errorText.Text = model.ErrorText;
        statusGlyph.Text = model.ValidationGlyph;
        statusGlyph.Foreground = SpikeTheme.Brush(
            model.HasError ? "Mocassin.ErrorForeground" : "Mocassin.SuccessForeground");
        statusText.Text = model.StatusText;
        undoText.Text = model.UndoText;
        redoText.Text = model.RedoText;

        fieldsPanel.Children.Clear();
        foreach (var field in model.Fields)
        {
            fieldsPanel.Children.Add(BuildField(field));
        }

        matrixHost.Children.Clear();
        if (model.Matrix is { } matrix)
        {
            matrixHost.Children.Add(
                new TextBlock
                {
                    Text = "Option 6 data-shape control inside the detail pane",
                    FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
                    Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
                });
            matrixHost.Children.Add(new SpikeMatrixControl(matrix));
        }

        applyButton.IsEnabled = model.ApplyCommand.CanExecute(null);
        revertButton.IsEnabled = model.RevertCommand.CanExecute(null);
        undoButton.IsEnabled = model.UndoCommand.CanExecute(null);
        redoButton.IsEnabled = model.RedoCommand.CanExecute(null);
    }

    private static Grid BuildField(SpikeEditorField field)
    {
        var label = new TextBlock
        {
            Text = field.Label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = SpikeTheme.Brush("Mocassin.ForegroundMutedBrush"),
            FontSize = SpikeTheme.Double("Mocassin.CaptionFontSize", 12),
        };
        Control input = field.Kind switch
        {
            SpikeFieldKind.Text => BuildTextInput(field),
            SpikeFieldKind.Choice => BuildChoiceInput(field),
            SpikeFieldKind.Reference => BuildReferenceInput(field),
            _ => new TextBlock
            {
                Text = field.Text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = SpikeTheme.Brush("Mocassin.ForegroundBrush"),
            },
        };
        input.HorizontalAlignment = HorizontalAlignment.Stretch;
        Grid.SetColumn(input, 1);
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("112,*"),
            Children = { label, input },
        };
        return row;
    }

    private static TextBox BuildTextInput(SpikeEditorField field)
    {
        var box = new TextBox
        {
            IsReadOnly = field.IsReadOnly,
            MinHeight = SpikeTheme.Double("Mocassin.ControlMinHeight", 34),
        };
        AutomationProperties.SetName(box, $"{field.Label} field");
        box.Bind(
            TextBox.TextProperty,
            new Binding(nameof(SpikeEditorField.Text))
            {
                Source = field,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
        return box;
    }

    private static ComboBox BuildChoiceInput(SpikeEditorField field)
    {
        var combo = new ComboBox
        {
            ItemsSource = field.Choices,
            MinHeight = SpikeTheme.Double("Mocassin.ControlMinHeight", 34),
        };
        AutomationProperties.SetName(combo, $"{field.Label} field");
        combo.Bind(
            SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SpikeEditorField.SelectedChoice))
            {
                Source = field,
                Mode = BindingMode.TwoWay,
            });
        return combo;
    }

    private static StackPanel BuildReferenceInput(SpikeEditorField field)
    {
        var search = new TextBox
        {
            PlaceholderText = $"Search {field.Label.ToLowerInvariant()} by name or details",
        };
        AutomationProperties.SetName(search, $"{field.Label} search");
        search.Bind(
            TextBox.TextProperty,
            new Binding(nameof(SpikeEditorField.SearchText))
            {
                Source = field,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
        var combo = new ComboBox
        {
            ItemsSource = field.VisibleOptions,
            MinHeight = SpikeTheme.Double("Mocassin.ControlMinHeight", 34),
            ItemTemplate = new FuncDataTemplate<SpikeReferenceOption>(
                (option, _) => new TextBlock
                {
                    Text = option is null ? string.Empty : option.Display,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                }),
        };
        AutomationProperties.SetName(combo, $"{field.Label} field");
        AutomationProperties.SetHelpText(combo, "Searchable reference picker: name plus details, never a raw id.");
        combo.Bind(
            SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SpikeEditorField.SelectedOption))
            {
                Source = field,
                Mode = BindingMode.TwoWay,
            });
        return new StackPanel
        {
            Spacing = 4,
            Children = { search, combo },
        };
    }

    private static Button Button(string text, EventHandler<RoutedEventArgs> onClick)
    {
        var button = new Button
        {
            Content = text,
            MinHeight = SpikeTheme.Double("Mocassin.CompactButtonMinHeight", 26),
            Padding = SpikeTheme.Thickness("Mocassin.CompactButtonPadding", new Thickness(8, 2)),
        };
        AutomationProperties.SetName(button, text);
        button.Click += onClick;
        return button;
    }
}

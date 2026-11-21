// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Identifies how a dialog was closed and carries its result value.
/// </summary>
/// <param name="Kind">The kind of result.</param>
/// <param name="Text">The optional entered text.</param>
/// <param name="ChoiceId">The optional selected choice id.</param>
internal sealed record DialogResult(DialogResultKind Kind, string? Text = null, string? ChoiceId = null)
{
    /// <summary>
    /// Gets a confirmed result.
    /// </summary>
    public static DialogResult Confirmed { get; } = new(DialogResultKind.Confirmed);

    /// <summary>
    /// Gets a cancelled result.
    /// </summary>
    public static DialogResult Canceled { get; } = new(DialogResultKind.Canceled);

    /// <summary>
    /// Gets a dismissed result.
    /// </summary>
    public static DialogResult Dismissed { get; } = new(DialogResultKind.Dismissed);
}

// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Classifies a dialog result.
/// </summary>
internal enum DialogResultKind
{
    /// <summary>
    /// The dialog was accepted.
    /// </summary>
    Confirmed,

    /// <summary>
    /// The dialog was cancelled.
    /// </summary>
    Canceled,

    /// <summary>
    /// The dialog was dismissed without a decision.
    /// </summary>
    Dismissed,

    /// <summary>
    /// The user entered text.
    /// </summary>
    TextEntered,

    /// <summary>
    /// The user selected a choice.
    /// </summary>
    ChoiceSelected,
}

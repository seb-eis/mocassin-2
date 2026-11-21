// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// The interactive validation state shown by the status bar, the Overview banner; and the Problems channel.
/// </summary>
internal enum ValidationState
{
    /// <summary>
    /// No validation result is available yet.
    /// </summary>
    NotValidated,

    /// <summary>
    /// A debounced validation run is pending or in flight.
    /// </summary>
    Validating,

    /// <summary>
    /// The current result has no error.
    /// </summary>
    Valid,

    /// <summary>
    /// The current result has at least one error.
    /// </summary>
    Invalid,
}

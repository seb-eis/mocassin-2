// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// Identifies where a recovery snapshot came from.
/// </summary>
internal enum RecoverySource
{
    /// <summary>
    /// The Step 01 <c>&lt;file&gt;.autosave</c> sidecar next to the project.
    /// </summary>
    Sidecar,

    /// <summary>
    /// The fallback snapshot in <c>recovery/</c> when a sidecar write failed.
    /// </summary>
    AutosaveFallback,

    /// <summary>
    /// A snapshot written by the crash-recovery action.
    /// </summary>
    Crash,
}

// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Notifications;

/// <summary>
/// Severity of a transient notification.
/// </summary>
internal enum NotificationSeverity
{
    /// <summary>
    /// Informational notification.
    /// </summary>
    Information,

    /// <summary>
    /// Warning notification.
    /// </summary>
    Warning,

    /// <summary>
    /// Error notification.
    /// </summary>
    Error,
}

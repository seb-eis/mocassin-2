// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Notifications;

/// <summary>
/// Describes a transient notification.
/// </summary>
/// <param name="Message">The notification message.</param>
/// <param name="Severity">The severity.</param>
internal sealed record NotificationRequest(
    string Message,
    NotificationSeverity Severity = NotificationSeverity.Information);

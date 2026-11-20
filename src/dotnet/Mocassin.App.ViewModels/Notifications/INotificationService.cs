// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Notifications;

/// <summary>
/// Publishes transient notifications shown by the shell.
/// </summary>
internal interface INotificationService
{
    /// <summary>
    /// Gets the currently visible notifications.
    /// </summary>
    public IReadOnlyList<NotificationViewModel> Notifications { get; }

    /// <summary>
    /// Shows a notification.
    /// </summary>
    /// <param name="request">The notification to show.</param>
    public void Show(NotificationRequest request);

    /// <summary>
    /// Removes a notification.
    /// </summary>
    /// <param name="notification">The notification to remove.</param>
    public void Dismiss(NotificationViewModel notification);
}

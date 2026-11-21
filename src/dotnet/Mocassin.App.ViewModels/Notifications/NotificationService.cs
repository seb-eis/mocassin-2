// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Notifications;

/// <summary>
/// Collects notifications and marshals them to the UI thread.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private readonly IUiDispatcher dispatcher;
    private readonly ObservableCollection<NotificationViewModel> notifications = [];

    /// <summary>
    /// Initializes a new notification service.
    /// </summary>
    /// <param name="dispatcher">The UI dispatcher.</param>
    public NotificationService(IUiDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotificationViewModel> Notifications => notifications;

    /// <inheritdoc />
    public void Show(NotificationRequest request)
    {
        if (!dispatcher.IsOnUiThread)
        {
            dispatcher.Post(() => Show(request));
            return;
        }

        notifications.Add(new NotificationViewModel(request.Message, request.Severity, Dismiss));
        while (notifications.Count > 5)
        {
            notifications.RemoveAt(0);
        }
    }

    /// <inheritdoc />
    public void Dismiss(NotificationViewModel notification)
    {
        if (!dispatcher.IsOnUiThread)
        {
            dispatcher.Post(() => Dismiss(notification));
            return;
        }

        notifications.Remove(notification);
    }
}

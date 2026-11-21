// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Notifications;

/// <summary>
/// One visible notification.
/// </summary>
internal sealed partial class NotificationViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly Action<NotificationViewModel> dismiss;

    /// <summary>
    /// Initializes a new notification.
    /// </summary>
    /// <param name="message">The notification message.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="dismiss">The callback that removes the notification.</param>
    public NotificationViewModel(string message, NotificationSeverity severity, Action<NotificationViewModel> dismiss)
    {
        Message = message;
        Severity = severity;
        this.dismiss = dismiss;
    }

    /// <summary>
    /// Gets the notification message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the notification severity.
    /// </summary>
    public NotificationSeverity Severity { get; }

    /// <summary>
    /// Dismisses the notification.
    /// </summary>
    [RelayCommand]
    private void Close() => dismiss(this);
}

/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Notifications/NotificationHub.cs
 * Purpose: Library component: NotificationHub.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Notifications;

public sealed class NotificationHub : INotificationHub
{
    public static NotificationHub Instance { get; } = new();

    private NotifyPosition _position = NotifyPosition.TopRight;

    public NotifyPosition Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            PositionChanged?.Invoke(_position);
        }
    }

    public event Action<NotificationItem, int>? Notified;
    public event Action<NotifyPosition>? PositionChanged;

    private NotificationHub() { }

    public void Show(string title, string message, NotificationType type = NotificationType.Info, int durationMs = 4000)
    {
        if (durationMs < 500) durationMs = 500;
        Notified?.Invoke(new NotificationItem(title, message, type), durationMs);
    }
}
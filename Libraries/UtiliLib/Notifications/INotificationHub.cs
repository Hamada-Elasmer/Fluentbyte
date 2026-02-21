/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Notifications/INotificationHub.cs
 * Purpose: Library component: INotificationHub.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Notifications;

public interface INotificationHub
{
    NotifyPosition Position { get; set; }

    event Action<NotificationItem, int>? Notified;
    event Action<NotifyPosition>? PositionChanged;

    void Show(string title, string message,
        NotificationType type = NotificationType.Info,
        int durationMs = 4000);
}
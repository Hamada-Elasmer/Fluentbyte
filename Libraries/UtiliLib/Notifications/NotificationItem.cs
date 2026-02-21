/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Notifications/NotificationItem.cs
 * Purpose: Library component: NotificationItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace UtiliLib.Notifications;

public sealed class NotificationItem
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }

    public long TimestampUnixMs { get; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public NotificationItem(string title, string message, NotificationType type)
    {
        Title = title;
        Message = message;
        Type = type;
    }
}
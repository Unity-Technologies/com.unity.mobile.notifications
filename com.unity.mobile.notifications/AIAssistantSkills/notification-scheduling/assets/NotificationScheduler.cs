using System;
using System.Collections;
using UnityEngine;
using Unity.Notifications;

public class NotificationScheduler : MonoBehaviour
{
    const int k_NotificationId = 1;

    [SerializeField] string m_Title = "Title placeholder";
    [SerializeField] string m_Body = "Body placeholder";

    // How long before the event the invitation is sent.
    [SerializeField] int m_LeadTimeMinutes = 10;

    // ID registered under Project Settings > Mobile Notifications > Custom Icons.
    // Leave empty to use the application icon. Requires package 2.5.0 or later.
    [SerializeField] string m_AndroidSmallIconId = "";

    IEnumerator Start()
    {
        var args = NotificationCenterArgs.Default;
        args.AndroidChannelId = "default";
        args.AndroidChannelName = "Notifications";
        args.AndroidChannelDescription = "App notifications";
        if (!string.IsNullOrEmpty(m_AndroidSmallIconId))
            args.AndroidSmallIcon = m_AndroidSmallIconId;
        NotificationCenter.Initialize(args);

        // The app suspends while the system asks the user, so no further checks are needed.
        var request = NotificationCenter.RequestPermission();
        if (request.Status == NotificationsPermissionStatus.RequestPending)
            yield return request;
    }

    // eventTime is when the in-game event happens (local time).
    public void ScheduleBefore(DateTime eventTime)
    {
        var fireTime = eventTime - TimeSpan.FromMinutes(m_LeadTimeMinutes);

        // Notifications are meant to be scheduled for the future, on the expectation that
        // the user is not using the app at that time. Scheduling in the past makes no sense.
        if (fireTime < DateTime.Now)
            throw new ArgumentException(
                $"Scheduling in the past: notification time {fireTime} is before the current time {DateTime.Now}.",
                nameof(eventTime));

        var notification = new Notification
        {
            Identifier = k_NotificationId, // same ID replaces the previous one
            Title = m_Title,
            Text = m_Body,
        };

        NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(fireTime));
    }

    public void Cancel()
    {
        NotificationCenter.CancelScheduledNotification(k_NotificationId);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Notifications;

public class UnifiedTest : MonoBehaviour
{
    #region permissions
    public NotificationPresentation presentationOptions;
    #endregion

    #region UI
    public Text log;
    public InputField notificationTitle;
    public InputField notificationText;
    public Slider notificationDelay;
    public Toggle notificationShowInForeground;
    public Toggle notificationUseDateTime;
    public Toggle notificationRepeat;
    #endregion

    List<string> logLines = new();
    int lastScheduledId;
    bool cancelAll;

    void AddLog(params string[] lines)
    {
        const int kLogLineCount = 30;

        logLines.AddRange(lines);
        if (logLines.Count > kLogLineCount)
            logLines.RemoveRange(0, logLines.Count - kLogLineCount);
        log.text = string.Join("\n", logLines);
    }

    void Start()
    {
        var args = NotificationCenterArgs.Default;
        args.AndroidChannelId = "UnifiedNotifications";
        args.AndroidChannelName = "Unified notifications";
        args.AndroidChannelDescription = "Unified test scene channel";
        args.PresentationOptions = presentationOptions;
        NotificationCenter.Initialize(args);
        NotificationCenter.OnNotificationReceived += OnNotificationReceived;
        StartCoroutine(PermissionRequest());
    }

    IEnumerator PermissionRequest()
    {
        var request = NotificationCenter.RequestPermission();
        yield return request;
        AddLog($"Permission: {request.Status}");
    }

    void OnNotificationReceived(Notification notification)
    {
        PrintNotification("Notification received:", notification);
    }

    void PrintNotification(string header, Notification notification)
    {
        AddLog(
            header,
            $"  ID: {notification.Identifier}",
            $"  Title: {notification.Title}",
            $"  Text: {notification.Text}"
        );
    }

    public void OnSendNotification()
    {
        Debug.Log("Sending notification");
        AddLog($"Scheduled in {notificationDelay.value} seconds");

        var notification = new Notification()
        {
            Title = notificationTitle.text,
            Text = notificationText.text,
            ShowInForeground = notificationShowInForeground.isOn,
        };

        NotificationSchedule schedule;
        if (notificationUseDateTime.isOn)
        {
            var repeat = notificationRepeat.isOn ? NotificationRepeatInterval.Hourly : NotificationRepeatInterval.OneTime;
            schedule = new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(notificationDelay.value), repeat);
            if (notificationRepeat.isOn)
                AddLog($"Set to repeat: {repeat}");
        }
        else
        {
            schedule = new NotificationIntervalSchedule(TimeSpan.FromSeconds(notificationDelay.value), notificationRepeat.isOn);
            if (notificationRepeat.isOn)
                AddLog("Repeats around the same interval, if possible");
        }

        lastScheduledId = NotificationCenter.ScheduleNotification(notification, schedule);
        AddLog($"ID of scheduled notification is {lastScheduledId}");
    }

    public void OnCancelLast()
    {
        if (lastScheduledId != 0)
        {
            NotificationCenter.CancelScheduledNotification(lastScheduledId);
            AddLog($"Cancelled notification {lastScheduledId}");
            lastScheduledId = 0;
            cancelAll = false;
        }
        else if (cancelAll)
        {
            NotificationCenter.CancelAllScheduledNotifications();
            cancelAll = false;
            AddLog("All scheduled notifications cancelled");
        }
        else
        {
            cancelAll = true;
            AddLog("No last notification, press again to cancel ALL");
        }
    }

    public void OnLastNotification()
    {
        var notification = NotificationCenter.LastRespondedNotification;
        if (notification.HasValue)
            PrintNotification("Last responded notification:", notification.Value);
        else
            AddLog("No responded notification");
    }

    public void OnShowHelp()
    {
        logLines.Clear();
        AddLog(
            "Send notification with properties set in fields",
            "Cancel button cancels last scheduled notification if it hadn't arrived yet",
            "Double press on Cancel cancels all scheduled notifications",
            "Last responded button show info for last tapped notification",
            "When show in foreground is not chcked, notifications arrive silently to foreground app",
            "The DateTime toggle uses different scheduling method, both should work the same",
            "Repeat causes to repeat at same interval or hourly depending on the method",
            "NOTE: on Android inexact scheduling may be used, expect delays"
        );
    }
}

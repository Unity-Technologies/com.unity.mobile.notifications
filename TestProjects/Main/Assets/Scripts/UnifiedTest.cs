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

        var notification = new Notification()
        {
            Title = notificationTitle.text,
            Text = notificationText.text,
            ShowInForeground = notificationShowInForeground.isOn,
        };

        lastScheduledId = NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule()
        {
            Interval = TimeSpan.FromSeconds(notificationDelay.value),
        });

        AddLog($"Scheduled {lastScheduledId} in {notificationDelay.value} seconds");
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
}

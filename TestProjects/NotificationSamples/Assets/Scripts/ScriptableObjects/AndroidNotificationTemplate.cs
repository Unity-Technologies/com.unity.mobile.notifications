using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Notifications.Android;

[CreateAssetMenu(menuName = "Mobile Notifications/Android Notification Template")]
public class AndroidNotificationTemplate : ScriptableObject
{
    [Space(10)]
    [Header("General")]
    public string ButtonName = "Send A Notifiation";
    public string Channel = "default_channel";
    public int FireInSeconds;

    [Space(10)]
    [Header("Notification Parameters")]
    public string Title = "";
    [TextArea]
    public string Text = "";
    public string SmallIcon = "";
    public string LargeIcon = "";
    public NotificationStyle NotificationStyle = NotificationStyle.None;
    public Color Color = Color.black;
    public int Number = -1;
    public bool ShouldAutoCancel = false;
    public bool UsesStopWatch = false;
    public string Group = "";
    public bool GroupSummary;
    public GroupAlertBehaviours GroupAlertBehaviours = GroupAlertBehaviours.GroupAlertAll;
    public string SortKey = "";
    [TextArea]
    public string IntentData = "";
    public bool ShowTimestamp = false;
}

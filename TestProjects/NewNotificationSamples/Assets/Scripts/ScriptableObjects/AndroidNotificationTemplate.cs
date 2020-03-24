using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_ANDROID
using Unity.Notifications.Android;
#endif

[CreateAssetMenu(menuName = "Mobile Notifications/Android Notification Template")]
public class AndroidNotificationTemplate : ScriptableObject
{
    #if PLATFORM_ANDROID
    [Space(10)]
    [Header("General")]
    public string buttonName = "Send A Notifiation";
    public string channel = "default_channel";
    public int fireInSeconds;

    [Space(10)]
    [Header("Notification Parameters")]
    public string title = "";
    [TextArea]
    public string text = "";
    public string smallIcon = "";
    public string largeIcon = "";
    public NotificationStyle notificationStyle = NotificationStyle.None;
    public Color color = Color.black;
    public int number = -1;
    public bool shouldAutoCancel = false;
    public bool usesStopWatch = false;
    public string group = "";
    public bool groupSummary;
    public GroupAlertBehaviours groupAlertBehaviours = GroupAlertBehaviours.GroupAlertAll;
    public string sortKey = "";
    [TextArea]
    public string intentData = "";
    public bool showTimestamp = false;
    #endif
}

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
    public string title;
    [TextArea]
    public string text;
    public string smallIcon;
    public string largeIcon;
    public NotificationStyle notificationStyle;
    public Color color;
    public int number;
    public bool shouldAutoCancel;
    public bool usesStopWatch;
    public string group;
    public bool groupSummary;
    public GroupAlertBehaviours groupAlertBehaviours;
    public string sortKey;
    [TextArea]
    public string intentData;
    public bool showTimestamp;
    #endif
}

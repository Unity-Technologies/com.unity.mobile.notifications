using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
#endif

[CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Location Trigger)")]
public class iOSNotificationTemplateLocationTrigger : ScriptableObject
{
    #if PLATFORM_IOS
    [Space(10)]
    [Header("General")]
    public string buttonName = "Send A Notification (Location Trigger)";

    [Space(10)]
    [Header("Notification Parameters")]
    public string identifier;
    public string categoryIdentifier = "";
    public string threadIdentifier = "";
    public string title = "";
    public string subtitle = "";
    [TextArea]
    public string body;
    public bool showInForeground = false;
    public PresentationOption presentationOptions = PresentationOption.Alert | PresentationOption.Sound;
    public Int32 badge = -1;
    [TextArea]
    public string data = "";

    [Space(10)]
    [Header("Location Trigger")]
    public float centerX = 0f;
    public float centerY = 0f;
    public float radius = 2f;
    public bool notifyOnEntry = true;
    public bool notifyOnExit = false;
    #endif
}

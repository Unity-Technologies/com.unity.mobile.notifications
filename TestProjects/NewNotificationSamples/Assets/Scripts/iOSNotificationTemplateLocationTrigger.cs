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
    public string categoryIdentifier;
    public string threadIdentifier;
    public string title;
    public string subtitle;
    [TextArea]
    public string body;
    public bool showInForeground;
    public PresentationOption presentationOptions;
    public Int32 badge;
    [TextArea]
    public string data;

    [Space(10)]
    [Header("Location Trigger")]
    public float locationTriggerCenterX;
    public float locationTriggerCenterY;
    public float locationTriggerRadius;
    public bool locationTriggerNotifyOnEntry;
    public bool locationTriggerNotifyOnExit;
    #endif
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
#endif

[CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Time Trigger)")]
public class iOSNotificationTemplateTimeTrigger : ScriptableObject
{
    #if PLATFORM_IOS
    [Space(10)]
    [Header("General")]
    public string buttonName = "Send A Notification (Time Trigger)";

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
    [Header("Time Trigger")]
    public Int32 timeTriggerInterval;
    public bool repeats;
    #endif
}

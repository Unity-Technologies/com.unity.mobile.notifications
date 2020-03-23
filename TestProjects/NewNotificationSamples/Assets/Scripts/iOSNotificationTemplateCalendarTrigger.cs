using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
#endif

[CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Calendar Trigger)")]
public class iOSNotificationTemplateCalendarTrigger : ScriptableObject
{
    #if PLATFORM_IOS
    [Space(10)]
    [Header("General")]
    public string buttonName = "Send A Notification (Calendar Trigger)";

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
    [Header("Calendar Trigger")]
    public Int32 calendarTriggerYear;
    public Int32 calendarTriggerMonth;
    public Int32 calendarTriggerDay;
    public Int32 calendarTriggerHour;
    public Int32 calendarTriggerMinute;
    public Int32 calendarTriggerSecond;
    #endif
}

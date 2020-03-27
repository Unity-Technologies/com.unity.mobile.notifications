using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PLATFORM_IOS
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications.Tests.Sample
{
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
        public string categoryIdentifier = "";
        public string threadIdentifier = "";
        public string title = "";
        public string subtitle = "";
        [TextArea]
        public string body = "";
        public bool showInForeground = false;
        public PresentationOption presentationOptions = PresentationOption.Alert | PresentationOption.Sound;
        public Int32 badge = -1;
        [TextArea]
        public string data = "";

        [Space(10)]
        [Header("Calendar Trigger")]
        public bool offsetFromCurrentDate = false;
        public Int32 year = -1;
        public Int32 month = -1;
        public Int32 day = -1;
        public Int32 hour = -1;
        public Int32 minute = -1;
        public Int32 second = -1;
    #endif
    }
}

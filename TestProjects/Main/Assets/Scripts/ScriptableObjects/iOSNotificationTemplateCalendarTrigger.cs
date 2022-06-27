using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS || UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications.Tests.Sample
{
    [CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Calendar Trigger)")]
    public class iOSNotificationTemplateCalendarTrigger : ScriptableObject
    {
#if UNITY_IOS || UNITY_EDITOR
        [Space(10)]
        [Header("General")]
        public string ButtonName = "Send A Notification (Calendar Trigger)";

        [Space(10)]
        [Header("Notification Parameters")]
        public string Identifier;
        public string CategoryIdentifier = "";
        public string ThreadIdentifier = "";
        public string Title = "";
        public string Subtitle = "";
        [TextArea]
        public string Body = "";
        public bool ShowInForeground = false;
        public PresentationOption PresentationOptions = PresentationOption.Alert | PresentationOption.Sound;
        public Int32 Badge = -1;
        [TextArea]
        public string Data = "";

        [Space(10)]
        [Header("Calendar Trigger")]
        public bool OffsetFromCurrentDate = false;
        public Int32 Year = -1;
        public Int32 Month = -1;
        public Int32 Day = -1;
        public Int32 Hour = -1;
        public Int32 Minute = -1;
        public Int32 Second = -1;
#endif
    }
}

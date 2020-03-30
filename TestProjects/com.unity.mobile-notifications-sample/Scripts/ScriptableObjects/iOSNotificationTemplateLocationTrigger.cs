using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS || UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications.Tests.Sample
{
    [CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Location Trigger)")]
    public class iOSNotificationTemplateLocationTrigger : ScriptableObject
    {
#if UNITY_IOS || UNITY_EDITOR
        [Space(10)]
        [Header("General")]
        public string ButtonName = "Send A Notification (Location Trigger)";

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
        [Header("Location Trigger")]
        public float CenterX = 0f;
        public float CenterY = 0f;
        public float Radius = 2f;
        public bool NotifyOnEntry = true;
        public bool NotifyOnExit = false;
#endif
    }
}

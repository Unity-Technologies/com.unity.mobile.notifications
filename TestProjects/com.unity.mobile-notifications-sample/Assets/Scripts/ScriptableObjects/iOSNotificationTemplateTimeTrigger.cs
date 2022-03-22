using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_IOS || UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications.Tests.Sample
{
    [CreateAssetMenu(menuName = "Mobile Notifications/iOS Notification Template (Time Trigger)")]
    public class iOSNotificationTemplateTimeTrigger : ScriptableObject
    {
#if UNITY_IOS || UNITY_EDITOR
        [Space(10)]
        [Header("General")]
        public string ButtonName = "Send A Notification (Time Trigger)";

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
        [Header("Time Trigger")]
        public Int32 TimeTriggerInterval;
        public bool Repeats = false;

        [Serializable]
        public struct UserDataItem
        {
            public string Key;
            public string Value;
        }
        public UserDataItem[] UserInfo;

        public Dictionary<string, string> UserInfoDict
        {
            get
            {
                var dict = new Dictionary<string, string>();
                if (UserInfo != null)
                    foreach (var item in UserInfo)
                        dict.Add(item.Key, item.Value);
                return dict;
            }
        }

        public string[] Attachments;
#endif
    }
}

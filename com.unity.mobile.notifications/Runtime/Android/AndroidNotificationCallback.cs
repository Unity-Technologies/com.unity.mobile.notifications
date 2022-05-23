using System;
using UnityEngine;

namespace Unity.Notifications.Android
{
    class NotificationCallback : AndroidJavaProxy
    {
        public NotificationCallback() : base("com.unity.androidnotifications.NotificationCallback")
        {
        }

        public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] args)
        {
            if (methodName.Equals("onSentNotification", StringComparison.InvariantCulture) && args != null && args.Length == 1)
            {
                onSentNotification(args[0]);
                return null;
            }

            return base.Invoke(methodName, args);
        }

        public void onSentNotification(AndroidJavaObject notification)
        {
            AndroidReceivedNotificationMainThreadDispatcher.GetInstance().EnqueueReceivedNotification(notification);
        }
    }
}

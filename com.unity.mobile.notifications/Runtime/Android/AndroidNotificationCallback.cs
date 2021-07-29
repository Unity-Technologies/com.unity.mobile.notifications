using UnityEngine;

namespace Unity.Notifications.Android
{
    class NotificationCallback : AndroidJavaProxy
    {
        public NotificationCallback() : base("com.unity.androidnotifications.NotificationCallback")
        {
        }

        public void onSentNotification(AndroidJavaObject notification)
        {
            AndroidReceivedNotificationMainThreadDispatcher.GetInstance().EnqueueReceivedNotification(notification);
        }
    }
}

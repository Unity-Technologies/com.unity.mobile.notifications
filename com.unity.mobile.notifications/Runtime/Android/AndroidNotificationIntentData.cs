using UnityEngine;

namespace Unity.Notifications.Android
{
    public class AndroidNotificationIntentData
    {
        public int Id { get; }

        public string Channel { get; }

        public AndroidNotification Notification { get; }

        public AndroidNotificationIntentData(int id, string channelId, AndroidNotification notification)
        {
            Id = id;
            Channel = channelId;
            Notification = notification;
        }
    }
}

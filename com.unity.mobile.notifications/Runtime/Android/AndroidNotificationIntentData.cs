using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// This is a wrapper for the AndroidNotification with the id and the channel.
    /// </summary>
    public class AndroidNotificationIntentData
    {
        /// <summary>
        /// The id of the notification.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The channel id that the notification was sent to.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Returns the AndroidNotification.
        /// </summary>
        public AndroidNotification Notification { get; }

        /// <summary>
        /// Create an AndroidNotificationIntentData with AndroidNotification, id and channel id.
        /// </summary>
        public AndroidNotificationIntentData(int id, string channelId, AndroidNotification notification)
        {
            Id = id;
            Channel = channelId;
            Notification = notification;
        }
    }
}

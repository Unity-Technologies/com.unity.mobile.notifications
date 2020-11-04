using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// TODO
    /// </summary>
    public class AndroidNotificationIntentData
    {
        /// <summary>
        /// TODO - maybe can be internal?
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// TODO - maybe can be internal?
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// TODO - maybe can be internal?
        /// </summary>
        public AndroidNotification Notification { get; }

        /// <summary>
        /// TODO
        /// </summary>
        public AndroidNotificationIntentData(int id, string channelId, AndroidNotification notification)
        {
            Id = id;
            Channel = channelId;
            Notification = notification;
        }
    }
}

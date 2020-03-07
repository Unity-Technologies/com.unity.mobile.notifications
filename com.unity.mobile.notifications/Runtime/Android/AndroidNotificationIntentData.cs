using UnityEngine;

namespace Unity.Notifications.Android
{
    public class AndroidNotificationIntentData : MonoBehaviour
    {
        internal int id;
        internal string channel;
        internal AndroidNotification notification;

        public int Id
        {
            get { return id; }
        }

        public string Channel
        {
            get { return channel; }
        }

        public AndroidNotification Notification
        {
            get { return notification; }
        }
    }
}

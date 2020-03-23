using UnityEngine;

namespace Unity.Notifications.Android
{
    public class AndroidNotificationIntentData
    {
        protected int m_Id;
        protected string m_Channel;
        protected AndroidNotification m_Notification;

        public AndroidNotificationIntentData(int id, string channel, AndroidNotification notification)
        {
            m_Id = id;
            m_Channel = channel;
            m_Notification = notification;
        }

        public int Id
        {
            get { return m_Id; }
        }

        public string Channel
        {
            get { return m_Channel; }
        }

        public AndroidNotification Notification
        {
            get { return m_Notification; }
        }
    }
}

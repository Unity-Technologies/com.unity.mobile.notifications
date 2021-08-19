using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// Class that queues the received notifications and triggers the notification callbacks.
    /// </summary>
    public class AndroidReceivedNotificationMainThreadDispatcher : MonoBehaviour
    {
        private static AndroidReceivedNotificationMainThreadDispatcher instance = null;

        private List<AndroidJavaObject> m_ReceivedNotificationQueue = new List<AndroidJavaObject>();

        private List<AndroidJavaObject> m_ReceivedNotificationList = new List<AndroidJavaObject>();

        internal void EnqueueReceivedNotification(AndroidJavaObject notification)
        {
            lock (this)
            {
                m_ReceivedNotificationQueue.Add(notification);
            }
        }

        internal static AndroidReceivedNotificationMainThreadDispatcher GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// Update is called once per frame.
        /// </summary>
        public void Update()
        {
            // Note: Don't call callbacks while locking receivedNotificationQueue, otherwise there's a risk
            //       that callback might introduce an operations which would create a deadlock
            lock (this)
            {
                if (m_ReceivedNotificationQueue.Count == 0)
                    return;
                var temp = m_ReceivedNotificationQueue;
                m_ReceivedNotificationQueue = m_ReceivedNotificationList;
                m_ReceivedNotificationList = temp;
            }

            foreach (var notification in m_ReceivedNotificationList)
            {
                try
                {
                    AndroidNotificationCenter.ReceivedNotificationCallback(notification);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            m_ReceivedNotificationList.Clear();
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        void OnDestroy()
        {
            instance = null;
        }
    }
}

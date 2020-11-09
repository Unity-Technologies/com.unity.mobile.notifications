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

        private static readonly Queue<AndroidJavaObject> s_ReceivedNotificationQueue = new Queue<AndroidJavaObject>();

        private static readonly List<AndroidJavaObject> s_ReceivedNotificationList = new List<AndroidJavaObject>();

        internal static void EnqueueReceivedNotification(AndroidJavaObject intent)
        {
            lock (s_ReceivedNotificationQueue)
            {
                s_ReceivedNotificationQueue.Enqueue(intent);
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
            lock (s_ReceivedNotificationQueue)
            {
                s_ReceivedNotificationList.AddRange(s_ReceivedNotificationQueue);
                s_ReceivedNotificationQueue.Clear();
            }

            foreach (var notification in s_ReceivedNotificationList)
            {
                AndroidNotificationCenter.ReceivedNotificationCallback(notification);
            }

            s_ReceivedNotificationList.Clear();
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

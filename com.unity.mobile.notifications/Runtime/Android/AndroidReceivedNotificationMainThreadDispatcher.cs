using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{
    public class AndroidReceivedNotificationMainThreadDispatcher : MonoBehaviour
    {
        private static AndroidReceivedNotificationMainThreadDispatcher instance = null;

        private static Queue<AndroidJavaObject> s_ReceivedNotificationQueue = new Queue<AndroidJavaObject>();

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

        public void Update()
        {
            var tempList = new List<AndroidJavaObject>();
            // Note: Don't call callbacks while locking receivedNotificationQueue, otherwise there's a risk
            //       that callback might introduce an operations which would create a deadlock
            lock (s_ReceivedNotificationQueue)
            {
                tempList.AddRange(s_ReceivedNotificationQueue);
                s_ReceivedNotificationQueue.Clear();
            }

            foreach (var t in tempList)
            {
                AndroidNotificationCenter.ReceivedNotificationCallback(t);
            }
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

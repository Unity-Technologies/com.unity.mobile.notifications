using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{
    public class AndroidReceivedNotificationMainThreadDispatcher : MonoBehaviour
    {
        private static AndroidReceivedNotificationMainThreadDispatcher instance = null;

        private static Queue<AndroidJavaObject>  receivedNotificationQueue = new Queue<AndroidJavaObject>();

        internal static void EnqueueReceivedNotification(AndroidJavaObject intent)
        {
            lock (receivedNotificationQueue)
            {
                receivedNotificationQueue.Enqueue(intent);
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
            lock (receivedNotificationQueue) 
            {
                tempList.AddRange(receivedNotificationQueue);
                receivedNotificationQueue.Clear();
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

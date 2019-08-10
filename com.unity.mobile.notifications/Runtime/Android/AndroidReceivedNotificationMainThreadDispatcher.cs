using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{    
    public class AndroidReceivedNotificationMainThreadDispatcher : MonoBehaviour {

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
        
        public void Update() {
            lock(receivedNotificationQueue) {
                while (receivedNotificationQueue.Count > 0)
                {
                    var intentData = receivedNotificationQueue.Dequeue();
                    AndroidNotificationCenter.ReceivedNotificationCallback(intentData);
                }
            }
        }

        void Awake() {
            if (instance == null) {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

        void OnDestroy() {
            instance = null;
        }
    }
}
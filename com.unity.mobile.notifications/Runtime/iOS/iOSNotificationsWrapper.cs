using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

#pragma warning disable 162

namespace Unity.Notifications.iOS
{
    internal class iOSNotificationsWrapper : MonoBehaviour
    {
#if DEVELOPMENT_BUILD
        [DllImport("__Internal")]
        private static extern int _NativeSizeof_iOSNotificationAuthorizationData();
#endif

        [DllImport("__Internal")]
        private static extern void _RequestAuthorization(IntPtr request, Int32 options, bool registerForRemote);

        [DllImport("__Internal")]
        private static extern void _ScheduleLocalNotification(IntPtr ptr);

        [DllImport("__Internal")]
        private static extern void _SetNotificationReceivedDelegate(NotificationReceivedCallback callback);

        [DllImport("__Internal")]
        private static extern void _SetRemoteNotificationReceivedDelegate(NotificationReceivedCallback callback);

        [DllImport("__Internal")]
        private static extern void _SetAuthorizationRequestReceivedDelegate(AuthorizationRequestCallback callback);

        [DllImport("__Internal")]
        private static extern IntPtr _GetNotificationSettings();

        [DllImport("__Internal")]
        private static extern Int32 _GetScheduledNotificationDataCount();

        [DllImport("__Internal")]
        private static extern IntPtr _GetScheduledNotificationDataAt(Int32 index);

        [DllImport("__Internal")]
        private static extern Int32 _GetDeliveredNotificationDataCount();

        [DllImport("__Internal")]
        private static extern IntPtr _GetDeliveredNotificationDataAt(Int32 index);

        [DllImport("__Internal")]
        internal static extern void _RemoveScheduledNotification(string identifier);

        [DllImport("__Internal")]
        internal static extern void _RemoveAllScheduledNotifications();

        [DllImport("__Internal")]
        internal static extern void _RemoveDeliveredNotification(string identifier);

        [DllImport("__Internal")]
        private static extern void _SetApplicationBadge(Int32 badge);

        [DllImport("__Internal")]
        private static extern Int32 _GetApplicationBadge();

        [DllImport("__Internal")]
        private static extern bool _GetAppOpenedUsingNotification();

        [DllImport("__Internal")]
        internal static extern void _RemoveAllDeliveredNotifications();

        [DllImport("__Internal")]
        private static extern IntPtr _GetLastNotificationData();

        [DllImport("__Internal")]
        private static extern void _FreeUnmanagedMemory(IntPtr ptr);

        [DllImport("__Internal")]
        private static extern void _FreeUnmanagediOSNotificationData(IntPtr ptr);

        private delegate void AuthorizationRequestCallback(IntPtr request, iOSAuthorizationRequestData data);
        private delegate void NotificationReceivedCallback(IntPtr notificationData);

#if UNITY_IOS && !UNITY_EDITOR
        private static NotificationReceivedCallback s_OnNotificationReceived = null;
        private static NotificationReceivedCallback s_OnRemoteNotificationReceived = null;
#endif

        public static void RegisterAuthorizationRequestCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
    #if DEVELOPMENT_BUILD
            {
                var nativeSize = _NativeSizeof_iOSNotificationAuthorizationData();
                var managedSize = Marshal.SizeOf(typeof(iOSAuthorizationRequestData));
                if (nativeSize != managedSize)
                {
                    var error = string.Format("Native/managed struct size missmatch: {0} vs {1}", nativeSize, managedSize);
                    Debug.LogError(error);
                    throw new Exception(error);
                }
            }
    #endif
            _SetAuthorizationRequestReceivedDelegate(AuthorizationRequestReceived);
#endif
        }

        public static void RegisterOnReceivedRemoteNotificationCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_OnRemoteNotificationReceived = new NotificationReceivedCallback(RemoteNotificationReceived);
            _SetRemoteNotificationReceivedDelegate(s_OnRemoteNotificationReceived);
#endif
        }

        public static void RegisterOnReceivedCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_OnNotificationReceived = new NotificationReceivedCallback(NotificationReceived);
            _SetNotificationReceivedDelegate(s_OnNotificationReceived);
#endif
        }

        [MonoPInvokeCallback(typeof(AuthorizationRequestCallback))]
        public static void AuthorizationRequestReceived(IntPtr request, iOSAuthorizationRequestData data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            AuthorizationRequest.OnAuthorizationRequestCompleted(request, data);
#endif
        }

        [MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
        public static void RemoteNotificationReceived(IntPtr notificationDataPtr)
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationData data;
            data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));

            iOSNotificationCenter.OnReceivedRemoteNotification(data);
#endif
        }

        [MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
        public static void NotificationReceived(IntPtr notificationDataPtr)
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationData data;
            data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));

            iOSNotificationCenter.OnSentNotification(data);
#endif
        }

        public static void RequestAuthorization(IntPtr request, int options, bool registerRemote)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _RequestAuthorization(request, options, registerRemote);
#endif
        }

        public static iOSNotificationSettings GetNotificationSettings()
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationSettings settings;

            IntPtr ptr = _GetNotificationSettings();
            settings = (iOSNotificationSettings)Marshal.PtrToStructure(ptr, typeof(iOSNotificationSettings));
            _FreeUnmanagedMemory(ptr);

            return settings;
#else
            return new iOSNotificationSettings();
#endif
        }

        public static void ScheduleLocalNotification(iOSNotificationData data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            Marshal.StructureToPtr(data, ptr, false);

            _ScheduleLocalNotification(ptr);
#endif
        }

        public static iOSNotificationData[] GetDeliveredNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var size = _GetDeliveredNotificationDataCount();

            var dataList = new List<iOSNotificationData>();
            for (var i = 0; i < size; i++)
            {
                iOSNotificationData data;
                IntPtr ptr = _GetDeliveredNotificationDataAt(i);

                if (ptr != IntPtr.Zero)
                {
                    data = (iOSNotificationData)Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
                    dataList.Add(data);
                    _FreeUnmanagediOSNotificationData(ptr);
                }
            }

            return dataList.ToArray();
#else
            return new iOSNotificationData[] {};
#endif
        }

        public static iOSNotificationData[] GetScheduledNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var size = _GetScheduledNotificationDataCount();

            var dataList = new List<iOSNotificationData>();
            for (var i = 0; i < size; i++)
            {
                iOSNotificationData data;
                IntPtr ptr = _GetScheduledNotificationDataAt(i);

                if (ptr != IntPtr.Zero)
                {
                    data = (iOSNotificationData)Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
                    dataList.Add(data);
                    _FreeUnmanagediOSNotificationData(ptr);
                }
            }

            return dataList.ToArray();
#else
            return new iOSNotificationData[] {};
#endif
        }

        public static void SetApplicationBadge(int badge)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _SetApplicationBadge(badge);
#endif
        }

        public static int GetApplicationBadge()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _GetApplicationBadge();
#else
            return 0;
#endif
        }

        public static bool GetAppOpenedUsingNotification()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _GetAppOpenedUsingNotification();
#else
            return false;
#endif
        }

        public static iOSNotificationData? GetLastNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_GetAppOpenedUsingNotification())
            {
                iOSNotificationData data;
                IntPtr ptr = _GetLastNotificationData();

                if (ptr != IntPtr.Zero)
                {
                    data = (iOSNotificationData)Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
                    _FreeUnmanagediOSNotificationData(ptr);
                    return data;
                }
            }
#endif
            return null;
        }
    }
}
#pragma warning restore 162

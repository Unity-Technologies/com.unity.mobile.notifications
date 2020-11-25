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

        [DllImport("__Internal")]
        private static extern int _NativeSizeof_iOSNotificationData();
#endif

        [DllImport("__Internal")]
        private static extern void _RequestAuthorization(IntPtr request, Int32 options, bool registerForRemote);

        [DllImport("__Internal")]
        private static extern void _ScheduleLocalNotification(iOSNotificationData data);

        [DllImport("__Internal")]
        private static extern void _SetNotificationReceivedDelegate(NotificationReceivedCallback callback);

        [DllImport("__Internal")]
        private static extern void _SetRemoteNotificationReceivedDelegate(NotificationReceivedCallback callback);

        [DllImport("__Internal")]
        private static extern void _SetAuthorizationRequestReceivedDelegate(AuthorizationRequestCallback callback);

        [DllImport("__Internal")]
        private static extern IntPtr _GetNotificationSettings();

        [DllImport("__Internal")]
        private static extern IntPtr _GetScheduledNotificationDataArray(out Int32 count);

        [DllImport("__Internal")]
        private static extern IntPtr _GetDeliveredNotificationDataArray(out Int32 count);

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

        [DllImport("__Internal")]
        private static extern void _FreeUnmanagediOSNotificationDataArray(IntPtr ptr, int count);

        private delegate void AuthorizationRequestCallback(IntPtr request, iOSAuthorizationRequestData data);
        private delegate void NotificationReceivedCallback(iOSNotificationData notificationData);

#if UNITY_IOS && !UNITY_EDITOR && DEVELOPMENT_BUILD
        static iOSNotificationsWrapper()
        {
            VerifyNativeManagedSize(_NativeSizeof_iOSNotificationAuthorizationData(), typeof(iOSAuthorizationRequestData));
            VerifyNativeManagedSize(_NativeSizeof_iOSNotificationData(), typeof(iOSNotificationData));
        }

        static void VerifyNativeManagedSize(int nativeSize, Type managedType)
        {
            var managedSize = Marshal.SizeOf(managedType);
            if (nativeSize != managedSize)
                throw new Exception(string.Format("Native/managed struct size missmatch: {0} vs {1}", nativeSize, managedSize));
        }

#endif

        public static void RegisterAuthorizationRequestCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _SetAuthorizationRequestReceivedDelegate(AuthorizationRequestReceived);
#endif
        }

        public static void RegisterOnReceivedRemoteNotificationCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _SetRemoteNotificationReceivedDelegate(RemoteNotificationReceived);
#endif
        }

        public static void RegisterOnReceivedCallback()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _SetNotificationReceivedDelegate(NotificationReceived);
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
        public static void RemoteNotificationReceived(iOSNotificationData data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.OnReceivedRemoteNotification(data);
#endif
        }

        [MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
        public static void NotificationReceived(iOSNotificationData data)
        {
#if UNITY_IOS && !UNITY_EDITOR
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
            _ScheduleLocalNotification(data);
#endif
        }

        public static iOSNotificationData[] GetDeliveredNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            int count;
            var ptr = _GetDeliveredNotificationDataArray(out count);
            return MarshalAndFreeNotificationDataArray(ptr, count);
#else
            return null;
#endif
        }

        public static iOSNotificationData[] GetScheduledNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            int count;
            var ptr = _GetScheduledNotificationDataArray(out count);
            return MarshalAndFreeNotificationDataArray(ptr, count);
#else
            return null;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        static iOSNotificationData[] MarshalAndFreeNotificationDataArray(IntPtr ptr, int count)
        {
            if (count == 0 || ptr == IntPtr.Zero)
                return null;

            var dataArray = new iOSNotificationData[count];
            var structSize = Marshal.SizeOf(typeof(iOSNotificationData));
            var next = ptr;
            for (var i = 0; i < count; ++i)
            {
                dataArray[i] = (iOSNotificationData)Marshal.PtrToStructure(next, typeof(iOSNotificationData));
                next = next + structSize;
            }
            _FreeUnmanagediOSNotificationDataArray(ptr, count);

            return dataArray;
        }

#endif

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

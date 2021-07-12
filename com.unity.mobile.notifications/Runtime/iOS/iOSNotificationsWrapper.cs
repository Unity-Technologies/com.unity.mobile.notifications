using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

#pragma warning disable 162

namespace Unity.Notifications.iOS
{
    internal struct iOSNotificationWithUserInfo
    {
        internal iOSNotificationData data;
        internal Dictionary<string, string> userInfo;
    }

    internal class iOSNotificationsWrapper : MonoBehaviour
    {
#if DEVELOPMENT_BUILD
        [DllImport("__Internal")]
        private static extern int _NativeSizeof_iOSNotificationAuthorizationData();

        [DllImport("__Internal")]
        private static extern int _NativeSizeof_iOSNotificationData();

        [DllImport("__Internal")]
        private static extern int _NativeSizeof_NotificationSettingsData();
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
        private static extern iOSNotificationSettings _GetNotificationSettings();

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
        private static extern void _FreeUnmanagediOSNotificationDataArray(IntPtr ptr, int count);

        [DllImport("__Internal")]
        internal static extern IntPtr _AddItemToNSDictionary(IntPtr dict, string key, string value);

        [DllImport("__Internal")]
        private static extern void _ReadNSDictionary(IntPtr handle, IntPtr nsDict, ReceiveNSDictionaryKeyValueCallback callback);

        private delegate void AuthorizationRequestCallback(IntPtr request, iOSAuthorizationRequestData data);
        private delegate void NotificationReceivedCallback(iOSNotificationData notificationData);
        private delegate void ReceiveNSDictionaryKeyValueCallback(IntPtr dict, string key, string value);

#if UNITY_IOS && !UNITY_EDITOR && DEVELOPMENT_BUILD
        static iOSNotificationsWrapper()
        {
            VerifyNativeManagedSize(_NativeSizeof_iOSNotificationAuthorizationData(), typeof(iOSAuthorizationRequestData));
            VerifyNativeManagedSize(_NativeSizeof_iOSNotificationData(), typeof(iOSNotificationData));
            VerifyNativeManagedSize(_NativeSizeof_NotificationSettingsData(), typeof(iOSNotificationSettings));
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
            iOSNotificationCenter.OnReceivedRemoteNotification(NotificationDataToDataWithUserInfo(data));
#endif
        }

        [MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
        public static void NotificationReceived(iOSNotificationData data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            iOSNotificationCenter.OnSentNotification(NotificationDataToDataWithUserInfo(data));
#endif
        }

        static iOSNotificationWithUserInfo NotificationDataToDataWithUserInfo(iOSNotificationData data)
        {
            iOSNotificationWithUserInfo ret;
            ret.data = data;
            ret.data.userInfo = IntPtr.Zero;
            ret.userInfo = NSDictionaryToCs(data.userInfo);
            return ret;
        }

        [MonoPInvokeCallback(typeof(ReceiveNSDictionaryKeyValueCallback))]
        private static void ReceiveNSDictionaryKeyValue(IntPtr dict, string key, string value)
        {
            GCHandle handle = GCHandle.FromIntPtr(dict);
            var dictionary = (Dictionary<string, string>)handle.Target;
            if (dictionary == null)
                return;
            dictionary[key] = value;
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
            return _GetNotificationSettings();
#else
            return new iOSNotificationSettings();
#endif
        }

        public static void ScheduleLocalNotification(iOSNotificationWithUserInfo data)
        {
#if UNITY_IOS && !UNITY_EDITOR
            data.data.userInfo = iOSNotificationsWrapper.CsDictionaryToObjC(data.userInfo);
            _ScheduleLocalNotification(data.data);
#endif
        }

        public static iOSNotificationWithUserInfo[] GetDeliveredNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            int count;
            var ptr = _GetDeliveredNotificationDataArray(out count);
            return MarshalAndFreeNotificationDataArray(ptr, count);
#else
            return null;
#endif
        }

        public static iOSNotificationWithUserInfo[] GetScheduledNotificationData()
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
        static iOSNotificationWithUserInfo[] MarshalAndFreeNotificationDataArray(IntPtr ptr, int count)
        {
            if (count == 0 || ptr == IntPtr.Zero)
                return null;

            var dataArray = new iOSNotificationWithUserInfo[count];
            var structSize = Marshal.SizeOf(typeof(iOSNotificationData));
            var next = ptr;
            for (var i = 0; i < count; ++i)
            {
                dataArray[i].data = (iOSNotificationData)Marshal.PtrToStructure(next, typeof(iOSNotificationData));
                dataArray[i].userInfo = NSDictionaryToCs(dataArray[i].data.userInfo);
                dataArray[i].data.userInfo = IntPtr.Zero;
                next = next + structSize;
            }
            _FreeUnmanagediOSNotificationDataArray(ptr, count);

            return dataArray;
        }

#endif

        public static IntPtr CsDictionaryToObjC(Dictionary<string, string> userInfo)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (userInfo == null)
                return IntPtr.Zero;

            IntPtr dict = IntPtr.Zero;
            foreach (var item in userInfo)
                dict = _AddItemToNSDictionary(dict, item.Key, item.Value);
            return dict;
#else
            return IntPtr.Zero;
#endif
        }

        public static Dictionary<string, string> NSDictionaryToCs(IntPtr dict)
        {
#if UNITY_IOS && !UNITY_EDITOR
            var ret = new Dictionary<string, string>();
            var handle = GCHandle.Alloc(ret);
            _ReadNSDictionary(GCHandle.ToIntPtr(handle), dict, ReceiveNSDictionaryKeyValue);
            handle.Free();
            return ret;
#else
            return new Dictionary<string, string>();
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

        public static iOSNotificationWithUserInfo? GetLastNotificationData()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_GetAppOpenedUsingNotification())
            {
                IntPtr ptr = _GetLastNotificationData();

                if (ptr != IntPtr.Zero)
                {
                    iOSNotificationWithUserInfo data;
                    data.data = (iOSNotificationData)Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
                    data.userInfo = NSDictionaryToCs(data.data.userInfo);
                    data.data.userInfo = IntPtr.Zero;
                    _FreeUnmanagediOSNotificationDataArray(ptr, 1);
                    return data;
                }
            }
#endif
            return null;
        }
    }
}
#pragma warning restore 162

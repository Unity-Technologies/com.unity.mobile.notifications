using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

#pragma warning disable 649, 162
namespace Unity.Notifications.iOS
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct iOSAuthorizationRequestData
	{
		internal bool granted;
		internal string error;
		internal bool finished;
		internal string deviceToken;
	}

	internal class iOSNotificationsWrapper : MonoBehaviour
	{

		[DllImport("__Internal")]
		private static extern void _RequestAuthorization(Int32 options, bool registerForRemote);

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
		internal static extern void _SetApplicationBadge(Int32 badge);
		
		[DllImport("__Internal")]
		internal static extern Int32 _GetApplicationBadge();

		[DllImport("__Internal")]
		internal static extern bool _GetAppOpenedUsingNotification();
		
		[DllImport("__Internal")]
		internal static extern void _RemoveAllDeliveredNotifications();
		
		[DllImport("__Internal")]
		private static extern IntPtr _GetLastNotificationData();


		
		[DllImport("__Internal")]
		internal static extern void _FreeUnmanagedStruct(IntPtr ptr);


		internal delegate void AuthorizationRequestCallback(IntPtr authdata);
		internal static AuthorizationRequestCallback onAuthenticationRequestFinished;

		
		internal delegate void NotificationReceivedCallback(IntPtr notificationData);
		internal static NotificationReceivedCallback onNotificationReceived;
		internal static NotificationReceivedCallback onRemoteNotificationReceived;

		
		public static void RegisterAuthorizationRequestCallback()
		{
#if UNITY_IOS && !UNITY_EDITOR
			onAuthenticationRequestFinished = new AuthorizationRequestCallback(AuthorizationRequestReceived);
			_SetAuthorizationRequestReceivedDelegate(onAuthenticationRequestFinished);
#endif
		}


		public static void RegisterOnReceivedRemoteNotificationCallback()
		{
#if UNITY_IOS && !UNITY_EDITOR
			onRemoteNotificationReceived = new NotificationReceivedCallback(RemoteNotificationReceived);
			_SetRemoteNotificationReceivedDelegate(onRemoteNotificationReceived);
#endif
		}
		
		public static void RegisterOnReceivedCallback()
		{
#if UNITY_IOS && !UNITY_EDITOR
			onNotificationReceived = new NotificationReceivedCallback(NotificationReceived);
			_SetNotificationReceivedDelegate(onNotificationReceived);
#endif
		}
		
		[MonoPInvokeCallback(typeof(AuthorizationRequestCallback))]
		public static void AuthorizationRequestReceived(IntPtr authRequestDataPtr)
		{
#if UNITY_IOS && !UNITY_EDITOR
			iOSAuthorizationRequestData data;
			data = (iOSAuthorizationRequestData)Marshal.PtrToStructure(authRequestDataPtr, typeof(iOSAuthorizationRequestData));
		
			iOSNotificationCenter.onFinishedAuthorizationRequest(data);
#endif
		}

		[MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
		public static void RemoteNotificationReceived(IntPtr notificationDataPtr)
		{
#if UNITY_IOS && !UNITY_EDITOR
			iOSNotificationData data;
			data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));

			iOSNotificationCenter.onReceivedRemoteNotification(data);
#endif
		}


		[MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
		public static void NotificationReceived(IntPtr notificationDataPtr)
		{
#if UNITY_IOS && !UNITY_EDITOR
			iOSNotificationData data;
			data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));
			
			iOSNotificationCenter.onSentNotification(data);
#endif
		}
		
		public static void RequestAuthorization(int options, bool registerRemote)
		{
#if UNITY_IOS && !UNITY_EDITOR
			_RequestAuthorization(options, registerRemote);
#endif
		}

		public static iOSNotificationSettings GetNotificationSettings()
		{
#if UNITY_IOS && !UNITY_EDITOR
			iOSNotificationSettings settings;

			IntPtr ptr = _GetNotificationSettings();
			settings = (iOSNotificationSettings) Marshal.PtrToStructure(ptr, typeof(iOSNotificationSettings));
			_FreeUnmanagedStruct(ptr);
	
				return settings;
#endif
			return new iOSNotificationSettings();
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
					data = (iOSNotificationData) Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
					dataList.Add(data);
					_FreeUnmanagedStruct(ptr);
				}
			}

			return dataList.ToArray();
#endif
			return null;
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
					data = (iOSNotificationData) Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
					dataList.Add(data);
					_FreeUnmanagedStruct(ptr);
				}
			}

			return dataList.ToArray();
#endif
			return new iOSNotificationData[]{};
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
#endif
			return 0;
		}

		public static bool GetAppOpenedUsingNotification()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return _GetAppOpenedUsingNotification();
#endif
			return false;

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
					data = (iOSNotificationData) Marshal.PtrToStructure(ptr, typeof(iOSNotificationData));
					_FreeUnmanagedStruct(ptr);
					return data;
				}
			}
#endif
			return null;
		}
	}
}
#pragma warning restore 649, 162
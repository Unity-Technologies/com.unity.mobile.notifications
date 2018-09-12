using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

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
		private static extern void _RequestAuthorization(int options, bool registerForRemote);

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
		private static extern int _GetScheduledNotificationDataCount();
		
		[DllImport("__Internal")]
		private static extern IntPtr _GetScheduledNotificationDataAt(int index);
		
		[DllImport("__Internal")]
		private static extern int _GetDeliveredNotificationDataCount();
		
		[DllImport("__Internal")]
		private static extern IntPtr _GetDeliveredNotificationDataAt(int index);

		[DllImport("__Internal")]
		internal static extern void _RemoveScheduledNotification(string identifier);
		
		[DllImport("__Internal")]
		internal static extern void _RemoveAllScheduledNotifications();

		[DllImport("__Internal")]
		internal static extern void _RemoveDeliveredNotification(string identifier);
		
		[DllImport("__Internal")]
		internal static extern void _RemoveAllDeliveredNotifications();



//

		internal delegate void AuthorizationRequestCallback(IntPtr authdata);
		internal static AuthorizationRequestCallback onAuthenticationRequestFinished;

		
		internal delegate void NotificationReceivedCallback(IntPtr notificationData);
		internal static NotificationReceivedCallback onNotificationReceived;
		internal static NotificationReceivedCallback onRemoteNotificationReceived;

		
		public static void RegisterAuthorizationRequestCallback()
		{
			onAuthenticationRequestFinished = new AuthorizationRequestCallback(AuthorizationRequestReceived);
			_SetAuthorizationRequestReceivedDelegate(onAuthenticationRequestFinished);
		}


		public static void RegisterOnReceivedRemoteNotificationCallback()
		{

			onRemoteNotificationReceived = new NotificationReceivedCallback(RemoteNotificationReceived);
			_SetRemoteNotificationReceivedDelegate(onRemoteNotificationReceived);
		}
		
		public static void RegisterOnReceivedCallback()
		{
			onNotificationReceived = new NotificationReceivedCallback(NotificationReceived);
			_SetNotificationReceivedDelegate(onNotificationReceived);
		}
		
		[MonoPInvokeCallback(typeof(AuthorizationRequestCallback))]
		public static void AuthorizationRequestReceived(IntPtr authRequestDataPtr)
		{
			Debug.Log("smt, smth1!!");
			iOSAuthorizationRequestData data;
			data = (iOSAuthorizationRequestData)Marshal.PtrToStructure(authRequestDataPtr, typeof(iOSAuthorizationRequestData));
		
			iOSNotificationCenter.onFinishedAuthorizationRequest(data);
		}

		[MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
		public static void RemoteNotificationReceived(IntPtr notificationDataPtr)
		{
			iOSNotificationData data;
			data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));
			
			iOSNotificationCenter.onReceivedRemoteNotification(data);
		}


		[MonoPInvokeCallback(typeof(NotificationReceivedCallback))]
		public static void NotificationReceived(IntPtr notificationDataPtr)
		{
			iOSNotificationData data;
			data = (iOSNotificationData)Marshal.PtrToStructure(notificationDataPtr, typeof(iOSNotificationData));
			
			iOSNotificationCenter.onSentNotification(data);
		}
		
		public static void RequestAuthorization(int options, bool registerRemote)
		{
			_RequestAuthorization(options, registerRemote);
		}

		public static iOSNotificationSettings GetNotificationSettings()
		{
			iOSNotificationSettings settings;
			IntPtr ptr = _GetNotificationSettings();
			settings = (iOSNotificationSettings) Marshal.PtrToStructure(ptr, typeof(iOSNotificationSettings));
			return settings;
		}

		public static void ScheduleLocalNotification(iOSNotificationData data)
		{
			// Initialize unmanged memory to hold the struct.
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
			Marshal.StructureToPtr(data, ptr, false);

			_ScheduleLocalNotification(ptr);
		}

		public static iOSNotificationData[] GetDeliveredNotificationData()
		{
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
				}
			}

			return dataList.ToArray();
		}

		public static iOSNotificationData[] GetScheduledNotificationData()
		{
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
				}
			}

			return dataList.ToArray();
		}

	}
}
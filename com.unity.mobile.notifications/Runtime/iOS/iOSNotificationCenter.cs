using System.Collections.Generic;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Use the iOSNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class iOSNotificationCenter
    {
        private static bool s_Initialized;

        /// <summary>
        /// The delegate type for the notification received callbacks.
        /// </summary>
        public delegate void NotificationReceivedCallback(iOSNotification notification);

        /// <summary>
        /// Subscribe to this event to receive a callback whenever a local notification or a remote is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived
        {
            add
            {
                if (!s_OnNotificationReceivedCallbackSet)
                {
                    iOSNotificationsWrapper.RegisterOnReceivedCallback();
                    s_OnNotificationReceivedCallbackSet = true;
                }

                s_OnNotificationReceived += value;
            }
            remove
            {
                s_OnNotificationReceived -= value;
            }
        }

        private static bool s_OnNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback s_OnNotificationReceived = delegate {};

        /// <summary>
        /// Subscribe to this event to receive a callback whenever a remote notification is received while the app is in foreground,
        /// if you subscribe to this event remote notification will not be shown while the app is in foreground and if you still want
        /// to show it to the user you will have to schedule a local notification with the data received from this callback.
        /// If you want remote notifications to be shown automatically subscribe to the [[OnNotificationReceived]] even instead and check the
        /// [[Notification.Trigger]] class type to determine whether the received notification is a remote notification.
        /// </summary>
        public static event NotificationReceivedCallback OnRemoteNotificationReceived
        {
            add
            {
                if (!s_OnRemoteNotificationReceivedCallbackSet)
                {
                    iOSNotificationsWrapper.RegisterOnReceivedRemoteNotificationCallback();
                    s_OnRemoteNotificationReceivedCallbackSet = true;
                }

                s_OnRemoteNotificationReceived += value;
            }
            remove
            {
                s_OnRemoteNotificationReceived -= value;
            }
        }

        private static bool s_OnRemoteNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback s_OnRemoteNotificationReceived = delegate {};

        internal delegate void AuthorizationRequestCompletedCallback(iOSAuthorizationRequestData data);

        /// <summary>
        /// The number currently set as the badge of the app icon.
        /// </summary>
        public static int ApplicationBadge
        {
            get { return iOSNotificationsWrapper.GetApplicationBadge(); }
            set { iOSNotificationsWrapper.SetApplicationBadge(value); }
        }

        static bool Initialize()
        {
#if UNITY_EDITOR || !UNITY_IOS
            return false;
#elif UNITY_IOS

            if (s_Initialized)
                return true;

            iOSNotificationsWrapper.RegisterOnReceivedCallback();
            return s_Initialized = true;
#endif
        }

        /// <summary>
        /// Schedules a local notification for delivery.
        /// </summary>
        public static void ScheduleNotification(iOSNotification notification)
        {
            if (!Initialize())
                return;

            iOSNotificationsWrapper.ScheduleLocalNotification(notification.GetDataForSending());
        }

        /// <summary>
        /// Returns all notifications that are currently scheduled.
        /// </summary>
        public static iOSNotification[] GetScheduledNotifications()
        {
            return NotificationDataToNotifications(iOSNotificationsWrapper.GetScheduledNotificationData());
        }

        /// <summary>
        /// Returns all of the app's delivered notifications that are currently shown in the Notification Center.
        /// </summary>
        public static iOSNotification[] GetDeliveredNotifications()
        {
            return NotificationDataToNotifications(iOSNotificationsWrapper.GetDeliveredNotificationData());
        }

        private static iOSNotification[] NotificationDataToNotifications(iOSNotificationData[] notificationData)
        {
            var iOSNotifications = new iOSNotification[notificationData == null ? 0 : notificationData.Length];

            for (int i = 0; i < iOSNotifications.Length; ++i)
                iOSNotifications[i] = new iOSNotification(notificationData[i]);

            return iOSNotifications;
        }

        /// <summary>
        /// Use this to retrieve the last local or remote notification received by the app.
        /// </summary>
        /// <returns>
        /// Returns the last local or remote notification used to open the app or clicked on by the user. If no notification is available it returns null.
        /// </returns>
        public static iOSNotification GetLastRespondedNotification()
        {
            var data = iOSNotificationsWrapper.GetLastNotificationData();

            if (data == null)
                return null;

            return new iOSNotification(data.Value);
        }

        /// <summary>
        /// Unschedules the specified notification.
        /// </summary>
        public static void RemoveScheduledNotification(string identifier)
        {
            if (Initialize())
                iOSNotificationsWrapper._RemoveScheduledNotification(identifier);
        }

        /// <summary>
        /// Removes the specified notification from Notification Center.
        /// </summary>
        public static void RemoveDeliveredNotification(string identifier)
        {
            if (Initialize())
                iOSNotificationsWrapper._RemoveDeliveredNotification(identifier);
        }

        /// <summary>
        /// Unschedules all pending notification.
        /// </summary>
        public static void RemoveAllScheduledNotifications()
        {
            if (Initialize())
                iOSNotificationsWrapper._RemoveAllScheduledNotifications();
        }

        /// <summary>
        /// Removes all of the app's delivered notifications from the Notification Center.
        /// </summary>
        public static void RemoveAllDeliveredNotifications()
        {
            if (Initialize())
                iOSNotificationsWrapper._RemoveAllDeliveredNotifications();
        }

        /// <summary>
        /// Get the notification settings for this app.
        /// </summary>
        public static iOSNotificationSettings GetNotificationSettings()
        {
            return iOSNotificationsWrapper.GetNotificationSettings();
        }

        internal static void OnReceivedRemoteNotification(iOSNotificationData data)
        {
            var notification = new iOSNotification(data);
            s_OnRemoteNotificationReceived(notification);
        }

        internal static void OnSentNotification(iOSNotificationData data)
        {
            var notification = new iOSNotification(data);
            s_OnNotificationReceived(notification);
        }
    }
}

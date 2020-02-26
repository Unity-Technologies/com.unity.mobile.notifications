using System.Collections.Generic;

#pragma warning disable 67

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Use the iOSNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class iOSNotificationCenter
    {
        private static bool initialized;

        public delegate void NotificationReceivedCallback(iOSNotification notification);

        /// <summary>
        /// Subscribe to this event to receive a callback whenever a local notification or a remote is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived
        {
            add
            {
                if (!onNotificationReceivedCallbackSet)
                {
                    iOSNotificationsWrapper.RegisterOnReceivedCallback();
                    onNotificationReceivedCallbackSet = true;
                }

                onNotificationReceived += value;
            }
            remove
            {
                onNotificationReceived -= value;
            }
        }

        private static bool onNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback onNotificationReceived = delegate(iOSNotification notification) {};

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
                if (!onRemoteNotificationReceivedCallbackSet)
                {
                    iOSNotificationsWrapper.RegisterOnReceivedRemoteNotificationCallback();
                    onRemoteNotificationReceivedCallbackSet = true;
                }

                onRemoteNotificationReceived += value;
            }
            remove
            {
                onRemoteNotificationReceived -= value;
            }
        }

        private static bool onRemoteNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback onRemoteNotificationReceived = delegate(iOSNotification notification) {};


        internal delegate void AuthorizationRequestCompletedCallback(iOSAuthorizationRequestData data);
        internal static event AuthorizationRequestCompletedCallback OnAuthorizationRequestCompleted = delegate {};


        static bool Initialize()
        {
            #if UNITY_EDITOR || !PLATFORM_IOS
            return false;
            #elif PLATFORM_IOS

            if (initialized)
                return true;

            iOSNotificationsWrapper.RegisterOnReceivedCallback();
            return initialized = true;
            #endif
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

            var notification = new iOSNotification(data.Value.identifier);
            notification.data = data.Value;

            return notification;
        }

        /// <summary>
        /// The number currently set as the badge of the app icon.
        /// </summary>
        public static int ApplicationBadge
        {
            get { return iOSNotificationsWrapper.GetApplicationBadge(); }
            set { iOSNotificationsWrapper.SetApplicationBadge(value); }
        }

        /// <summary>
        /// Un-schedule the specified notification.
        /// </summary>
        public static void RemoveScheduledNotification(string identifier)
        {
            if (!Initialize())
                return;

            iOSNotificationsWrapper._RemoveScheduledNotification(identifier);
        }

        /// <summary>
        /// Removes the specified notification from Notification Center.
        /// </summary>
        public static void RemoveDeliveredNotification(string identifier)
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveDeliveredNotification(identifier);
        }

        /// <summary>
        /// Unschedules all pending notification.
        /// </summary>
        public static void RemoveAllScheduledNotifications()
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveAllScheduledNotifications();
        }

        /// <summary>
        /// Removes all of the app’s delivered notifications from the Notification Center.
        /// </summary>
        public static void RemoveAllDeliveredNotifications()
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveAllDeliveredNotifications();
        }

        /// <summary>
        /// Get the notification settings for this app.
        /// </summary>
        public static iOSNotificationSettings GetNotificationSettings()
        {
            return iOSNotificationsWrapper.GetNotificationSettings();
        }

        /// <summary>
        /// Returns all notifications that are currently scheduled.
        /// </summary>
        public static iOSNotification[] GetScheduledNotifications()
        {
            var iOSNotifications = new List<iOSNotification>();

            foreach (var d in iOSNotificationsWrapper.GetScheduledNotificationData())
            {
                iOSNotifications.Add(new iOSNotification(d));
            }

            return iOSNotifications.ToArray();
        }

        /// <summary>
        /// Returns all of the app’s delivered notifications that are currently shown in the Notification Center.
        /// </summary>
        public static iOSNotification[] GetDeliveredNotifications()
        {
            var iOSNotifications = new List<iOSNotification>();

            foreach (var d in iOSNotificationsWrapper.GetDeliveredNotificationData())
            {
                iOSNotifications.Add(new iOSNotification(d));
            }

            return iOSNotifications.ToArray();
        }

        /// <summary>
        /// Schedules a local notification for delivery.
        /// </summary>
        public static void ScheduleNotification(iOSNotification notification)
        {
            if (!Initialize())
                return;

            notification.Verify();
            iOSNotificationsWrapper.ScheduleLocalNotification(notification.data);
        }

        internal static void onReceivedRemoteNotification(iOSNotificationData data)
        {
            var notification = new iOSNotification(data.identifier);
            notification.data = data;
            onRemoteNotificationReceived(notification);
        }

        internal static void onSentNotification(iOSNotificationData data)
        {
            var notification = new iOSNotification(data.identifier);
            notification.data = data;
            onNotificationReceived(notification);
        }

        internal static void onFinishedAuthorizationRequest(iOSAuthorizationRequestData data)
        {
            OnAuthorizationRequestCompleted(data);
        }
    }
}

#pragma warning restore 67

using System;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using System.Globalization;
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications
{
    [Flags]
    public enum NotificationPresentation
    {
        Alert = 1,
        Badge = 1 << 1,
        Sound = 1 << 2,
        Vibrate = 1 << 3,
    }

    public struct NotificationCenterArgs
    {
        public static NotificationCenterArgs Default => new NotificationCenterArgs()
        {
            PresentationOptions = NotificationPresentation.Badge | NotificationPresentation.Sound,
        };

        public NotificationPresentation PresentationOptions { get; set; }
        public string AndroidChannelId { get; set; }
        public string AndroidChannelName { get; set; }
        public string AndroidChannelDescription { get; set; }
        public bool iOSRegisterForRemoteNotifications { get; set; }
    }

    public static class NotificationCenter
    {
        public delegate void NotificationReceivedCallback(Notification notification);

        static bool s_Initialized = false;
        static NotificationCenterArgs s_Args;
        static event NotificationReceivedCallback s_OnNotificationReceived;
        static bool s_OnNotificationReceivedSet = false;

#if UNITY_ANDROID
        static void NotificationReceived(AndroidNotificationIntentData data)
#else
        static void NotificationReceived(iOSNotification notif)
#endif
        {
            if (s_OnNotificationReceived != null)
            {
#if UNITY_ANDROID
                var notification = new Notification(data.Notification, data.Id);
#else
                var notification = new Notification(notif);
#endif

                s_OnNotificationReceived(notification);
            }
        }

        public static event NotificationReceivedCallback OnNotificationReceived
        {
            add
            {
                if (!s_OnNotificationReceivedSet)
                {
#if UNITY_ANDROID
                    AndroidNotificationCenter.OnNotificationReceived += NotificationReceived;
#else
                    iOSNotificationCenter.OnNotificationReceived += NotificationReceived;
#endif
                    s_OnNotificationReceivedSet = true;
                }
                s_OnNotificationReceived += value;
            }
            remove
            {
                s_OnNotificationReceived -= value;
            }
        }

        public static void Initialize(NotificationCenterArgs args)
        {
            if (s_Initialized)
                return;
            if (string.IsNullOrEmpty(args.AndroidChannelId))
                throw new ArgumentException("AndroidChannel not provided");

            s_Args = args;
            s_Initialized = true;

#if UNITY_ANDROID
            AndroidNotificationCenter.Initialize();
            if (args.AndroidChannelName != null || args.AndroidChannelDescription != null)
            {
                Importance importance = Importance.Low;
                if (0 != (args.PresentationOptions & NotificationPresentation.Alert))
                    importance = Importance.High;
                else if (0 != (args.PresentationOptions & NotificationPresentation.Sound))
                    importance = Importance.Default;

                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel()
                {
                    Id = args.AndroidChannelId,
                    Name = args.AndroidChannelName,
                    Description = args.AndroidChannelDescription,
                    Importance = importance,
                    CanShowBadge = 0 != (args.PresentationOptions & NotificationPresentation.Badge),
                    EnableVibration = 0 != (args.PresentationOptions & NotificationPresentation.Vibrate),
                });
            }
#endif
        }

        public static NotificationsPermissionRequest RequestPermission()
        {
            int iOSAuthorizationOptions = 0;
#if UNITY_IOS
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Alert))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Alert;
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Badge))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Badge;
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Sound))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Sound;
#endif
            return new NotificationsPermissionRequest(iOSAuthorizationOptions, s_Args.iOSRegisterForRemoteNotifications);
        }

        static void CheckInitialized()
        {
            if (!s_Initialized)
                throw new Exception("NotificationCenter not initialized");
        }

        public static void ScheduleNotification(Notification notification)
        {
            CheckInitialized();

#if UNITY_ANDROID
            if (notification.Identifier.HasValue)
                AndroidNotificationCenter.SendNotificationWithExplicitID((AndroidNotification)notification, s_Args.AndroidChannelId, notification.Identifier.Value);
            else
                AndroidNotificationCenter.SendNotification((AndroidNotification)notification, s_Args.AndroidChannelId);
#else
            var n = (iOSNotification)notification;
            if (n != null)
                iOSNotificationCenter.ScheduleNotification(n);
#endif
        }

        public static Notification? LastRespondedNotification
        {
            get
            {
                CheckInitialized();

#if UNITY_ANDROID
                var intent = AndroidNotificationCenter.GetLastNotificationIntent();
                if (intent == null)
                    return null;
                return new Notification(intent.Notification, intent.Id);
#else
                var notification = iOSNotificationCenter.GetLastRespondedNotification();
                if (notification == null)
                    return null;
                return new Notification(notification);
#endif
            }
        }

        public static void CancelScheduledNotification(int id)
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(id);
#else
            iOSNotificationCenter.RemoveScheduledNotification(id.ToString(CultureInfo.InvariantCulture));
#endif
        }

        public static void CancelDeliveredNotification(int id)
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelDisplayedNotification(id);
#else
            iOSNotificationCenter.RemoveDeliveredNotification(id.ToString(CultureInfo.InvariantCulture));
#endif
        }

        public static void CancelAllScheduledNotifications()
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#else
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }

        public static void CancelAllDeliveredNotifications()
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#else
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }

        public static void ClearBadge()
        {
            CheckInitialized();

#if UNITY_IOS
            Unity.Notifications.iOS.iOSNotificationCenter.ApplicationBadge = 0;
#endif
        }
    }
}

using System;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using System.Globalization;
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications
{
    /// <summary>
    /// Options, specifying how notifications should be presented to the user.
    /// These option can be bitwise-ored to combine them.
    /// </summary>
    /// <remarks>
    /// On Android Alert and Sound flags are used to choose importance level for the channel, so specifying Alert also includes sound.
    /// </remarks>
    /// <seealso cref="NotificationCenterArgs.PresentationOptions"/>
    [Flags]
    public enum NotificationPresentation
    {
        /// <summary>
        /// Specifies that when notification arrives, a pop-up should show up on screen.
        /// Alerts can be disabled by user in device settings. They may also be disabled by default.
        /// </summary>
        Alert = 1,

        /// <summary>
        /// Whether notifications can set a badge on applications launcher.
        /// </summary>
        Badge = 1 << 1,

        /// <summary>
        /// Whether notifications cause device to play sound uppon arrival.
        /// </summary>
        Sound = 1 << 2,

        /// <summary>
        /// Causes device to vibrate when notification is received. Android only.
        /// </summary>
        Vibrate = 1 << 3,
    }

    /// <summary>
    /// The settings section to open, if possible.
    /// </summary>
    public enum NotificationSettingsSection
    {
        /// <summary>
        /// Opens settings for application and tries to open the section for notifications.
        /// </summary>
        Application,

        /// <summary>
        /// Tries to navigate to section for a particular notification category.
        /// Since Android 8.0 will open notification settings for the specific notification channel.
        /// </summary>
        Category,
    }

    /// <summary>
    /// Initialization arguments for <see cref="NotificationCenter"/>.
    /// Recommended to use <see cref="Default"/> to retrieve recommened default values and then alter it.
    /// It is required to manually set <see cref="AndroidChannelId"/>.
    /// </summary>
    public struct NotificationCenterArgs
    {
        /// <summary>
        /// Returns recommended default values for all settings.
        /// </summary>
        public static NotificationCenterArgs Default => new NotificationCenterArgs()
        {
            PresentationOptions = NotificationPresentation.Badge | NotificationPresentation.Sound,
        };

        /// <summary>
        /// Options specifying how notifications should be presented to user when they arrive.
        /// On Android these only have effect if notification channel is created uppon initialization (all channel related properties are set).
        /// On iOS these only have effect if permission to post notification is later requested using <see cref="NotificationCenter.RequestPermission"/>
        /// </summary>
        public NotificationPresentation PresentationOptions { get; set; }

        /// <summary>
        /// A custom non-empty string to identify notification channel. Required, Android only.
        /// All notifications will be sent to this channel.
        /// Channel is created automatically during initialization if <see cref="AndroidChannelName"/> and <see cref="AndroidChannelDescription"/> are both set.
        /// If name and description are left null, channel with given identifier has to be created manually (for example using <see cref="AndroidNotificationCenter.RegisterNotificationChannel(AndroidNotificationChannel)"/>).
        /// </summary>
        public string AndroidChannelId { get; set; }

        /// <summary>
        /// A user visible name for notification channel. Optional, Android only.
        /// Leave null, if you wish to create channel manually.
        /// Set this to channel name for it to be created automatically during initialization.
        /// If this is set, then <see cref="AndroidChannelDescription"/> must also be set.
        /// </summary>
        /// <seealso cref="AndroidChannelId"/>
        public string AndroidChannelName { get; set; }

        /// <summary>
        /// A user visible description for the channel. Optional, Android only.
        /// Leave null, if you wish to create channel manually.
        /// Set this to channel description for it to be created automatically during initialization.
        /// If this is set, then <see cref="AndroidChannelName"/> must also be set.
        /// </summary>
        /// <seealso cref="AndroidChannelId"/>
        public string AndroidChannelDescription { get; set; }
    }

    /// <summary>
    /// Send, receive and manage notifications.
    /// Must be initialized before use. See <see cref="Initialize(NotificationCenterArgs)"/>.
    /// </summary>
    public static class NotificationCenter
    {
        /// <summary>
        /// Delegate for <see cref="OnNotificationReceived"/>.
        /// </summary>
        /// <param name="notification">Notification that has been received.</param>
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

        /// <summary>
        /// An event that fires when notification is received.
        /// Application must be running for this event to work.
        /// On Android this even fires if application is in foreground or when it's brought back from background.
        /// On iOS this even only fires when notification is received while app is in foreground.
        /// </summary>
        /// <remarks>
        /// This event is the same as AndroidNotificationCenter.OnNotificationReceived and iOSNotificationCenter.OnNotificationReceived.
        /// </remarks>
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

        /// <summary>
        /// Initializes notification center with given arguments.
        /// On Android it will also create notification channel if arguments are set accordingly.
        /// </summary>
        /// <param name="args">Arguments for initialization.</param>
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

        /// <summary>
        /// Request for users permission to post notifications.
        /// Requests users permission to send notifications. Unless already allowed or permanently denied, shows UI to the user.
        /// Users can revoke permission in Settings while app is not running.
        /// </summary>
        /// <returns>An object for tracking the request and obtaining results.</returns>
        /// <remarks>Before Android 13 no permission is required.</remarks>
        public static NotificationsPermissionRequest RequestPermission()
        {
            CheckInitialized();

            int iOSAuthorizationOptions = 0;
#if UNITY_IOS
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Alert))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Alert;
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Badge))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Badge;
            if (0 != (s_Args.PresentationOptions & NotificationPresentation.Sound))
                iOSAuthorizationOptions |= (int)AuthorizationOption.Sound;
#endif
            return new NotificationsPermissionRequest(iOSAuthorizationOptions);
        }

        static void CheckInitialized()
        {
            if (!s_Initialized)
                throw new Exception("NotificationCenter not initialized");
        }

        /// <summary>
        /// Schedule notification to be shown in the future.
        /// </summary>
        /// <typeparam name="T">Type of the schedule, usually deduced from actually passed one.</typeparam>
        /// <param name="notification">Notification to send.</param>
        /// <param name="schedule">Schedule, specifying, when notification should be shown.</param>
        /// <returns>Notification identifier.</returns>
        public static int ScheduleNotification<T>(Notification notification, T schedule)
            where T : NotificationSchedule
        {
            string category = null;
#if UNITY_ANDROID
            category = s_Args.AndroidChannelId;
#endif
            return ScheduleNotification(notification, category, schedule);
        }

        /// <summary>
        /// Schedule notification to be shown in the future.
        /// Allows to explicitly specify the category to send notification to. On Android it is notification channel.
        /// Channel or category has to be created manually using AndroidNotificationCenter and iOSNotificationCenter respectively.
        /// </summary>
        /// <typeparam name="T">Type of the schedule, usually deduced from actually passed one.</typeparam>
        /// <param name="notification">Notification to send.</param>
        /// <param name="category">Identifier for iOS category or Android channel.</param>
        /// <param name="schedule">Schedule, specifying, when notification should be shown.</param>
        /// <returns>Notification identifier.</returns>
        public static int ScheduleNotification<T>(Notification notification, string category, T schedule)
            where T : NotificationSchedule
        {
            CheckInitialized();

#if UNITY_ANDROID
            var n = (AndroidNotification)notification;
            schedule.Schedule(ref n);
            if (notification.Identifier.HasValue)
            {
                AndroidNotificationCenter.SendNotificationWithExplicitID(n, category, notification.Identifier.Value);
                return notification.Identifier.Value;
            }
            else
                return AndroidNotificationCenter.SendNotification(n, category);
#else
            var n = (iOSNotification)notification;
            if (n == null)
                throw new ArgumentException("Passed notifiation is empty");
            n.CategoryIdentifier = category;
            schedule.Schedule(ref n);
            iOSNotificationCenter.ScheduleNotification(n);
            if (notification.Identifier.HasValue)
                return notification.Identifier.Value;
            // iOSNotification is class and has auto-generated id set at this point
            // for consistency with Android set it back to null, so same Notification can be sent again as new one
            int id = int.Parse(n.Identifier, NumberStyles.None, CultureInfo.InvariantCulture);
            n.Identifier = null;
            return id;
#endif
        }

        /// <summary>
        /// Returns last notification tapped by user, or null.
        /// </summary>
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

        /// <summary>
        /// Cancel a scheduled notification.
        /// </summary>
        /// <param name="id">ID of the notification to cancel.</param>
        public static void CancelScheduledNotification(int id)
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(id);
#else
            iOSNotificationCenter.RemoveScheduledNotification(id.ToString(CultureInfo.InvariantCulture));
#endif
        }

        /// <summary>
        /// Cancel delivered notification.
        /// Removes notification from the tray.
        /// </summary>
        /// <param name="id">ID of the notification to cancel.</param>
        public static void CancelDeliveredNotification(int id)
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelDisplayedNotification(id);
#else
            iOSNotificationCenter.RemoveDeliveredNotification(id.ToString(CultureInfo.InvariantCulture));
#endif
        }

        /// <summary>
        /// Cancel all future notifications.
        /// </summary>
        public static void CancelAllScheduledNotifications()
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#else
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }

        /// <summary>
        /// Remove all already delivered notifications.
        /// </summary>
        public static void CancelAllDeliveredNotifications()
        {
            CheckInitialized();

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#else
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
        }

        /// <summary>
        /// Clear Application badge. iOS only.
        /// iOS applications can set numeric badge on app icon. Calling this method removes that badge.
        /// On Android badge is removed automatically when notifications are removed.
        /// </summary>
        public static void ClearBadge()
        {
            CheckInitialized();

#if UNITY_IOS
            iOSNotificationCenter.ApplicationBadge = 0;
#endif
        }

        /// <summary>
        /// Opens settings for the application.
        /// If possible, will try to navigate as close to requested section as it can.
        /// On iOS and Android prior to 8.0 will open settings for the application.
        /// Since Android 8.0 will open notification settings for either application or the default channel.
        /// </summary>
        /// <param name="section">The section to navigate to.</param>
        public static void OpenNotificationSettings(NotificationSettingsSection section = NotificationSettingsSection.Application)
        {
            CheckInitialized();

#if UNITY_ANDROID
            string channel = section switch
            {
                NotificationSettingsSection.Category => s_Args.AndroidChannelId,
                NotificationSettingsSection.Application => null,
                _ => null,
            };

            AndroidNotificationCenter.OpenNotificationSettings(channel);
#else
            iOSNotificationCenter.OpenNotificationSettings();
#endif
        }
    }
}

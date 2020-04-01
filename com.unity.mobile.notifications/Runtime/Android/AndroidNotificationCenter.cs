using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// Current status of a scheduled notification, can be queried using CheckScheduledNotificationStatus.
    /// </summary>
    public enum NotificationStatus
    {
        //// <summary>
        /// Status of the specified notification cannot be determined, this is only supported on Android Marshmallow (6.0) and above.
        /// </summary>
        Unavailable = -1,

        //// <summary>
        /// A notification with the specified id could not be found.
        /// </summary>
        Unknown = 0,

        //// <summary>
        /// A notification with the specified is scheduled but not yet delivered.
        /// </summary>
        Scheduled = 1,

        //// <summary>
        /// A notification with the specified was already delivered.
        /// </summary>
        Delivered = 2,
    }

    /// <summary>
    /// Use the AndroidNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class AndroidNotificationCenter
    {
        public delegate void NotificationReceivedCallback(AndroidNotificationIntentData data);

        /// <summary>
        /// Subscribe to this event to receive callbacks whenever a scheduled notification is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived = delegate {};

        const int ANDROID_OREO = 26;
        const int ANDROID_M = 23;

        const string DEFAULT_APP_ICON_ADAPTIVE = "ic_launcher_foreground";
        const string DEFAULT_APP_ICON_LEGACY = "app_icon";

        static AndroidJavaObject notificationManager;
        static int AndroidSDK;

        static bool initialized;

        private GameObject receivedNotificationDispatcher;

        public static bool Initialize()
        {
            if (initialized)
                return true;

            if (AndroidReceivedNotificationMainThreadDispatcher.GetInstance() == null)
            {
                var receivedNotificationDispatcher = new GameObject("AndroidReceivedNotificationMainThreadDispatcher");
                receivedNotificationDispatcher.AddComponent<AndroidReceivedNotificationMainThreadDispatcher>();
            }

#if UNITY_EDITOR || !UNITY_ANDROID
            notificationManager = null;
            initialized = false;
#elif UNITY_ANDROID
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

            AndroidJavaClass managerClass =
                new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");

            notificationManager =
                managerClass.CallStatic<AndroidJavaObject>("getNotificationManagerImpl", context, activity);
            notificationManager.Call("setNotificationCallback", new NotificationCallback());

            AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
            AndroidSDK = buildVersion.GetStatic<int>("SDK_INT");

            initialized = true;
#endif
            return initialized;
        }

        /// <summary>
        /// Allows retrieving the notification used to open the app. You can save arbitrary string data in the 'AndroidNotification.IntentData' field.
        /// </summary>
        /// <returns>
        /// Returns the AndroidNotification used to open the app, returns ‘null ‘ if the app was not opened with a notification.
        /// </returns>
        public static AndroidNotificationIntentData GetLastNotificationIntent()
        {
            if (!Initialize())
                return null;

            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            return ParseNotificationIntentData(intent);
        }

        /// <summary>
        ///  Creates a notification channel that notifications can be posted to.
        ///  Notification channel settings can be changed by users on devices running Android 8.0 and above.
        ///  On older Android versions settings set on the notification channel struct will still be applied to the notification
        ///  if they are supported to by the Android version the app is running on.
        /// </summary>
        /// <remarks>
        ///  When a channel is deleted and recreated, all of the previous settings are restored. In order to change any settings
        ///  besides the name or description an entirely new channel (with a different channel ID) must be created.
        /// </remarks>
        public static void RegisterNotificationChannel(AndroidNotificationChannel channel)
        {
            if (!Initialize())
                return;

            if (string.IsNullOrEmpty(channel.id))
            {
                throw new Exception("Cannot register notification channel, the channel ID is not specified.");
            }
            else if (string.IsNullOrEmpty(channel.id))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Name is not set.", channel.id));
            }
            else if (string.IsNullOrEmpty(channel.description))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Description is not set.", channel.id));
            }

            notificationManager.Call("registerNotificationChannel",
                channel.id,
                channel.name,
                Enum.IsDefined(typeof(Importance), channel.importance) ? channel.importance : (int)Importance.Default,
                channel.description,
                channel.enableLights,
                channel.enableVibration,
                channel.canBypassDnd,
                channel.canShowBadge,
                channel.VibrationPattern,
                Enum.IsDefined(typeof(LockScreenVisibility), channel.lockscreenVisibility) ? channel.lockscreenVisibility : (int)LockScreenVisibility.Public
            );
        }

        /// <summary>
        /// Cancel a scheduled or previously shown notification.
        /// The notification will no longer be displayed on it's scheduled time. If it's already delivered it will be removed from the status bar.
        /// </summary>
        public static void CancelNotification(int id)
        {
            if (!Initialize())
                return;

            CancelScheduledNotification(id);
            CancelDisplayedNotification(id);
        }

        /// <summary>
        /// Cancel a scheduled notification.
        /// The notification will no longer be displayed on it's scheduled time. It it will not be removed from the status bar if it's already delivered.
        /// </summary>
        public static void CancelScheduledNotification(int id)
        {
            if (!Initialize())
                return;

            notificationManager.Call("cancelPendingNotificationIntent", id);
        }

        /// <summary>
        /// Cancel a previously shown notification.
        /// The notification will be removed from the status bar.
        /// </summary>
        public static void CancelDisplayedNotification(int id)
        {
            if (!Initialize())
                return;

            AndroidJavaObject androidNotificationManager =
                notificationManager.Call<AndroidJavaObject>("getNotificationManager");
            androidNotificationManager.Call("cancel", id);
        }

        /// <summary>
        /// Cancel all notifications scheduled or previously shown by the app.
        /// All scheduled notifications will be canceled. All notifications shown by the app will be removed from the status bar.
        /// </summary>
        public static void CancelAllNotifications()
        {
            CancelAllScheduledNotifications();
            CancelAllDisplayedNotifications();
        }

        /// <summary>
        /// Cancel all notifications scheduled by the app.
        /// All scheduled notifications will be canceled. Notifications will not be removed from the status bar if they are already shown.
        /// </summary>
        public static void CancelAllScheduledNotifications()
        {
            if (!Initialize())
                return;

            notificationManager.Call("cancelAllPendingNotificationIntents");
        }

        /// <summary>
        /// Cancel all previously shown notifications.
        /// All notifications shown by the app will be removed from the status bar. All scheduled notifications will still be shown on their scheduled time.
        /// </summary>
        public static void CancelAllDisplayedNotifications()
        {
            if (!Initialize())
                return;

            notificationManager.Call("cancelAllNotifications");
        }

        /// <summary>
        /// Returns all notification channels that were created by the app.
        /// </summary>
        public static AndroidNotificationChannel[] GetNotificationChannels()
        {
            if (!Initialize())
                return new AndroidNotificationChannel[0];

            List<AndroidNotificationChannel> channels = new List<AndroidNotificationChannel>();

            var androidChannels = notificationManager.Call<AndroidJavaObject[]>("getNotificationChannels");

            foreach (var channel in androidChannels)
            {
                var ch = new AndroidNotificationChannel();
                ch.id = channel.Get<string>("id");
                ch.name = channel.Get<string>("name");
                ch.importance = channel.Get<int>("importance");

                ch.description = channel.Get<string>("description");

                ch.enableLights = channel.Get<bool>("enableLights");
                ch.enableVibration = channel.Get<bool>("enableVibration");
                ch.canBypassDnd = channel.Get<bool>("canBypassDnd");
                ch.canShowBadge = channel.Get<bool>("canShowBadge");
                // There is an issue with IL2CPP failing to compile a struct which contains an array of long on Unity 2019.2+ (see case 1173310).
                var vibrationPattern = channel.Get<long[]>("vibrationPattern");
                if (vibrationPattern != null)
                    ch.vibrationPattern = vibrationPattern.Select(i => (int)i).ToArray();
                ch.lockscreenVisibility = channel.Get<int>("lockscreenVisibility");

                channels.Add(ch);
            }
            return channels.ToArray();
        }

        /// <summary>
        /// Returns the notification channel with the specified id.
        /// The notification channel struct fields might not be identical to the channel struct used to initially register the channel if they were changed by the user.
        /// </summary>
        public static AndroidNotificationChannel GetNotificationChannel(string id)
        {
            return GetNotificationChannels().SingleOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Update an already scheduled notification.
        /// If a notification with the specified id was already scheduled it will be overridden with the information from the passed notification struct.
        /// </summary>
        public static void UpdateScheduledNotification(int id, AndroidNotification notification, string channel)
        {
            if (!Initialize())
                return;

            if (notificationManager.CallStatic<bool>("checkIfPendingNotificationIsRegistered", id))
                SendNotification(id, notification, channel);
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The specified id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        public static void SendNotificationWithExplicitID(AndroidNotification notification, string channel, int id)
        {
            if (!Initialize())
                return;

            SendNotification(id, notification, channel);
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The returned id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        public static int SendNotification(AndroidNotification notification, string channel)
        {
            if (!Initialize())
                return -1;

            int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()) + (new System.Random().Next(10000));
            SendNotification(id, notification, channel);

            return id;
        }

        /// <summary>
        /// Return the status of a scheduled notification.
        /// Only available in API  23 and above.
        /// </summary>
        public static NotificationStatus CheckScheduledNotificationStatus(int id)
        {
            var status = notificationManager.Call<int>("checkNotificationStatus", id);
            return (NotificationStatus)status;
        }

        internal static void SendNotification(int id, AndroidNotification notification, string channel)
        {
            if (notification.fireTime < 0L)
            {
                Debug.LogError("Failed to schedule notification, it did not contain a valid FireTime");
            }

            AndroidJavaClass managerClass =
                new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
            AndroidJavaObject context = notificationManager.Get<AndroidJavaObject>("mContext");

            AndroidJavaObject notificationIntent =
                new AndroidJavaObject("android.content.Intent", context, managerClass);

            notificationIntent.Call<AndroidJavaObject>("putExtra", "id", id);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "channelID", channel);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "textTitle", notification.title);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "textContent", notification.text);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "smallIconStr", notification.smallIcon);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "autoCancel", notification.shouldAutoCancel);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "usesChronometer", notification.usesStopwatch);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "fireTime", notification.fireTime);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "repeatInterval", notification.repeatInterval);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "largeIconStr", notification.largeIcon);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "style", notification.style);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "color", notification.color);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "number", notification.number);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "data", notification.intentData);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "group", notification.group);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "groupSummary", notification.groupSummary);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "sortKey", notification.sortKey);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "groupAlertBehaviour", notification.groupAlertBehaviour);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "showTimestamp", notification.showTimestamp);

            long timestampValue =
                notification.showCustomTimestamp ? notification.customTimestamp : notification.fireTime;

            notificationIntent.Call<AndroidJavaObject>("putExtra", "timestamp", timestampValue);

            notificationManager.Call("scheduleNotificationIntent", notificationIntent);
        }

        /// <summary>
        /// Delete the specified notification channel.
        /// </summary>
        public static void DeleteNotificationChannel(string id)
        {
            if (!Initialize())
                return;

            notificationManager.Call("deleteNotificationChannel", id);
        }

        internal static AndroidNotificationIntentData ParseNotificationIntentData(AndroidJavaObject notificationIntent)
        {
            var id = notificationIntent.Call<int>("getIntExtra", "id", -1);
            var channel = notificationIntent.Call<string>("getStringExtra", "channelID");

            if (id == -1)
                return null;

            var notification = new AndroidNotification();

            notification.title = notificationIntent.Call<string>("getStringExtra", "textTitle");
            notification.text = notificationIntent.Call<string>("getStringExtra", "textContent");
            notification.shouldAutoCancel = notificationIntent.Call<bool>("getBooleanExtra", "autoCancel", false);
            notification.usesStopwatch =
                notificationIntent.Call<bool>("getBooleanExtra", "usesChronometer", false);
            notification.fireTime = notificationIntent.Call<long>("getLongExtra", "fireTime", -1L);
            notification.repeatInterval = notificationIntent.Call<long>("getLongExtra", "repeatInterval", -1L);
            notification.style = notificationIntent.Call<int>("getIntExtra", "style", -1);
            notification.color = notificationIntent.Call<int>("getIntExtra", "color", 0);
            notification.number = notificationIntent.Call<int>("getIntExtra", "number", -1);
            notification.intentData = notificationIntent.Call<string>("getStringExtra", "data");
            notification.group = notificationIntent.Call<string>("getStringExtra", "group");
            notification.groupSummary = notificationIntent.Call<bool>("getBooleanExtra", "groupSummary", false);
            notification.sortKey = notificationIntent.Call<string>("getStringExtra", "sortKey");
            notification.groupAlertBehaviour = notificationIntent.Call<int>("getIntExtra", "groupAlertBehaviour", -1);

            return new AndroidNotificationIntentData(id, channel, notification);
        }

        internal static void ReceivedNotificationCallback(AndroidJavaObject intent)
        {
            var data = ParseNotificationIntentData(intent);
            OnNotificationReceived(data);
        }
    }
}

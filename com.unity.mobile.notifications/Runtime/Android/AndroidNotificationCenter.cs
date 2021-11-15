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
        /// <summary>
        /// Status of a specified notification cannot be determined. This is only supported on Android Marshmallow (6.0) and above.
        /// </summary>
        Unavailable = -1,

        /// <summary>
        /// A notification with a specified id could not be found.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A notification with a specified id is scheduled but not yet delivered.
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// A notification with a specified id was already delivered.
        /// </summary>
        Delivered = 2,
    }

    /// <summary>
    /// Use the AndroidNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class AndroidNotificationCenter
    {
        /// <summary>
        /// The delegate type for the notification received callbacks.
        /// </summary>
        public delegate void NotificationReceivedCallback(AndroidNotificationIntentData data);

        /// <summary>
        /// Subscribe to this event to receive callbacks whenever a scheduled notification is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived = delegate { };

        private static AndroidJavaClass s_NotificationManagerClass;
        private static AndroidJavaObject s_NotificationManager;
        private static AndroidJavaObject s_CurrentActivity;
        private static bool s_Initialized;

        private static AndroidJavaObject Notification_EXTRA_TITLE;
        private static AndroidJavaObject Notification_EXTRA_TEXT;
        private static AndroidJavaObject Notification_EXTRA_SHOW_CHRONOMETER;
        private static AndroidJavaObject Notification_EXTRA_BIG_TEXT;
        private static AndroidJavaObject Notification_EXTRA_SHOW_WHEN;
        private static int Notification_FLAG_AUTO_CANCEL;
        private static int Notification_FLAG_GROUP_SUMMARY;

        private static AndroidJavaObject KEY_FIRE_TIME;
        private static AndroidJavaObject KEY_ID;
        private static AndroidJavaObject KEY_INTENT_DATA;
        private static AndroidJavaObject KEY_LARGE_ICON;
        private static AndroidJavaObject KEY_REPEAT_INTERVAL;
        private static AndroidJavaObject KEY_NOTIFICATION;
        private static AndroidJavaObject KEY_SMALL_ICON;

        /// <summary>
        /// Initialize the AndroidNotificationCenter class.
        /// Can be safely called multiple times
        /// </summary>
        /// <returns>True if has been successfully initialized</returns>
        public static bool Initialize()
        {
            if (s_Initialized)
                return true;

            if (AndroidReceivedNotificationMainThreadDispatcher.GetInstance() == null)
            {
                var receivedNotificationDispatcher = new GameObject("AndroidReceivedNotificationMainThreadDispatcher");
                receivedNotificationDispatcher.AddComponent<AndroidReceivedNotificationMainThreadDispatcher>();
            }

#if UNITY_EDITOR || !UNITY_ANDROID
            s_NotificationManager = null;
            s_Initialized = false;
            s_NotificationManagerClass = null;
            s_CurrentActivity = null;
#elif UNITY_ANDROID
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            s_CurrentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context = s_CurrentActivity.Call<AndroidJavaObject>("getApplicationContext");

            s_NotificationManagerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
            s_NotificationManager = s_NotificationManagerClass.CallStatic<AndroidJavaObject>("getNotificationManagerImpl", context, s_CurrentActivity);
            s_NotificationManager.Call("setNotificationCallback", new NotificationCallback());

            using (var notificationClass = new AndroidJavaClass("android.app.Notification"))
            {
                Notification_EXTRA_TITLE = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_TITLE");
                Notification_EXTRA_TEXT = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_TEXT");
                Notification_EXTRA_SHOW_CHRONOMETER = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_SHOW_CHRONOMETER");
                Notification_EXTRA_BIG_TEXT = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_BIG_TEXT");
                Notification_EXTRA_SHOW_WHEN = notificationClass.GetStatic<AndroidJavaObject>("EXTRA_SHOW_WHEN");
                Notification_FLAG_AUTO_CANCEL = notificationClass.GetStatic<int>("FLAG_AUTO_CANCEL");
                Notification_FLAG_GROUP_SUMMARY = notificationClass.GetStatic<int>("FLAG_GROUP_SUMMARY");
            }

            KEY_FIRE_TIME = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_FIRE_TIME");
            KEY_ID = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_ID");
            KEY_INTENT_DATA = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_INTENT_DATA");
            KEY_LARGE_ICON = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_LARGE_ICON");
            KEY_REPEAT_INTERVAL = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_REPEAT_INTERVAL");
            KEY_NOTIFICATION = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_NOTIFICATION");
            KEY_SMALL_ICON = s_NotificationManagerClass.GetStatic<AndroidJavaObject>("KEY_SMALL_ICON");

            s_Initialized = true;
#endif
            return s_Initialized;
        }

        /// <summary>
        ///  Creates a notification channel that notifications can be posted to.
        ///  Notification channel settings can be changed by users on devices running Android 8.0 and above.
        ///  On older Android versions settings set on the notification channel struct will still be applied to the notification
        ///  if they are supported to by the Android version the app is running on.
        /// </summary>
        /// <param name="channel">Channel parameters</param>
        /// <remarks>
        ///  When a channel is deleted and recreated, all of the previous settings are restored. In order to change any settings
        ///  besides the name or description an entirely new channel (with a different channel ID) must be created.
        /// </remarks>
        public static void RegisterNotificationChannel(AndroidNotificationChannel channel)
        {
            if (!Initialize())
                return;

            if (string.IsNullOrEmpty(channel.Id))
            {
                throw new Exception("Cannot register notification channel, the channel ID is not specified.");
            }
            if (string.IsNullOrEmpty(channel.Name))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Name is not set.", channel.Id));
            }
            if (string.IsNullOrEmpty(channel.Description))
            {
                throw new Exception(string.Format("Cannot register notification channel: {0} , the channel Description is not set.", channel.Id));
            }

            s_NotificationManager.Call("registerNotificationChannel",
                channel.Id,
                channel.Name,
                (int)channel.Importance,
                channel.Description,
                channel.EnableLights,
                channel.EnableVibration,
                channel.CanBypassDnd,
                channel.CanShowBadge,
                channel.VibrationPattern,
                (int)channel.LockScreenVisibility
            );
        }

        /// <summary>
        /// Returns the notification channel with the specified id.
        /// The notification channel struct fields might not be identical to the channel struct used to initially register the channel if they were changed by the user.
        /// </summary>
        /// <param name="channelId">ID of the channel to retrieve</param>
        /// <returns>Channel with given ID or empty struct if such channel does not exist</returns>
        public static AndroidNotificationChannel GetNotificationChannel(string channelId)
        {
            return GetNotificationChannels().SingleOrDefault(channel => channel.Id == channelId);
        }

        /// <summary>
        /// Returns all notification channels that were created by the app.
        /// </summary>
        /// <returns>All existing channels</returns>
        public static AndroidNotificationChannel[] GetNotificationChannels()
        {
            if (!Initialize())
                return new AndroidNotificationChannel[0];

            List<AndroidNotificationChannel> channels = new List<AndroidNotificationChannel>();

            var androidChannels = s_NotificationManager.Call<AndroidJavaObject[]>("getNotificationChannels");

            foreach (var channel in androidChannels)
            {
                var ch = new AndroidNotificationChannel();
                ch.Id = channel.Get<string>("id");
                ch.Name = channel.Get<string>("name");
                ch.Importance = channel.Get<int>("importance").ToImportance();
                ch.Description = channel.Get<string>("description");

                ch.EnableLights = channel.Get<bool>("enableLights");
                ch.EnableVibration = channel.Get<bool>("enableVibration");
                ch.CanBypassDnd = channel.Get<bool>("canBypassDnd");
                ch.CanShowBadge = channel.Get<bool>("canShowBadge");
                ch.VibrationPattern = channel.Get<long[]>("vibrationPattern");
                ch.LockScreenVisibility = channel.Get<int>("lockscreenVisibility").ToLockScreenVisibility();

                channels.Add(ch);
            }
            return channels.ToArray();
        }

        /// <summary>
        /// Delete the specified notification channel.
        /// </summary>
        /// <param name="channelId">ID of the channel to delete</param>
        public static void DeleteNotificationChannel(string channelId)
        {
            if (Initialize())
                s_NotificationManager.Call("deleteNotificationChannel", channelId);
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The returned id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        /// <returns>The generated ID for the notification</returns>
        public static int SendNotification(AndroidNotification notification, string channelId)
        {
            if (!Initialize())
                return -1;

            // Now.ToString("yyMMddHHmmssffffff"), but avoiding any culture-related formatting or dependencies
            var now = DateTime.UtcNow;
            var nowFormatted = $"{now.Year}{now.Month}{now.Day}{now.Hour}{now.Minute}{now.Second}{now.Millisecond}";
            int id = Math.Abs(nowFormatted.GetHashCode()) + (new System.Random().Next(10000));
            using (var builder = CreateNotificationBuilder(id, notification, channelId))
                SendNotification(builder);

            return id;
        }

        /// <summary>
        /// Schedule a notification which will be shown at the time specified in the notification struct.
        /// The specified id can later be used to update the notification before it's triggered, it's current status can be tracked using CheckScheduledNotificationStatus.
        /// </summary>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        /// <param name="id">A unique ID for the notification</param>
        public static void SendNotificationWithExplicitID(AndroidNotification notification, string channelId, int id)
        {
            if (Initialize())
                using (var builder = CreateNotificationBuilder(id, notification, channelId))
                {
                    SendNotification(builder);
                }
        }

        /// <summary>
        /// Schedule a notification created using the provided Notification.Builder object.
        /// Notification builder should be created by calling CreateNotificationBuilder.
        /// </summary>
        public static void SendNotification(AndroidJavaObject notificationBuilder)
        {
            if (Initialize())
                s_NotificationManager.Call("scheduleNotification", notificationBuilder);
        }

        /// <summary>
        /// Update an already scheduled notification.
        /// If a notification with the specified id was already scheduled it will be overridden with the information from the passed notification struct.
        /// </summary>
        /// <param name="id">ID of the notification to update</param>
        /// <param name="notification">Data for the notification</param>
        /// <param name="channelId">ID of the channel to send notification to</param>
        public static void UpdateScheduledNotification(int id, AndroidNotification notification, string channelId)
        {
            if (!Initialize())
                return;

            if (s_NotificationManager.Call<bool>("checkIfPendingNotificationIsRegistered", id))
            {
                using (var builder = CreateNotificationBuilder(id, notification, channelId))
                {
                    SendNotification(builder);
                }
            }
        }

        /// <summary>
        /// Cancel a scheduled or previously shown notification.
        /// The notification will no longer be displayed on it's scheduled time. If it's already delivered it will be removed from the status bar.
        /// </summary>
        /// <param name="id">ID of the notification to cancel</param>
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
        /// <param name="id">ID of the notification to cancel</param>
        public static void CancelScheduledNotification(int id)
        {
            if (Initialize())
                s_NotificationManager.Call("cancelPendingNotification", id);
        }

        /// <summary>
        /// Cancel a previously shown notification.
        /// The notification will be removed from the status bar.
        /// </summary>
        /// <param name="id">ID of the notification to cancel</param>
        public static void CancelDisplayedNotification(int id)
        {
            if (Initialize())
                s_NotificationManager.Call("cancelDisplayedNotification", id);
        }

        /// <summary>
        /// Cancel all notifications scheduled or previously shown by the app.
        /// All scheduled notifications will be canceled. All notifications shown by the app will be removed from the status bar.
        /// </summary>
        public static void CancelAllNotifications()
        {
            if (!Initialize())
                return;

            CancelAllScheduledNotifications();
            CancelAllDisplayedNotifications();
        }

        /// <summary>
        /// Cancel all notifications scheduled by the app.
        /// All scheduled notifications will be canceled. Notifications will not be removed from the status bar if they are already shown.
        /// </summary>
        public static void CancelAllScheduledNotifications()
        {
            if (Initialize())
                s_NotificationManager.Call("cancelAllPendingNotificationIntents");
        }

        /// <summary>
        /// Cancel all previously shown notifications.
        /// All notifications shown by the app will be removed from the status bar. All scheduled notifications will still be shown on their scheduled time.
        /// </summary>
        public static void CancelAllDisplayedNotifications()
        {
            if (Initialize())
                s_NotificationManager.Call("cancelAllNotifications");
        }

        /// <summary>
        /// Return the status of a scheduled notification.
        /// Only available in API  23 and above.
        /// </summary>
        /// <param name="id">ID of the notification to check</param>
        /// <returns>The status of the notification</returns>
        public static NotificationStatus CheckScheduledNotificationStatus(int id)
        {
            if (!Initialize())
                return NotificationStatus.Unavailable;

            var status = s_NotificationManager.Call<int>("checkNotificationStatus", id);
            return (NotificationStatus)status;
        }

        /// <summary>
        /// Allows retrieving the notification used to open the app. You can save arbitrary string data in the 'AndroidNotification.IntentData' field.
        /// </summary>
        /// <returns>
        /// Returns the AndroidNotification used to open the app, returns null if the app was not opened with a notification.
        /// </returns>
        public static AndroidNotificationIntentData GetLastNotificationIntent()
        {
            if (!Initialize())
                return null;

            var intent = s_CurrentActivity.Call<AndroidJavaObject>("getIntent");
            var notification = intent.Call<AndroidJavaObject>("getParcelableExtra", KEY_NOTIFICATION);
            if (notification == null)
                return null;
            return GetNotificationData(notification);
        }

        /// <summary>
        /// Opens settings.
        /// On Android versions lower than 8.0 opens settings for the application.
        /// On Android 8.0 and later opens notification settings for the specified channel, or for the application, if channelId is null.
        /// Note, that opening settings will suspend the application and switch to settings app.
        /// </summary>
        /// <param name="channelId">ID for the channel to open or null to open notification settings for the application.</param>
        public static void OpenNotificationSettings(string channelId = null)
        {
            if (!Initialize())
                return;

            s_NotificationManager.Call("showNotificationSettings", channelId);
        }

        /// <summary>
        /// Create Notification.Builder.
        /// Will automatically generate the ID for notification.
        /// <see cref="CreateNotificationBuilder(int, AndroidNotification, string)"/>
        /// </summary>
        public static AndroidJavaObject CreateNotificationBuilder(AndroidNotification notification, string channelId)
        {
            int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()) + (new System.Random().Next(10000));
            return CreateNotificationBuilder(id, notification, channelId);
        }

        /// <summary>
        /// Create Notification.Builder object on Java side using privided AndroidNotification.
        /// </summary>
        /// <param name="id">ID for the notification</param>
        /// <param name="notification">Struct with notification data</param>
        /// <param name="channelId">Channel id</param>
        /// <returns>A proxy object for created Notification.Builder</returns>
        public static AndroidJavaObject CreateNotificationBuilder(int id, AndroidNotification notification, string channelId)
        {
            long fireTime = notification.FireTime.ToLong();
            if (fireTime < 0L)
            {
                Debug.LogError("Failed to schedule notification, it did not contain a valid FireTime");
            }

            var notificationBuilder = s_NotificationManager.Call<AndroidJavaObject>("createNotificationBuilder", channelId);
            s_NotificationManagerClass.CallStatic("setNotificationIcon", notificationBuilder, KEY_SMALL_ICON, notification.SmallIcon);
            if (!string.IsNullOrEmpty(notification.LargeIcon))
                s_NotificationManagerClass.CallStatic("setNotificationIcon", notificationBuilder, KEY_LARGE_ICON, notification.LargeIcon);
            notificationBuilder.Call<AndroidJavaObject>("setContentTitle", notification.Title).Dispose();
            notificationBuilder.Call<AndroidJavaObject>("setContentText", notification.Text).Dispose();
            notificationBuilder.Call<AndroidJavaObject>("setAutoCancel", notification.ShouldAutoCancel).Dispose();
            if (notification.Number >= 0)
                notificationBuilder.Call<AndroidJavaObject>("setNumber", notification.Number).Dispose();
            if (notification.Style == NotificationStyle.BigTextStyle)
            {
                using (var style = new AndroidJavaObject("android.app.Notification$BigTextStyle"))
                {
                    style.Call<AndroidJavaObject>("bigText", notification.Text).Dispose();
                    notificationBuilder.Call<AndroidJavaObject>("setStyle", style).Dispose();
                }
            }
            long timestampValue = notification.ShowCustomTimestamp ? notification.CustomTimestamp.ToLong() : fireTime;
            notificationBuilder.Call<AndroidJavaObject>("setWhen", timestampValue).Dispose();
            if (!string.IsNullOrEmpty(notification.Group))
                notificationBuilder.Call<AndroidJavaObject>("setGroup", notification.Group).Dispose();
            if (notification.GroupSummary)
                notificationBuilder.Call<AndroidJavaObject>("setGroupSummary", notification.GroupSummary).Dispose();
            if (!string.IsNullOrEmpty(notification.SortKey))
                notificationBuilder.Call<AndroidJavaObject>("setSortKey", notification.SortKey).Dispose();
            notificationBuilder.Call<AndroidJavaObject>("setShowWhen", notification.ShowTimestamp).Dispose();
            int color = notification.Color.ToInt();
            if (color != 0)
                s_NotificationManagerClass.CallStatic("setNotificationColor", notificationBuilder, color);
            s_NotificationManagerClass.CallStatic("setNotificationUsesChronometer", notificationBuilder, notification.UsesStopwatch);
            s_NotificationManagerClass.CallStatic("setNotificationGroupAlertBehavior", notificationBuilder, (int)notification.GroupAlertBehaviour);

            using (var extras = notificationBuilder.Call<AndroidJavaObject>("getExtras"))
            {
                extras.Call("putInt", KEY_ID, id);
                extras.Call("putLong", KEY_REPEAT_INTERVAL, notification.RepeatInterval.ToLong());
                extras.Call("putLong", KEY_FIRE_TIME, fireTime);
                if (!string.IsNullOrEmpty(notification.IntentData))
                    extras.Call("putString", KEY_INTENT_DATA, notification.IntentData);
            }

            return notificationBuilder;
        }

        internal static AndroidNotificationIntentData GetNotificationData(AndroidJavaObject notificationObj)
        {
            using (var extras = notificationObj.Get<AndroidJavaObject>("extras"))
            {
                var id = extras.Call<int>("getInt", KEY_ID, -1);
                if (id == -1)
                    return null;

                var channelId = s_NotificationManagerClass.CallStatic<string>("getNotificationChannelId", notificationObj);
                int flags = notificationObj.Get<int>("flags");

                var notification = new AndroidNotification();
                notification.Title = extras.Call<string>("getString", Notification_EXTRA_TITLE);
                notification.Text = extras.Call<string>("getString", Notification_EXTRA_TEXT);
                notification.SmallIcon = extras.Call<string>("getString", KEY_SMALL_ICON);
                notification.LargeIcon = extras.Call<string>("getString", KEY_LARGE_ICON);
                notification.ShouldAutoCancel = 0 != (flags & Notification_FLAG_AUTO_CANCEL);
                notification.UsesStopwatch = extras.Call<bool>("getBoolean", Notification_EXTRA_SHOW_CHRONOMETER, false);
                notification.FireTime = extras.Call<long>("getLong", KEY_FIRE_TIME, -1L).ToDatetime();
                notification.RepeatInterval = extras.Call<long>("getLong", KEY_REPEAT_INTERVAL, -1L).ToTimeSpan();

                if (extras.Call<bool>("containsKey", Notification_EXTRA_BIG_TEXT))
                    notification.Style = NotificationStyle.BigTextStyle;
                else
                    notification.Style = NotificationStyle.None;

                using (var color = s_NotificationManagerClass.CallStatic<AndroidJavaObject>("getNotificationColor", notificationObj))
                {
                    if (color == null)
                        notification.Color = null;
                    else
                        notification.Color = color.Call<int>("intValue").ToColor();
                }
                notification.Number = notificationObj.Get<int>("number");
                notification.IntentData = extras.Call<string>("getString", KEY_INTENT_DATA);
                notification.Group = notificationObj.Call<string>("getGroup");
                notification.GroupSummary = 0 != (flags & Notification_FLAG_GROUP_SUMMARY);
                notification.SortKey = notificationObj.Call<string>("getSortKey");
                notification.GroupAlertBehaviour = s_NotificationManagerClass.CallStatic<int>("getNotificationGroupAlertBehavior", notificationObj).ToGroupAlertBehaviours();
                var showTimestamp = extras.Call<bool>("getBoolean", Notification_EXTRA_SHOW_WHEN, false);
                notification.ShowTimestamp = showTimestamp;
                if (showTimestamp)
                    notification.CustomTimestamp = notificationObj.Get<long>("when").ToDatetime();

                var data = new AndroidNotificationIntentData(id, channelId, notification);
                data.NativeNotification = notificationObj;
                return data;
            }
        }

        internal static void ReceivedNotificationCallback(AndroidJavaObject notification)
        {
            var data = GetNotificationData(notification);
            OnNotificationReceived(data);
        }
    }
}

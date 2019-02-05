using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#pragma warning disable 162, 67, 414

namespace Unity.Notifications.Android
{
    /// <summary>
    /// The level of interruption of this notification channel.
    /// The importance of a notification is used to determine how much the notification should interrupt the user (visually and audibly). The higher the importance of a notification, the more interruptive the notification will be.
    /// </summary>
    /// <remarks>
    /// The exact behaviour of each importance level might vary depending on the device and OS version on devices running Android 7.1 or older.
    /// </remarks>
    public enum Importance
    {
        /// <summary>
        /// A notification with no importance: does not show in the shade.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Low importance, notification is shown everywhere, but is not intrusive. 
        /// </summary>
        Low = 2,

        /// <summary> 
        /// Default importance, notification is shown everywhere, makes noise, but does not intrude visually.
        /// </summary>
        Default = 3,

        /// <summary>
        /// High importance, notification is shown everywhere, makes noise and is shown on the screen.
        /// </summary>
        High = 4,
    }

    /// <summary>
    /// Determines whether notifications appear on the lock screen.
    /// </summary>
    public enum LockScreenVisibility
    {
        /// <summary>
        /// Do not reveal any part of this notification on a secure lock screen.
        /// </summary>
        Secret = -1,

        /// <summary>
        /// Show this notification on all lock screens, but conceal sensitive or private information on secure lock screens.
        /// </summary>
        Private = -1000,

        /// <summary>
        /// Show this notification in its entirety on the lock screen.
        /// </summary>
        Public = 1,
    }

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
    /// Allows applying a rich notification style to a notification.
    /// </summary>
    public enum NotificationStyle
    {
        /// <summary>
        /// Use the default style.
        /// </summary>
        None = 0,

//        // TODO Currently disabled, BigPicture style requires additional logic that will be implemented in a future release
//        /// <summary>
//        /// Generate a large-format notification.
//        /// </summary>
//        BigPicture = 1,

        /// <summary>
        /// Generate a large-format notification that includes a lot of text.
        /// </summary>
        BigTextStyle = 2
    }

    /// <summary>
    /// The AndroidNotification is used schedule a local notification, which includes the content of the notification.
    /// </summary>
    public struct AndroidNotification
    {
        /// <summary>
        /// Notification title.
        /// Set the first line of text in the notification.
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        /// <summary>
        /// Notification body.
        /// Set the second line of text in the notification.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        
        /// <summary>
        /// Notification small icon.
        /// It will be used to represent the notification in the status bar and content view (unless overridden there by a large icon)
        /// The icon PNG file has to be placed in the `/Assets/Plugins/Android/res/drawable` folder and it's name has to be specified without the extension.
        /// </summary>
        public string SmallIcon
        {
            get { return smallIcon; }
            set { smallIcon = value; }
        }

        /// <summary>
        /// The date and time when the notification should be delivered.
        /// </summary>
        public DateTime FireTime
        {
            get
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                return origin.AddMilliseconds(fireTime).ToLocalTime();
            }
            set
            {
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan diff = value.ToUniversalTime() - origin;

                fireTime = (long) Math.Floor(diff.TotalMilliseconds);
            }
        }

        /// <summary>
        /// The notification will be be repeated on every specified time interval.
        /// Do not set for one time notifications.
        /// </summary>
        public TimeSpan? RepeatInterval
        {
            get { return  TimeSpan.FromMilliseconds(repeatInterval); }
            set
            {
                if (value != null)
                    repeatInterval = (long)value.Value.TotalMilliseconds;
                else
                    repeatInterval = -1L;
            }
        }

        /// <summary>
        /// Notification large icon.
        /// Add a large icon to the notification content view. This image will be shown on the left of the notification view in place of the small icon (which will be placed in a small badge atop the large icon).
        /// The icon PNG file has to be placed in the `/Assets/Plugins/Android/res/drawable folder` and it's name has to be specified without the extension.
        /// </summary>
        public string LargeIcon
        {
            get { return largeIcon; }
            set { largeIcon = value; }
        }

        /// <summary>
        /// Apply a custom style to the notification.
        /// Currently only BigPicture and BigText styles are supported.
        /// </summary>
        public NotificationStyle Style
        {
            get { return (NotificationStyle) style; }
            set { style = (int) value; }
        }

        /// <summary>
        /// Accent color to be applied by the standard style templates when presenting this notification.
        /// The template design constructs a colorful header image by overlaying the icon image (stenciled in white) atop a field of this color. Alpha components are ignored.
        /// </summary>
        public Color? Color
        {
            get
            {
                if (color < 0)
                    return null;

                int a = (color >> 24) & 0xff;
                int r = (color >> 16) & 0xff;
                int g = (color >> 8) & 0xff;
                int b = (color) & 0xff;

                return new Color32((byte) a, (byte) r, (byte) g, (byte) b);
            }
            set
            {
                if (value == null)
                    color = -1;
                else
                {
                    var color32 = (Color32) value.Value;
                    color = (color32.a & 0xff) << 24 | (color32.r & 0xff) << 16 | (color32.g & 0xff) << 8 |
                              (color32.b & 0xff);
                }
            }
        }

        /// <summary>
        /// Sets the number of items this notification represents.
        /// Is displayed as a badge count on the notification icon if the launcher supports this behavior.
        /// </summary>
        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        /// <summary>
        /// This notification will automatically be dismissed when the user touches it.
        /// By default this behavior is turned off.
        /// </summary>
        public bool ShouldAutoCancel
        {
            get { return shouldAutoCancel; }
            set { shouldAutoCancel = value; }
        }

        /// <summary>
        /// Show the notification time field as a stopwatch instead of a timestamp.
        /// </summary>
        public bool UsesStopwatch
        {
            get { return usesStopwatch; }
            set { usesStopwatch = value; }
        }

        internal string title;
        internal string text;
        
        internal string smallIcon;
        internal long fireTime;
        internal bool shouldAutoCancel;

        internal string largeIcon;

        internal int style;
        internal int color;

        internal int number;
        internal bool usesStopwatch;
        internal long repeatInterval;

        /// <summary>
        /// Create a notification struct with all optional fields set to default values.
        /// </summary>
        public AndroidNotification(String title, String text, DateTime fireTime)
        {
            this.title = title;
            this.text = text;

            repeatInterval = -1;
            smallIcon = "";
            shouldAutoCancel = false;
            largeIcon = "";
            style = (int) NotificationStyle.None;
            color = -1;
            number = -1;
            usesStopwatch = false;
            this.fireTime = -1;
            
            this.FireTime = fireTime;
        }

        /// <summary>
        /// Create a repeatable notification struct with all optional fields set to default values.
        /// </summary>
        /// <remarks>
        /// There is a minimum period of 1 minute for repeating notifications.
        /// </remarks>
        public AndroidNotification(String title, String text, DateTime fireTime, TimeSpan repeatInterval)
            : this(title, text, fireTime)
        {
            this.RepeatInterval = repeatInterval;
        }

        public AndroidNotification(String title, String text, DateTime fireTime, TimeSpan repeatInterval,
            String smallIcon)
            : this(title, text, fireTime, repeatInterval)
        {
            this.SmallIcon = smallIcon;
        }
    }

    public struct AndroidNotificationChannel
    {
        /// <summary>
        /// Create a notification channel struct with all optional fields set to default values.
        /// </summary>
        public AndroidNotificationChannel(string id, string title, string description, Importance importance)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.importance = (int) importance;

            canBypassDnd = false;
            canShowBadge = true;
            enableLights = false;
            enableVibration = true;
            lockscreenVisibility = (int) LockScreenVisibility.Public;
            vibrationPattern = null;
        }

        /// <summary>
        /// Notification channel identifier.
        /// Must be specified when scheduling notifications.
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// Notification channel name which is visible to users.
        /// </summary>
        public string Name
        {
            get { return title; }
            set { title = value; }
        }

        /// <summary>
        /// User visible description of the notification channel.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Importance level which is applied to all notifications sent to the channel.
        /// This can be changed by users in the settings app. Android uses importance to determine how much the notification should interrupt the user (visually and audibly).
        /// The higher the importance of a notification, the more interruptive the notification will be.
        /// The possible importance levels are the following:
        ///    High: Makes a sound and appears as a heads-up notification.
        ///    Default: Makes a sound.
        ///    Low: No sound.
        ///    None: No sound and does not appear in the status bar.
        /// </summary>
        public Importance Importance
        {
            get { return (Importance) importance; }
            set { importance = (int) value; }
        }

        /// <summary>
        /// Whether or not notifications posted to this channel can bypass the Do Not Disturb.
        /// This can be changed by users in the settings app.
        /// </summary>
        public bool CanBypassDnd
        {
            get { return canBypassDnd; }
            set { canBypassDnd = value; }
        }

        /// <summary>
        /// Whether notifications posted to this channel can appear as badges in a Launcher application.
        /// </summary>
        public bool CanShowBadge
        {
            get { return canShowBadge; }
            set { canShowBadge = value; }
        }

        /// <summary>
        /// Sets whether notifications posted to this channel should display notification lights, on devices that support that feature.
        /// This can be changed by users in the settings app.
        /// </summary>/
        public bool EnableLights
        {
            get { return enableLights; }
            set { enableLights = value; }
        }

        /// <summary>
        /// Sets whether notification posted to this channel should vibrate.
        /// This can be changed by users in the settings app.
        /// </summary>
        public bool EnableVibration
        {
            get { return enableVibration; }
            set { enableVibration = value; }
        }

        /// <summary>
        /// Sets whether or not notifications posted to this channel are shown on the lockscreen in full or redacted form.
        /// This can be changed by users in the settings app.
        /// </summary>
        public LockScreenVisibility LockScreenVisibility
        {
            get { return (LockScreenVisibility) lockscreenVisibility; }
            set { lockscreenVisibility = (int) value; }
        }

        /// <summary>
        /// Sets the vibration pattern for notifications posted to this channel.
        /// </summary>
        public long[] VibrationPattern
        {
            get { return vibrationPattern; }
            set { vibrationPattern = value; }
        }
        
        /// <summary>
        /// Returns false if the user has blocked this notification in the settings app. Channels can be manually blocked by settings it's Importance to None.
        /// </summary>
        public bool Enabled
        {
            get { return Importance != Importance.None; }
        }

        internal string id;
        internal string title;
        internal string description;
        internal int importance;

        internal bool canBypassDnd;
        internal bool canShowBadge;
        internal bool enableLights;
        internal bool enableVibration;
        internal int lockscreenVisibility;
        internal long[] vibrationPattern;
    }

    /// <summary>
    /// Use the AndroidNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class AndroidNotificationCenter
    {
        public delegate void NotificationReceivedCallback(int id, AndroidNotification notification, string channel);

        /// <summary>
        /// Subscribe to this event to receive callbacks whenever a scheduled notification is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived = delegate { };

        const int ANDROID_OREO = 26;
        const int ANDROID_M = 23;
        
        const string DEFAULT_APP_ICON_ADAPTIVE = "ic_launcher_foreground";
        const string DEFAULT_APP_ICON_LEGACY = "app_icon";

        static AndroidJavaObject notificationManager;
        static int AndroidSDK;
        
        static bool initialized;

        static bool Initialize()
        {
            if (initialized)
                return true;

            #if UNITY_EDITOR || !UNITY_ANDROID
                return false;
            #endif

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

            return initialized = true;
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
                throw new Exception(string.Format("Cannot register notification channel: {} , the channel Name is not set.", channel.id));
            }
            else if (string.IsNullOrEmpty(channel.description))
            {
                throw new Exception(string.Format("Cannot register notification channel: {} , the channel Description is not set.", channel.id));
            }
            
            notificationManager.Call("registerNotificationChannel",
                channel.id,
                channel.title,
                Enum.IsDefined(typeof(Importance), channel.importance) ? channel.importance : (int)Importance.Default,
                channel.description,
                channel.enableLights,
                channel.enableVibration,
                channel.canBypassDnd,
                channel.canShowBadge,
                channel.vibrationPattern,
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
                ch.title = channel.Get<string>("name");
                ch.importance = channel.Get<int>("importance");

                ch.description = channel.Get<string>("description");

                ch.enableLights = channel.Get<bool>("enableLights");
                ch.enableVibration = channel.Get<bool>("enableVibration");
                ch.canBypassDnd = channel.Get<bool>("canBypassDnd");
                ch.canShowBadge = channel.Get<bool>("canShowBadge");
                ch.vibrationPattern = channel.Get<long[]>("vibrationPattern");
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

            int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
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
            return (NotificationStatus) status;
        }

        internal static void SendNotification(int id, AndroidNotification notification, string channel)
        {
            if (notification.fireTime < 0L)
            {
                Debug.LogError("Failed to schedule notification, it did not contain a valid FireTime");
            }

            AndroidJavaClass managerClass =
                new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
            AndroidJavaObject activity = notificationManager.Get<AndroidJavaObject>("mActivity");

            AndroidJavaObject notificationIntent =
                new AndroidJavaObject("android.content.Intent", activity, managerClass);

            AndroidJavaObject androidContext = notificationManager.Get<AndroidJavaObject>("mContext");

            int smallIconId = notificationManager.CallStatic<int>("findResourceidInContextByName",
                notification.smallIcon, androidContext, activity);
            int largeIconId = notificationManager.CallStatic<int>("findResourceidInContextByName",
                notification.largeIcon, androidContext, activity);


            if (smallIconId == 0)
            {
                smallIconId = notificationManager.CallStatic<int>("findResourceidInContextByName",
                    DEFAULT_APP_ICON_ADAPTIVE, androidContext, activity);

                if (smallIconId == 0)
                {
                    smallIconId = notificationManager.CallStatic<int>("findResourceidInContextByName",
                        DEFAULT_APP_ICON_LEGACY, androidContext, activity);
                }
            }

            notificationIntent.Call<AndroidJavaObject>("putExtra", "id", id);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "channelID", channel);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "textTitle", notification.title);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "textContent", notification.text);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "smallIcon", smallIconId);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "autoCancel", notification.shouldAutoCancel);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "usesChronometer", notification.usesStopwatch);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "fireTime", notification.fireTime);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "repeatInterval", notification.repeatInterval);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "largeIcon", largeIconId);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "style", notification.style);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "color", notification.color);
            notificationIntent.Call<AndroidJavaObject>("putExtra", "number", notification.number);

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

        class NotificationCallback : AndroidJavaProxy
        {
            public NotificationCallback() : base("com.unity.androidnotifications.NotificationCallback")
            {
            }

            public void onSentNotification(AndroidJavaObject notificationIntent)
            {                
                var notification = new AndroidNotification();

                var id = notificationIntent.Call<int>("getIntExtra", "id", -1);
                var channel = notificationIntent.Call<string>("getStringExtra", "channelID");
                notification.title = notificationIntent.Call<string>("getStringExtra", "textTitle");
                notification.text = notificationIntent.Call<string>("getStringExtra", "textContent");
                notification.shouldAutoCancel = notificationIntent.Call<bool>("getBooleanExtra", "autoCancel", false);
                notification.usesStopwatch =
                    notificationIntent.Call<bool>("getBooleanExtra", "usesChronometer", false);
                notification.fireTime = notificationIntent.Call<long>("getLongExtra", "fireTime", -1L);
                notification.repeatInterval = notificationIntent.Call<long>("getLongExtra", "repeatInterval", -1L);
                notification.style = notificationIntent.Call<int>("getIntExtra", "style", -1);
                notification.color = notificationIntent.Call<int>("getIntExtra", "color", -1);
                notification.number = notificationIntent.Call<int>("getIntExtra", "number", -1);

                OnNotificationReceived(id, notification, channel);
            }
        }
    }
}

#pragma warning restore 162, 67, 414
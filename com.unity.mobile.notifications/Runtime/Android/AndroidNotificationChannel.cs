using System.Linq;

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

    public struct AndroidNotificationChannel
    {
        /// <summary>
        /// Notification channel identifier.
        /// Must be specified when scheduling notifications.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Notification channel name which is visible to users.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User visible description of the notification channel.
        /// </summary>
        public string Description { get; set; }

        internal int importance;
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
            get { return (Importance)importance; }
            set { importance = (int)value; }
        }

        /// <summary>
        /// Whether or not notifications posted to this channel can bypass the Do Not Disturb.
        /// This can be changed by users in the settings app.
        /// </summary>
        public bool CanBypassDnd { get; set; }

        /// <summary>
        /// Whether notifications posted to this channel can appear as badges in a Launcher application.
        /// </summary>
        public bool CanShowBadge { get; set; }

        /// <summary>
        /// Sets whether notifications posted to this channel should display notification lights, on devices that support that feature.
        /// This can be changed by users in the settings app.
        /// </summary>/
        public bool EnableLights { get; set; }

        /// <summary>
        /// Sets whether notification posted to this channel should vibrate.
        /// This can be changed by users in the settings app.
        /// </summary>
        public bool EnableVibration { get; set; }

        internal int lockscreenVisibility;
        /// <summary>
        /// Sets whether or not notifications posted to this channel are shown on the lockscreen in full or redacted form.
        /// This can be changed by users in the settings app.
        /// </summary>
        public LockScreenVisibility LockScreenVisibility
        {
            get { return (LockScreenVisibility)lockscreenVisibility; }
            set { lockscreenVisibility = (int)value; }
        }

        internal int[] vibrationPattern;
        /// <summary>
        /// Sets the vibration pattern for notifications posted to this channel.
        /// </summary>
        public long[] VibrationPattern
        {
            // There is an issue with IL2CPP failing to compile a struct which contains an array of long on Unity 2019.2+ (see case 1173310).
            get
            {
                if (vibrationPattern == null)
                    return null;
                return vibrationPattern.Select(i => (long)i).ToArray();
            }
            set
            {
                if (value != null)
                    vibrationPattern = value.Select(i => (int)i).ToArray();
            }
        }

        /// <summary>
        /// Returns false if the user has blocked this notification in the settings app. Channels can be manually blocked by settings it's Importance to None.
        /// </summary>
        public bool Enabled
        {
            get { return Importance != Importance.None; }
        }

        /// <summary>
        /// Create a notification channel struct with all optional fields set to default values.
        /// </summary>
        public AndroidNotificationChannel(string id, string name, string description, Importance importance)
        {
            Id = id;
            Name = name;
            Description = description;
            this.importance = (int)importance;

            CanBypassDnd = false;
            CanShowBadge = true;
            EnableLights = false;
            EnableVibration = true;
            lockscreenVisibility = (int)LockScreenVisibility.Public;
            vibrationPattern = null;
        }
    }
}

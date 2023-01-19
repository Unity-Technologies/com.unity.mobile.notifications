using System;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// The level of interruption of this notification channel.
    /// The importance of a notification is used to determine how much the notification should interrupt the user (visually and audibly).
    /// The higher the importance of a notification, the more interruptive the notification will be.
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
        Private = 0,

        /// <summary>
        /// Show this notification in its entirety on the lock screen.
        /// </summary>
        Public = 1,
    }

    /// <summary>
    /// The wrapper of the Android notification channel. Use this to group notifications by groups.
    /// </summary>
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

        /// <summary>
        /// The ID of the registered channel group this channel belongs to.
        /// </summary>
        public string Group { get; set; }

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
        public Importance Importance { get; set; }

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

        /// <summary>
        /// Sets the vibration pattern for notifications posted to this channel.
        /// </summary>
        public long[] VibrationPattern { get; set; }

        /// <summary>
        /// Sets whether or not notifications posted to this channel are shown on the lockscreen in full or redacted form.
        /// This can be changed by users in the settings app.
        /// </summary>
        public LockScreenVisibility LockScreenVisibility { get; set; }

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
        /// <param name="id">ID for the channel</param>
        /// <param name="name">Channel name</param>
        /// <param name="description">Channel description</param>
        /// <param name="importance">Importance of the channel</param>
        public AndroidNotificationChannel(string id, string name, string description, Importance importance)
        {
            Id = id;
            Name = name;
            Description = description;
            Group = null;
            this.Importance = importance;

            CanBypassDnd = false;
            CanShowBadge = true;
            EnableLights = false;
            EnableVibration = true;
            VibrationPattern = null;

            this.LockScreenVisibility = LockScreenVisibility.Public;
        }
    }

    /// <summary>
    /// Notification channel group description.
    /// It is optional to put channels into groups, but looks nicer in Settings UI.
    /// </summary>
    public struct AndroidNotificationChannelGroup
    {
        /// <summary>
        /// A unique ID for this group. Will rename the group if already exists.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// A user visible name for this group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description for this group.
        /// </summary>
        public string Description { get; set; }
    }
}

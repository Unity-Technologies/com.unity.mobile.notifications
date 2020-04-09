using System;
using UnityEngine;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// Allows applying a rich notification style to a notification.
    /// </summary>
    public enum NotificationStyle
    {
        /// <summary>
        /// Use the default style.
        /// </summary>
        None = 0,

        //// todo currently disabled, bigpicture style requires additional logic that will be implemented in a future release
        ///// <summary>
        ///// generate a large-format notification.
        ///// </summary>
        //bigpicture = 1,

        /// <summary>
        /// Generate a large-format notification that includes a lot of text.
        /// </summary>
        BigTextStyle = 2
    }

    /// <summary>
    /// Allows applying an alert behaviour to grouped notifications.
    /// </summary>
    public enum GroupAlertBehaviours
    {
        /// <summary>
        /// All notifications in a group with sound or vibration will make sound or vibrate, so this notification will not be muted when it is in a group.
        /// </summary>
        GroupAlertAll = 0,

        /// <summary>
        /// The summary notification in a group will be silenced (no sound or vibration) even if they would otherwise make sound or vibrate.
        /// Use this to mute this notification if this notification is a group summary.
        /// </summary>
        GroupAlertSummary = 1,

        /// <summary>
        /// All children notification in a group will be silenced (no sound or vibration) even if they would otherwise make sound or vibrate.
        /// Use this to mute this notification if this notification is a group child. This must be set on all children notifications you want to mute.
        /// </summary>
        GroupAlertChildren = 2,
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
        public string Title { get; set; }

        /// <summary>
        /// Notification body.
        /// Set the second line of text in the notification.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Notification small icon.
        /// It will be used to represent the notification in the status bar and content view (unless overridden there by a large icon)
        /// The icon PNG file has to be placed in the `/Assets/Plugins/Android/res/drawable` folder and it's name has to be specified without the extension.
        /// </summary>
        public string SmallIcon { get; set; }

        /// <summary>
        /// The date and time when the notification should be delivered.
        /// </summary>
        public DateTime FireTime { get; set; }

        /// <summary>
        /// The notification will be be repeated on every specified time interval.
        /// Do not set for one time notifications.
        /// </summary>
        public TimeSpan? RepeatInterval
        {
            get { return m_RepeatInterval; }
            set { m_RepeatInterval = value.HasValue ? value.Value : (-1L).ToTimeSpan(); }
        }

        /// <summary>
        /// Notification large icon.
        /// Add a large icon to the notification content view. This image will be shown on the left of the notification view in place of the small icon (which will be placed in a small badge atop the large icon).
        /// The icon PNG file has to be placed in the `/Assets/Plugins/Android/res/drawable folder` and it's name has to be specified without the extension.
        /// </summary>
        public string LargeIcon { get; set; }

        /// <summary>
        /// Apply a custom style to the notification.
        /// Currently only BigPicture and BigText styles are supported.
        /// </summary>
        public NotificationStyle Style { get; set; }

        /// <summary>
        /// Accent color to be applied by the standard style templates when presenting this notification.
        /// The template design constructs a colorful header image by overlaying the icon image (stenciled in white) atop a field of this color. Alpha components are ignored.
        /// </summary>
        public Color? Color
        {
            get { return m_Color; }
            set { m_Color = value.HasValue ? value.Value : new Color(0, 0, 0, 0); }
        }

        /// <summary>
        /// Sets the number of items this notification represents.
        /// Is displayed as a badge count on the notification icon if the launcher supports this behavior.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// This notification will automatically be dismissed when the user touches it.
        /// By default this behavior is turned off.
        /// </summary>
        public bool ShouldAutoCancel { get; set; }

        /// <summary>
        /// Show the notification time field as a stopwatch instead of a timestamp.
        /// </summary>
        public bool UsesStopwatch { get; set; }

        /// <summary>
        ///Set this property for the notification to be made part of a group of notifications sharing the same key.
        /// Grouped notifications may display in a cluster or stack on devices which support such rendering.
        /// Only available on Android 7.0 (API level 24) and above.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Set this notification to be the group summary for a group of notifications. Requires the 'Group' property to also be set.
        /// Grouped notifications may display in a cluster or stack on devices which support such rendering.
        /// Only available on Android 7.0 (API level 24) and above.
        /// </summary>
        public bool GroupSummary { get; set; }

        /// <summary>
        /// Sets the group alert behavior for this notification. Set this property to mute this notification if alerts for this notification's group should be handled by a different notification.
        /// This is only applicable for notifications that belong to a group. This must be set on all notifications you want to mute.
        /// Only available on Android 8.0 (API level 26) and above.
        /// </summary>
        public GroupAlertBehaviours GroupAlertBehaviour { get; set; }

        /// <summary>
        /// The sort key will be used to order this notification among other notifications from the same package.
        /// Notifications will be sorted lexicographically using this value.
        /// </summary>
        public string SortKey { get; set; }

        /// <summary>
        /// Use this to save arbitrary string data related to the notification.
        /// </summary>
        public string IntentData { get; set; }

        /// <summary>
        /// Enable it to show a timestamp on the notification when it's delivered, unless the "CustomTimestamp" property is set "FireTime" will be shown.
        /// </summary>
        public bool ShowTimestamp { get; set; }

        /// <summary>
        /// Set this to show custom date instead of the notification's "FireTime" as the notification's timestamp'.
        /// </summary>
        public DateTime CustomTimestamp
        {
            get { return m_CustomTimestamp; }
            set
            {
                ShowCustomTimestamp = true;
                m_CustomTimestamp = value;
            }
        }

        internal bool ShowCustomTimestamp { get; set; }

        private Color m_Color;
        private TimeSpan m_RepeatInterval;
        private DateTime m_CustomTimestamp;

        /// <summary>
        /// Create a notification struct with all optional fields set to default values.
        /// </summary>
        public AndroidNotification(string title, string text, DateTime fireTime)
        {
            Title = title;
            Text = text;
            FireTime = fireTime;

            SmallIcon = string.Empty;
            ShouldAutoCancel = false;
            LargeIcon = string.Empty;
            Style = NotificationStyle.None;
            Number = -1;
            UsesStopwatch = false;
            IntentData = string.Empty;
            Group = string.Empty;
            GroupSummary = false;
            SortKey = string.Empty;
            GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertAll;
            ShowTimestamp = false;
            ShowCustomTimestamp = false;

            m_RepeatInterval = (-1L).ToTimeSpan();
            m_Color = new Color(0, 0, 0, 0);
            m_CustomTimestamp = (-1L).ToDatetime();
        }

        /// <summary>
        /// Create a repeatable notification struct with all optional fields set to default values.
        /// </summary>
        /// <remarks>
        /// There is a minimum period of 1 minute for repeating notifications.
        /// </remarks>
        public AndroidNotification(string title, string text, DateTime fireTime, TimeSpan repeatInterval)
            : this(title, text, fireTime)
        {
            RepeatInterval = repeatInterval;
        }

        public AndroidNotification(string title, string text, DateTime fireTime, TimeSpan repeatInterval, string smallIcon)
            : this(title, text, fireTime, repeatInterval)
        {
            SmallIcon = smallIcon;
        }
    }
}

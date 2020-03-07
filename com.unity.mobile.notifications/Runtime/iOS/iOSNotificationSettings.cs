using System.Runtime.InteropServices;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Enum indicating whether the app is allowed to schedule notifications.
    /// </summary>
    public enum AuthorizationStatus
    {
        /// <summary>
        /// The user has not yet made a choice regarding whether the application may post notifications.
        /// </summary>
        NotDetermined = 0,
        /// <summary>
        /// The application is not authorized to post notifications.
        /// </summary>
        Denied,
        /// <summary>
        /// The application is authorized to post notifications.
        /// </summary>
        Authorized
    }

    /// <summary>
    /// Presentation styles for alerts.
    /// </summary>
    public enum AlertStyle
    {
        /// <summary>
        /// No alert.
        /// </summary>
        None = 0,
        /// <summary>
        /// Banner alerts.
        /// </summary>
        Banner = 1,
        /// <summary>
        /// Modal alerts.
        /// </summary>
        Alert = 2,
    }

    /// <summary>
    /// The style for previewing a notification's content.
    /// </summary>
    public enum ShowPreviewsSetting
    {
        /// <summary>
        /// The notification's content is always shown, even when the device is locked.
        /// </summary>
        Always = 0,
        /// <summary>
        /// The notification's content is shown only when the device is unlocked.
        /// </summary>
        WhenAuthenticated = 1,
        /// <summary>
        /// The notification's content is never shown, even when the device is unlocked
        /// </summary>
        Never = 2,
    }

    /// <summary>
    /// Enum indicating the current status of a notification setting.
    /// </summary>
    public enum NotificationSetting
    {
        /// <summary>
        /// The app does not support this notification setting.
        /// </summary>
        NotSupported = 0,

        /// <summary>
        /// The notification setting is turned off.
        /// </summary>
        Disabled,

        /// <summary>
        /// The notification setting is turned on.
        /// </summary>
        Enabled,
    }

    /// <summary>
    /// iOSNotificationSettings  contains the current authorization status and notification-related settings for your app. Your app must receive authorization to schedule notifications.
    /// </summary>
    /// <remarks>
    /// Use this struct to determine what notification-related actions your app is allowed to perform by the user. This information should be used to enable, disable, or adjust your app's notification-related behaviors.
    /// The system enforces your app's settings by preventing denied interactions from occurring.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct iOSNotificationSettings
    {
        internal int authorizationStatus;
        internal int notificationCenterSetting;
        internal int lockScreenSetting;
        internal int carPlaySetting;
        internal int alertSetting;
        internal int badgeSetting;
        internal int soundSetting;

        internal int alertStyle;
        internal int showPreviewsSetting;


        /// <summary>
        /// When the value is set to Authorized your app is allowed to schedule and receive local and remote notifications.
        /// </summary>
        /// <remarks>
        /// When authorized, use the alertSetting, badgeSetting, and soundSetting properties to specify which types of interactions are allowed.
        /// When the `AuthorizationStatus` value is `Denied`, the system doesn't deliver notifications to your app, and the system ignores any attempts to schedule local notifications.
        /// </remarks>
        public AuthorizationStatus AuthorizationStatus
        {
            get { return (AuthorizationStatus)authorizationStatus; }
        }

        /// <summary>
        /// The setting that indicates whether your app’s notifications are displayed in Notification Center.
        /// </summary>
        public NotificationSetting NotificationCenterSetting
        {
            get { return (NotificationSetting)notificationCenterSetting; }
        }

        /// <summary>
        /// The setting that indicates whether your app’s notifications appear onscreen when the device is locked.
        /// </summary>
        public NotificationSetting LockScreenSetting
        {
            get { return (NotificationSetting)lockScreenSetting; }
        }

        /// <summary>
        /// The setting that indicates whether your app’s notifications may be displayed in a CarPlay environment.
        /// </summary>
        public NotificationSetting CarPlaySetting
        {
            get { return (NotificationSetting)carPlaySetting; }
        }

        /// <summary>
        /// The authorization status for displaying alerts.
        /// </summary>
        public NotificationSetting AlertSetting
        {
            get { return (NotificationSetting)alertSetting; }
        }

        /// <summary>
        /// The authorization status for badging your app’s icon.
        /// </summary>
        public NotificationSetting BadgeSetting
        {
            get { return (NotificationSetting)badgeSetting; }
        }

        /// <summary>
        /// The authorization status for playing sounds for incoming notifications.
        /// </summary>
        public NotificationSetting SoundSetting
        {
            get { return (NotificationSetting)soundSetting; }
        }

        /// <summary>
        /// The type of alert that the app may display when the device is unlocked.
        /// </summary>
        /// <remarks>
        /// This property specifies the presentation style for alerts when the device is unlocked.
        /// The user may choose to display alerts as automatically disappearing banners or as modal windows that require explicit dismissal (the user may also choose not to display alerts at all.
        /// </remarks>
        public AlertStyle AlertStyle
        {
            get { return (AlertStyle)alertStyle; }
        }

        /// <summary>
        /// The setting that indicates whether the app shows a preview of the notification's content.
        /// </summary>
        public ShowPreviewsSetting ShowPreviewsSetting
        {
            get { return (ShowPreviewsSetting)showPreviewsSetting; }
        }
    }
}

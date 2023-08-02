using System;

#if UNITY_ANDROID
using PlatformNotification = Unity.Notifications.Android.AndroidNotification;
#else
using System.Globalization;
using PlatformNotification = Unity.Notifications.iOS.iOSNotification;
#endif

namespace Unity.Notifications
{
    /// <summary>
    /// Represents a notification to be sent or a received one.
    /// Can be converted to platform specific notification via explicit cast.
    /// <code>
    /// var n1 = (AndroidNotification)notification; // convert to Android
    /// var n1 = (iOSNotification)notification; // convert to iOS
    /// </code>
    /// </summary>
    public struct Notification
    {
        PlatformNotification notification;

        public static explicit operator PlatformNotification(Notification n)
        {
#if UNITY_ANDROID
            n.notification.ShowInForeground = n.ShowInForeground;
            n.notification.ShouldAutoCancel = true; // iOS always auto-cancels
            return n.notification;
#else
            var ret = n.notification;
            if (ret != null && n.Identifier.HasValue)
                ret.Identifier = n.Identifier.Value.ToString(CultureInfo.InvariantCulture);
            ret.ShowInForeground = n.ShowInForeground;
            return ret;
#endif
        }

        internal Notification(PlatformNotification notification
#if UNITY_ANDROID
            , int id
#endif
            )
        {
            this.notification = notification;
            Identifier = default;
            ShowInForeground = notification.ShowInForeground;

#if UNITY_ANDROID
            Identifier = id;
#else
            if (int.TryParse(notification.Identifier, NumberStyles.None, CultureInfo.InvariantCulture, out int val))
                Identifier = val;
#endif
        }

        /// <summary>
        /// A unique identifier for this notification.
        /// If null, a unique ID will be generated when scheduling.
        /// </summary>
        public int? Identifier { get; set; }

        /// <summary>
        /// String that is shown on notification as title.
        /// </summary>
        public string Title
        {
            get
            {
#if UNITY_IOS
                if (notification == null)
                    return null;
#endif
                return notification.Title;
            }
            set
            {
#if UNITY_IOS
                if (notification == null)
                    notification = new PlatformNotification();
#endif
                notification.Title = value;
            }
        }

        /// <summary>
        /// String that is shown on notification as it's main body.
        /// </summary>
        public string Text
        {
            get
            {
#if UNITY_ANDROID
                return notification.Text;
#else
                return notification?.Body;
#endif
            }
            set
            {
#if UNITY_ANDROID
                notification.Text = value;
#else
                if (notification == null)
                    notification = new PlatformNotification();
                notification.Body = value;
#endif
            }
        }

        /// <summary>
        /// Arbitrary data that is sent with notification.
        /// Can be used to store some useful information in the notification to be later retrieved when notification arrives or is tapped by user.
        /// </summary>
        public string Data
        {
            get
            {
#if UNITY_ANDROID
                return notification.IntentData;
#else
                return notification.Data;
#endif
            }
            set
            {
#if UNITY_ANDROID
                notification.IntentData = value;
#else
                notification.Data = value;
#endif
            }
        }

        /// <summary>
        /// Number, associated with the notification. Zero is ignored.
        /// When supported, shows up as badge on application launcher.
        /// </summary>
        public int Badge
        {
            get
            {
#if UNITY_ANDROID
                return notification.Number;
#else
                return notification.Badge;
#endif
            }
            set
            {
#if UNITY_ANDROID
                notification.Number = value;
#else
                notification.Badge = value;
#endif
            }
        }

        // Default value differs on Android/iOS, so have separate here and unify uppon conversion
        /// <summary>
        /// Indicated, whether notification should be shown if it arrives while application is in foreground.
        /// When notification arrives with app in foreground <see cref="NotificationCenter.OnNotificationReceived"/> even fires regardless of this.
        /// Default is false, meaning notifications are silent when app is in foreground.
        /// </summary>
        public bool ShowInForeground { get; set; }
    }
}

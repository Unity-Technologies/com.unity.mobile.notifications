using System;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using System.Globalization;
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications
{
    public enum NotificationRepeatInterval
    {
        OneTime,
        Hourly,
        Daily,
    }

    public struct Notification
    {
#if UNITY_ANDROID
        AndroidNotification notification;
#else
        iOSNotification notification;
#endif

#if UNITY_ANDROID
        public static explicit operator AndroidNotification(Notification n)
        {
            n.notification.ShowInForeground = n.ShowInForeground;
            n.notification.ShouldAutoCancel = true; // iOS always auto-cancels
            return n.notification;
        }
#else
        public static explicit operator iOSNotification(Notification n)
        {
            var ret = n.notification;
            if (ret != null && n.Identifier.HasValue)
                ret.Identifier = n.Identifier.Value.ToString(CultureInfo.InvariantCulture);
            ret.ShowInForeground = n.ShowInForeground;
            return ret;
        }
#endif

#if UNITY_ANDROID
        internal Notification(AndroidNotification notification, int id)
#else
        internal Notification(iOSNotification notification)
#endif
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

        public int? Identifier { get; set; }

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
                    notification = new iOSNotification();
#endif
                notification.Title = value;
            }
        }

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
                    notification = new iOSNotification();
                notification.Body = value;
#endif
            }
        }

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
        public bool ShowInForeground { get; set; }

        public void FireAfter(TimeSpan interval, bool repeats = false)
        {
#if UNITY_ANDROID
            notification.FireTime = DateTime.Now + interval;
            if (repeats)
                notification.RepeatInterval = interval;
#else
            if (notification == null)
                notification = new iOSNotification();
            notification.Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = interval,
                Repeats = repeats,
            };
#endif
        }

        public void FireOn(DateTime fireTime, NotificationRepeatInterval repeat = NotificationRepeatInterval.OneTime)
        {
#if UNITY_ANDROID
            notification.FireTime = fireTime; // TODO handle UTC
            notification.RepeatInterval = repeat switch
            {
                NotificationRepeatInterval.OneTime => new TimeSpan(),
                NotificationRepeatInterval.Hourly => TimeSpan.FromHours(1),
                NotificationRepeatInterval.Daily => TimeSpan.FromDays(1),
            };
#else
            var trigger = new iOSNotificationCalendarTrigger()
            {
                Year = fireTime.Year,
                Month = fireTime.Month,
                Day = fireTime.Day,
                Hour = fireTime.Hour,
                Minute = fireTime.Minute,
                Second = fireTime.Second,
                UtcTime = fireTime.Kind == DateTimeKind.Utc,
            };

            switch (repeat)
            {
                case NotificationRepeatInterval.OneTime:
                    break;
                case NotificationRepeatInterval.Hourly:
                    trigger.Hour = null;
                    trigger.Repeats = true;
                    break;
                case NotificationRepeatInterval.Daily:
                    trigger.Day = null;
                    trigger.Repeats = true;
                    break;
            }

            notification.Trigger = trigger;
#endif
        }
    }
}

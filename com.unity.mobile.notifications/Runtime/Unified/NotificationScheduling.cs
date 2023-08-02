using System;
#if UNITY_ANDROID
using PlatformNotification = Unity.Notifications.Android.AndroidNotification;
#else
using Unity.Notifications.iOS;
using PlatformNotification = Unity.Notifications.iOS.iOSNotification;
#endif

namespace Unity.Notifications
{
    /// <summary>
    /// Interval, at which notification should repeat.
    /// </summary>
    public enum NotificationRepeatInterval
    {
        /// <summary>
        /// Indicates, that notification does not repeat.
        /// </summary>
        OneTime,

        /// <summary>
        /// Indicates, that notification should repeat at one hour intervals.
        /// </summary>
        Hourly,

        /// <summary>
        /// Indicates, that notification should repeat daily.
        /// </summary>
        Daily,
    }

    public interface NotificationSchedule
    {
        internal void Schedule(ref PlatformNotification notification);
    }

    public struct NotificationIntervalSchedule
        : NotificationSchedule
    {
        public TimeSpan Interval { get; set; }
        public bool Repeats { get; set; }

        public NotificationIntervalSchedule(TimeSpan interval, bool repeats = false)
        {
            Interval = interval;
            Repeats = repeats;
        }

        void NotificationSchedule.Schedule(ref PlatformNotification notification)
        {
#if UNITY_ANDROID
            notification.FireTime = DateTime.Now + Interval;
            if (Repeats)
                notification.RepeatInterval = Interval;
#else
            notification.Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = Interval,
                Repeats = Repeats,
            };
#endif
        }
    }

    public struct NotificationDateTimeSchedule
        : NotificationSchedule
    {
        public DateTime FireTime { get; set; }
        public NotificationRepeatInterval RepeatInterval { get; set; }

        public NotificationDateTimeSchedule(DateTime fireTime, NotificationRepeatInterval repeatInterval = NotificationRepeatInterval.OneTime)
        {
            FireTime = fireTime;
            RepeatInterval = repeatInterval;
        }

        void NotificationSchedule.Schedule(ref PlatformNotification notification)
        {
#if UNITY_ANDROID
            notification.FireTime = FireTime; // TODO handle UTC
            notification.RepeatInterval = RepeatInterval switch
            {
                NotificationRepeatInterval.OneTime => new TimeSpan(),
                NotificationRepeatInterval.Hourly => TimeSpan.FromHours(1),
                NotificationRepeatInterval.Daily => TimeSpan.FromDays(1),
                _ => new TimeSpan(),
            };
#else
            var trigger = new iOSNotificationCalendarTrigger()
            {
                Year = FireTime.Year,
                Month = FireTime.Month,
                Day = FireTime.Day,
                Hour = FireTime.Hour,
                Minute = FireTime.Minute,
                Second = FireTime.Second,
                UtcTime = FireTime.Kind == DateTimeKind.Utc,
            };

            switch (RepeatInterval)
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

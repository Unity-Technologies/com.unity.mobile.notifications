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
        OneTime = 0,

        /// <summary>
        /// Indicates, that notification should repeat daily.
        /// When used in <see cref="NotificationDateTimeSchedule"/>, only time is used for scheduling.
        /// </summary>
        Daily = 1,
    }

    /// <summary>
    /// Marker interface for different schedule types.
    /// </summary>
    public interface NotificationSchedule
    {
        internal void Schedule(ref PlatformNotification notification);
    }

    /// <summary>
    /// Schedule notification to show up after a certain amount of time, optionally repeating at the same time interval.
    /// </summary>
    public struct NotificationIntervalSchedule
        : NotificationSchedule
    {
        /// <summary>
        /// Time interval to show notification from current time.
        /// Only full seconds are considered.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Whether notification should repeat.
        /// If true, notification will repeat at the same interval as initial time from the current one.
        /// </summary>
        public bool Repeats { get; set; }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="interval">Value for <see cref="Interval"/></param>
        /// <param name="repeats">Value for <see cref="Repeats"/></param>
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

    /// <summary>
    /// Schedule to show notification at particular date and time.
    /// Optionally can repeat at predefined intervals.
    /// </summary>
    public struct NotificationDateTimeSchedule
        : NotificationSchedule
    {
        /// <summary>
        /// Date and time when notification has to be shown if does not repeat.
        /// If notification is set to repeat, the meaning of this value depends on <see cref="NotificationRepeatInterval"/>.
        /// </summary>
        public DateTime FireTime { get; set; }

        /// <summary>
        /// Interval, at which notification should repeat from the first delivery.
        /// </summary>
        public NotificationRepeatInterval RepeatInterval { get; set; }

        /// <summary>
        /// Convenience constructor.
        /// </summary>
        /// <param name="fireTime">Value for <see cref="FireTime"/></param>
        /// <param name="repeatInterval">Value for <see cref="RepeatInterval"/></param>
        public NotificationDateTimeSchedule(DateTime fireTime, NotificationRepeatInterval repeatInterval = NotificationRepeatInterval.OneTime)
        {
            FireTime = fireTime;
            RepeatInterval = repeatInterval;
        }

        void NotificationSchedule.Schedule(ref PlatformNotification notification)
        {
#if UNITY_ANDROID
            // TODO handle UTC
            switch (RepeatInterval)
            {
                case NotificationRepeatInterval.OneTime:
                    notification.FireTime = FireTime;
                    break;
                case NotificationRepeatInterval.Daily:
                    {
                        var currentTime = DateTime.Now;
                        var fireTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, FireTime.Hour, FireTime.Minute, FireTime.Second);
                        if (fireTime < currentTime)
                            fireTime = fireTime.AddDays(1);
                        notification.FireTime = fireTime;
                        notification.RepeatInterval = TimeSpan.FromDays(1);
                        break;
                    }
            }
#else
            var trigger = new iOSNotificationCalendarTrigger()
            {
                Hour = FireTime.Hour,
                Minute = FireTime.Minute,
                Second = FireTime.Second,
                UtcTime = FireTime.Kind == DateTimeKind.Utc,
            };

            switch (RepeatInterval)
            {
                case NotificationRepeatInterval.OneTime:
                    trigger.Year = FireTime.Year;
                    trigger.Month = FireTime.Month;
                    trigger.Day = FireTime.Day;
                    break;
                case NotificationRepeatInterval.Daily:
                    trigger.Day = null;
                    trigger.Repeats = true;
                    break;
                default:
                    throw new Exception($"Unsupported repeat interval {RepeatInterval}");
            }

            notification.Trigger = trigger;
#endif
        }
    }
}

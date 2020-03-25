using System;
using UnityEngine;

namespace Unity.Notifications.iOS
{
    internal enum NotificationTriggerType
    {
        TimeTrigger = 0,
        CalendarTrigger = 10,
        LocationTrigger = 20,
        PushTrigger = 3
    }

    /// <summary>
    /// iOSNotificationTrigger interface is implemented by notification trigger types representing an event that triggers the delivery of a notification.
    /// </summary>
    public interface iOSNotificationTrigger {}

    /// <summary>
    /// A trigger condition that causes a notification to be delivered when the user's device enters or exits the specified geographic region.
    /// </summary>
    /// <remarks>
    /// Create a UNLocationNotificationTrigger instance when you want to schedule the delivery of a local notification when the device enters or leaves a specific geographic region.
    /// The system limits the number of location-based triggers that may be scheduled at the same time. Before scheduling any notifications using this trigger,
    /// your app must have authorization to use Core Location and must have when-in-use permissions. Use the Unity LocationService API to request for this authorization.
    /// Region-based notifications aren't always triggered immediately when the edge of the boundary is crossed. The system applies heuristics to ensure that the boundary crossing
    /// represents a deliberate event and is not the result of spurious location data.
    /// See https://developer.apple.com/documentation/corelocation/clregion?language=objc for additional information.
    ///</remarks>
    public struct iOSNotificationLocationTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.LocationTrigger; } }

        /// <summary>iOSNotificationLocationTrigger
        /// The center point of the geographic area.
        /// </summary>
        public Vector2 Center { get; set; }

        /// <summary>
        /// The radius (measured in meters) that defines the geographic area’s outer boundary.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// When this property is enabled, a device crossing from outside the region to inside the region triggers the delivery of a notification
        /// </summary>
        public bool NotifyOnEntry { get; set; }

        /// <summary>
        /// When this property is enabled, a device crossing from inside the region to outside the region triggers the delivery of a notification
        /// </summary>
        public bool NotifyOnExit { get; set; }
    }

    /// <summary>
    /// A trigger condition that indicates the notification was sent from Apple Push Notification Service (APNs).
    /// </summary>
    /// <remarks>
    /// You should not create instances of this type manually. Instead compare the Trigger property of notification objects received in `OnNotificationReceived` to this type to
    /// determine whether the received notification was scheduled locally or remotely.
    /// </remarks>
    /// <example>
    /// notification.Trigger is iOSNotificationPushTrigger
    /// </example>

    public struct iOSNotificationPushTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.PushTrigger; } }
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered after the specified amount of time elapses.
    /// </summary>
    /// <remarks>
    /// Create a iOSNotificationTimeIntervalTrigger instance when you want to schedule the delivery of a local notification after the specified time span has elapsed.
    /// </remarks>
    public struct iOSNotificationTimeIntervalTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.TimeTrigger; } }

        internal int timeInterval;

        public TimeSpan TimeInterval
        {
            get { return TimeSpan.FromMilliseconds(timeInterval); }
            set
            {
                timeInterval = (int)value.TotalSeconds;
                if (timeInterval <= 0)
                    throw new ArgumentException("Time interval must be greater than 0.");
            }
        }
        public bool Repeats { get; set; }
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered at a specific date and time.
    /// </summary>
    /// <remarks>
    /// Create an instance of <see cref="iOSNotificationCalendarTrigger"/>  when you want to schedule the delivery of a local notification at the specified date and time.
    /// You are not required to set all of the fields because the system uses the provided information to determine the next date and time that matches the specified information automatically.
    /// </remarks>
    public struct iOSNotificationCalendarTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.CalendarTrigger; } }

        /// <summary>
        /// Year
        /// </summary>
        public int? Year { get; set; }
        /// <summary>
        /// Month
        /// </summary>
        public int? Month { get; set; }
        /// <summary>
        /// Day
        /// </summary>
        public int? Day { get; set; }
        /// <summary>
        /// Hour
        /// </summary>
        public int? Hour { get; set; }
        /// <summary>
        /// Minute
        /// </summary>
        public int? Minute { get; set; }
        /// <summary>
        /// Second
        /// </summary>
        public int? Second { get; set; }
        /// <summary>
        /// Indicate whether the notification is repeated every defined time period. For instance if hour and minute fields are set the notification will be triggered every day at the specified hour and minute.
        /// </summary>
        public bool Repeats { get; set; }
    }
}

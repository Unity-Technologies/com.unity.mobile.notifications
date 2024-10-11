using System;
using UnityEngine;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Describes notification trigger type
    /// </summary>
    public enum iOSNotificationTriggerType
    {
        /// <summary>
        /// Time interval trigger
        /// </summary>
        TimeInterval = 0,
        /// <summary>
        /// Calendar trigger
        /// </summary>
        Calendar = 10,
        /// <summary>
        /// Location trigger
        /// </summary>
        Location = 20,
        /// <summary>
        /// Push notification trigger
        /// </summary>
        Push = 3,
        /// <summary>
        /// Trigger, that is not known to this version of notifications package
        /// </summary>
        Unknown = -1,
    }

    /// <summary>
    /// iOSNotificationTrigger interface is implemented by notification trigger types representing an event that triggers the delivery of a notification.
    /// </summary>
    public interface iOSNotificationTrigger
    {
        /// <summary>
        /// The type of notification trigger. For internal use.
        /// </summary>
        /// <value>The type of the trigger</value>
        iOSNotificationTriggerType Type { get; }
    }

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
        /// <inheritdoc/>
        public iOSNotificationTriggerType Type { get { return iOSNotificationTriggerType.Location; } }

        /// <summary>
        /// The center point of the geographic area.
        /// </summary>
        [Obsolete("Use Latitude and Longitude", false)]
        public Vector2 Center
        {
            get
            {
                return new Vector2((float)Latitude, (float)Longitude);
            }
            set
            {
                Latitude = value.x;
                Longitude = value.y;
            }
        }

        /// <summary>
        /// The latitude of the center point of the geographic area.
        /// </summary>
        /// <value>Latitude in degrees</value>
        public double Latitude { get; set; }

        /// <summary>
        /// The longitude of the center point of the geographic area.
        /// </summary>
        /// <value>Longitude in degrees</value>
        public double Longitude { get; set; }

        /// <summary>
        /// The radius (measured in meters) that defines the geographic area’s outer boundary.
        /// </summary>
        /// <value>Radius in meters</value>
        public float Radius { get; set; }

        /// <summary>
        /// When this property is enabled, a device crossing from outside the region to inside the region triggers the delivery of a notification
        /// </summary>
        /// <value>If true, a notification triggers when entering the region.</value>
        public bool NotifyOnEntry { get; set; }

        /// <summary>
        /// When this property is enabled, a device crossing from inside the region to outside the region triggers the delivery of a notification
        /// </summary>
        /// <value>If true, a notification triggers when leaving the region.</value>
        public bool NotifyOnExit { get; set; }

        /// <summary>
        /// Whether the notification should repeat.
        /// </summary>
        /// <value>If true, the notification repeats.</value>
        public bool Repeats { get; set; }
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
        /// <inheritdoc/>
        public iOSNotificationTriggerType Type { get { return iOSNotificationTriggerType.Push; } }
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered after the specified amount of time elapses.
    /// </summary>
    /// <remarks>
    /// Create a iOSNotificationTimeIntervalTrigger instance when you want to schedule the delivery of a local notification after the specified time span has elapsed.
    /// </remarks>
    public struct iOSNotificationTimeIntervalTrigger : iOSNotificationTrigger
    {
        /// <inheritdoc/>
        public iOSNotificationTriggerType Type { get { return iOSNotificationTriggerType.TimeInterval; } }

        internal int timeInterval;

        /// <summary>
        /// Time interval after which the notification should be delivered (only total of full seconds is considered).
        /// </summary>
        /// <value>Time interval until delivery</value>
        public TimeSpan TimeInterval
        {
            get { return TimeSpan.FromSeconds(timeInterval); }
            set
            {
                timeInterval = (int)value.TotalSeconds;
                if (timeInterval <= 0)
                    throw new ArgumentException("Time interval must be greater than 0.");
            }
        }

        /// <summary>
        /// Whether the notification should repeat.
        /// </summary>
        /// <value>If true, the notification repeats with the same interval.</value>
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
        /// <inheritdoc/>
        public iOSNotificationTriggerType Type { get { return iOSNotificationTriggerType.Calendar; } }

        /// <summary>
        /// Specify the year to schedule the notification for or set to null to indicate any year.
        /// </summary>
        /// <value>Number indicating year or null to ignore year</value>
        public int? Year { get; set; }

        /// <summary>
        /// Specify the month to schedule the notification for or set to null to indicate any month.
        /// </summary>
        /// <value>Number indicating month or null to ignore month</value>
        public int? Month { get; set; }

        /// <summary>
        /// Specify the day to schedule the notification for or set to null to indicate any day.
        /// </summary>
        /// <value>Number indicating day or null to ignore day</value>
        public int? Day { get; set; }

        /// <summary>
        /// Specify the hour to schedule the notification for or set to null to indicate any hour.
        /// </summary>
        /// <value>Number indicating hour or null to ignore hour</value>
        public int? Hour { get; set; }

        /// <summary>
        /// Specify the minute to schedule the notification for or set to null to indicate any minute.
        /// </summary>
        /// <value>Number indicating minute or null to ignore minute</value>
        public int? Minute { get; set; }

        /// <summary>
        /// Specify the second to schedule the notification for or set to null to indicate any second.
        /// </summary>
        /// <value>Number indicating second or null to ignore second</value>
        public int? Second { get; set; }

        /// <summary>
        /// Are Date and Time field in UTC time. When false, use local time.
        /// </summary>
        /// <value>If true, use UTC time.</value>
        public bool UtcTime { get; set; }

        /// <summary>
        /// Indicate whether the notification is repeated every defined time period. For instance if hour and minute fields are set the notification will be triggered every day at the specified hour and minute.
        /// </summary>
        /// <value>If true, the notification repeats for each matching date and time. If false, the notification triggers only on the first match.</value>
        public bool Repeats { get; set; }

        /// <summary>
        /// Converts this trigger into the one using UTC time.
        /// </summary>
        /// <returns>A new trigger with UtcTime set to true and other field adjusted accordingly.</returns>
        public iOSNotificationCalendarTrigger ToUtc()
        {
            if (UtcTime)
                return this;

            var notificationTime = AssignDateTimeComponents(DateTime.Now).ToUniversalTime();
            iOSNotificationCalendarTrigger result = this;
            result.UtcTime = true;
            result.AssignNonEmptyComponents(notificationTime);
            return result;
        }

        /// <summary>
        /// Converts this trigger into the one using local time.
        /// </summary>
        /// <returns>A new trigger with UtcTime set to false and other field adjusted accordingly.</returns>
        public iOSNotificationCalendarTrigger ToLocal()
        {
            if (!UtcTime)
                return this;

            var notificationTime = AssignDateTimeComponents(DateTime.UtcNow).ToLocalTime();
            iOSNotificationCalendarTrigger result = this;
            result.UtcTime = false;
            result.AssignNonEmptyComponents(notificationTime);
            return result;
        }

        internal DateTime AssignDateTimeComponents(DateTime dt)
        {
            int year = Year != null ? Year.Value : dt.Year;
            int month = Month != null ? Month.Value : dt.Month;
            int day = Day != null ? Day.Value : dt.Day;
            int hour = Hour != null ? Hour.Value : dt.Hour;
            int minute = Minute != null ? Minute.Value : dt.Minute;
            int second = Second != null ? Second.Value : dt.Second;
            return new DateTime(year, month, day, hour, minute, second, dt.Kind);
        }

        internal void AssignNonEmptyComponents(DateTime dt)
        {
            if (Year != null)
                Year = dt.Year;
            if (Month != null)
                Month = dt.Month;
            if (Day != null)
                Day = dt.Day;
            if (Hour != null)
                Hour = dt.Hour;
            if (Minute != null)
                Minute = dt.Minute;
            if (Second != null)
                Second = dt.Second;
        }
    }
}

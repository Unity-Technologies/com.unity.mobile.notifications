using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Constants indicating how to present a notification in a foreground app
    /// </summary>
    [Flags]
    public enum PresentationOption
    {
        /// <summary>
        /// No options are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Apply the notification's badge value to the app’s icon.
        /// </summary>
        Badge = 1 << 0,

        /// <summary>
        /// Play the sound associated with the notification.
        /// </summary>
        Sound = 1 << 1,

        /// <summary>
        /// Display the alert using the content provided by the notification.
        /// </summary>
        Alert = 1 << 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iOSNotificationData
    {
        public string identifier;
        public string title;
        public string body;
        public Int32 badge;
        public string subtitle;
        public string categoryIdentifier;
        public string threadIdentifier;

        public IntPtr userInfo;

        // Trigger
        public Int32 triggerType;
        public bool repeats;

        //Time trigger
        public Int32 timeTriggerInterval;

        //Calendar trigger
        public Int32 calendarTriggerYear;
        public Int32 calendarTriggerMonth;
        public Int32 calendarTriggerDay;
        public Int32 calendarTriggerHour;
        public Int32 calendarTriggerMinute;
        public Int32 calendarTriggerSecond;

        //Location trigger
        public float locationTriggerCenterX;
        public float locationTriggerCenterY;
        public float locationTriggerRadius;
        public bool locationTriggerNotifyOnEntry;
        public bool locationTriggerNotifyOnExit;
    }

    /// <summary>
    /// The iOSNotification class is used schedule local notifications. It includes the content of the notification and the trigger conditions for delivery.
    /// An instance of this class is also returned when receiving remote notifications..
    /// </summary>
    /// <remarks>
    /// Create an instance of this class when you want to schedule the delivery of a local notification. It contains the entire notification  payload to be delivered
    /// (which corresponds to UNNotificationContent) and  also the NotificationTrigger object with the conditions that trigger the delivery of the notification.
    /// To schedule the delivery of your notification, pass an instance of this class to the <see cref="iOSNotificationCenter.ScheduleNotification"/>  method.
    /// </remarks>
    public class iOSNotification
    {
        /// <summary>
        /// The unique identifier for this notification request.
        /// </summary>
        /// <remarks>
        /// If not explicitly specified the identifier  will be automatically generated when creating the notification.
        /// </remarks>
        public string Identifier
        {
            get { return data.identifier; }
            set { data.identifier = value; }
        }

        /// <summary>
        /// The identifier of the app-defined category object.
        /// </summary>
        public string CategoryIdentifier
        {
            get { return data.categoryIdentifier; }
            set { data.categoryIdentifier = value; }
        }

        /// <summary>
        /// An identifier that used to group related notifications together.
        /// </summary>
        /// <remarks>
        /// Automatic notification grouping according to the thread identifier is only supported on iOS 12 and above.
        /// </remarks>
        public string ThreadIdentifier
        {
            get { return data.threadIdentifier; }
            set { data.threadIdentifier = value; }
        }

        /// <summary>
        /// A short description of the reason for the notification.
        /// </summary>
        public string Title
        {
            get { return data.title; }
            set { data.title = value; }
        }

        /// <summary>
        /// A secondary description of the reason for the notification.
        /// </summary>
        public string Subtitle
        {
            get { return data.subtitle; }
            set { data.subtitle = value; }
        }

        /// <summary>
        /// The message displayed in the notification alert.
        /// </summary>
        public string Body
        {
            get { return data.body; }
            set { data.body = value; }
        }

        /// <summary>
        /// Indicates whether the notification alert should be shown when the app is open.
        /// </summary>
        /// <remarks>
        /// Subscribe to the <see cref="iOSNotificationCenter.OnNotificationReceived"/> event to receive a callback when the notification is triggered.
        /// </remarks>
        public bool ShowInForeground
        {
            get
            {
                string value;
                if (userInfo.TryGetValue("showInForeground", out value))
                    return value == "YES";
                return false;
            }
            set { userInfo["showInForeground"] = value ? "YES" : "NO"; }
        }


        /// <summary>
        /// Presentation options for displaying the local of notification when the app is running. Only works if  <see cref="iOSNotification.ShowInForeground"/> is enabled and user has allowed enabled the requested options for your app.
        /// </summary>
        public PresentationOption ForegroundPresentationOption
        {
            get
            {
                try
                {
                    string value;
                    if (userInfo.TryGetValue("showInForegroundPresentationOptions", out value))
                        return (PresentationOption)Int32.Parse(value);
                    return default;
                }
                catch (Exception)
                {
                    return default;
                }
            }
            set { userInfo["showInForegroundPresentationOptions"] = ((int)value).ToString(); }
        }


        /// <summary>
        /// The number to display as a badge on the app’s icon.
        /// </summary>
        public int Badge
        {
            get { return data.badge; }
            set { data.badge = value; }
        }

        /// <summary>
        /// Arbitrary string data which can be retrieved when the notification is used to open the app or is received while the app is running.
        /// </summary>
        public string Data
        {
            get
            {
                string value;
                userInfo.TryGetValue("data", out value);
                return value;
            }
            set { userInfo["data"] = value; }
        }

        /// <summary>
        /// Key-value collection sent or received with the notification.
        /// Note, that some of the other notification properties are transfered using this collection, it is not recommended to modify existing items.
        /// </summary>
        public Dictionary<string, string> UserInfo
        {
            get { return userInfo; }
        }

        /// <summary>
        /// The conditions that trigger the delivery of the notification.
        /// For notification that were already delivered and whose instance was returned by <see cref="iOSNotificationCenter.OnRemoteNotificationReceived"/> or <see cref="iOSNotificationCenter.OnRemoteNotificationReceived"/>
        /// use this property to determine what caused the delivery to occur. You can do this by comparing <see cref="iOSNotification.Trigger"/>  to any of the notification trigger types that implement it, such as
        /// <see cref="iOSNotificationLocationTrigger"/>, <see cref="iOSNotificationPushTrigger"/>, <see cref="iOSNotificationTimeIntervalTrigger"/>, <see cref="iOSNotificationCalendarTrigger"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// notification.Trigger is iOSNotificationPushTrigger
        /// </code>
        /// </example>
        public iOSNotificationTrigger Trigger
        {
            set
            {
                if (value is iOSNotificationTimeIntervalTrigger)
                {
                    var trigger = (iOSNotificationTimeIntervalTrigger)value;
                    data.triggerType = iOSNotificationTimeIntervalTrigger.Type;
                    data.timeTriggerInterval = trigger.timeInterval;

                    if (trigger.Repeats && trigger.timeInterval < 60)
                        throw new ArgumentException("Time interval must be 60 seconds or greater for repeating notifications.");

                    data.repeats = trigger.Repeats;
                }
                else if (value is iOSNotificationCalendarTrigger)
                {
                    var trigger = (iOSNotificationCalendarTrigger)value;
                    data.triggerType = iOSNotificationCalendarTrigger.Type;
                    data.calendarTriggerYear = trigger.Year != null ? trigger.Year.Value : -1;
                    data.calendarTriggerMonth = trigger.Month != null ? trigger.Month.Value : -1;
                    data.calendarTriggerDay = trigger.Day != null ? trigger.Day.Value : -1;
                    data.calendarTriggerHour = trigger.Hour != null ? trigger.Hour.Value : -1;
                    data.calendarTriggerMinute = trigger.Minute != null ? trigger.Minute.Value : -1;
                    data.calendarTriggerSecond = trigger.Second != null ? trigger.Second.Value : -1;
                    data.repeats = trigger.Repeats;
                }
                else if (value is iOSNotificationLocationTrigger)
                {
                    var trigger = (iOSNotificationLocationTrigger)value;
                    data.triggerType = iOSNotificationLocationTrigger.Type;
                    data.locationTriggerCenterX = trigger.Center.x;
                    data.locationTriggerCenterY = trigger.Center.y;
                    data.locationTriggerNotifyOnEntry = trigger.NotifyOnEntry;
                    data.locationTriggerNotifyOnExit = trigger.NotifyOnExit;
                    data.locationTriggerRadius = trigger.Radius;
                }
                else if (value is iOSNotificationPushTrigger)
                {
                    data.triggerType = 3;
                }
            }

            get
            {
                iOSNotificationTrigger trigger;
                if (data.triggerType == iOSNotificationTimeIntervalTrigger.Type)
                {
                    trigger = new iOSNotificationTimeIntervalTrigger()
                    {
                        timeInterval = data.timeTriggerInterval,
                        Repeats = data.repeats
                    };
                }
                else if (data.triggerType == iOSNotificationCalendarTrigger.Type)
                {
                    trigger = new iOSNotificationCalendarTrigger()
                    {
                        Year = (data.calendarTriggerYear > 0) ? (int?)data.calendarTriggerYear : null,
                        Month = (data.calendarTriggerMonth > 0) ? (int?)data.calendarTriggerMonth : null,
                        Day = (data.calendarTriggerDay > 0) ? (int?)data.calendarTriggerDay : null,
                        Hour = (data.calendarTriggerHour >= 0) ? (int?)data.calendarTriggerHour : null,
                        Minute = (data.calendarTriggerMinute >= 0) ? (int?)data.calendarTriggerMinute : null,
                        Second = (data.calendarTriggerSecond >= 0) ? (int?)data.calendarTriggerSecond : null,
                        Repeats = data.repeats
                    };
                }
                else if (data.triggerType == iOSNotificationLocationTrigger.Type)
                {
                    trigger = new iOSNotificationLocationTrigger()
                    {
                        Center = new Vector2(data.locationTriggerCenterX, data.locationTriggerCenterY),
                        Radius = data.locationTriggerRadius,
                        NotifyOnEntry = data.locationTriggerNotifyOnEntry,
                        NotifyOnExit = data.locationTriggerNotifyOnExit
                    };
                }
                else
                {
                    trigger = new iOSNotificationPushTrigger();
                }
                return trigger;
            }
        }

        private static string GenerateUniqueID()
        {
            return Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()).ToString();
        }

        /// <summary>
        /// Create a new instance of <see cref="iOSNotification"/> and automatically generate an unique string for <see cref="iOSNotification.Identifier"/>  with all optional fields set to default values.
        /// </summary>
        public iOSNotification() : this(GenerateUniqueID())
        {
        }

        /// <summary>
        /// Specify a <see cref="iOSNotification.Identifier"/> and create a notification object with all optional fields set to default values.
        /// </summary>
        /// <param name="identifier">  Unique identifier for the local notification tha can later be used to track or change it's status.</param>
        public iOSNotification(string identifier)
        {
            data = new iOSNotificationData();
            data.identifier = identifier;
            data.title = "";
            data.body = "";
            data.badge = -1;
            data.subtitle = "";
            data.categoryIdentifier = "";
            data.threadIdentifier = "";

            data.triggerType = -1;
            data.repeats = false;

            //Time trigger
            data.timeTriggerInterval = -1;

            //Calendar trigger
            data.calendarTriggerYear = -1;
            data.calendarTriggerMonth = -1;
            data.calendarTriggerDay = -1;
            data.calendarTriggerHour = -1;
            data.calendarTriggerMinute = -1;
            data.calendarTriggerSecond = -1;

            //Location trigger
            data.locationTriggerCenterX = 0f;
            data.locationTriggerCenterY = 0f;
            data.locationTriggerRadius = 2f;
            data.locationTriggerNotifyOnEntry = true;
            data.locationTriggerNotifyOnExit = false;

            data.userInfo = IntPtr.Zero;
            userInfo = new Dictionary<string, string>();
            Data = "";
            ShowInForeground = false;
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound;
        }

        internal iOSNotification(iOSNotificationWithUserInfo data)
        {
            this.data = data.data;
            userInfo = data.userInfo;
        }

        iOSNotificationData data;
        Dictionary<string, string> userInfo;

        internal iOSNotificationWithUserInfo GetDataForSending()
        {
            if (data.identifier == null)
                data.identifier = GenerateUniqueID();
            iOSNotificationWithUserInfo ret;
            ret.data = data;
            ret.userInfo = userInfo;
            return ret;
        }
    }
}

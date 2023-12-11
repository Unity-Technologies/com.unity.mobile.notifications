using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Constants indicating how to present a notification in a foreground app
    /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationpresentationoptions?language=objc"/>
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

        /// <summary>
        /// Show the notification in Notification Center.
        /// </summary>
        List = 1 << 3,

        /// <summary>
        /// Present the notification as a banner.
        /// </summary>
        Banner = 1 << 4,
    }

    /// <summary>
    /// The type of sound to use for the notification.
    /// See Apple documentation for details.
    /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationsound?language=objc"/>
    /// </summary>
    public enum NotificationSoundType
    {
        /// <summary>
        /// Play the default sound.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Critical sound (bypass Do Not Disturb)
        /// </summary>
        Critical = 1,

        /// <summary>
        /// Ringtone sound.
        /// </summary>
        Ringtone = 2,

        /// <summary>
        /// No sound.
        /// </summary>
        None = 4,
    }

    /// <summary>
    /// Importance and delivery timing of a notification.
    /// See Apple documentation for details. Available since iOS 15, always Active on lower versions.
    /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationinterruptionlevel?language=objc"/>
    /// </summary>
    public enum NotificationInterruptionLevel
    {
        /// <summary>
        /// Default level. The system presents the notification immediately, lights up the screen, and can play a sound.
        /// </summary>
        Active = 0,

        /// <summary>
        /// The system presents the notification immediately, lights up the screen, and bypasses the mute switch to play a sound.
        /// </summary>
        Critical = 1,

        /// <summary>
        /// The system adds the notification to the notification list without lighting up the screen or playing a sound.
        /// </summary>
        Passive = 2,

        /// <summary>
        /// The system presents the notification immediately, lights up the screen, and can play a sound, but won’t break through system notification controls.
        /// </summary>
        TimeSensitive = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TimeTriggerData
    {
        public Int32 interval;
        public Byte repeats;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CalendarTriggerData
    {
        public Int32 year;
        public Int32 month;
        public Int32 day;
        public Int32 hour;
        public Int32 minute;
        public Int32 second;
        public Byte repeats;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LocationTriggerData
    {
        public double latitude;
        public double longitude;
        public float radius;
        public Byte notifyOnEntry;
        public Byte notifyOnExit;
        public Byte repeats;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct TriggerData
    {
        [FieldOffset(0)]
        public TimeTriggerData timeInterval;
        [FieldOffset(0)]
        public CalendarTriggerData calendar;
        [FieldOffset(0)]
        public LocationTriggerData location;
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
        public Int32 soundType;
        public float soundVolume;
        public string soundName;
        public Int32 interruptionLevel;
        public double relevanceScore;

        public IntPtr userInfo;
        public IntPtr attachments;

        // Trigger
        public Int32 triggerType;
        public TriggerData trigger;
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
        /// See Apple's documentation for details.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcontent/1649863-body"/>
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
        /// The type of sound to be played.
        /// </summary>
        public NotificationSoundType SoundType
        {
            get { return (NotificationSoundType)data.soundType; }
            set { data.soundType = (int)value; }
        }

        /// <summary>
        /// The name of the sound to be played. Use null for system default sound.
        /// See Apple documentation for named sounds and sound file placement.
        /// </summary>
        public string SoundName
        {
            get { return data.soundName; }
            set { data.soundName = value; }
        }

        /// <summary>
        /// The volume for the sound. Use null to use the default volume.
        /// See Apple documentation for supported values.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationsound/2963118-defaultcriticalsoundwithaudiovol?language=objc"/>
        /// </summary>
        public float? SoundVolume { get; set; }

        /// <summary>
        /// The notification’s importance and required delivery timing.
        /// </summary>
        public NotificationInterruptionLevel InterruptionLevel
        {
            get { return (NotificationInterruptionLevel)data.interruptionLevel; }
            set { data.interruptionLevel = (int)value; }
        }

        /// <summary>
        /// The score the system uses to determine if the notification is the summary’s featured notification.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcontent/3821031-relevancescore?language=objc"/>
        /// </summary>
        public double RelevanceScore
        {
            get { return data.relevanceScore; }
            set { data.relevanceScore = value; }
        }

        /// <summary>
        /// Arbitrary string data which can be retrieved when the notification is used to open the app or is received while the app is running.
        /// Push notification is sent to the device as <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/generating_a_remote_notification?language=objc">JSON</see>.
        /// The value for data key is set to the Data property on notification.
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
        /// A list of notification attachments.
        /// Notification attachments can be images, audio or video files. Refer to Apple documentation on supported formats.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unmutablenotificationcontent/1649857-attachments?language=objc"/>
        /// </summary>
        public List<iOSNotificationAttachment> Attachments { get; set; }

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
                switch (value.Type)
                {
                    case iOSNotificationTriggerType.TimeInterval:
                        {
                            var trigger = (iOSNotificationTimeIntervalTrigger)value;
                            data.trigger.timeInterval.interval = trigger.timeInterval;

                            if (trigger.Repeats && trigger.timeInterval < 60)
                                throw new ArgumentException("Time interval must be 60 seconds or greater for repeating notifications.");

                            data.trigger.timeInterval.repeats = (byte)(trigger.Repeats ? 1 : 0);
                            break;
                        }
                    case iOSNotificationTriggerType.Calendar:
                        {
                            var trigger = ((iOSNotificationCalendarTrigger)value);
                            if (userInfo == null)
                                userInfo = new Dictionary<string, string>();
                            userInfo["OriginalUtc"] = trigger.UtcTime ? "1" : "0";
                            data.trigger.calendar.year = trigger.Year != null ? trigger.Year.Value : -1;
                            data.trigger.calendar.month = trigger.Month != null ? trigger.Month.Value : -1;
                            data.trigger.calendar.day = trigger.Day != null ? trigger.Day.Value : -1;
                            data.trigger.calendar.hour = trigger.Hour != null ? trigger.Hour.Value : -1;
                            data.trigger.calendar.minute = trigger.Minute != null ? trigger.Minute.Value : -1;
                            data.trigger.calendar.second = trigger.Second != null ? trigger.Second.Value : -1;
                            data.trigger.calendar.repeats = (byte)(trigger.Repeats ? 1 : 0);
                            break;
                        }
                    case iOSNotificationTriggerType.Location:
                        {
                            var trigger = (iOSNotificationLocationTrigger)value;
                            data.trigger.location.latitude = trigger.Latitude;
                            data.trigger.location.longitude = trigger.Longitude;
                            data.trigger.location.notifyOnEntry = (byte)(trigger.NotifyOnEntry ? 1 : 0);
                            data.trigger.location.notifyOnExit = (byte)(trigger.NotifyOnExit ? 1 : 0);
                            data.trigger.location.radius = trigger.Radius;
                            data.trigger.location.repeats = (byte)(trigger.Repeats ? 1 : 0);
                            break;
                        }
                    case iOSNotificationTriggerType.Push:
                        break;
                    default:
                        throw new Exception($"Unknown trigger type {value.Type}");
                }

                data.triggerType = (int)value.Type;
            }

            get
            {
                switch ((iOSNotificationTriggerType)data.triggerType)
                {
                    case iOSNotificationTriggerType.TimeInterval:
                        return new iOSNotificationTimeIntervalTrigger()
                        {
                            timeInterval = data.trigger.timeInterval.interval,
                            Repeats = data.trigger.timeInterval.repeats != 0,
                        };
                    case iOSNotificationTriggerType.Calendar:
                        {
                            var trigger = new iOSNotificationCalendarTrigger()
                            {
                                Year = (data.trigger.calendar.year > 0) ? (int?)data.trigger.calendar.year : null,
                                Month = (data.trigger.calendar.month > 0) ? (int?)data.trigger.calendar.month : null,
                                Day = (data.trigger.calendar.day > 0) ? (int?)data.trigger.calendar.day : null,
                                Hour = (data.trigger.calendar.hour >= 0) ? (int?)data.trigger.calendar.hour : null,
                                Minute = (data.trigger.calendar.minute >= 0) ? (int?)data.trigger.calendar.minute : null,
                                Second = (data.trigger.calendar.second >= 0) ? (int?)data.trigger.calendar.second : null,
                                UtcTime = false,
                                Repeats = data.trigger.calendar.repeats != 0
                            };
                            if (userInfo != null)
                            {
                                string utc;
                                if (userInfo.TryGetValue("OriginalUtc", out utc))
                                {
                                    if (utc == "1")
                                        trigger.UtcTime = true;
                                }
                            }
                            return trigger;
                        }
                    case iOSNotificationTriggerType.Location:
                        return new iOSNotificationLocationTrigger()
                        {
                            Latitude = data.trigger.location.latitude,
                            Longitude = data.trigger.location.longitude,
                            Radius = data.trigger.location.radius,
                            NotifyOnEntry = data.trigger.location.notifyOnEntry != 0,
                            NotifyOnExit = data.trigger.location.notifyOnExit != 0,
                            Repeats = data.trigger.location.repeats != 0,
                        };
                    case iOSNotificationTriggerType.Push:
                        return new iOSNotificationPushTrigger();
                    default:
                        throw new Exception($"Unknown trigger type {data.triggerType}");
                }
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

            data.userInfo = IntPtr.Zero;
            userInfo = new Dictionary<string, string>();
            Data = "";
            ShowInForeground = false;
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound;
            InterruptionLevel = NotificationInterruptionLevel.Active;
            RelevanceScore = 0;
        }

        internal iOSNotification(iOSNotificationWithUserInfo data)
        {
            this.data = data.data;
            userInfo = data.userInfo;
            Attachments = data.attachments;
        }

        iOSNotificationData data;
        Dictionary<string, string> userInfo;

        internal iOSNotificationWithUserInfo GetDataForSending()
        {
            if (data.identifier == null)
                data.identifier = GenerateUniqueID();
            if (SoundVolume.HasValue)
                data.soundVolume = SoundVolume.Value;
            else
                data.soundVolume = -1.0f;

            iOSNotificationWithUserInfo ret;
            ret.data = data;
            ret.userInfo = userInfo;
            ret.attachments = Attachments;
            return ret;
        }
    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using UnityEngine;

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
        AuthorizationStatusNotDetermined = 0,
        /// <summary>
        /// The application is not authorized to post notifications.
        /// </summary>
        AuthorizationStatusDenied,
        /// <summary>
        /// The application is authorized to post notifications.
        /// </summary>
        AuthorizationStatusAuthorized
    }
    

    /// <summary>
    /// Enum indicating the current status of a notification setting. 
    /// </summary>
    public enum NotificationSetting
    {
        /// <summary>
        /// The app does not support this notification setting.
        /// </summary>
        NotificationSettingNotSupported  = 0,
        
        /// <summary>
        /// The notification setting is turned off.
        /// </summary>
        NotificationSettingDisabled,
        
        /// <summary>
        /// The notification setting is turned on.
        /// </summary>
        NotificationSettingEnabled,
    }

    /// <summary>
    /// Enum for requesting authorization to interact with the user.
    /// </summary>
    [Flags]
    public enum AuthorizationOption
    {
        /// <summary>
        /// The ability to update the app’s badge.
        /// </summary>
        AuthorizationOptionBadge   = (1 << 0),
        
        /// <summary>
        /// The ability to play sounds.
        /// </summary>
        AuthorizationOptionSound   = (1 << 1),
        
        /// <summary>
        /// The ability to display alerts.
        /// </summary>
        AuthorizationOptionAlert   = (1 << 2),
        
        /// <summary>
        /// The ability to display notifications in a CarPlay environment.
        /// </summary>
        AuthorizationOptionCarPlay = (1 << 3),
    }
    /// <summary>
    /// Constants indicating how to present a notification in a foreground app
    /// </summary>
    [Flags]
    public enum PresentationOption
    {
        NotificationPresentationOptionNone  = 0,
        
        /// <summary>
        /// Apply the notification's badge value to the app’s icon.
        /// </summary>
        NotificationPresentationOptionBadge = 1 << 0,
        
        /// <summary>
        /// Play the sound associated with the notification.
        /// </summary>
        NotificationPresentationOptionSound = 1 << 1,
        
        /// <summary>
        /// Display the alert using the content provided by the notification.
        /// </summary>
        NotificationPresentationOptionAlert = 1 << 2,
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct iOSNotificationData
    {
        public string identifier;
        public string title;
        public string body;
        public int badge;
        public string subtitle;
        public string categoryIdentifier;
        public string threadIdentifier;

        //Custom information
        public bool showInForeground;
        public int showInForegroundPresentationOptions;
        
        // Trigger
        public int triggerType; //0 - time, 1 - calendar, 2 - location, 3 - push.
        public bool repeats;
        
        //Time trigger
        public int timeTriggerInterval;

        //Location trigger
        public float locationTriggerCenterX;
        public float locationTriggerCenterY;
        public float locationTriggerRadius;
        public bool locationTriggerNotifyOnEntry;
        public bool locationTriggerNotifyOnExit;
        
        //Calendar trigger
        public int calendarTriggerYear;
        public int calendarTriggerMonth;
        public int calendarTriggerDay;
        public int calendarTriggerHour;
        public int calendarTriggerMinute;
        public int calendarTriggerSecond;

        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(identifier) &&
                !string.IsNullOrEmpty(title) &&
                !string.IsNullOrEmpty(body) &&
                !string.IsNullOrEmpty(subtitle) &&
                !string.IsNullOrEmpty(threadIdentifier) &&
                !string.IsNullOrEmpty(categoryIdentifier);
        }
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
        
        /// <summary>
        /// When the value is set to AuthorizationStatusAuthorized your app is allowed to schedule and receive local and remote notifications.
        /// </summary>
        /// <remarks>
        /// When authorized, use the alertSetting, badgeSetting, and soundSetting properties to specify which types of interactions are allowed.
        /// When the value is AuthorizationStatusDenied, the system doesn't deliver notifications to your app, and the system ignores any attempts to schedule local notifications.
        /// </remarks>
        public AuthorizationStatus AuthorizationStatus => (AuthorizationStatus)authorizationStatus;
        
        /// <summary>
        /// The setting that indicates whether your app’s notifications are displayed in Notification Center.
        /// </summary>
        public NotificationSetting NotificationCenterSetting => (NotificationSetting)notificationCenterSetting;
        
        /// <summary>
        /// The setting that indicates whether your app’s notifications appear onscreen when the device is locked.
        /// </summary>
        public NotificationSetting LockScreenSetting => (NotificationSetting)lockScreenSetting;
        
        /// <summary>
        /// The setting that indicates whether your app’s notifications may be displayed in a CarPlay environment.
        /// </summary>
        public NotificationSetting CarPlaySetting => (NotificationSetting)carPlaySetting;
        
        /// <summary>
        /// The authorization status for displaying alerts.
        /// </summary>
        public NotificationSetting AlertSetting => (NotificationSetting)alertSetting;
        
        /// <summary>
        /// The authorization status for badging your app’s icon.
        /// </summary>
        public NotificationSetting BadgeSetting => (NotificationSetting)badgeSetting;
        
        /// <summary>
        /// The authorization status for playing sounds for incoming notifications.
        /// </summary>
        public NotificationSetting SoundSetting => (NotificationSetting)soundSetting;
    }

    /// <summary>
    /// The iOSNotification is used schedule a local notification, which includes the content of the notification and the trigger conditions for delivery.
    /// An instance of this class is also returned when receiving remote or notifications after they had already be triggered.
    /// </summary>
    /// <remarks>
    /// Create an instance of this class when you want to schedule the delivery of a local notification. It contains the entire notification  payload to be delivered
    /// (which corresponds to UNNotificationContent) and  also the NotificationTrigger object with the conditions that trigger the delivery of the notification.
    /// To schedule the delivery of your notification, pass an instance of this class to the iOSNotificationCenter.ScheduleNotification method.
    /// </remarks>
    public class iOSNotification
    {
        /// <summary>
        /// The unique identifier for this notification request.
        /// </summary>
        /// <remarks>
        /// If not set the identifier an unique string will be automatically generated when creating the object.
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
        /// An identifier that you use to group related notifications together.
        /// </summary>
        /// <remarks>
        /// Automatic notification grouping according to the thread identifier is only supported on iOS 12+.
        /// </remarks>
        public string ThreadIdentifier
        {
            get { return  data.threadIdentifier; }
            set {  data.threadIdentifier = value; }
        }

        /// <summary>
        /// A short description of the reason for the notification.
        /// </summary>
        public string Title
        {
            get { return  data.title; }
            set {  data.title = value; }
        }

        /// <summary>
        /// A secondary description of the reason for the notification.
        /// </summary>
        public string Subtitle
        {
            get { return  data.subtitle; }
            set {  data.subtitle = value; }
        }
        
        /// <summary>
        /// The message displayed in the notification alert.
        /// </summary>
        public string Body
        {
            get { return  data.body; }
            set {  data.body = value; }
        }

        /// <summary>
        /// Whether the notification alert should be shown when the app is open.
        /// </summary>
        /// <remarks>
        /// Subscribe to the iOSNotificationCenter.OnNotificationReceived even to receive a callback when the notification is triggered.
        /// </remarks>
        public bool ShowInForeground
        {
            get { return  data.showInForeground; }
            set {  data.showInForeground = value; }
        }

        
        /// <summary>
        /// Only works if ShowInForeground is enabled and user has allowed enabled the requested options for your app. 
        /// </summary>
        public PresentationOption ForegroundPresentationOption
        {
            get {
                return (PresentationOption) data.showInForegroundPresentationOptions; 
            }
            set { data.showInForegroundPresentationOptions = (int) value; }
        }
            

        /// <summary>
        /// The number to display as the app’s icon badge.
        /// </summary>
        public int Badge
        {
            get { return  data.badge; }
            set {  data.badge = value; }
        }
        
        
        /// <summary>
        /// The conditions that trigger the delivery of the notification.
        /// </summary>
        /// <remarks>
        /// For notification that were already delivered and whose instance was returned by [[iOSNotificationCenter.OnRemoteNotificationReceived]] or [[iOSNotificationCenter.OnRemoteNotificationReceived]]
        /// use this property to determine what caused the delivery to occur. You can do this by comparing the trigger object type to any of the notification trigger types that implement it, such as
        /// [[iOSNotificationLocationTrigger]], [[iOSNotificationPushTrigger]], [[iOSNotificationTimeIntervalTrigger]], [[iOSNotificationCalendarTrigger]]
        /// </remarks>
        /// <example>
        /// notification.Trigger is iOSNotificationPushTrigger
        /// </example>
        public iOSNotificationTrigger Trigger
        {
            set
            {
                if (value is iOSNotificationTimeIntervalTrigger)
                {
                    var trigger = (iOSNotificationTimeIntervalTrigger) value;
                    data.triggerType = iOSNotificationTimeIntervalTrigger.Type;
                    data.timeTriggerInterval = trigger.timeInterval;

                    if (trigger.timeInterval > 60)
                    {
                        data.repeats = trigger.Repeats;
                    }
                    else
                    {
                        if (trigger.Repeats)
                        {
                            Debug.LogWarning("Time interval must be at least 60 for repeating notifications.");
                        }
                    }

                }
                else if (value is iOSNotificationCalendarTrigger)
                {
                    var trigger = (iOSNotificationCalendarTrigger) value;
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
                    var trigger = (iOSNotificationLocationTrigger) value;
                    data.triggerType = iOSNotificationLocationTrigger.Type;
                    data.locationTriggerCenterX = trigger.Center.x;
                    data.locationTriggerCenterY = trigger.Center.x;
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
                        Year = (data.calendarTriggerYear > 0) ? (int?) data.calendarTriggerYear : null,
                        Month = (data.calendarTriggerMonth > 0) ? (int?) data.calendarTriggerMonth : null,
                        Day = (data.calendarTriggerDay > 0) ? (int?) data.calendarTriggerDay : null,
                        Hour = (data.calendarTriggerHour >= 0) ? (int?) data.calendarTriggerHour : null,
                        Minute = (data.calendarTriggerMinute >= 0) ? (int?) data.calendarTriggerMinute : null,
                        Second = (data.calendarTriggerSecond >= 0) ? (int?) data.calendarTriggerSecond : null,
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

        /// <summary>
        /// Create a new instance of iOSNotification and automatically generate an unique string for iOSNotification.identifier and with all optional fields set to default values.
        /// </summary>
        public iOSNotification() : this(
            Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()).ToString())
        {
            
        }

        /// <summary>
        /// Create a notification struct with all optional fields set to default values.
        /// </summary>
        public iOSNotification(string identifier)
        {          
            data = new iOSNotificationData();
            data.identifier = identifier;
            data.title = "";
            data.body = "";
            data.badge = 0;
            data.subtitle = "";
            data.categoryIdentifier = "";
            data.threadIdentifier = "";

            data.showInForeground = false;
            data.showInForegroundPresentationOptions = (int) (PresentationOption.NotificationPresentationOptionAlert |
                                                         PresentationOption.NotificationPresentationOptionSound);

            data.triggerType = -1;
            data.repeats = false;
            //Location trigger
            data.locationTriggerCenterX = 0f;
            data.locationTriggerCenterY = 0f;
            data.locationTriggerRadius = 2f;
            data.locationTriggerNotifyOnEntry = true;
            data.locationTriggerNotifyOnExit = false;
    
            //Time trigger
            data.timeTriggerInterval = -1;
            
            //Calendar trigger
            data.calendarTriggerYear = -1;
            data.calendarTriggerMonth = -1;
            data.calendarTriggerDay = -1;
            data.calendarTriggerHour = -1;
            data.calendarTriggerMinute = -1;
            data.calendarTriggerSecond = -1;
        }

        internal iOSNotification(iOSNotificationData data)
        {
            this.data = data;
        }

        internal iOSNotificationData data;

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
        public static int Type { get { return 2; }}

        /// <summary>
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
        public static  int Type { get { return 3; }}
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered after the specified amount of time elapses.
    /// </summary>
    /// <remarks>
    /// Create a iOSNotificationTimeIntervalTrigger instance when you want to schedule the delivery of a local notification after the specified time span has elapsed.
    /// </remarks>
    public struct iOSNotificationTimeIntervalTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return 0; }}
        
        internal int timeInterval;
        
        public TimeSpan TimeInterval
        {
            get { return TimeSpan.FromMilliseconds(timeInterval); }
            set
            {
                timeInterval = (int) value.TotalSeconds;
            }
        }
        public bool Repeats { get; set; }
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered at a specific date and time.
    /// </summary>
    /// <remarks>
    /// Create an instance of iOSNotificationCalendarTrigger  when you want to schedule the delivery of a local notification at the specified date and time.
    /// You are not required to set all of the fields because the system uses the provided information to determine the next date and time that matches the specified information automatically.
    /// </remarks>
    public struct iOSNotificationCalendarTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return 1; }}
        
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public int? Second { get; set; }
        public bool Repeats { get; set; }
    }
    
    /// <summary>
    /// Use this to request authorization to interact with the user when local and remote notifications are delivered to the user's device.
    /// </summary>
    /// <remarks>
    /// This method must be called before you attempt to schedule any local notifications. If \"Request Authorization on App Launch\" is enabled in
    /// \"Edit -> Project Settings -> Mobile Notification Settings\" this method will be caled automatically when the app launches. You might call this method again to determine the current
    /// authorizations status or the DeviceToken for Push Notifications, but the UI system prompt will not be shown if the user has already granted or denied authorization for this app.
    /// </remarks>
    /// <example>
    /// using (var req = new RequestAuthorizationRequest(AuthorizationOption.AuthorizationOptionAlert | AuthorizationOption.AuthorizationOptionBadge, true))
    /// {
    ///     while (!req.IsFinished)
    ///     {
    ///         yield return null;
    ///     };
    /// 
    ///     string ressult = "\n RequestAuthorization: \n";
    ///     ressult += "\n finished: " + req.IsFinished;
    ///     ressult += "\n granted :  " + req.Granted;
    ///     ressult += "\n error:  " + req.Error;
    ///     ressult += "\n deviceToken:  " + req.DeviceToken;
    ///     Debug.Log(res);
    /// }
    /// </example>
    public class RequestAuthorizationRequest : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsFinished { get; private set; }
        
        /// <summary>
        /// A property indicating whether authorization was granted. The value of this parameter is set tot true when authorization was granted for one or more options. The value is setr false when authorization is denied for all options.
        /// </summary>
        public bool Granted { get; private set; }
        
        /// <summary>
        /// Contains error information of the request failed for some reason or an empty string if no error occurred.
        /// </summary>
        public string Error { get; private set; }
        /// <summary>
        /// A globally unique token that identifies this device to Apple Push Notification Network. Send this token to the server that you use to generate remote notifications.
        /// Your server must pass this token unmodified back to APNs when sending those remote notifications.
        /// This property will be empty if you set the [[registerForRemoteNotifications]] to false when creating the Authorization requests or if the app fails registering with the APN.
        /// </summary>
        public string DeviceToken { get; private set; }

        internal delegate void AuthorizationRequestCallback(iOSAuthorizationRequestData notification);
        internal static event AuthorizationRequestCallback OnAuthRequest = delegate { };

        /// <summary>
        /// Initiate an authorization request.
        /// </summary>
        /// <param name="authorizationOption"> The authorization options your app is requesting. You may multiple options to request authorization for multiple items. Request only the authorization options that you plan to use.</param>
        /// <param name="registerForRemoteNotifications"> Set this to true to initiate the registration process with Apple Push Notification service after the user has granted authorization
        /// If registration succeeds the DeviceToken property will be set. You should pass this token along to the server you use to generate remote notifications for the device. </param>
        public RequestAuthorizationRequest(AuthorizationOption authorizationOption, bool registerForRemoteNotifications)
        {
            
            iOSNotificationsWrapper.RegisterAuthorizationRequestCallback();
            iOSNotificationsWrapper.RequestAuthorization((int)authorizationOption, registerForRemoteNotifications);
            
            iOSNotificationCenter.OnAuthorizationRequestCompleted += data =>
                {
                    Debug.Log("            iOSNotificationsWrapper.onAuthenticationRequestFinished += data => ");
                    IsFinished = data.finished;
                    Granted = data.granted;
                    Error = data.error;
                    DeviceToken = data.deviceToken;
                }
            ;
        }

        internal void OnCompletion(iOSAuthorizationRequestData data)
        {
            IsFinished = data.finished;
            Granted = data.granted;
            Error = data.error;
        }
        
        public void Dispose()
        {
            ;// TODO
        }
    }
    
    
    /// <summary>
    /// Use the iOSNotificationCenter to register notification channels and schedule local notifications.
    /// </summary>
    public class iOSNotificationCenter
    {
        private static bool initialized;
        
        public delegate void NotificationReceivedCallback(iOSNotification notification);

        /// <summary>
        /// Subscribe to this event to receive a callback whenever a local notification or a remote is shown to the user.
        /// </summary>
        public static event NotificationReceivedCallback OnNotificationReceived = delegate { };

        /// <summary>
        /// Subscribe to this event to receive a callback whenever a remote notification is received while the app is in foreground,
        /// if you subscribe to this event remote notification will not be shown while the app is in foreground and if you still want
        /// to show it to the user you will have to schedule a local notification with the data received from this callback.
        /// If you want remote notifications to be shown automatically subscribe to the [[OnNotificationReceived]] even instead and check the
        /// [[Notification.Trigger]] class type to determine whether the received notification is a remote notification.
        /// </summary>
        public static event NotificationReceivedCallback OnRemoteNotificationReceived 
        {
            add
            {
                if (!onRemoteNotificationReceivedCallbackSet)
                {
                    iOSNotificationsWrapper.RegisterOnReceivedRemoteNotificationCallback();
                    onRemoteNotificationReceivedCallbackSet = true;
                }

                onRemoteNotificationReceived += value;
            }
            remove { onRemoteNotificationReceived -= value; }
        }

        private static bool onRemoteNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback onRemoteNotificationReceived = delegate(iOSNotification notification) {  };


        internal delegate void AuthorizationRequestCompletedCallback(iOSAuthorizationRequestData data);
        internal static event AuthorizationRequestCompletedCallback OnAuthorizationRequestCompleted = delegate { };


        static bool Initialize()
        {
            #if UNITY_EDITOR || !PLATFORM_IOS
                        return false;
            #endif

            if (initialized)
                return true;
            
            iOSNotificationsWrapper.RegisterOnReceivedCallback();
            return initialized = true;
        }

        /// <summary>
        /// Unschedule the specified notification.
        /// </summary>
        public static void RemoveScheduledNotification(string identifier)
        {
            if (!Initialize())
                return;

            iOSNotificationsWrapper._RemoveScheduledNotification(identifier);
        }

        /// <summary>
        /// Removes the specified notification from Notification Center.
        /// </summary>
        public static void RemoveDeliveredNotification(string identifier)
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveDeliveredNotification(identifier);
        }


        /// <summary>
        /// Unschedules all pending notification.
        /// </summary>
        public static void RemoveAllScheduledNotifications()
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveAllScheduledNotifications();
        }

        /// <summary>
        /// Removes all of the app’s delivered notifications from the Notification Center.
        /// </summary>
        public static void RemoveAllDeliveredNotifications()
        {
            if (!Initialize())
                return;
            iOSNotificationsWrapper._RemoveAllDeliveredNotifications();

        }

        /// <summary>
        /// Get the notification settings for this app.
        /// </summary>
        public static iOSNotificationSettings GetNotificationSettings()
        {
            return iOSNotificationsWrapper.GetNotificationSettings();
        }
        
        /// <summary>
        /// Returns all notifications that are currently scheduled.
        /// </summary>
        public static iOSNotification[] GetScheduledNotifications()
        {
            var iOSNotifications = new List<iOSNotification>();

            foreach (var d in iOSNotificationsWrapper.GetScheduledNotificationData())
            {
                iOSNotifications.Add(new iOSNotification(d));
            }

            return iOSNotifications.ToArray();
        }
        
        /// <summary>
        /// Returns all of the app’s delivered notifications that are currently shown in the Notification Center.
        /// </summary>
        public static iOSNotification[] GetDeliveredNotifications()
        {
            var iOSNotifications = new List<iOSNotification>();

            foreach (var d in iOSNotificationsWrapper.GetDeliveredNotificationData())
            {
                iOSNotifications.Add(new iOSNotification(d));
            }

            return iOSNotifications.ToArray();
        }


        /// <summary>
        /// Schedules a local notification for delivery.
        /// </summary>
        public static void ScheduleNotification(iOSNotification notification)
        {
            if (!Initialize())
                return;
                        
            if (!notification.data.IsValid())
                throw new Exception("Attempting to schedule an invalid notification!");
            
            iOSNotificationsWrapper.ScheduleLocalNotification(notification.data);
        }

        internal static void onReceivedRemoteNotification(iOSNotificationData data)
        {
            var notification = new iOSNotification(data.identifier);
            notification.data = data;
            onRemoteNotificationReceived(notification);
        }
        
        internal static void onSentNotification(iOSNotificationData data)
        {                
            var notification = new iOSNotification(data.identifier);
            notification.data = data;
            OnNotificationReceived(notification);
        }

        internal static void onFinishedAuthorizationRequest(iOSAuthorizationRequestData data)
        {
            OnAuthorizationRequestCompleted(data);
        }
    }
}
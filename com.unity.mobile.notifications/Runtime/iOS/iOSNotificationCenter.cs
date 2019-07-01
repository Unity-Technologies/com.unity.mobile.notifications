using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#pragma warning disable 67

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
        NotSupported  = 0,
        
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
    /// Enum for requesting authorization to interact with the user.
    /// </summary>
    [Flags]
    public enum AuthorizationOption
    {
        /// <summary>
        /// The ability to update the app’s badge.
        /// </summary>
        Badge   = (1 << 0),
        
        /// <summary>
        /// The ability to play sounds.
        /// </summary>
        Sound   = (1 << 1),
        
        /// <summary>
        /// The ability to display alerts.
        /// </summary>
        Alert   = (1 << 2),
        
        /// <summary>
        /// The ability to display notifications in a CarPlay environment.
        /// </summary>
        CarPlay = (1 << 3),
    }
    /// <summary>
    /// Constants indicating how to present a notification in a foreground app
    /// </summary>
    [Flags]
    public enum PresentationOption
    {
        None  = 0,
        
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

    internal enum NotificationTriggerType
    {
        TimeTrigger = 0, 
        CalendarTrigger = 10,
        LocationTrigger = 20,
        PushTrigger = 3
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

        //Custom information
        public string data;
        public bool showInForeground;
        public Int32 showInForegroundPresentationOptions;
        
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
            get { return (AuthorizationStatus) authorizationStatus; }
        }

        /// <summary>
        /// The setting that indicates whether your app’s notifications are displayed in Notification Center.
        /// </summary>
        public NotificationSetting NotificationCenterSetting
        {
            get { return (NotificationSetting) notificationCenterSetting; }
        }

    /// <summary>
        /// The setting that indicates whether your app’s notifications appear onscreen when the device is locked.
        /// </summary>
        public NotificationSetting LockScreenSetting
        {
            get { return (NotificationSetting) lockScreenSetting;}
        }

        /// <summary>
        /// The setting that indicates whether your app’s notifications may be displayed in a CarPlay environment.
        /// </summary>
        public NotificationSetting CarPlaySetting
        {
            get { return (NotificationSetting)carPlaySetting;}
        }

        /// <summary>
        /// The authorization status for displaying alerts.
        /// </summary>
        public NotificationSetting AlertSetting
        {
            get { return  (NotificationSetting)alertSetting;}
        }
        
        /// <summary>
        /// The authorization status for badging your app’s icon.
        /// </summary>
        public NotificationSetting BadgeSetting
        {
            get { return (NotificationSetting)badgeSetting;}
        }

        /// <summary>
        /// The authorization status for playing sounds for incoming notifications.
        /// </summary>
        public NotificationSetting SoundSetting
        {
            get { return (NotificationSetting)soundSetting;}
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
            get { return  (AlertStyle)alertStyle; }
        }
        
        /// <summary>
        /// The setting that indicates whether the app shows a preview of the notification's content.
        /// </summary>
        public ShowPreviewsSetting ShowPreviewsSetting
        {
            get { return  (ShowPreviewsSetting)showPreviewsSetting;}
        }
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
        /// Automatic notification grouping according to the thread identifier is only supported on iOS 12 and above.
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
        /// Indicates whether the notification alert should be shown when the app is open.
        /// </summary>
        /// <remarks>
        /// Subscribe to the <see cref="iOSNotificationCenter.OnNotificationReceived"/> event to receive a callback when the notification is triggered.
        /// </remarks>
        public bool ShowInForeground
        {
            get { return  data.showInForeground; }
            set {  data.showInForeground = value; }
        }

        
        /// <summary>
        /// Presentation options for displaying the local of notification when the app is running. Only works if  <see cref="iOSNotification.ShowInForeground"/> is enabled and user has allowed enabled the requested options for your app. 
        /// </summary>
        public PresentationOption ForegroundPresentationOption
        {
            get {
                return (PresentationOption) data.showInForegroundPresentationOptions; 
            }
            set { data.showInForegroundPresentationOptions = (int) value; }
        }
            

        /// <summary>
        /// The number to display as a badge on the app’s icon.
        /// </summary>
        public int Badge
        {
            get { return  data.badge; }
            set {  data.badge = value; }
        }
        
        /// <summary>
        /// Arbitrary string data which can be retrieved when the notification is used to open the app or is received while the app is running.
        /// </summary>
        public string Data
        {
            get { return data.data; }
            set { data.data = value; }
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
        /// Create a new instance of <see cref="iOSNotificationCenter.iOSNotification"/> and automatically generate an unique string for <see cref="iOSNotificationCenter.iOSNotification.identifier"/>  with all optional fields set to default values.
        /// </summary>
        public iOSNotification() : this(
            Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode()).ToString())
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
            
            data.data = "";
            data.showInForeground = false;
            data.showInForegroundPresentationOptions = (int) (PresentationOption.Alert |
                                                         PresentationOption.Sound);

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
        public static int Type { get { return (int)NotificationTriggerType.LocationTrigger; }}

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
        public static  int Type { get { return (int)NotificationTriggerType.PushTrigger; }}
    }

    /// <summary>
    /// A trigger condition that causes a notification to be delivered after the specified amount of time elapses.
    /// </summary>
    /// <remarks>
    /// Create a iOSNotificationTimeIntervalTrigger instance when you want to schedule the delivery of a local notification after the specified time span has elapsed.
    /// </remarks>
    public struct iOSNotificationTimeIntervalTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.TimeTrigger; }}
        
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
    /// Create an instance of <see cref="iOSNotificationCalendarTrigger"/>  when you want to schedule the delivery of a local notification at the specified date and time.
    /// You are not required to set all of the fields because the system uses the provided information to determine the next date and time that matches the specified information automatically.
    /// </remarks>
    public struct iOSNotificationCalendarTrigger : iOSNotificationTrigger
    {
        public static int Type { get { return (int)NotificationTriggerType.CalendarTrigger; }}
        
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
    
    /// <summary>
    /// Use this to request authorization to interact with the user when you with to deliver local and remote notifications are delivered to the user's device.
    /// </summary>
    /// <remarks>
    /// This method must be called before you attempt to schedule any local notifications. If "Request Authorization on App Launch" is enabled in
    /// "Edit -> Project Settings -> Mobile Notification Settings" this method will be called automatically when the app launches. You might call this method again to determine the current
    /// authorizations status or retrieve the DeviceToken for Push Notifications. However the UI system prompt will not be shown if the user has already granted or denied authorization for this app.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
    /// {
    ///     while (!req.IsFinished)
    ///     {
    ///         yield return null;
    ///     };
    /// 
    ///     string result = "\n RequestAuthorization: \n";
    ///     result += "\n finished: " + req.IsFinished;
    ///     result += "\n granted :  " + req.Granted;
    ///     result += "\n error:  " + req.Error;
    ///     result += "\n deviceToken:  " + req.DeviceToken;
    ///     Debug.Log(res);
    /// }
    /// </code>
    /// </example>
    public class AuthorizationRequest : IDisposable
    {
        /// <summary>
        /// Indicates whether the authorization request has completed.
        /// </summary>
        public bool IsFinished { get; private set; }
        
        /// <summary>
        /// A property indicating whether authorization was granted. The value of this parameter is set to true when authorization was granted for one or more options. The value is set to false when authorization is denied for all options.
        /// </summary>
        public bool Granted { get; private set; }
        
        /// <summary>
        /// Contains error information of the request failed for some reason or an empty string if no error occurred.
        /// </summary>
        public string Error { get; private set; }
        /// <summary>
        /// A globally unique token that identifies this device to Apple Push Notification Network. Send this token to the server that you use to generate remote notifications.
        /// Your server must pass this token unmodified back to APNs when sending those remote notifications.
        /// This property will be empty if you set the registerForRemoteNotifications parameter to false when creating the Authorization request or if the app fails registration with the APN.
        /// </summary>
        public string DeviceToken { get; private set; }

        internal delegate void AuthorizationRequestCallback(iOSAuthorizationRequestData notification);
        internal static event AuthorizationRequestCallback OnAuthRequest = delegate { };

        /// <summary>
        /// Initiate an authorization request.
        /// </summary>
        /// <param name="authorizationOption"> The authorization options your app is requesting. You may specify multiple options to request authorization for. Request only the authorization options that you plan to use.</param>
        /// <param name="registerForRemoteNotifications"> Set this to true to initiate the registration process with Apple Push Notification service after the user has granted authorization
        /// If registration succeeds the DeviceToken will be returned. You should pass this token along to the server you use to generate remote notifications for the device. </param>
        public AuthorizationRequest(AuthorizationOption authorizationOption, bool registerForRemoteNotifications)
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
        public static event NotificationReceivedCallback OnNotificationReceived{
            add
            {
            if (!onNotificationReceivedCallbackSet)
            {
                iOSNotificationsWrapper.RegisterOnReceivedCallback();
                onNotificationReceivedCallbackSet = true;
            }

                onNotificationReceived += value;
            }
            remove
            {
                onNotificationReceived -= value;
            }
        }

        private static bool onNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback onNotificationReceived = delegate(iOSNotification notification) {  };

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
            remove
            {
                onRemoteNotificationReceived -= value;
            }
        }

        private static bool onRemoteNotificationReceivedCallbackSet;
        private static event NotificationReceivedCallback onRemoteNotificationReceived = delegate(iOSNotification notification) {  };


        internal delegate void AuthorizationRequestCompletedCallback(iOSAuthorizationRequestData data);
        internal static event AuthorizationRequestCompletedCallback OnAuthorizationRequestCompleted = delegate { };


        static bool Initialize()
        {
            #if UNITY_EDITOR || !PLATFORM_IOS
                        return false;
            #elif PLATFORM_IOS

            if (initialized)
                return true;
            
            iOSNotificationsWrapper.RegisterOnReceivedCallback();
            return initialized = true;
            #endif
        }
        
        /// <summary>
        /// Use this to retrieve the last local or remote notification received by the app. 
        /// </summary>
        /// <returns>
        /// Returns the last local or remote notification used to open the app or clicked on by the user. If no notification is available it returns null.
        /// </returns>
        public static iOSNotification GetLastRespondedNotification()
        {
            var data = iOSNotificationsWrapper.GetLastNotificationData();

            if (data == null)
                return null;
            
            var notification = new iOSNotification(data.Value.identifier);
            notification.data = data.Value;

            return notification;
        }

        /// <summary>
        /// The number currently set as the badge of the app icon.
        /// </summary>
        public static int ApplicationBadge
        {
            get { return iOSNotificationsWrapper.GetApplicationBadge(); }
            set { iOSNotificationsWrapper.SetApplicationBadge(value); }
        }

        /// <summary>
        /// Un-schedule the specified notification.
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
            onNotificationReceived(notification);
        }

        internal static void onFinishedAuthorizationRequest(iOSAuthorizationRequestData data)
        {
            OnAuthorizationRequestCompleted(data);
        }
    }
}

#pragma warning restore 67
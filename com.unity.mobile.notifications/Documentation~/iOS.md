# iOS Notifications

## Authorization request <a name="RequestAuthorizationExample"/>

You need to request permissions from the system to send local notifications and receive remote ones by [AuthorizationRequest](../api/Unity.Notifications.iOS.AuthorizationRequest.html). You can ask the user for permissions to only send certain notification types. The below example shows how to request permissions to show UI Alert dialogs and add a badge on the app icon.

```c#
IEnumerator RequestAuthorization()
{
    var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
    using (var req = new AuthorizationRequest(authorizationOption, true))
    {
        while (!req.IsFinished)
        {
            yield return null;
        };
        
        string res = "\n RequestAuthorization: \n";
        res += "\n finished: " + req.IsFinished;
        res += "\n granted :  " + req.Granted;
        res += "\n error:  " + req.Error;
        res += "\n deviceToken:  " + req.DeviceToken;
        Debug.Log(res);
    }
}
```

You can do the request again to check the current authorization status afterwards. The permissions request dialog won’t display again if the user has already granted or denied authorization.

Users might change the authorization status for each notification type at any time in the system settings, you can call [iOSNotificationCenter.GetNotificationSettings](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_GetNotificationSettings) to check the actual authorization status when necessary.

### Device token

A device token is a data that contains a unique identifier assigned by Apple to a specific app on a specific device. If you intend to send push notifications to the users after they confirm the authorization request, you need to retrieve the device token first.

To retrieve the device token, you need to:
- Check `Enable Push Notifications` in the [notification settings](settings.html#EnablePushNotifications).
- Create the authorization request with `registerForRemoteNotifications` set to true.

For more information on how to send push notifications to a device and how to add push notification support to your app, please check [Apple Developer Document](https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/HandlingRemoteNotifications.html#//apple_ref/doc/uid/TP40008194-CH6-SW1).


## Manage notifications

In the notification package, a set of APIs are provided to manage notifications including sending, updating, deleting etc. Please refer to [iOSNotificationCenter](../api/Unity.Notifications.iOS.iOSNotificationCenter.html) for more notification related APIs.

### Send a simple notification

The below example shows how to schedule a notification with the [time interval trigger](../api/Unity.Notifications.iOS.iOSNotificationTimeIntervalTrigger.html).

```c#
var timeTrigger = new iOSNotificationTimeIntervalTrigger()
{
    TimeInterval = new TimeSpan(0, minutes, seconds),
    Repeats = false
};

var notification = new iOSNotification()
{
    // You can optionally specify a custom identifier which can later be 
    // used to cancel the notification, if you don't set one, a unique 
    // string will be generated automatically.
    Identifier = "_notification_01",
    Title = "Title",
    Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
    Subtitle = "This is a subtitle, something, something important...",
    ShowInForeground = true,
    ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
    CategoryIdentifier = "category_a",
    ThreadIdentifier = "thread1",
    Trigger = timeTrigger,
};

iOSNotificationCenter.ScheduleNotification(notification);
```

The following code example cancels a notification if it doesn’t trigger.

```c#
iOSNotificationCenter.RemoveScheduledNotification(notification.Identifier);
```

The following code example removes a notification from the Notification Center if it's already delivered.

```c#
iOSNotificationCenter.RemoveDeliveredNotification(notification.Identifier);
```

### Other triggers

Besides the time interval trigger, this package also provides the calendar trigger, location triggers and push triggger. 

#### Calendar trigger

All the fields in [iOSNotificationCalendarTrigger](../api/Unity.Notifications.iOS.iOSNotificationCalendarTrigger.html) are optional, but you need to set at least one field for the trigger to work. For example, if you only set the hour and minute fields, the system automatically triggers the notification on the next specified hour and minute.

```c#
var calendarTrigger = new iOSNotificationCalendarTrigger()
{
    // Year = 2020,
    // Month = 6,
    //Day = 1,
    Hour = 12,
    Minute = 0,
    // Second = 0
    Repeats = false
};
```

#### Location trigger

You can also create an [iOSNotificationLocationTrigger](../api/Unity.Notifications.iOS.iOSNotificationLocationTrigger.html) if you want to schedule the delivery of a notification when the device enters or leaves a specific geographic region.

Before you schedule any notifications with this trigger, you need to check `Include CoreLocation Framework` in the [notifications settings](settings.html#IncludeCoreLocation). Your app must have authorization to use Core Location and must have when-in-use permissions. You can use the Unity LocationService API to request this authorization. 
For additional information, see the [Core Location](https://developer.apple.com/documentation/corelocation/clregion?language=objc) documentation on the Apple Developer website.

In the below example, the center coordinate is defined using the WGS 84 system. The app triggers the notification when the user enters an area within a 250 meter radius around the Eiffel Tower in Paris.

```c#
var locationTrigger = new iOSNotificationLocationTrigger()
{
    Center = new Vector2(2.294498f, 48.858263f),
    Radius = 250f,
    NotifyOnEntry = true,
    NotifyOnExit = false,
}
```

#### Push trigger

You shouldn't really create an [iOSNotificationPushTrigger](../api/Unity.Notifications.iOS.iOSNotificationPushTrigger.html) instance as you can't really send a push notification from your app. This notification trigger is used to indicate if the notification was sent from Apple Push Notification Service (APNs).

### Notification received callbacks

#### [iOSNotificationCenter.OnNotificationReceived](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_OnNotificationReceived)

By default, if your app triggers a local notification while it is in the foreground, the device won’t display an alert for that notification. If you want the notification to behave as though the device isn’t running the app, set the `ShowInForeground` property when you schedule the notification as below.

```c#
notification.ShowInForeground = true;

// In this case you need to specify its 'ForegroundPresentationOption'
notification.ForegroundPresentationOption = (PresentationOption.Sound | PresentationOption.Alert);
```

By subscribing to the `iOSNotificationCenter.OnNotificationReceived` event, you can also perform other actions when the app triggers a notification. For example, you can display the notification content using the app’s UI. Your app calls this event whenever a local or a remote notification is received, regardless if it's shown in the foreground.

#### [iOSNotificationCenter.OnRemoteNotificationReceived](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_OnRemoteNotificationReceived)

To modify or hide the content of a received remote notification while your app is running, subscribe to the `iOSNotificationCenter.OnRemoteNotificationReceived` event. With subscribing to this event, the remote notification won’t display when your app is running. If you still want to show an alert for it, schedule a local notification using the remote notification’s content, like below:

```c#
iOSNotificationCenter.OnRemoteNotificationReceived += remoteNotification =>
{
    // When a remote notification is received, modify its contents and show it after 1 second.
    var timeTrigger = new iOSNotificationTimeIntervalTrigger()
    {
        TimeInterval = new TimeSpan(0, 0, 1),
        Repeats = false
    };
    
    iOSNotification notification = new iOSNotification()
    {
        Title = "Remote: " + remoteNotification.Title,
        Body = "Remote: " + remoteNotification.Body,
        Subtitle = "Remote: " + remoteNotification.Subtitle,
        ShowInForeground = true,
        ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert,
        CategoryIdentifier = remoteNotification.CategoryIdentifier,
        ThreadIdentifier = remoteNotification.ThreadIdentifier,
        Trigger = timeTrigger,
    };
    iOSNotificationCenter.ScheduleNotification(notification);
};
```

### Store and retrieve custom data
**Save custom data and retrieve it when the user opens the app from the notification**

You can store arbitrary string data on the notification with the `Data` property.

```c#
var notification = new iOSNotification();
notification.Data = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
iOSNotificationCenter.ScheduleNotification(notification);
```

The following code example shows how to retrieve the last notification the app received:

```c#
var notification = iOSNotificationCenter.GetLastRespondedNotification();
if (notification != null)
{
    var msg = "Last Received Notification : " + n.Identifier + "\n";
    msg += "\n - Notification received: ";
    msg += "\n - .Title: " + n.Title;
    msg += "\n - .Badge: " + n.Badge;
    msg += "\n - .Body: " + n.Body;
    msg += "\n - .CategoryIdentifier: " + n.CategoryIdentifier;
    msg += "\n - .Subtitle: " + n.Subtitle;
    msg += "\n - .Data: " + n.Data;
    Debug.Log(msg);
}
```

If the user opens the app from a notification, `GetLastRespondedNotification` also returns that notification. Otherwise it returns null.

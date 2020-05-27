# iOS notifications

## Authorization request

You need to request permissions from the system to send local notifications and receive remote notifications. To do this, use [AuthorizationRequest](../api/Unity.Notifications.iOS.AuthorizationRequest.html). You can ask for permissions to only send certain types of notification. The example below shows how to request permissions to show UI Alert dialogs and add a badge on the app icon.

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
        
        string res = "\n RequestAuthorization:";
        res += "\n finished: " + req.IsFinished;
        res += "\n granted :  " + req.Granted;
        res += "\n error:  " + req.Error;
        res += "\n deviceToken:  " + req.DeviceToken;
        Debug.Log(res);
    }
}
```

You can send the same request again to check the current authorization status. If the user has already granted or denied authorization, the permissions request dialogue doesn't display again.

You can also enable an automatic authorization request when the user launches the app. For more details, see [notification settings](settings.html#request-authorization-on-app-launch).

Users might change the authorization status for each notification type at any time in the system settings. You can call [iOSNotificationCenter.GetNotificationSettings](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_GetNotificationSettings) to check the actual authorization status when necessary.

### Device token

A device token is a piece of data that contains a unique identifier assigned by Apple to a specific app on a specific device. If you intend to send push notifications to the users after they confirm the authorization request, you need to retrieve the device token first.

To retrieve the device token, you need to:
- Enable the **Enable Push Notifications** option in the [notification settings](settings.html#enable-push-notifications).
- Create the authorization request with `registerForRemoteNotifications` set to true.

For more information on how to send push notifications to a device and how to add push notification support to your app, see Apple developer documentation on [handling remote notifications](https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/HandlingRemoteNotifications.html#//apple_ref/doc/uid/TP40008194-CH6-SW1).


## Manage notifications

This package provides a set of APIs to manage notifications. These APIs enable actions such as sending, updating, and deleting notifications. For more notification-related APIs, see [iOSNotificationCenter](../api/Unity.Notifications.iOS.iOSNotificationCenter.html).

### Send a simple notification

The example below shows how to schedule a notification with the [time interval trigger](../api/Unity.Notifications.iOS.iOSNotificationTimeIntervalTrigger.html).

```c#
var timeTrigger = new iOSNotificationTimeIntervalTrigger()
{
    TimeInterval = new TimeSpan(0, minutes, seconds),
    Repeats = false
};

var notification = new iOSNotification()
{
    // You can specify a custom identifier which can be used to manage the notification later.
    // If you don't provide one, a unique string will be generated automatically.
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

Besides the time interval trigger, this package provides three additional types of triggers:
* Calendar trigger
* Location trigger
* Push trigger

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

Before you schedule any notifications with this trigger, you need to enable the **Include CoreLocation Framework** option in the [notifications settings](settings.html#include-corelocation-framework). Your app must have authorization to use Core Location and must have when-in-use permissions. You can use the Unity LocationService API to request this authorization. 
For additional information, see the [Core Location](https://developer.apple.com/documentation/corelocation/clregion?language=objc) documentation on the Apple Developer website.

In the example below, the center coordinate is defined using the WGS 84 system. The app triggers the notification when the user enters an area within a 250 meter radius around the Eiffel Tower in Paris.

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

You shouldn't need to create an [iOSNotificationPushTrigger](../api/Unity.Notifications.iOS.iOSNotificationPushTrigger.html) instance as you can't really send a push notification from your app. This notification trigger is used to indicate if the notification was sent from Apple Push Notification Service (APNs).

### Notification received callbacks

#### [iOSNotificationCenter.OnNotificationReceived](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_OnNotificationReceived)

By default, if your app triggers a local notification while it is in the foreground, the device won’t display an alert for that notification. If you want the notification to behave as though the device isn’t running the app, set the `ShowInForeground` property to true when you schedule the notification, as shown below.

```c#
notification.ShowInForeground = true;

// In this case you need to specify its 'ForegroundPresentationOption'
notification.ForegroundPresentationOption = (PresentationOption.Sound | PresentationOption.Alert);
```

By subscribing to the `iOSNotificationCenter.OnNotificationReceived` event, you can also perform other actions when the app triggers a notification. For example, you can display the notification content using the app’s UI. Your app calls this event whenever a local or a remote notification is received, regardless if it's shown in the foreground.

#### [iOSNotificationCenter.OnRemoteNotificationReceived](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_OnRemoteNotificationReceived)

To modify or hide the content of a received remote notification while your app is running, subscribe to the `iOSNotificationCenter.OnRemoteNotificationReceived` event. When subscribing to this event, the remote notification won’t display when your app is running. If you still want to show an alert for it, schedule a local notification using the remote notification’s content. The example below shows how to do this.

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

You can store arbitrary string data on the notification with [iOSNotification.Data](../api/Unity.Notifications.iOS.iOSNotification.html#Unity_Notifications_iOS_iOSNotification_Data), and retrieve it later from the received notification.

```c#
var notification = new iOSNotification();
notification.Data = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
iOSNotificationCenter.ScheduleNotification(notification);
```

The following code example shows how to retrieve the last notification the app received.

```c#
var notification = iOSNotificationCenter.GetLastRespondedNotification();
if (notification != null)
{
    var msg = "Last Received Notification: " + notification.Identifier;
    msg += "\n - Notification received: ";
    msg += "\n - .Title: " + notification.Title;
    msg += "\n - .Badge: " + notification.Badge;
    msg += "\n - .Body: " + notification.Body;
    msg += "\n - .CategoryIdentifier: " + notification.CategoryIdentifier;
    msg += "\n - .Subtitle: " + notification.Subtitle;
    msg += "\n - .Data: " + notification.Data;
    Debug.Log(msg);
}
```

If the user opens the app from a notification, [iOSNotificationCenter.GetLastRespondedNotification](../api/Unity.Notifications.iOS.iOSNotificationCenter.html#Unity_Notifications_iOS_iOSNotificationCenter_GetLastRespondedNotification) also returns that notification. Otherwise, it returns null.

#### Set custom data for remote notifications

Sometimes, you might want to set custom data on the payload for a remote notification and retrieve it using [iOSNotification.Data](../api/Unity.Notifications.iOS.iOSNotification.html#Unity_Notifications_iOS_iOSNotification_Data). The example below shows how to set a string as `data` on the payload.

```
{
    "aps": {
        "alert": {
            "title": "Hello world!",
            "body": "This is an example of a remote notification"
            }
    },
    "data": "Test data"
}
```

You have to use the exact `data` as the key of your custom data, because this is what the package looks for.

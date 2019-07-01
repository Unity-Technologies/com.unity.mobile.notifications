

# Unity Mobile Notifications Package

The runtime API is split into two parts: `AndroidNotificationCenter` and `iOSNotificationCenter`. These can be used to schedule and manage notifications for their respective platforms. You can download a sample Project which implements a high-level wrapper that you can use to send notifications to both Android and iOS with the same API from our [GitHub page](https://github.com/Unity-Technologies/NotificationsSamples), or see the code samples below.

This package supports the following features:



*   Schedule local one-time or repeatable notifications.
*   Cancel already displayed and upcoming (scheduled) notifications.
*   Android:
    *   Create and modify notification channels (categories) on Android Oreo and above.
    *   Preserve notifications when the device restarts.
    *   Set custom notification icons.
*   iOS:
    *   Use the Apple Push Notification Service (APNs) to receive remote notifications.
    *   Modify remote notification content if the device receives notifications from other apps while your app is running.
    *   Group notifications into threads (only supported on iOS 12+).

**Requirements:**

*   Supports Android 4.4 (API 19) and iOS 10 or above.
*   Compatible with Unity 2018.3 or above.  


## Android

**Create a notification channel**

Every local notification must belong to a notification channel. Notification channels are only supported on Android 8.0 Oreo and above. On older Android versions, this package emulates notification channel behavior. Settings such as priority (`Importance`) set for notification channels apply to individual notifications even on Android versions prior to 8.0.


```c#
var c = new AndroidNotificationChannel()
{
    Id = "channel_id",
    Name = "Default Channel",
    Importance = Importance.High,
    Description = "Generic notifications",
};
AndroidNotificationCenter.RegisterNotificationChannel(c);
```




**Send a simple notification**

This example shows you how to schedule a simple text notification and send it to the notification channel you created in the previous step.


```c#
var notification = new AndroidNotification();
notification.Title = "SomeTitle";
notification.Text = "SomeText";
notification.FireTime = System.DateTime.Now.AddMinutes(5);

AndroidNotificationCenter.SendNotification(notification, "channel_id");
```


If you don’t specify a custom icon for each notification, the default Unity icon displays in the status bar instead. You can configure notification icons in the **Project Settings **window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**). Whenever you schedule a notification in your script, use the icon ID you define in the **Mobile Notification Settings** window.


```c#
notification.SmallIcon = "my_custom_icon_id";
```


You can optionally set a large icon which also displays in the notification view. The smaller icon displays as a small badge on top of the large one.


```c#
notification.LargeIcon = "my_custom_large_icon_id";
```


Unity assigns a unique identifier to each notification after you schedule it. You can use the identifier to track the notification status or to cancel it. Notification status tracking only works on Android 6.0 Marshmallow and above.


```c#
var identifier = AndroidNotificationCenter.SendNotification(n, "channel_id");
```


Use the following code example to check if your app has delivered the notification to the device and perform any actions depending on the result.


```c#
if ( AndroidNotificationCenter.CheckScheduledNotificationStatus(identifier) == NotificationStatus.Scheduled)
{
	// Replace the currently scheduled notification with a new notification.
	AndroidNotificationCenter.UpdateScheduledNotification(identifier, newNotification, channel);
}
else if ( AndroidNotificationCenter.CheckScheduledNotificationStatus(identifier) == NotificationStatus.Delivered)
{
	//Remove the notification from the status bar
	AndroidNotificationCenter.CancelNotification(identifier);
}
else if ( AndroidNotificationCenter.CheckScheduledNotificationStatus(identifier) == NotificationStatus.Unknown)
{
	AndroidNotificationCenter.SendNotification(newNotification, "channel_id");
}
```


**Preserve scheduled notifications after the device restarts**

By default, apps remove scheduled notifications when the device restarts. To automatically reschedule all notifications when the user turns the device back on, enable the **Reschedule Notifications on Device Restart** setting in the **Project Settings** window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**). This adds the `RECEIVE_BOOT_COMPLETED` permissions to your app's manifest.

 

**Handle received notifications while the app is running**

You can subscribe to the `AndroidNotificationCenter.OnNotificationReceived` event to receive a callback whenever the device receives a remote notification while your app is running.


```c#
AndroidNotificationCenter.NotificationReceivedCallback receivedNotificationHandler = 
    delegate(AndroidNotificationIntentData data)
    {
        var msg = "Notification received : " + data.Id + "\n";
        msg += "\n Notification received: ";
        msg += "\n .Title: " + data.Notification.Title;
        msg += "\n .Body: " + data.Notification.Text;
        msg += "\n .Channel: " + data.Channel;
        Debug.Log(msg);
    };

AndroidNotificationCenter.OnNotificationReceived += receivedNotificationHandler;
```


**Save custom data and retrieve it when the user opens the app from the notification**

To store arbitrary string data in a notification object set the `IntentData` property.


```c#
var notification = new AndroidNotification();
notification.IntentData = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
 AndroidNotificationCenter.SendNotification(notification, "channel_id");
```


If the user opens the app from the notification, you can retrieve it any and any data it has assigned to it like this:


```c#
var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();

if (notificationIntentData != null)
{
  var id = notificationIntentData.Id;
  var channel = notificationIntentData.Channel;
  var notification = notificationIntentData.Notification;
}
```


If the app is opened in any other way, `GetLastNotificationIntent` returns null. 


## iOS

 

**Request authorization**

You need to request permission from the system to post local notifications and receive remote ones. If you intend to send the user remote notifications after they confirm the authorization request, you need to retrieve the `DeviceToken`. To do this, the request must be created with `registerForRemoteNotifications` set to true. For more information on how to send push notifications to a device and how to add push notification support to your app, see the [Apple Developer website](https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/HandlingRemoteNotifications.html#//apple_ref/doc/uid/TP40008194-CH6-SW1) documentation. 

Optionally, you can ask the user for permission to only send certain notification types. The example below shows how to request permission to show UI Alert dialogs and add a badge on the app icon. However, the user might change the authorization status for each notification type at any time in the settings app, so to check the actual authorization status, call `iOSNotificationCenter.GetNotificationSettings`.

Alternatively, you can enable the **Request Authorization on App Launch **setting in the **Project Settings **window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**), which makes the app automatically show a permissions request dialog when the user launches the app. Afterwards, you can call this method again to determine the current authorization status. The permissions request dialog won’t display again if the user has already granted or denied authorization.


```c#
IEnumerator RequestAuthorization()
{
  using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
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
```

**Send a simple notification**


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


The following code example cancels the notification if it doesn’t trigger:


```c#
iOSNotificationCenter.RemoveScheduledNotification(notification.Identifier);
```


The following code example removes the notification from the Notification Center if it was already shown to the user:


```c#
iOSNotificationCenter.RemoveDeliveredNotification(notification.Identifier);
```






**Other triggers**

As well as the time interval trigger, you can use calendar and location triggers. All the fields in `iOSNotificationCalendarTrigger` are optional, but you need to set at least one field for the trigger to work. For example, if you only set the hour and minute fields, the system  automatically triggers the notification on the next specified hour and minute.


```c#
var calendarTrigger = new iOSNotificationCalendarTrigger()
{
    // Year = 2018,
    // Month = 8,
    //Day = 30,
    Hour = 12,
    Minute = 0,
    // Second = 0
    Repeats = false
};
```


You can also create location triggers if you want to schedule the delivery of a notification when the device enters or leaves a specific geographic region. Before you schedule any notifications with this trigger, your app must have authorization to use Core Location and must have when-in-use permissions. Use the Unity LocationService API to request this authorization. For additional information, see the [Core Location](https://developer.apple.com/documentation/corelocation/clregion?language=objc) documentation on the Apple Developer website.

In this example, the center coordinate is defined using the WGS 84 system. The app triggers the notification when the user enters an area within a 250 meter radius around the Eiffel Tower in Paris.


```c#
var locationTrigger = new iOSNotificationLocationTrigger()
{
    Center = new Vector2(2.294498f, 48.858263f),
    Radius = 250f,
    NotifyOnEntry = true,
    NotifyOnExit = false,
}
```


**Handle received notifications when the app is running**

If your app triggers a notification while it’s running, you can perform a custom action instead of showing a notification alert. By default, if your app triggers a local notification while it is in the foreground, the device won’t display an alert for that notification. If you want the notification to behave as though the device isn’t running the app, set the `ShowInForeground` property when you schedule the notification:


```c#
notification.ShowInForeground = true;

// In this case you need to specify its 'ForegroundPresentationOption'
notification.ForegroundPresentationOption = (PresentationOption.Sound |                                                     PresentationOption.Alert);

```


Alternatively, you can perform another action when the app triggers the notification. For example, you can display the notification content using the app’s UI. To do this, subscribe to the `OnNotificationReceived` event. Your app calls this event whenever a local or a remote notification is received, regardless if it's shown in the foreground.

To modify or hide the content of a remote notification your app receives while its running, subscribe to the `OnRemoteNotificationReceived `event. If you do this, the remote notification won’t display when your app is running. If you still want to show an alert for it, schedule a local notification using the remote notification’s content, like this:


```c#
iOSNotificationCenter.OnRemoteNotificationReceived += notification =>
{
    // When a remote notification is received, modify its contents and show it
    // after 1 second.
    var timeTrigger = new iOSNotificationTimeIntervalTrigger()
    {
        TimeInterval = new TimeSpan(0, 0, 1),
        Repeats = false
    };

    iOSNotification  n = new iOSNotification()
    {
        Title = "Remote : " + notification.Title,
        Body =  "Remote : " + notification.Body,
        Subtitle =  "Remote: " + notification.Subtitle,
        ShowInForeground = true,
				ForegroundPresentationOption = PresentationOption.Sound | PresentationOption.Alert | PresentationOption.Badge,
        CategoryIdentifier = notification.CategoryIdentifier,
        ThreadIdentifier = notification.ThreadIdentifier,
        Trigger = timeTrigger,
    };
    iOSNotificationCenter.ScheduleNotification(n);

    Debug.Log("Rescheduled remote notifications with id: " + notification.Identifier);
};
```


**Save custom data and retrieve it when the user opens the app from the notification**

To store arbitrary string data in a notification object, set the `Data` property:


```c#
var notification = new iOSNotification();
notification.Data = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
//..assign other fields..
iOSNotificationCenter.ScheduleNotification(notification);
```


The following code example shows how to retrieve the last notification the app received:


```c#
var n = iOSNotificationCenter.GetLastRespondedNotification();
if (n != null)
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
else
{
    Debug.Log("No notifications received.");
}
```


If the user opens the app from a notification, `GetLastRespondedNotification` also returns that notification. Otherwise it returns null.


## FAQ

 

**Why are Notifications not delivered on certain Huawei and Xiaomi phones when my app is closed and not running in the background ?**

Seems that Huawei (including Honor) and Xiaomi utilize aggresive batter saver [techniques](https://stackoverflow.com/questions/47145722/how-to-deal-with-huaweis-and-xiaomis-battery-optimizations)  which restrict app background activities unless the app has been whitelisted by the user in device settings. This means that any scheduled notifications will not be delivered if the app is closed or not running in the bacgkround. We are not aware of any way to workaround this behaviour besides encouraging the user to whitelist your app.

**What can I do if notifications with a location trigger don’t work?**

Make sure you add the CoreLocation framework to your Project. You can do this in the **Mobile Notification Settings** menu in the Unity Editor (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**) Alternatively, add it manually to the Xcode project, or use the Unity Xcode API. You also need to use the [Location Service API](https://docs.unity3d.com/ScriptReference/LocationService.html) to request permission for your app to access location data.

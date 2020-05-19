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
//.f.assign other fields..
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

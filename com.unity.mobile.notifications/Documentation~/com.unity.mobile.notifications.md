

# Unity Mobile Notifications Package

The runtime API is split into two classes AndroidNotificationCenter and iOSNotificationCenter which can be used to schedule and manage notifications for Android and iOS respectively (see appropriate sections for code samples).

**Supported features:**

- Schedule local repeatable or one-time notifications.
- Cancel scheduled or already displayed notifications.
- Android: 
  - Create and modify notification channels (categories) on Android Oreo and above.
  - Set custom notification icons.
- iOS:
  - Use the Apple Push Notification Service  (APNs) to receive remote notifications.
  - Modify remote notification content if notifications are received while the app is running.
  - Group notifications into threads (only  supported on iOS 12+)

**Requirements:**

- Supports Android 4.1 (API 16)/iOS 10 or above.
- Compatible with Unity 2018.2 or above.
&nbsp;

## Android

&nbsp;

**Create a notification channel:**

Every local notification must belong to a notification channel, notification channels are only supported by the system on Android Oreo (8.0) and above. On previous versions the channel behavior is emulated by the this package. Therefore settings which were set individually for each notification before 8.0 (such as priority  (`Importance`)) should be still set on the channel even when using the package on older Android versions.

```
var c = new AndroidNotificationChannel()
{
    Id = "channel_id",
    Name = "Default Channel",
    Importance = Importance.High,
    Description = "Generic notifications",
}
AndroidNotificationCenter.RegisterNotficationChannel(c);
```
&nbsp;

&nbsp;

**Send a simple notification:**

This example shows how to schedule a simple notification with some text in it.

```

var notification = new AndroidNotification();
notificationTitle = "SomeTitle";
notification.Text = "SomeText";
notification.FireTime = System.DateTime.Now.AddMinutes(5);
```
You should specify a custom icon for each notification, otherwise a default Unity icon will be shown in the status bar instead. You can configure notification icons in "Edit -> Project Settings -> Mobile Notification Settings -> Android" (if using Unity 2018.2) or in "Edit -> Settings -> Mobile Notification Settings -> Android" (on 2018.3 and above).
```
notification.Icon = "my_custom_icon_id";
```
Optionally you can also set a large icon which will be shown in the notification view in place of the small icon (which will be placed in a small badge atop of the large icon). 
```
notification.LargeIcon = "my_custom_large_icon_id"
```
After it's scheduled each notification is assigned an unique identifier number which can later be used to track the notification's status or to cancel it.
```
var identifier = AndroidNotificationCenter.SendNotification(n, "channel_id");
```
You can check if the notification has already been delivered and perform any actions depending on the result. However notification status can only be tracked on Android Marshmallow (6.0) and above.
```
if ( CheckScheduledNotificationStatus(identifier) == NotificationStatus.Scheduled)
{
	// Replace the currently scheduled notification with a new notification.
	UpdateScheduledNotifcation(identifier, newNotification);
}
else if ( CheckScheduledNotificationStatus(identifier) == NotificationStatus.Delivered)
{
	//Remove the notification from the status bar
	CancelNotification(identifier)
}
else if ( CheckScheduledNotificationStatus(identifier) == NotificationStatus.Unknown)
{
    var identifier = AndroidNotificationCenter.SendNotification(n, "channel_id");
}
```

&nbsp;

&nbsp;

**Handling received notifications while the app is running:**

You can subscribe to the *AndroidNotificationCenter.OnNotificationReceived* event to receive a callback whenever a notification is delivered while the app is running.

```
AndroidNotificationCenter.OnNotificationReceived +=(int identifier, AndroidNotification notification, string channel)
{
	var msg = "Notification received : " + identifier + "\n";
	msg += "\n Notification received: ";
	msg += "\n .Title: " + notification.Title;
	msg += "\n .Body: " + notification.Text;
	msg += "\n .Channel: " + channel.Name;
	Debug.Log(msg);
};
```

## iOS

&nbsp;

**Request authorization:**

Request the system for permission to post local and receive remote notifications. After completion you can retrieve the *DeviceToken* if you created the request with *registerForRemoteNotifications* set to true and the app successfully registered on the APN. See [Apple Developer Site](https://developer.apple.com/library/archive/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/HandlingRemoteNotifications.html#//apple_ref/doc/uid/TP40008194-CH6-SW1) on how to use push notification to a device and on how to add push notification support to your app. 

You can only request the user to grant permission for certain notification features (in example below we requested the permission to show UI Alert dialogs and to show a badge on the app icon). However the user might change the authorization status for each notification type at any point in the settings app, you can check the actual authorization status by calling *iOSNotificationCenter.GetNotificationSettings*. 

Alternatively you can enable *Request Authorization on App Launch* in *Edit -> Project Settings -> Mobile Notification Settings* then the app will automatically request authorization when it's launched. Afterwards you might call this method again to determine the current authorization status but the UI system prompt will not be shown again if the user has already granted or denied authorization for this app.
```
using (var req = new AuthorizationRequest(AuthorizationOption.AuthorizationOptionAlert | AuthorizationOption.AuthorizationOptionBadge, true))
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
&nbsp;

&nbsp;

**To Send a Simple and Cancel Notification in X seconds:**

```
var timeTrigger = new iOSNotificationTimeIntervalTrigger()
{
	TimeInterval = new TimeSpan(0, minutes, seconds),
	Repeats = false
};
				
var notification = new iOSNotification()
{
	// You can optionally specify a custom Identifier which can later be 
	// used to cancel the notification, if you don't set one, an unique 
	// string will be generated automatically.
	Identifier = "_notification_01" 
	Title = title,
	Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
	Subtitle = "This is a subtitle, something, something important...",
	ShowInForeground = true,
	ForegroundPresentationOption = foregoungOption,
	CategoryIdentifier = "category_a",
	ThreadIdentifier = thread,
	Trigger = timeTrigger,
};
		
iOSNotificationCenter.ScheduleNotification(notification);
```
You can cancel the notification if wasn't yet triggered:
```
iOSNotificationCenter.RemoveScheduledNotification(notification.Identifier);
```
If the notification was already displayed to the user, you can remove it from the Notification Center:
```
iOSNotificationCenter.RemoveDeliveredNotification(notification.Identifier)
```
&nbsp;

&nbsp;

**Other triggers:**

Besides the time interval trigger you can also use calendar and location triggers. All the fields in iOSNotificationCalendarTrigger are optional but you need to set atleast one for it work. For example if you only set the hour and minute fields the system will automatically trigger the notification on the next specified hour and minute.

```
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

You can also create location triggers when you want to schedule the delivery of a notification when the device enters or leaves a specific geographic region. Before scheduling any notifications using this trigger, your app must have authorization to use Core Location and must have when-in-use permissions. Use the Unity LocationService API to request for this authorization. See https://developer.apple.com/documentation/corelocation/clregion?language=objc for additional information.

In this exampe the center coordinate is defined using the WGS 84 system. In this case the notification would be triggered if the user entered an area within a 250 meter radius around Eiffel Tower in Paris.

```
var locationTrigger = new iOSNotificationLocationTrigger()
{
    Vector2 Center = new Vector2(2.294498f, 48.858263f),
    Radius = 250f,
    NotifyOnEntry = true,
    NotifyOnExit = false,
}
```
&nbsp;

&nbsp;

**Handling received notifications when the app is running:**

You might want to perform a custom action instead of just showing a notification alert if it's triggered while the app is running. By default if a local notification is triggered while the app that scheduled it is
in the foreground an alert will not be shown for that notification. If you wish the notification to behave the same way as if the the app was not running you need to set the `ShowInForeground` property when scheduling the notification:

```

notification.ShowInForeground = True

// In this case you will also need to specify it's 'ForegroundPresentationOption'
notification.ForegroundPresentationOption = (PresentationOption.NotificationPresentationOptionSound | PresentationOption.NotificationPresentationOptionAlert)
```

Alternatively you might wish to perform some other action, like displaying the notification content using the in-game UI, when the notification is triggered. In this case you need to subscribe to the `OnNotificationReceived` event which will be called whenever a local or a remote notification is received (irregardless if it's shown in the foregound).

```

iOSNotificationCenter.OnNotificationReceived += notification =>
{
	var msg = "Notification received : " + notification.Identifier + "\n";
	msg += "\n Notification received: ";
	msg += "\n .Title: " + notification.Title;
	msg += "\n .Badge: " + notification.Badge;
	msg += "\n .Body: " + notification.Body;
	msg += "\n .CategoryIdentifier: " + notification.CategoryIdentifier;
	msg += "\n .Subtitle: " + notification.Subtitle;
	Debug.Log(msg);
};
```

When receiving remote notification while the app is running you might wish modify the remote notification content or not show it at all. You can do this by subscribing  to the `OnRemoteNotificationReceived` event. Please note that if you do this remotecnotifications will never be displayed when the app is running. If you still wish to show an alert for it you'll have to schedule a local notification using the remote notifications content:

```

iOSNotificationCenter.OnRemoteNotificationReceived += notification =>
{
	// When a remote notification is received modify it's contents and show it
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
		Subtitle =  "RERemote: " + notification.Subtitle,
		ShowInForeground = true,
		ForegroundPresentationOption = PresentationOption.NotificationPresentationOptionSound | PresentationOption.NotificationPresentationOptionAlert | PresentationOption.NotificationPresentationOptionBadge,
		CategoryIdentifier = notification.CategoryIdentifier,
		ThreadIdentifier = notification.ThreadIdentifier,
		Trigger = timeTrigger,
	};
	iOSNotificationCenter.ScheduleNotification(n);
			
	Debug.Log("Rescheduled remote notifications with id: " + notification.Identifier);

};
```



## FAQ
&nbsp;

**Notifications with a location trigger do not work.**

Make sure the CoreLocation framework is added to your project. You can do this in the `Mobile Notification Settings` menu in the Unity Editor. Or by adding it manually to the Xcode project (or using the Unity Xcode API). Also you need to request permission to use locaiton in your app, you can do this using the [Location Service API](https://docs.unity3d.com/ScriptReference/LocationService.html).





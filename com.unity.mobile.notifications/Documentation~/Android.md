# Android

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

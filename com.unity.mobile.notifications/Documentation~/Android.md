# Android

## Manage notification channels

Starting in Android 8.0, all notifications must be assgined to a notification channel. In the notification package, a set of APIs are provided to manage notification channels. Below is an example of how to create a notification channel.

```c#
var channel = new AndroidNotificationChannel()
{
    Id = "channel_id",
    Name = "Default Channel",
    Importance = Importance.Default,
    Description = "Generic notifications",
};
AndroidNotificationCenter.RegisterNotificationChannel(channel);
```

You can check what other properties you can set at [AndroidNotificationChannel](../api/Unity.Notifications.Android.AndroidNotificationChannel.html).

You can also delete or get a notification channel, etc. Please refer to [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html) for more notification channel related APIs.

One thing to keep in mind is you cannot change the behavior of a created notification channel, read more about these at [Android Notification Channel Document](https://developer.android.com/training/notify-user/channels).

On devices which is lower than Android 8.0, this package emulates this behavior by applying properties on notification channels like `Importance` to individual notifications.

## Manage notifications

In the notification package, a set of APIs are provided to manage notifications including sending, updating, deleting etc. Please refer to [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html) for more notification related APIs.

### Send a simple notification

The below example shows how to schedule a simple text notification with the notification channel created in the previous step.

```c#
var notification = new AndroidNotification();
notification.Title = "Your Title";
notification.Text = "Your Text";
notification.FireTime = System.DateTime.Now.AddMinutes(1);

AndroidNotificationCenter.SendNotification(notification, "channel_id");
```
You can check what other properties you can set at [AndroidNotification](../api/Unity.Notifications.Android.AndroidNotification.html).

### Set icons

You can set a custom icon as small icon for each notification. If you don't specify any icons as small icon, the default application icon will be used instead. You can optionally set a large icon which also displays in the notification drawer. You can configure icons in the notification settings, please refer to [Notification Settings](Settings.html) for more info.

Below is an example shows how to set the small and large icons with the icon ids you set in the notification settings.

```c#
notification.SmallIcon = "my_custom_icon_id";
notification.LargeIcon = "my_custom_large_icon_id";
```

### Notification id

Usually Unity generates a unique id for each notification after you schedule it. The below is an example shows how to get the generated notification id.

```c#
var id = AndroidNotificationCenter.SendNotification(notification, "channel_id");
```

You can use this id to track, cancel or update the notification. The following example shows how to check the notification status and perform any actions depending on the result. Notification status tracking only works on Android 6.0 Marshmallow and above.

```c#
var notificationStatus = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);

if (notificationStatus == NotificationStatus.Scheduled)
{
    // Replace the scheduled notification with a new notification.
    AndroidNotificationCenter.UpdateScheduledNotification(id, newNotification, "channel_id");
}
else if (notificationStatus == NotificationStatus.Delivered)
{
    // Remove the previously shown notification from the status bar.
    AndroidNotificationCenter.CancelNotification(id);
}
else if (notificationStatus == NotificationStatus.Unknown)
{
    AndroidNotificationCenter.SendNotification(newNotification, "channel_id");
}
```

You can also set your own notification id explicitly.

```c#
var notificationID = 10000;
AndroidNotificationCenter.SendNotificationWithExplicitID(notification, "channel_id", notificationId);
```
And this API can be used to update a delivered notification with the same id.

### Notification received callback

You can subscribe to the `AndroidNotificationCenter.OnNotificationReceived` event to receive a callback after a notification is delivered while your app is running.

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

### Store and retrieve custom data

You can store arbitrary string data on the notification with the `IntentData` property, and retrieve it when the user taps the notification to open the app.

```c#
var notification = new AndroidNotification();
notification.IntentData = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
 AndroidNotificationCenter.SendNotification(notification, "channel_id");
```

If the user taps the notification to open the app, you can retrieve it any and any data it has assigned to it like this:

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

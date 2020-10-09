# Android notifications

## Manage notification channels

Starting in Android 8.0, all notifications must be assigned to a notification channel. The Unity Mobile Notifications package provides a set of APIs to manage notification channels. The example below shows how to create a notification channel.

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

For details about other properties you can set, see [AndroidNotificationChannel](../api/Unity.Notifications.Android.AndroidNotificationChannel.html).

You can also perform other actions on notification channels, such as get or delete. For more notification channel related APIs, see [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html).

After you create a notification channel, you can't change its behavior. For more information, see Android developer documentation on [creating and managing notification channels](https://developer.android.com/training/notify-user/channels).

On devices that use Android versions prior to 8.0, this package emulates the same behavior by applying notification channel properties, such as `Importance`, to individual notifications.

## Manage notifications

This package provides a set of APIs to manage notifications. These APIs allow you to perform actions such as sending, updating, and deleting notifications. For more notification-related APIs, see [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html).

### Send a simple notification

The example below shows how to schedule a simple text notification on the notification channel created in the previous step.

```c#
var notification = new AndroidNotification();
notification.Title = "Your Title";
notification.Text = "Your Text";
notification.FireTime = System.DateTime.Now.AddMinutes(1);

AndroidNotificationCenter.SendNotification(notification, "channel_id");
```
For details about other properties you can set, see [AndroidNotification](../api/Unity.Notifications.Android.AndroidNotification.html).

### Set icons

You can set a custom icon as a small icon to display for each notification. If you don't specify any small icons, notifications will display the default application icon instead. You can optionally set a large icon which also displays in the notification drawer. You can configure icons in the notification settings; for more information, see [Notification Settings](Settings.html#custom-icons).

The example below shows how to set the small and large icons with the icon ids you set in the notification settings.

```c#
notification.SmallIcon = "my_custom_icon_id";
notification.LargeIcon = "my_custom_large_icon_id";
```

### Notification id

Usually, Unity generates a unique id for each notification after you schedule it. The example below shows how to get the generated notification id.

```c#
var id = AndroidNotificationCenter.SendNotification(notification, "channel_id");
```

You can use this id to track, cancel, or update the notification. The following example shows how to check the notification status and perform any actions depending on the result. Notification status tracking only works on Android 6.0 Marshmallow and above.

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
You can use this API to update a delivered notification with the same id.

### Notification received callback

You can subscribe to the [AndroidNotificationCenter.OnNotificationReceived](../api/Unity.Notifications.Android.AndroidNotificationCenter.html#Unity_Notifications_Android_AndroidNotificationCenter_OnNotificationReceived) event to receive a callback after a notification is delivered while your app is running.

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

You can store arbitrary string data on the notification with [AndroidNotification.IntentData](../api/Unity.Notifications.Android.AndroidNotification.html#Unity_Notifications_Android_AndroidNotification_IntentData), and retrieve it when the user taps the notification to open the app.

```c#
var notification = new AndroidNotification();
notification.IntentData = "{\"title\": \"Notification 1\", \"data\": \"200\"}";
AndroidNotificationCenter.SendNotification(notification, "channel_id");
```

If a user taps the notification to open the app, you can get the notification and retrieve any data assigned to it like in the example below.

```c#
var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();
if (notificationIntentData != null)
{
    var id = notificationIntentData.Id;
    var channel = notificationIntentData.Channel;
    var notification = notificationIntentData.Notification;
}
```

If the app is opened in any other way, [AndroidNotificationCenter.GetLastNotificationIntent](../api/Unity.Notifications.Android.AndroidNotificationCenter.html#Unity_Notifications_Android_AndroidNotificationCenter_GetLastNotificationIntent) returns null.

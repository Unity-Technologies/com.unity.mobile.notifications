# Android notifications

## Manage notification channels

Starting in Android 8.0, all notifications must be assigned to a notification channel. The Unity Mobile Notifications package provides a set of APIs to manage notification channels. The example below shows how to create a notification channel.

Notification channels can be grouped together. This is not required, but it may look better in the Settings UI.

```c#
var group = new AndroidNotificationChannelGroup()
{
    Id = "Main",
    Name = "Main notifications",
};
AndroidNotificationCenter.RegisterNotificationChannelGroup(group);
var channel = new AndroidNotificationChannel()
{
    Id = "channel_id",
    Name = "Default Channel",
    Importance = Importance.Default,
    Description = "Generic notifications",
    Group = "Main",  // must be same as Id of previously registered group
};
AndroidNotificationCenter.RegisterNotificationChannel(channel);
```

For details about other properties you can set, see [AndroidNotificationChannel](../api/Unity.Notifications.Android.AndroidNotificationChannel.html).

You can also perform other actions on notification channels, such as get or delete. For more notification channel related APIs, see [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html).

After you create a notification channel, you can't change its behavior. For more information, see Android developer documentation on [creating and managing notification channels](https://developer.android.com/training/notify-user/channels).

On devices that use Android versions prior to 8.0, this package emulates the same behavior by applying notification channel properties, such as `Importance`, to individual notifications.

## Schedule notifications at exact time

Before Android 6.0 notifications can be scheduled only at approximate time.

Since Android 12.0 (API level 31) android.permission.SCHEDULE_EXACT_ALARM permission has to be added to the manifest to enable exact scheduling, see [documentation](https://developer.android.com/reference/android/Manifest.permission#SCHEDULE_EXACT_ALARM). Note, that adding this permission does not guarantee you'll be able to use exact scheduling. You can check it by calling AndroidNotificationCenter.UsingExactScheduling. You may need to request user permission to schedule at exact times by calling AndroidNotificationCenter.RequestExactScheduling().

Since Android 13.0 (API level 33) the android.permission.USE_EXACT_ALARM is and alternative permission to enable exact scheduling.

On devices with Android version less than 12 and battery saving on, exact scheduling may not work. It can be improved by requesting user to bypass battery saving via AndroidNotificationCenter.RequestIgnoreBatteryOptimizations() (query it via AndroidNotificationCenter.IgnoringBatteryOptimizations). To be able to request it, you need the [permission](https://developer.android.com/reference/android/Manifest.permission#REQUEST_IGNORE_BATTERY_OPTIMIZATIONS).

Android recommends to not use exact scheduling due to higher power consumption it entails. Use notification settings in Unity to enable the automatic addition of the mentioned permissions or to always use inexact scheduling.

> [!NOTE]
> Google recommends applications that require exact scheduling as a key feature to declare **SCHEDULE_EXACT_ALARM** and **USE_EXACT_ALARM** permissions. Applications that do not require exact scheduling and still declare these permissions are prohibited to publish on Google Play. For more information, refer to Google's policy on restricted permission requirement for [**Exact Alarm API**](https://support.google.com/googleplay/android-developer/answer/13161072?sjid=4970395232627262797-EU).

## Request permission to post notifications

Starting with Android 13.0 (API level 33) notifications cannot be posted without user's permission. They can still be scheduled, but will work silently with no UI shown to the user. You can request the permission by running this method in the [coroutine](https://docs.unity3d.com/6000.0/Documentation/Manual/Coroutines.html):

```c#
IEnumerator RequestNotificationPermission()
{
    var request = new PermissionRequest();
    while (request.Status == PermissionStatus.RequestPending)
        yield return null;
    // here use request.Status to determine user's response
}
```

The coroutine allows you to asynchronously wait until the user responds to the permission request. You can use `request.Status` to check whether the user has granted or denied the permission and proceed with posting notifications accordingly.

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

### Send notification with big picture style

BigPictureStyle is a predefined notification style centered around an image. Unity supports picture specified as resource ID, file path or URI. URI must be one of the type supported by Android. File path must be an absolute path on file system (note, that streaming assets on Android are inside .apk and accessed via URI, not path).

Below is a simple example for downloading image from the internet and sending the notification with it.

```c#
IEnumerator DownloadAndShow(string url)
{
    var path = Path.Combine(Application.persistentDataPath, "image.jpg");
    using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
    {
        uwr.downloadHandler = new DownloadHandlerFile(path);
        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(uwr.error);
            yield break;
        }
    }

    var notification = new AndroidNotification("Image", "Downloaded image", DateTime.Now);
    notification.BigPicture = new BigPictureStyle()
    {
        Picture = path,
    };
    AndroidNotificationCenter.SendNotification(notification, ChannelId);
}
```

### Set icons

You can set a custom icon as a small icon to display for each notification. If you don't specify any small icons, notifications will display the default application icon instead. You can optionally set a large icon which also displays in the notification drawer. You can configure icons in the notification settings; for more information, see [Notification Settings](Settings.md#custom-icons).

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

If app is opened via notification and then sent to background, bringing it back to foreground from recent app list is considered resume, so app is still opened by tapping notification. If app receives multiple notifications, they can get grouped by the Operating System. In such case no notification will be available even if app was opened by tapping such grouped notification.

If the app is opened in any other way, [AndroidNotificationCenter.GetLastNotificationIntent](../api/Unity.Notifications.Android.AndroidNotificationCenter.html#Unity_Notifications_Android_AndroidNotificationCenter_GetLastNotificationIntent) returns null.

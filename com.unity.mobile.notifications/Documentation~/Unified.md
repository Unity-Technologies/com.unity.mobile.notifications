# Unified APIs for Android and iOS

Mobile notifications package provides separate APIs to schedule notifications on Android and iOS platforms. Unified APIs combine the most commonly used notification APIs for both Android and iOS platforms. This gives you a convenient way to schedule notifications without using platform specific code.

## Prerequisites for sending notifications

There are a few prerequisites before you can send notifications using Unified APIs:

* Android 8.0 and above require a notification channel to send notifications to.
* Android 13 and above require user’s permission to send notifications. The application must target Android 13 to be able to request user’s permission, otherwise the operating system requests user’s permission on behalf of the application.
* iOS requires user’s permission to send notifications.

## Initialize the system

The [NotificationCenter](../api/Unity.Notifications.NotificationCenter.html) class is your entry point to the package functionality. The first step is to initialize the system.



```c#
var args = NotificationCenterArgs.Default;
args.AndroidChannelId = "default";
args.AndroidChannelName = "Notifications";
args.AndroidChannelDescription = "Main notifications";
NotificationCenter.Initialize(args);
```


`NotificationCenterArgs.Default` pre-populates `NotificationCenterArgs` with the recommended default values, so you only need to modify the ones that you need. `AndroidChannelId` is the only required property to set. On Android, this property only specifies which channel to send notifications to, assuming the channel either already exists, or will be created later using [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html) directly. If you also set `AndroidChannelName` and `AndroidChannelDescription` properties, the channel gets created when the system initializes. You only need to initialize the system once at any point in your application.



## Handle user permission

After initializing `NotificationCenter`, you can request the user’s permission to send notifications.

The following code example runs a coroutine until the permission is granted or denied. The status of the request indicates if the permission has been already granted, permanently denied, or is not required at all.

```c#
IEnumerator Start()
{
    var request = NotificationCenter.RequestPermission();
    if (request.Status == NotificationsPermissionStatus.RequestPending)
        yield return request;
    Debug.Log("Permission result: " + request.Status);
}
```

`NotificationsPermissionStatus.RequestPending` is the only status that indicates the actual permission request to the user. Returning the permission request object, the coroutine suspends the coroutine until the user responds to the request.


## Schedule notifications

As a starting point, you must fill the [Notification](../api/Unity.Notifications.Notification.html) structure with the content you want to display in the notification.


```c#
var notification = new Notification()
{
    Title = "Title text",
    Text = "Main text",
};
```

You can set more properties. `Title` and `Text` are the two properties that indicate the actual notification you want to display. Without these properties notification doesn't have much use to the user.

Usually, you would want to display the notification at a later time. By default, a notification is not displayed right away when you send it. Also, a notification is not displayed at a scheduled time if your application is in the foreground. You can set a notification to be displayed at all times.

### Schedule notifications at specific time

The following code example demonstrates scheduling a notification to be displayed at the same time tomorrow:

```c#
var when = System.DateTime.Now.AddDays(1);
NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(when));
```

You can use the current time to display the notification right away. You can also schedule repeated notifications, however, frequent notifications may cause inconvenience to the users.

### Schedule notifications after specific time

The following code example demonstrates scheduling a notification to be displayed three hours from now:

```c#
NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule(TimeSpan.FromHours(3)));
```

You can set a repeated notification schedule, in which case the notification is repeated every three hours. However, be aware that repeated notifications may cause inconvenience to the users. Make sure to set up notification cancelation when using repeated notifications.


## Update or cancel notifications

From cancelation perspective, there are two notification status: delivered and scheduled. Delivered notifications are the notifications that are already displayed (On iOS, the users can choose to only display notifications in the scheduled summary). Scheduled notifications are set to display in the future. If a notification is set to repeat, it may be both scheduled and delivered at the same time.

When an application is opened, it is common to remove displayed notifications as the notification is usually an invitation back into the application. You may want to cancel the scheduled notifications and schedule a new notification at an updated time. The following code example demonstrates how to cancel the notifications:

```c#
NotificationCenter.CancelAllScheduledNotifications();
NotificationCenter.CancelAllDeliveredNotifications();
```

You can also cancel individual notifications. The recommended practice is to give each distinct notification a unique integer ID and assign it to the `Identifier` property when creating that notification. Otherwise a unique ID is generated and returned when you call `NotificationCenter.ScheduleNotification`. In this case, you must save the ID, as your application may get terminated before the notification is displayed.

The following code example demonstrates how to cancel individual notification using the ID:

```c#
NotificationCenter.CancelScheduledNotification(id);
NotificationCenter.CancelDeliveredNotification(id);
```

To update an existing notification, schedule a notification with the same ID. This action replaces the previously scheduled notification.

## Receive notifications


If you want to check whether your application is launched by tapping a notification, you can query the last responded notification using the following code example:

```c#
var notification = NotificationCenter.LastRespondedNotification;
if (notification.HasValue)
{
    var lastNotification = notification.Value;
}
```

This also works if you send notification to the background and tap the notification to bring it back to foreground. You can query the properties of returned notification.

If you want to receive notifications that are displayed while your application is in the foreground (by default such notifications aren't presented to the user), you must subscribe to an event, as demonstrated in the following code example:

```c#
void Start()
{
    NotificationCenter.OnNotificationReceived += NotificationReceived;
}

void NotificationReceived(Notification notification)
{
    Debug.Log($"Received notification with title: {notification.Title}");
}
```

## Advanced features

You can manage notifications with the advanced features such as categories and groups. You can also present notifications by setting up badges, alerts, and device sound.
### Categories and groups

The terms Category and Group indicate different things on Android and iOS.

**Terminology differences**:

* **Category** - a channel on Android, a category on iOS.
* **Group** - a group on Android, a thread on iOS.

By default, notifications have no category on iOS, while on Android they are sent to the default channel that you set when initializing the system. There is a separate overload of `ScheduleNotification` taking category as a parameter, that sends notification to the provided category or channel respectively. To create the [category](xref:Unity.Notifications.iOS.iOSNotificationCenter.SetNotificationCategories(System.Collections.Generic.IEnumerable{Unity.Notifications.iOS.iOSNotificationCategory})) or [channel](xref:Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(Unity.Notifications.Android.AndroidNotificationChannel)), you must use platform specific APIs.

Grouping can visually stack multiple related notifications together. Both Android and iOS support grouping (not in all versions or devices). On Android, there is a separate feature called group summary, which is a separate notification that summarizes the group. The operating system may not display group summary, if the group includes a small number of notifications.

On iOS, there is no group summary. The notification marked as summary is just another notification in the thread. You can group the notifications by assigning the same unique string identifier for a group on each notification (with separate property to mark as summary). For more information, refer to API documentation for [Notification](../api/Unity.Notifications.Notification.html).


### Alerts, sounds, and badges

There are different ways a notification can appear on the user’s device screen, such as, just appearing in the app tray or immediately displaying a notification popup with the device sound and vibration. The user can disable more intrusive ways.

When requesting permission on iOS, the user can make choices for alert, sound, and badge on application launcher. On Android, the notification channels are assigned importance levels.

You can choose the required features by setting `NotificationCenterArgs.PresentationOptions`. To take effect, you must create Android notification channel during initialization and request for permission using `NotificationCenter` on iOS. If you use platform specific APIs to create channels and request for permission, `NotificationCenterArgs.PresentationOptions` setting has no effect.

**Note**: On some devices, certain features may not be available or are disabled by default. For example, some Android devices disable alerts by default. On such devices, users can enable alerts in the notification settings.

Badges are visuals on app launchers. It can be a simple dot or a circle with a number. Some devices may not support badges or users can disable badges in settings. When a badge is enabled, it is usually displayed when the application delivers notifications or displays a number of notifications. The application can override the number using `Notification` struct `Badge` property.

**Note**: If you set the badge manually, you must also remove it by calling `NotificationCenter.ClearBadge()` (Unlike iOS, the badge is removed automatically on Android when notifications are canceled).

You can open the application settings by calling `NotificationCenter.OpenNotificationSettings`. This way you can request the user to enable notifications if they are previously denied and control particular features, such as alert or device vibration.

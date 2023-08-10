# Using notifications

## Different APIs available

Notification functionality is very different on Android and iOS. In order to provide features and for convenience, the package provides three sets of APIs:

* Unified APIs that provided only the common functionality in *Unity.Notifications* namespace, described bellow.
* [Android](Android.md) specific APIs in *Unity.Notifications.Android* namespace.
* [iOS](iOS.md) specific APIs in *Unity.Notifications.iOS* namespace.

## Preparing for sending notifications

There are few prerequisites before you can send notifications:
* Android 8.0 and newer requires notification channel to send notification to.
* Android 13 and newer requires users permission to send notifications. The application must target Android 13 to be able to request it, otherwise OS asks users permission on apps behalf.
* iOS requires users permission to send notifications.

### Initialization

The [NotificationCenter](../api/Unity.Notifications.NotificationCenter.html) class is your gate to package functionality. The first thing to do is to initialize the system:

```c#
var args = NotificationCenterArgs.Default;
args.AndroidChannelId = "default";
args.AndroidChannelName = "Notifications";
args.AndroidChannelDescription = "Main notifications";
NotificationCenter.Initialize(args);
```

*NotificationCenterArgs.Default* gives you NotificationCenterArgs prefilled with recommended default values, so you only need to modify the ones that you need. The only required property to set is *AndroidChannelId*, however, it only specifies which channel to send notifications to on Android, assuming the channel either already exists, or will be created later using [AndroidNotificationCenter](../api/Unity.Notifications.Android.AndroidNotificationCenter.html) directly. If you also set *AndroidChannelName* and *AndroidChannelDescription*, then then channel itself will be created when the system is initialized. You only need to initialize once at any point in your app.

### Handling user permission

Usually, after initializing *NotificationCenter* you want to request user permission to send notifications:

```c#
IEnumerator Start()
{
    var request = NotificationCenter.RequestPermission();
    if (request.Status == NotificationsPermissionStatus.RequestPending)
        yield return request;
    Debug.Log("Permission result: " + request.Status);
}
```

The code sample above runs a coroutine until permission is granted or denied. If permission has been already granted, permanently denied or is not required at all, the status of request will reflect that. *NotificationsPermissionStatus.RequestPending* is the only status, that indicates an actual permission request to the user. Returning permission request object the coroutine will suspend the coroutine until user responds to request.

## Scheduling notifications

First you need to fill the [Notification](../api/Unity.Notifications.Notification.html) structure with what you want to show in the notification:

```c#
var notification = new Notification()
{
    Title = "Title text",
    Text = "Main text",
};
```

There are more properties you can set, but Title and Text are the two without which notification does not have much use to user.

Usually you want to show notification at some later time. By default sending notification right away will not display it, nor it will be shown if on the time it was meant to be displayed the application happens to be in foreground. You can set notification to always be displayed.

### Scheduling on particular time

Bellow example schedules notification to be shown at the same time Tomorrow:

```c#
var when = System.DateTime.Now.AddDays(1);
NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(when));
```

Using current time will show it right away. You can also set notification on a repeating schedule, but you have to careful justify that, as people may not be happy with frequent notifications.

### Scheduling after specific time

The example bellow schedules notification to be shown three hours from now:

```c#
NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule(TimeSpan.FromHours(3)));
```

Schedule can also be set to repeat, then notification will be repeated every three hours. You have to be careful with that, as users may not like it. Make sure to setup notification cancelation when using repeated notifications.

## Updating or canceling notifications

From cancelation perspective there are two types of notifications: scheduled and delivered. Delivered notification are the ones that have been displayed already (on iOS user can choose to only show them in scheduled summary). Scheduled notifications are set to happen in the future. If notification is set to repeat, it may be both scheduled and delivered at the same time.

When app is opened, it is common to remove displayed notifications, given that notification is usually an invitation back into an app. Sometimes it is desirable to cancel the scheduled one as well and schedule new ones on an updated shedule. To do this is very straight-forward:

```c#
NotificationCenter.CancelAllScheduledNotifications();
NotificationCenter.CancelAllDeliveredNotifications();
```

It is a bit more difficult to cancel individual notification. The recommendation is to give each distinct notification a unique integer ID and assign it to the *Identifier* property when creating notification. Otherwise a unique ID will be generated and returned when calling *NotificationCenter.ScheduleNotification*, which you then would have to save somewhere, as your application may be terminated before notification arrives. When you have the ID, canceling notification is trivial:

```c#
NotificationCenter.CancelScheduledNotification(id);
NotificationCenter.CancelDeliveredNotification(id);
```

In order to update existing notification simply schedule a notification with the same ID, it will replace the previously scheduled one.

## Receiving notifications

In you want to check whether your application was launched by tapping a notification, query last responded notification:

```c#
var notification = NotificationCenter.LastRespondedNotification;
if (notification.HasValue)
{
    var lastNotification = notification.Value;
}
```

This also works if you send notification to background and tap the notification to bring it back into foreground. You can query the properties of returned notification.

In order to receive notifications that happen while your app is in foreground (by default they are not shown to the user), you need to subscribe to an event:

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

### Terminology differences

* **Category** - a channel on Android, a category on iOS.
* **Group** - a group on Android, a thread on iOS.

### Using categories, groups

By default notifications have no category on iOS, while on Android they are sent to the default channel that you've set when initializing. There is a separate overload of *ScheduleNotification* taking category as a parameter, that will send notification to provided category or channel respectovely. Creating that category or channel is out of scope, you have to use platform specific APIs to do it.

Both Android and iOS support grouping (not in all versions or devices). Grouping can visually stack multiple notifications together. On Android there is a separate feature called group summary, a separate notification that summarizes the group, though is may not be shown if OS does not consider the number of notifications big enouth. There is no such summary on iOS, so notification marked as summary will be just another notification in the thread. You group the notifications by assigning the same unique string identifier for a group on each notification (with separate property to mark as summary). See API documentation for [Notification](../api/Unity.Notifications.Notification.html) to learn more.

### Alerts, sounds and badges

The are different ways for notification to inform the user about it's arrival, ranging from just appearing in the tray to immediately showing a notification popup on screen with the sound and the phone vibrating. More intrusive ways can be disabled by user.

iOS gives individual choices for altert, sound and badge on applications launcher when requesting for permission. Android instead has importance level on notification channel.

In the simplest form you can choose the desired features by setting *NotificationCenterArgs.PresentationOptions*. To take effect, this requires that you create Android notification channel during initialization and request for permission using *NotificationCenter* on iOS. If you use platform specific APIs to create channels and request for permission, these options have no effect. Note, that on some devices certain features may be unavailable or disabled by default (for example some Android phones disable alterts by default, user has to go no notification settings and enable them).

Badges are visuals on app launchers, can be a simple dot or a circle with a number. Some device may not support them and they can be disabled in settings. When badge is enabled, it is usually shown when app has delivered notifications or shows a number of them. Application can override the number, to facilitate that, *Notification* struct has property *Badge*. Note, that if you set the badge manually, you also have to remove it by calling *NotificationCenter.ClearBadge()* (this method does nothing on Android).

By calling *NotificationCenter.OpenNotificationSettings* you can open settings app. This way you can request user to enable notifications when they were previously denied and control particular features, such as alerts or vibration.

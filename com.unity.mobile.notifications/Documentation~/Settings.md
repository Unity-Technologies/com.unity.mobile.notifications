# Notification Settings

You can access `Notification settings` by menu `Edit > Project Settings -> Mobile Notifications`, where you can control the behavior of the notification package to some extent.

## Android

### Reschedule Notifications on Device Restart 

By default scheduled notifications will be removed after device restarts. To preserve scheduled notifications after the device restarts, you need to check the `Reschedule Notifications on Device Restart` setting. This adds the `RECEIVE_BOOT_COMPLETED` permissions to your app's manifest.

### Custom Activity

You can check `Use Custom Activity` to override the activity which will be opened when the user taps the notification, and `UnityPlayerActivity` will be used by default.

### Custom Icons

Add custom icons to this list which can be used for notifications. Whenever you schedule notifications in your script, use the icon IDs you define in the list.

## iOS

### Request Authorization on App Launch

Normally you can request authorization in your script, check [the example](iOS.html#RequestAuthorizationExample) for details.

Alternatively, you can check `Request Authorization on App Launch` which makes the app automatically request the authorization when the user launches the app.

### Enable Push Notifications

You can check `Enable Push Notifications` to add the push notification capability to the Xcode project.

Also you need to check it to retrieve the device token from an [AuthorizationRequest](../api/Unity.Notifications.iOS.AuthorizationRequest.html).

### Include CoreLocation Framework

You need to check `Include CoreLocation Framework` to use the [iOSNotificationLocationTrigger](../api/Unity.Notifications.iOS.iOSNotificationLocationTrigger.html). as it will add the `CoreLocation` framework to your Xcode project.
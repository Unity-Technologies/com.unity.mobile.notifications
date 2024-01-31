# Notification settings

From the **Notification Settings** window, you can control this package's behavior. Access this window from Unity's main menu: **Edit &gt; Project Settings &gt; Mobile Notifications**.

## Android

The following settings are available for Android notifications.

### Reschedule Notifications on Device Restart

By default, scheduled notifications are removed after the device restarts. To preserve scheduled notifications after the device restarts, you need to enable the **Reschedule Notifications on Device Restart** option. This adds the `RECEIVE_BOOT_COMPLETED` permissions to your app's manifest.

### Custom Activity

You can enable the **Use Custom Activity** option to override the activity that opens when the user taps the notification. By default, your app will only use the active activity in your app. If more than one activity is available, it will either use `UnityPlayerActivity` or `UnityPlayerGameActivity` (Starting from Unity 2023.1), depending on their availability.

### Custom Icons

Add custom icons your app can use for notifications. Whenever you schedule notifications in your script, use the icon ids you define in the list.

## iOS

The following settings are available for iOS notifications.

<a name="request-authorization"></a>
### Request Authorization on App Launch

You can configure your app to request authorization in one of two ways:

- From script (see this [example](iOS.html#authorization-request) for details).
- By enabling the **Request Authorization on App Launch** option, which makes the app automatically request the authorization when the user launches the app.

<a name="enable-push-notifications"></a>
### Enable Push Notifications

You can enable the **Enable Push Notifications** options to add the push notification capability to the Xcode project.

You also need to enable this option to retrieve the device token from an [AuthorizationRequest](../api/Unity.Notifications.iOS.AuthorizationRequest.html).

### Include CoreLocation Framework

You must enable the **Include CoreLocation Framework** option to use the [iOSNotificationLocationTrigger](../api/Unity.Notifications.iOS.iOSNotificationLocationTrigger.html). This option adds the `CoreLocation` framework to your Xcode project.

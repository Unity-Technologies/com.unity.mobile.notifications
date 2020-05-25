# Notification Settings

## Android

### Preserve scheduled notifications after the device restarts

By default, apps remove scheduled notifications when the device restarts. To automatically reschedule all notifications when the user turns the device back on, enable the **Reschedule Notifications on Device Restart** setting in the **Project Settings** window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**). This adds the `RECEIVE_BOOT_COMPLETED` permissions to your app's manifest.

**Project Settings **window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**). Whenever you schedule a notification in your script, use the icon ID you define in the **Mobile Notification Settings** window.

## iOS

### Enable Push Notifications <a name="EnablePushNotifications"/>

Alternatively, you can enable the **Request Authorization on App Launch **setting in the **Project Settings **window (menu: **Edit** > **Project Settings** > **Mobile Notification Settings**), which makes the app automatically show a permissions request dialog when the user launches the app. Afterwards, you can call this method again to determine the current authorization status. The permissions request dialog won’t display again if the user has already granted or denied authorization.

### Include CoreLocation Framework <a name="IncludeCoreLocation"/>
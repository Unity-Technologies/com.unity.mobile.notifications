# Changelog

All notable changes to this package will be documented in this file.



## [1.0.3] - 2019-08-21

- [iOS] It's no longer necessary to enable the `Request Authorization on App Start` setting in UI to be able to turn on  `Enable Push Notifications`.
- [iOS] `GetLastRespondedNotification` should now properly return the notification used to open the app even if "Request Authorization on App Start" is turned off. 
- [iOS] `iOSNotification.data` field should now work properly with all valid JSON structures and not only with dictionaries and strings.
- [Android] Added a temporary fix for an IL2CPP compilation issue on Unity 2019.2 and above.
- Fixed an issue with NUnit Test assemblies not being detected correctly due to which exceptions were thrown in the editor.
- [Android][1165178](https://issuetracker.unity3d.com/product/unity/issues/guid/1165178/): An Android JAVA exception should no longer be thrown when attempting to schedule more than 500 notifications on Samsung devices. Samsung seems to impose a fixed limit of concurrent Alarms so if the limit is reached all attempts to schedule new notifications will be ignored until the currently scheduled ones are triggered or manually cancelled.
- [[1114987](https://issuetracker.unity3d.com/product/unity/issues/guid/1114987/)]  Reopening the project should no longer override Mobile Notification Settings.
- [iOS] Fixed an issue with `iOSNotification.data` not being set correctly for remote notifications if the data field is not a string. It will now return a full JSON string for the data field.
- [Android] Notifications cancelled using `CancelScheduledNotification` or `CancelAllScheduledNotifications` should no longer be recreated on device restart if the device is restarted before the time they were supposed to be triggered. 

### Changes & Improvements:

- [Android] `OnNotificationReceived`is now executed on the main Unity thread so it should now be possible to directly call Unity API methods the only work on the main thread from delegates and functions that subscribe to the this method.

- [iOS] Turning on `Enable Push Notifications` will add the `remote-notification ` setting to `UIBackgroundModes` array in the app’s `info.plist` file.

- [iOS] The notification returned by `iOSNotificationCenter.GetLastRespondedNotification()` is now cleared each time the app is moved to the background and not only when the app is fully terminated. Now it should only return the notification used to open the app or the last notification activated by the user while the app was running in the foreground.

- [Android] Added an option to override the Android  app activity which should be opened when a notification is clicked. By default the main activity assigned to the `UnityPlayer` Java class will be used.

- Exposed notification settings (previously only accessible in UI) in a public Editor API (see the `Unity.Notifications.UnityNotificationSettings`) class.

- [Android] Increased the minimum requirements to Android 4.4 (API 19)


## [1.0.2] - 2019-07-01

### Fixes:

- [iOS] Querying notification settings on iOS 10 will no longer cause a crash.

- [Android] Changing notification icon color now works.

- Minor UI improvements.

- Documentation improvements.


## [1.0.0] - 2019-05-20

Includes all changes from previous preview releases.

### Fixes:

- [Android] Notification API Java classes are no longer stripped when building with Proguard enabled.

- [Editor] Editor settings window should not automatically detect changes to Android icon source texture assets.

- [Editor] The settings window should work properly  and no longer throw exceptions when opening a project used with a previous version of the package. 

- [iOS] `AuthorizationRequest` should properly finish even when the user denies the request.

- [Android] Canceling scheduled notifications now works correctly when the app is restarted.

- [iOS] Subscribing to OnNotificationReceived on iOS should now work, even if no other notification was called 

- Notification settings should no longer dissappear if Unity is closed while settings editor screen is not opened.

- Compatibility fixes for Unity 2019.2.

- Fixed warning messages that were being thrown after importing the package on 2018.2.

- Compatibility fix for Unity 2019.3.

- Fixed issues with editor scripts and .asmdef files on Unity 2018.2.

- Made CancelDisplayedNotification public.

- Fixed  an editor script issue on 2018.3.


### Improvements & changes:

- [Android] Added support for notification groups:

  - Set `Group` property to group multiple notifications in a single thread.
  - Enable `GroupSummary`on a notification to use it as the summary notification for it's group.
  - `GroupAlertBehaviour`  can be used to override the alert behaviour for all notifications in a group. 

- [Android] Added a `SortKey` property for Android.

  - Used to lexicographically order this notification among other notifications from the same package or the same notification group.

- [Android] Changed ‘GetLastIntentData’ to ‘GetLastNotificationIntent’, it nows returns a `AndroidNotificationIntentData` object (which encapsulates the received `AndroidNotification` and it's `Channel` and `Id` fields) instead of just a string:

  - `OnNotificationReceived` now returns `AndroidNotificationIntentData`.
  - Arbitrary data can be stored in the `AndroidNotification.intentData` field.

- [iOS] Added `GetLastNotification()` to `iOSNotificationCenter` :

  - Can be used to retrieve the notification which was used to open the app.
  - If any new notifications are received while the app is active they will override the original one.

- [iOS] Exposed additional `iOSNotificationSettings` properties:

  - ShowPreviewsSetting`indicates whether the app can a preview of the notification's contenton the lock screen.
  - `AlertStyle` indicates the type of alerts the user has authorized (`Banner`, `Alert` or `None`). 

- [Android] Added an option to reshedule all non expired notification on device restart.

- [Android] Added a `data` field to notification object and a method to retrieve the `data` assigned to a notification that was used to open the app or bring it back from background.

- Changed the minimum Unity version supported by the package to 2018.3.

- Added an option to not add Remote Notifications capability to the Xcode project. 

- Allow sending notifications without title or body. Allows scheduling sounds only notifications that do not have an alert or are shown in the notification center.

  ## 
# Changelog

All notable changes to this package will be documented in this file.

## [2.3.2] - 2024-02-09

### Fixes:
- [Android] - Remove excessive permissions and metadata from default manifest (auto-add them based on settings).
- [Android] - Fixed notification status reporting after dismissing notification and re-launching the app.
- [iOS] - Fixed a possible crash when sending push notifications with attachment (like image).

## [2.3.1] - 2023-12-11

### Fixes:
- [Android] - Mark Android library to be compatible only with Android.
- [iOS] - Calendar trigger will use local or UTC time depending on which one is set on trigger.
- Various documentation fixes.

## [2.3.0] - 2023-10-09

### Changes & Improvements:

- Added unified APIs for basic notifications that work on both platforms in the `Unity.Notifications` namespace.
- Sample project modified to use unified APIs.
- [Android] - Package now placed to Gradle project as a separate module (.androidlib).
- [iOS] - Added APIs for checking and unregistering for push notifications.
- [iOS] - Added new presentation options, supported since iOS 14 (list and banner).

### Fixes:
- [Android] - [issue 290](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/290) Fix large icon not displayed when privided as file path.

## [2.2.2] - 2023-09-07

### Changes & Improvements:
- [Android] - Reschedule after reboot will send all notifications that expired less than 10 minutes ago.
- [iOS] - Remote notifications now support showInForeground key (allows to show notification while app is the foreground).

### Fixes:
- [Android] - [issue 271](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/271) Fix possible ANR when sending/rescheduling after reboot.
- [iOS] - Remote notification presentation options work regardless of when authorization is performed.
- [iOS] - Remote notifications have presentation independent of whether callback is registered or not.

## [2.2.1] - 2023-07-17

### Fixes:

- Changing settings will make settings file editable if read-only by version control.
- Documentation updates.

## [2.2.0] - 2023-04-06

### Changes & Improvements:
- [Android] - Added support for notification channel groups.
- [Android] - Added support for BigPicture style.
- [Android] - Icons now support local files and URIs.
- [Android] - Added API to check if UI with permission rationale for notifications should be shown before requesting permission.
- [Android] - Added APIs to request exact scheduling and bypasssing battery optimizations.
- [Android] - Minification seetings now managed automatically by package.
- [Android] - AndroidNotificationCenter.UserPermissionToPost now will also report if notifications are blocked in Settings (Android 7+).
- [iOS] - Added support for notification action icons (requires iOS 15).
- [iOS] - Added property to set interruption level (requires iOS 15).
- [iOS] - Added property to set relevance score (requires iOS 15).

### Fixes:
- [Android] - Fix updating scheduled notification not updating if app is not killed.

## [2.1.1] - 2023-01-04

### Fixes:
- [Android] - No longer use DeniedAndDontAskAgain permission response, only Denied.
- [iOS] - [issue 205](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/205) Fix occasional crash when registering for push notifications.
- [iOS] - [issue 242](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/242) Fix exception when OriginalUtc key is missing in data (for example notification was scheduled using older package version).

## [2.1.0] - 2022-09-23

### Changes & Improvements:
- [Android] - Notification scheduling is made faster.
- [Android] - Notifications can be set to not be shown when app is in foreground (contributed by InnoGames).
- [Android] - Support for Android 13 notification permission.
- [Android] - Support for exact scheduling on Android 12 and newer and ability to disable it.
- [iOS] - Updated AuthorizationStatus enum with new values.
- [iOS] - Location trigger now uses more accurate latitude/longitude and can repeat.
- [iOS] [issue 202](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/202) Added support for different sound types and notifications without sound.

### Fixes:
- [Android] - Scheduled notifications are saved in more lightweight and reliable way.
- [Android] [issue 196](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/196) Fix occasional Resources$NotFound exception.
- [Android] [issue 198](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/198) Fix occasional OutOfMemoryError.
- [iOS] - When using Calendar trigger time type (Local/UTC/...) will be preserved.

## [2.0.2] - 2022-05-13

### Changes & Improvements:
- [Android] - AndroidNotificationCenter.CheckScheduledNotificationStatus now works on devices with all API levels

### Fixes:
- [Android] [issue 186](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/186) Mitigate OutOfMemory errors causing application crash
- [Android] [issue 188](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/188) Fix unguarded API usage from level 23

## [2.0.1] - 2022-04-15

### Fixes:
- [Android] Change exported attribute to false in the manifest for package receivers.
- [Android] [issue 144](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/144) Fix Transaction.TooLargeException exception on some devices for notifications with large icons or more content.
- [Android] [issue 160](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/160) Fix UnityNotificationRestartOnBootReceiver not setting FLAG_IMMUTABLE.

### Changes & Improvements:
- [Android] Build will now fail if minimum SDK in Unity is lower than supported by package.

## [2.0.0] - 2022-02-02

### Fixes:
- [1360115](https://issuetracker.unity3d.com/issues/mobile-notifications-project-settings-window-loses-focus-when-clicking-on-any-category-while-mobile-notifications-pane-is-open) Fixed focus loss when trying to switching from notifications settings.
- [1373835]() Fixed calendar notifications for buddhist calendars on iOS.
- [1375744](https://issuetracker.unity3d.com/issues/ios-app-freezes-slash-crashes-when-both-mobile-notifications-and-firebase-are-used) Fixed application startup hang when application uses Unity notifications or Firebase.
- [1382960](https://issuetracker.unity3d.com/issues/ios-crash-on-pthread-kill-when-clicking-on-a-notification-that-contains-an-integer-value-in-the-apns-file) Fixed a crash when push notification JSON file has integer item at the top level.

### Changes & Improvements:
- [iOS] Added support for the following notification attachements: images, sounds, videos.
- [Android] Introduced AndroidNotificationCenter.CreateNotificationBuilder, which allows the greater customizability while creating notifications.
- [iOS] Added support for actionable notifications
- [iOS] Added API for registering notification categories with associated actions.
- [Android] Optimize notification saving.
- [iOS][Android] Added API to open notification settings
- [Android] Added API which allows you to change notification icons in Editor.
- [iOS] Correctly handle boolean values in UserInfo, previously boolean values were detected as numbers and get parsed as 1 or 0, now they will be correctly handled as 'true' and 'false'
- [Android] Fixed notification package for Android 12 - NotificationRestartOnBootReceiver needs to be marked as exported in manifest file, disable exact alarms because they require special permissions.
- [iOS] Fix NotificationSettings.iOSSettings.DefaultAuthorizationOptions having incorrect return type (was PresentationOption, now AuthorizationOption).
- [iOS] Mobile Notifications 2.0.0: iOSNotificationTrigger.Type is no longer static and returns iOSNotificationTriggerType instead of int.

## [1.4.2] - 2021-07-22

### Fixes:
- [Android] [issue 99](https://github.com/Unity-Technologies/com.unity.mobile.notifications/issues/99) Use FLAG_IMMUTABLE for PendingIntent
- [iOS] Do not touch Plist file unless modifications are required
- [iOS] [1348518](https://issuetracker.unity3d.com/product/unity/issues/guid/1348518) Fix crash in push notification (case )

## [1.4.1] - 2021-05-06

### Fixes:
- Disable warnings and remove leftover .meta file from samples.

## [1.4.0] - 2021-04-29

### Changes & Improvements:
- [iOS] Less memory allocations when scheduling and retrieving notifications.
- [iOS] Exposed notification UserInfo in API.
- [Samples] Various small improvements to the test project.

### Fixes:
- [Editor] Improved layout of elements in settings.
- [Editor] iOS header files are no longer copied when building for other platforms.
- [iOS] Multiple authorization requests can now be performed simultaneously.
- [iOS] [[1315365](https://issuetracker.unity3d.com/product/unity/issues/guid/1315365)] Fix TimeInterval getter.
- [iOS] Identifier for location based notifications is no longer hardcoded and now allows multiple triggers.

## [1.3.2] - 2020-11-10

### Changes & Improvements:
- [Editor] [[1195293](https://issuetracker.unity3d.com/product/unity/issues/guid/1195293)] Improved content scaling in the Mobile Notifications settings UI.
- [Samples] Added IGameNotification.Data propery, so that notification data could be read in cross-platform fashion.
- [Samples] Added IGameNotification.GetLastNotification method to retrieve the last opened notification.
- [Samples] Added retrieval of notification enabled status on Android.

### Fixes:
- [Samples] [[1258554](https://issuetracker.unity3d.com/product/unity/issues/guid/1258554)] Fixed errors and warnings on startup in the demo scene.
- [iOS] [[1259811](https://issuetracker.unity3d.com/product/unity/issues/guid/1259811)] Fixed UNITY_USES_REMOTE_NOTIFICATIONS being falsely set to 1 when Mobile Notifications isn't used.
- [Android] [[1271866](https://issuetracker.unity3d.com/product/unity/issues/guid/1271866)] Fixed AndroidReceivedNotificationMainThreadDispatcher allocating a new list on every frame.
- [Editor] [[1254618](https://issuetracker.unity3d.com/product/unity/issues/guid/1254618)] Fixed "SerializedObject target has been destroyed" errors when navigating Mobile Notifications settings.
- [Android] [[1172850](https://issuetracker.unity3d.com/product/unity/issues/guid/1172850)] Fixed Android crashing when many notifications are scheduled at once.

## [1.3.0] - 2020-06-02

### Changes & Improvements:
- [iOS] Improved the iOS plugin code: fixed the warnings and a potential dangling pointers issue, also made the code well structured and more readable.
- [iOS] Improved the iOS post processor code: split the function into several small ones, also fixed the wrong required iOS version.
- [Document] Improved the document: split the single page into multiple pages to have the document better organized, also added the missing document and updated the unclear document.

### Fixes:
- [Android] Supressed the deprecated warnings from Android plugin as some deprecated functions have to be called for Android 7 and below.
- [Android] Fixed the AndroidJavaException when calling AndroidNotificationCenter.UpdateScheduledNotification().
- [Android] Fixed the issue where AndroidNotification.Color doesn't work as expected.
- [iOS] [[1244642](https://issuetracker.unity3d.com/product/unity/issues/guid/1244642)] Fixed the bug that users can't set the foreground presentation options to "Nothing" in the Notification Settings.
- [Editor] [[1199310](https://issuetracker.unity3d.com/product/unity/issues/guid/1199310)] Fixed the issue that Mobile Notification icons are gray when project was built from batch mode with -nographics flag.

## [1.2.1-preview] - 2020-04-30

### Fixes:
- [Android] [[1231218](https://issuetracker.unity3d.com/product/unity/issues/guid/1231218)] Fixed the issue by removing the "com.android.support:appcompat-v7:27.1.1" dependency.

## [1.2.0-preview] - 2020-04-16

### Changes & Improvements:
- [Editor] Improved the Notification Settings UI, also stored the notification setting asset under 'ProjectSettings' folder rather than 'Assets' folder.

### Fixes:
- Fixed a lot of notification settings UI related bugs.
- [iOS] Fixed the issue that the authorization request callback hasn't been removed.
- [Android] Fixed the wrong value of LockscreenVisiblity.Private.

## [1.1.0-preview] - 2020-03-24

### Changes & Improvements:
- [Android] Exposed the java source code in the package rather than building into an .aar. Users can now debug the java code in Android Studio by exporting to a gradle project.
- Refactored the managed plugin code to have them better structured.

### Fixes:
- Fixed a potential deadlock issue in AndroidReceivedNotificationMainThreadDispatcher while handling the notification received callback.
- [iOS] [[1221133](https://issuetracker.unity3d.com/product/unity/issues/guid/1221133)] Fixed the issue that user generated entitlements file will be rewritten by the one generated by mobile notifications.
- [iOS] Fixed the crash on iOS if the time interval on iOSNotificationTimeIntervalTrigger is set to 0.
- [iOS] Fixed the crash on iOS if iOSNotificationData.data is set to null.

## [1.0.4-preview.9] - 2020-02-10

### Fixes:

- [Android] Write all `SharedPreferences` to disk asynchronously.

## [1.0.4-preview.6] - 2020-01-27

### Fixes:
- [Android] Duplicate notifications will no longer be scheduled after the device is restarted (when "Reschedule Notification on Device Restart" is turned on).
- [Android] Unity's Notification package will no longer attempt to load AlarmManager intents which were not scheduledby it.
- [Editor] It's now possible to use `Unity.Notifications' editor classes without having to create manually referenc it in your asmdef file.

## [1.0.4-preview.5] - 2019-11-13

### Fixes:

- [iOS/tvOS] The iOS source plugins provided by the package will no longer be included when building for Apple TV.

## [1.0.4-preview.4] - 2019-10-29

### Fixes:

- [Android] Icon resources will now be queried when the notification is supposed to be triggered instead of when it's scheduled. This should fix crashes due to missing resources in cases where the app is updated after a notification is scheduled but before it's delivered.

### Changes & Improvements:

- [Android] Added 'AndroidNotification.ShowTimestamp' to allow displaying timestamp on delivered notifications. The specific time stamp value can be overriden using 'AndroidNotification.CustomTimestamp' if it's not set the time at which the notification was delivered will be shown.

## [1.0.4-preview.3] - 2019-10-09

### Fixes:

- [iOS] Remote notification `deviceToken` is now be returned correctly (and without angle brackets)on iOS 13.

## [1.0.4-preview.2] - 2019-09-16

### Fixes:

- Embedded the [Notification Samples project](https://github.com/Unity-Technologies/NotificationsSamples) into the package, I can now be imported from the Package Manager UI.
- [Android] It should now be possible to set `AndroidNotificationChannel.VibrationPattern` to null.

## [1.0.4-preview.1] - 2019-09-09

### Fixes:

- [Android] Trying to register an ``AndroidNotificationChannel` with a specified `VibrationPattern` should no longer trigger a missing method exception.
-  [Android][[1178665](https://issuetracker.unity3d.com/product/unity/issues/guid/1178665/)] Repeatable notifications should now work properly and should be recreated when the device is restarted. 1.0.4-preview.alpha.1

## [1.0.3] - 2019-08-21

### Fixes:

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

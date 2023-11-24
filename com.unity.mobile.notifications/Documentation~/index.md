# Mobile Notifications package

The Unity Mobile Notifications package adds support for scheduling local one-time or repeatable notifications on Android and iOS.

Notification functionality works differently on Android and iOS platforms. The mobile notifications package provides features in three sets of APIs for your convenience.

* [Android](Android.md) specific APIs in `Unity.Notifications.Android` namespace.
* [iOS](iOS.md) specific APIs in `Unity.Notifications.iOS` namespace.
* [Unified APIs](Unified.md) that provide Android and iOS specific common functionalities in `Unity.Notifications` namespace.

### Requirements

- Compatible with Unity 2021.3 or above.
- Compatible with same minimum Android and iOS versions as the oldest supported Unity version.
- Requires Android SDK with API level 33 or higher.
- Requires Xcode with SDK for iOS 15.2 or newer.

### Supported features

The runtime notification APIs are split into two parts for both Android and iOS. These APIs can be used to schedule and manage notifications as listed below:

*   Schedule local one-time or repeatable notifications.
*   Cancel already displayed and upcoming (scheduled) notifications.
*   Android:
    *   Create and modify notification channels (categories) on Android 8.0 (Oreo) and above.
    *   Preserve notifications when the device restarts.
    *   Set custom notification icons.
*   iOS:
    *   Use the Apple Push Notification Service (APNs) to receive remote notifications.
    *   Modify remote notification content if the device receives notifications from other apps while your app is running.
    *   Group notifications into threads (only supported on iOS 12+).
    *   Add attachments to notifications.
    *   Support for notification actions.

### Install the package

To add the Mobile Notifications package to your project, use the [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html). For information on how to add the package, refer to [Adding and removing packages](https://docs.unity3d.com/Manual/upm-ui-actions.html).

The package name is `com.unity.mobile.notifications` and the display name is **Mobile Notifications**.

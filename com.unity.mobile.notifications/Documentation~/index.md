# Introduction

The Unity Mobile Notifications package adds support for scheduling local one-time or repeatable notifications on Android and iOS.

### Requirements

- Compatible with Unity 2018.3 or above.
- Compatible with Android 4.4+ (API 19) and iOS 10.0+.

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

### Installing
To install the Mobile Notification package, please follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).

# Introduction

Mobile Notification Package adds support for scheduling local one-time or repeatable notifications on Android and iOS platforms.

### Requirements

- Compatible with Android 4.4+ (API 19) and iOS 10.0+.
- Compatible with Unity 2018.3 or above.

### Supported features

The runtime notification APIs are split into two parts for both Android and iOS. These APIs can be used to schedule and manage notifications as listed blow:

*   Schedule local one-time or repeatable notifications.
*   Cancel already displayed and upcoming (scheduled) notifications.
*   Android:
    *   Create and modify notification channels (categories) on Android Oreo and above.
    *   Preserve notifications when the device restarts.
    *   Set custom notification icons.
*   iOS:
    *   Use the Apple Push Notification Service (APNs) to receive remote notifications.
    *   Modify remote notification content if the device receives notifications from other apps while your app is running.
    *   Group notifications into threads (only supported on iOS 12+).
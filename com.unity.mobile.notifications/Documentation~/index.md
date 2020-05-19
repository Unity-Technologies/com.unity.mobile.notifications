# Unity Mobile Notifications Package

The runtime API is split into two parts: `AndroidNotificationCenter` and `iOSNotificationCenter`. These can be used to schedule and manage notifications for their respective platforms. You can download a sample Project which implements a high-level wrapper that you can use to send notifications to both Android and iOS with the same API from our [GitHub page](https://github.com/Unity-Technologies/NotificationsSamples), or see the code samples below.

This package supports the following features:

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

**Requirements:**

*   Supports Android 4.4 (API 19) and iOS 10 or above.
*   Compatible with Unity 2018.3 or above.  

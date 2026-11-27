---
name: notification-scheduling
description: Add scheduled local notifications (Android and iOS) to a Unity game using the Mobile Notifications package (com.unity.mobile.notifications). Use when a user asks for a Unity game to show a notification at or around a specific time, wants to integrate the Mobile Notifications package, or needs a notification scheduler script.
---

# Unity Mobile Notifications (local, scheduled)

Guides adding local scheduled notifications to a Unity game with the Unified API (`Unity.Notifications.NotificationCenter`).

## Scope

- **Local notifications** are scheduled on the device by the game. No server is involved. This skill covers them.
- **Remote push** (APNs/FCM, a backend sending at a given time) is a different approach. If the user needs server-driven content or timing, say so and treat it as out of scope for this skill.

If it is unclear which the user wants, ask. "A notification at a specific time" usually means local.

## Workflow

### 1. Determine the package version

Do not guess a version. Use the version verified for the user's Unity Editor.

Each editor install lists verified package versions in a manifest:

| OS | File |
|---|---|
| macOS | `<Editor>/Unity.app/Contents/Resources/PackageManager/Editor/manifest.json` |
| Windows / Linux | the equivalent `.../Resources/PackageManager/Editor/manifest.json` inside the install (check the actual layout) |

Look up the entry:

```bash
grep -A4 '"com.unity.mobile.notifications"' "<Editor>/Unity.app/Contents/Resources/PackageManager/Editor/manifest.json"
```

Use the `version` field. Ask the user for the editor path or version if you don't know it (Hub installs are typically under `/Applications/Unity/Hub/Editor/<version>` on macOS).

Version notes:
- If the user's editor verifies an older version than they want, using a newer one is a manual override. Mention that, and have them test on a device.
- If the package source is available locally (for example a checkout of the package repository), read it to confirm API signatures before relying on them. Bundled editor packages are also available as a `.tgz` next to the editor manifest, in `Resources/PackageManager/Editor/`. Extract into a scratch directory, never into the project.

### 2. Install by editing the manifest

When the version is known, editing `Packages/manifest.json` directly is faster and simpler than the Package Manager UI. Add inside `"dependencies"`:

```json
"com.unity.mobile.notifications": "<version>"
```

### 3. Ask the user, in this order

1. **Which platforms are targeted: Android, iOS, or both?** This decides which settings apply.
2. **If Android is targeted: use the app icon or a custom icon?**
   - App icon: leave `AndroidSmallIcon` unset.
   - Custom icon: ask the user to put the icon file in the project and register it under **Project Settings > Mobile Notifications > Custom Icons** with an ID. Pass that ID as `NotificationCenterArgs.AndroidSmallIcon`. (Requires 2.5.0 or later. It applies to all notifications scheduled through the Unified API.)
3. **What should the notification show?** Title and body text, and anything else it should carry.
4. **How long before the event should it arrive?** The notification should invite the user back into the game, with no expectation of immediate action, so it is sent some time *before* the event time. Ask for the lead time (for example 5 minutes, 15 minutes, 30 minutes).

Replace the placeholders in the script with the answers.

### 4. Add the scheduler script

Use `assets/NotificationScheduler.cs` as the starting point. Points about how it works:

- `NotificationCenter.Initialize` creates the Android channel when `AndroidChannelName` and `AndroidChannelDescription` are set.
- `NotificationCenter.RequestPermission()` returns one request object for both platforms. When the status is `RequestPending`, the app suspends while the system asks the user, so yielding it in a coroutine is safe.
- A fixed `Identifier` means scheduling again with the same ID replaces the previous notification, and it can be cancelled with `CancelScheduledNotification`.
- `ScheduleBefore` throws `ArgumentException` if the computed notification time is in the past. Notifications are meant for the future, when the user is not using the app. An event closer than the lead time therefore throws, so callers must handle that.

### 5. Tell the user what else is available

Present these three groups.

**Notification settings** (**Edit > Project Settings > Mobile Notifications**). Only enable what the game uses:
- **Android, Reschedule Notifications on Device Restart:** Android removes scheduled notifications on device restart, iOS keeps them. **When both platforms are targeted, recommend enabling this** to match iOS behavior. It adds the `RECEIVE_BOOT_COMPLETED` permission.
- **iOS, Enable Push Notifications:** only if the game uses remote push. Not needed for local notifications.
- **iOS, Include CoreLocation Framework:** only if the game uses `iOSNotificationLocationTrigger`. Otherwise it adds an unneeded framework dependency.
- **Android, Schedule at exact time:** not advisable, and Google discourages it. It costs more power, and apps declaring `SCHEDULE_EXACT_ALARM` or `USE_EXACT_ALARM` without exact scheduling as a key feature can be prohibited from publishing on Google Play. Approximate delivery is the recommended default and suits the lead-time approach.

**Repeatable notifications.**
- `new NotificationDateTimeSchedule(time, NotificationRepeatInterval.Daily)` repeats daily at that time of day (only the time is used).
- `new NotificationIntervalSchedule(TimeSpan.FromHours(3), repeats: true)` repeats at a fixed interval.
- Make sure repeating notifications can be cancelled.

**Platform-specific features.** The Unified API covers the common cases. Further features come from `Unity.Notifications.Android.AndroidNotificationCenter` and `Unity.Notifications.iOS.iOSNotificationCenter`: extra Android channels and iOS categories, iOS location triggers and weekday scheduling, Android large icons, and so on.

**Frequency.** Recommend being mindful of how many notifications are sent. Too many push players to disable notifications or uninstall. Send only what is useful, be especially careful with repeating ones, and cancel notifications that are no longer relevant (for example when the player returns).

Other useful Unified API calls to mention if relevant: `QueryLastRespondedNotification()` (app launched by tapping a notification, for deep links), `OnNotificationReceived` (notification shown while the app is in the foreground, which is hidden by default), `OpenNotificationSettings()` (user denied permission).

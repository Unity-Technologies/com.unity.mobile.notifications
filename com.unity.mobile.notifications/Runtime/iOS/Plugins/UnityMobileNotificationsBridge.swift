#if UNITY_XCODE_PROJECT_TYPE_SWIFT

import Foundation

@_cdecl("UnityMobileNotifications_applicationWillFinishLaunchingName")
func applicationWillFinishLaunchingName() -> Notification.Name {
    UnityNotifications.applicationWillFinishLaunching
}

@_cdecl("UnityMobileNotifications_applicationDidRegisterForRemoteNotificationsName")
func applicationDidRegisterForRemoteNotificationsName() -> Notification.Name {
    UnityNotifications.applicationDidRegisterForRemoteNotifications
}

@_cdecl("UnityMobileNotifications_applicationDidFailToRegisterForRemoteNotificationsName")
func applicationDidFailToRegisterForRemoteNotificationsName() -> Notification.Name {
    UnityNotifications.applicationDidFailToRegisterForRemoteNotifications
}

@_cdecl("UnityMobileNotifications_remoteNotificationsDeviceTokenKey")
func remoteNotificationsDeviceTokenKey() -> NSString {
    UnityNotifications.remoteNotificationsDeviceTokenKey as NSString
}

#endif

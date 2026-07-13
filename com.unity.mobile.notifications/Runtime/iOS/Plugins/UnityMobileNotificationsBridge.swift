#if UNITY_XCODE_PROJECT_TYPE_SWIFT

import Foundation

@_cdecl("UnityMobileNotifications_applicationWillFinishLaunchingName")
func applicationWillFinishLaunchingName() -> String {
    UnityNotifications.applicationWillFinishLaunching.rawValue
}

@_cdecl("UnityMobileNotifications_applicationDidRegisterForRemoteNotificationsName")
func applicationDidRegisterForRemoteNotificationsName() -> String {
    UnityNotifications.applicationDidRegisterForRemoteNotifications.rawValue
}

@_cdecl("UnityMobileNotifications_applicationDidFailToRegisterForRemoteNotificationsName")
func applicationDidFailToRegisterForRemoteNotificationsName() -> String {
    UnityNotifications.applicationDidFailToRegisterForRemoteNotifications.rawValue
}

@_cdecl("UnityMobileNotifications_remoteNotificationsDeviceTokenKey")
func remoteNotificationsDeviceTokenKey() -> String {
    UnityNotifications.remoteNotificationsDeviceTokenKey
}

#endif

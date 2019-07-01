using System.Runtime.InteropServices;
using Unity.Notifications.iOS;
using UnityEditor;

namespace Unity.Notifications
{
    public class UnityNotificationSettings
    {
        private static NotificationEditorSetting GetSetting(BuildTargetGroup target, string key)
        {
            var manager = UnityNotificationEditorManager.Initialize();

            NotificationEditorSetting setting = null;
            if (target == BuildTargetGroup.Android)
            {
                setting = manager.AndroidNotificationEditorSettingsFlat.Find(i => i.key == key);
            }
            else if (target == BuildTargetGroup.iOS)
            {
                setting = manager.iOSNotificationEditorSettingsFlat.Find(i => i.key == key);

            }

            return setting;
        }


        private static void SetSettingValue<T>(BuildTargetGroup target, string key, T value)
        {
            var manager = UnityNotificationEditorManager.Initialize();

            NotificationEditorSetting setting = GetSetting(target, key);
            if (setting != null)
            {
                setting.val = value;
                manager.SaveSetting(setting, target);
            }
        }

        private static T GetSettingValue<T>(BuildTargetGroup target, string key)
        {
            var setting = GetSetting(target, key);
            return (T) setting.val;
        }

        public static class AndroidSettings
        {
            /// <summary>
            /// By default AndroidSettings removes all scheduled notifications when the device is restarted. Enable this to automatically reschedule all non expired notifications when the device is turned back on.
            /// </summary>
            public static bool RescheduleOnDeviceRestart
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.Android, "UnityNotificationAndroidRescheduleOnDeviceRestart");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.Android, "UnityNotificationAndroidRescheduleOnDeviceRestart", value);
                }
            }

            /// <summary>
            /// Enable this if you want to override the activity which will opened when the user click on the notification. By default activity assigned to `com.unity3d.player.UnityPlayer.currentActivity` will be used.
            /// </summary>
            public static bool UseCustomActivity
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.Android, "UnityNotificationAndroidUseCustomActivity");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.Android, "UnityNotificationAndroidUseCustomActivity", value);
                }
            }

            /// <summary>
            /// The full class name of the activity that you wish to be assigned to the notification.
            /// </summary>
            public static string CustomActivityString
            {
                get
                {
                    return GetSettingValue<string>(BuildTargetGroup.Android, "UnityNotificationAndroidCustomActivityString");
                }
                set
                {
                    SetSettingValue<string>(BuildTargetGroup.Android, "UnityNotificationAndroidCustomActivityString", value);
                }
            }
        }

        public static class iOSSettings
        {
            /// <summary>
            /// It's recommended to make the authorization request during the app's launch cycle. If this is enabled the user will be shown the authorization pop-up immediately when the app launches. If it’s unchecked you’ll need to manually create an AuthorizationRequest before your app can send or receive notifications.
            /// </summary>
            public static bool RequestAuthorizationOnAppLaunch
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, "UnityNotificationRequestAuthorizationOnAppLaunch");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, "UnityNotificationRequestAuthorizationOnAppLaunch", value);
                }
            }
            
            /// <summary>
            /// Configure the notification interaction types your app will include in the authorisation request if RequestAuthorizationOnAppLaunch is enabled. Alternatively you can specify them when creating a `AuthorizationRequest` from a script.
            /// </summary>
            public static PresentationOption DefaultAuthorizationOptions
            {
                get
                {
                    return GetSettingValue<PresentationOption>(BuildTargetGroup.iOS, "UnityNotificationDefaultAuthorizationOptions");
                }
                set
                {
                    SetSettingValue<PresentationOption>(BuildTargetGroup.iOS, "UnityNotificationDefaultAuthorizationOptions", value);
                }
            }

            /// <summary>
            /// Enable this to add the push notification capability to you Xcode project.
            /// </summary>
            public static bool AddRemoteNotificationCapability
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, "UnityAddRemoteNotificationCapability");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, "UnityAddRemoteNotificationCapability", value);
                }
            }
            
            /// <summary>
            /// If this is enabled the app will automatically register your app with APNs after the launch which would enable it to receive remote notifications. You’ll have to manually create a AuthorizationRequest to get the device token.
            /// </summary>
            public static bool NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch 
            {                 
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch", value);
                } 
            }

            /// <summary>
            /// The default presentation options for received remote notifications. In order for the specified presentation options to be used your app must had received the authorization to use them (the user might change it at any time).
            /// </summary>
            public static PresentationOption RemoteNotificationForegroundPresentationOptions
            {
                get
                {
                    return GetSettingValue<PresentationOption>(BuildTargetGroup.iOS, "UnityRemoteNotificationForegroundPresentationOptions");
                }
                set
                {
                    SetSettingValue<PresentationOption>(BuildTargetGroup.iOS, "UnityRemoteNotificationForegroundPresentationOptions", value);
                }
            }
            
            /// <summary>
            /// Enable this when signing the app with a production certificate.
            /// </summary>
            public static bool UseAPSReleaseEnvironment
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, "UnityUseAPSReleaseEnvironment");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, "UnityUseAPSReleaseEnvironment", value);
                } 

            }
            
            /// <summary>
            /// If you intend to use the iOSNotificationLocationTrigger in your notifications you must include the CoreLocation framework in your project.
            /// </summary>
            public static bool UseLocationNotificationTrigger {                 
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, "UnityUseLocationNotificationTrigger");
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, "UnityUseLocationNotificationTrigger", value);
                } 
            }
        }
    }
}
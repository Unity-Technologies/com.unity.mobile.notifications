using Unity.Notifications.iOS;
using UnityEditor;
using UnityEngine;

namespace Unity.Notifications
{
    /// <summary>
    /// Class used to access notification settings for a specific platform.
    /// </summary>
    public class NotificationSettings
    {
        private static NotificationSetting GetSetting(BuildTargetGroup target, string key)
        {
            var manager = NotificationSettingsManager.Initialize();

            NotificationSetting setting = null;
            if (target == BuildTargetGroup.Android)
            {
                setting = manager.AndroidNotificationSettingsFlat.Find(i => i.Key == key);
            }
            else if (target == BuildTargetGroup.iOS)
            {
                setting = manager.iOSNotificationSettingsFlat.Find(i => i.Key == key);
            }

            return setting;
        }

        private static void SetSettingValue<T>(BuildTargetGroup target, string key, T value)
        {
            var manager = NotificationSettingsManager.Initialize();

            NotificationSetting setting = GetSetting(target, key);
            if (setting != null)
            {
                setting.Value = value;
                manager.SaveSetting(setting, target);
            }
        }

        private static T GetSettingValue<T>(BuildTargetGroup target, string key)
        {
            var setting = GetSetting(target, key);
            return (T)setting.Value;
        }

        /// <summary>
        /// Class used to access Android-specific notification settings.
        /// </summary>
        public static class AndroidSettings
        {
            internal static readonly string RESCHEDULE_ON_RESTART = "UnityNotificationAndroidRescheduleOnDeviceRestart";
            internal static readonly string EXACT_ALARM = "UnityNotificationAndroidScheduleExactAlarms";
            internal static readonly string USE_CUSTOM_ACTIVITY = "UnityNotificationAndroidUseCustomActivity";
            internal static readonly string CUSTOM_ACTIVITY_CLASS = "UnityNotificationAndroidCustomActivityString";

            /// <summary>
            /// By default AndroidSettings removes all scheduled notifications when the device is restarted. Enable this to automatically reschedule all non expired notifications when the device is turned back on.
            /// </summary>
            public static bool RescheduleOnDeviceRestart
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.Android, RESCHEDULE_ON_RESTART);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.Android, RESCHEDULE_ON_RESTART, value);
                }
            }

            /// <summary>
            /// Enable this if you want to override the activity which will opened when the user click on the notification. By default activity assigned to `com.unity3d.player.UnityPlayer.currentActivity` will be used.
            /// </summary>
            public static bool UseCustomActivity
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.Android, USE_CUSTOM_ACTIVITY);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.Android, USE_CUSTOM_ACTIVITY, value);
                }
            }

            /// <summary>
            /// The full class name of the activity that you wish to be assigned to the notification.
            /// </summary>
            public static string CustomActivityString
            {
                get
                {
                    return GetSettingValue<string>(BuildTargetGroup.Android, CUSTOM_ACTIVITY_CLASS);
                }
                set
                {
                    SetSettingValue<string>(BuildTargetGroup.Android, CUSTOM_ACTIVITY_CLASS, value);
                }
            }

            /// <summary>
            /// A set of flags indicating whether to use exact scheduling and add supporting permissions.
            /// </summary>
            public static AndroidExactSchedulingOption ExactSchedulingOption
            {
                get
                {
                    return GetSettingValue<AndroidExactSchedulingOption>(BuildTargetGroup.Android, EXACT_ALARM);
                }
                set
                {
                    SetSettingValue<AndroidExactSchedulingOption>(BuildTargetGroup.Android, EXACT_ALARM, value);
                }
            }

            /// <summary>
            /// Add image to notification settings.
            /// </summary>
            /// <param name="id">Image identifier</param>
            /// <param name="image">Image texture, must be obtained from asset database</param>
            /// <param name="type">Image type</param>
            public static void AddDrawableResource(string id, Texture2D image, NotificationIconType type)
            {
                var manager = NotificationSettingsManager.Initialize();
                manager.AddDrawableResource(id, image, type);
#if UNITY_2020_2_OR_NEWER
                SettingsService.RepaintAllSettingsWindow();
#endif
            }

            /// <summary>
            /// Remove icon at given index from notification settings.
            /// </summary>
            /// <param name="index">Index of image to remove</param>
            public static void RemoveDrawableResource(int index)
            {
                var manager = NotificationSettingsManager.Initialize();
                manager.RemoveDrawableResourceByIndex(index);
#if UNITY_2020_2_OR_NEWER
                SettingsService.RepaintAllSettingsWindow();
#endif
            }

            /// <summary>
            /// Remove icon with given identifier from notification settings.
            /// </summary>
            /// <param name="id">ID of the image to remove</param>
            public static void RemoveDrawableResource(string id)
            {
                var manager = NotificationSettingsManager.Initialize();
                manager.RemoveDrawableResourceById(id);
#if UNITY_2020_2_OR_NEWER
                SettingsService.RepaintAllSettingsWindow();
#endif
            }

            /// <summary>
            /// Remove all images from notification settings.
            /// </summary>
            public static void ClearDrawableResources()
            {
                var manager = NotificationSettingsManager.Initialize();
                manager.ClearDrawableResources();
#if UNITY_2020_2_OR_NEWER
                SettingsService.RepaintAllSettingsWindow();
#endif
            }


        }

        /// <summary>
        /// Class used to access iOS-specific notification settings.
        /// </summary>
        public static class iOSSettings
        {
            internal static readonly string REQUEST_AUTH_ON_LAUNCH = "UnityNotificationRequestAuthorizationOnAppLaunch";
            internal static readonly string DEFAULT_AUTH_OPTS = "UnityNotificationDefaultAuthorizationOptions";
            internal static readonly string ADD_PUSH_CAPABILITY = "UnityAddRemoteNotificationCapability";
            internal static readonly string REQUEST_PUSH_AUTH_ON_LAUNCH = "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch";
            internal static readonly string PUSH_NOTIFICATION_PRESENTATION = "UnityRemoteNotificationForegroundPresentationOptions";
            internal static readonly string USE_APS_RELEASE = "UnityUseAPSReleaseEnvironment";
            internal static readonly string USE_LOCATION_TRIGGER = "UnityUseLocationNotificationTrigger";
            internal static readonly string ADD_TIME_SENSITIVE_ENTITLEMENT = "UnityAddTimeSensitiveEntitlement";

            /// <summary>
            /// It's recommended to make the authorization request during the app's launch cycle. If this is enabled the user will be shown the authorization pop-up immediately when the app launches. If it’s unchecked you’ll need to manually create an AuthorizationRequest before your app can send or receive notifications.
            /// </summary>
            public static bool RequestAuthorizationOnAppLaunch
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, REQUEST_AUTH_ON_LAUNCH);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, REQUEST_AUTH_ON_LAUNCH, value);
                }
            }

            /// <summary>
            /// Configure the notification interaction types your app will include in the authorisation request if RequestAuthorizationOnAppLaunch is enabled. Alternatively you can specify them when creating a `AuthorizationRequest` from a script.
            /// </summary>
            public static AuthorizationOption DefaultAuthorizationOptions
            {
                get
                {
                    return GetSettingValue<AuthorizationOption>(BuildTargetGroup.iOS, DEFAULT_AUTH_OPTS);
                }
                set
                {
                    SetSettingValue<AuthorizationOption>(BuildTargetGroup.iOS, DEFAULT_AUTH_OPTS, value);
                }
            }

            /// <summary>
            /// Enable this to add the push notification capability to you Xcode project.
            /// </summary>
            public static bool AddRemoteNotificationCapability
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, ADD_PUSH_CAPABILITY);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, ADD_PUSH_CAPABILITY, value);
                }
            }

            /// <summary>
            /// If this is enabled the app will automatically register your app with APNs after the launch which would enable it to receive remote notifications. You’ll have to manually create a AuthorizationRequest to get the device token.
            /// </summary>
            public static bool NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, REQUEST_PUSH_AUTH_ON_LAUNCH);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, REQUEST_PUSH_AUTH_ON_LAUNCH, value);
                }
            }

            /// <summary>
            /// The default presentation options for received remote notifications. In order for the specified presentation options to be used your app must had received the authorization to use them (the user might change it at any time).
            /// </summary>
            public static PresentationOption RemoteNotificationForegroundPresentationOptions
            {
                get
                {
                    return GetSettingValue<PresentationOption>(BuildTargetGroup.iOS, PUSH_NOTIFICATION_PRESENTATION);
                }
                set
                {
                    SetSettingValue<PresentationOption>(BuildTargetGroup.iOS, PUSH_NOTIFICATION_PRESENTATION, value);
                }
            }

            /// <summary>
            /// Enable this when signing the app with a production certificate.
            /// </summary>
            public static bool UseAPSReleaseEnvironment
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, USE_APS_RELEASE);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, USE_APS_RELEASE, value);
                }
            }

            /// <summary>
            /// If you intend to use the iOSNotificationLocationTrigger in your notifications you must include the CoreLocation framework in your project.
            /// </summary>
            public static bool UseLocationNotificationTrigger
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, USE_LOCATION_TRIGGER);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, USE_LOCATION_TRIGGER, value);
                }
            }

            /// <summary>
            /// Add entitlement to enable notifications with time-sensitive interruption level.
            /// </summary>
            public static bool AddTimeSensitiveEntitlement
            {
                get
                {
                    return GetSettingValue<bool>(BuildTargetGroup.iOS, ADD_TIME_SENSITIVE_ENTITLEMENT);
                }
                set
                {
                    SetSettingValue<bool>(BuildTargetGroup.iOS, ADD_TIME_SENSITIVE_ENTITLEMENT, value);
                }
            }
        }
    }
}

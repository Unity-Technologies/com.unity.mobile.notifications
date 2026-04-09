#if UNITY_ANDROID

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Android;
using Unity.Android.Gradle.Manifest;

namespace Unity.Notifications
{
    [Serializable]
    struct NotificationIcon
    {
        public string Name;
        public byte[] Data;
    }

    [Serializable]
    struct NotificationResources
    {
        public NotificationIcon[] Icons;
    }

    [Serializable]
    internal struct ManifestSettings
    {
        public bool UseCustomActivity;
        public string CustomActivity;
        public bool RescheduleOnRestart;
        public AndroidExactSchedulingOption ExactAlarm;
    }

    internal static class AndroidNotificationPostProcessorUtils
    {
        internal static ManifestSettings GetManifestSettings()
        {
            var settings = NotificationSettingsManager.Initialize().AndroidNotificationSettingsFlat;
            return new ManifestSettings()
            {
                UseCustomActivity = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.USE_CUSTOM_ACTIVITY),
                CustomActivity = GetSetting<string>(settings, NotificationSettings.AndroidSettings.CUSTOM_ACTIVITY_CLASS),
                RescheduleOnRestart = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.RESCHEDULE_ON_RESTART),
                ExactAlarm = GetSetting<AndroidExactSchedulingOption>(settings, NotificationSettings.AndroidSettings.EXACT_ALARM),
            };
        }

        private static T GetSetting<T>(List<NotificationSetting> settings, string key)
        {
            return (T)settings.Find(i => i.Key == key).Value;
        }
    }

    class AndroidNotificationPostProcessor : AndroidProjectFilesModifier
    {
        private string ToIconPath(string name)
        {
            return $"unityLibrary/src/main/res/{name}";
        }

        public override AndroidProjectFilesModifierContext Setup()
        {
            var ctx = new AndroidProjectFilesModifierContext();
            PrepareResources(ctx);

            ctx.SetData(nameof(ManifestSettings), AndroidNotificationPostProcessorUtils.GetManifestSettings());

            ctx.Outputs.AddManifestFile("unityLibrary/mobilenotifications.androidlib/src/main/AndroidManifest.xml");

            return ctx;
        }

        public override void OnModifyAndroidProjectFiles(AndroidProjectFiles projectFiles)
        {
            var icons = projectFiles.GetData<NotificationResources>(nameof(NotificationResources));
            foreach (var icon in icons.Icons)
                projectFiles.SetFileContents(ToIconPath(icon.Name), icon.Data);

            var manifestSettings = projectFiles.GetData<ManifestSettings>(nameof(ManifestSettings));
            var manifest = new AndroidManifestFile();
            InjectAndroidManifest(manifest.Manifest, manifestSettings);
            projectFiles.SetManifestFile("unityLibrary/mobilenotifications.androidlib/src/main/AndroidManifest.xml", manifest);
        }

        private void PrepareResources(AndroidProjectFilesModifierContext context)
        {
            var icons = NotificationSettingsManager.Initialize().GenerateDrawableResourcesForExport();
            var resources = new NotificationResources()
            {
                Icons = new NotificationIcon[icons.Count]
            };

            var idx = 0;
            foreach (var icon in icons)
            {
                resources.Icons[idx++] = new NotificationIcon
                {
                    Name = icon.Key,
                    Data = icon.Value
                };
                context.Outputs.AddFileWithContents(ToIconPath(icon.Key));
            };
            context.SetData(nameof(NotificationResources), resources);
        }

        private static void InjectReceivers(Manifest manifest, ManifestSettings settings)
        {
            var receiverkNotificationManager = new Receiver();
            receiverkNotificationManager.Attributes.Name.Set("com.unity.androidnotifications.UnityNotificationManager");
            receiverkNotificationManager.Attributes.Exported.Set(false);
            manifest.Application.ReceiverList.AddElement(receiverkNotificationManager);

            if (settings.RescheduleOnRestart)
            {
                manifest.AddUsesPermission("android.permission.RECEIVE_BOOT_COMPLETED");

                var receiverNotificationRestartOnBoot = manifest.Application.AddReceiver("com.unity.androidnotifications.UnityNotificationRestartReceiver");
                receiverNotificationRestartOnBoot.Attributes.Exported.Set(false);

                var receiverNotificationRestartOnBootIntentFilter = new IntentFilter();
                var receiverNotificationRestartOnBootAction = new Unity.Android.Gradle.Manifest.Action();
                receiverNotificationRestartOnBootAction.Attributes.Name.Set("android.intent.action.BOOT_COMPLETED");
                receiverNotificationRestartOnBootIntentFilter.ActionList.AddElement(receiverNotificationRestartOnBootAction);

                receiverNotificationRestartOnBoot.IntentFilterList.AddElement(receiverNotificationRestartOnBootIntentFilter);
            }
        }

        private static void InjectAndroidManifest(Manifest manifest, ManifestSettings settings)
        {
            manifest.SetCustomAttribute("xmlns:android", "http://schemas.android.com/apk/res/android");

            InjectReceivers(manifest, settings);

            if (settings.UseCustomActivity)
                manifest.Application.AddMetaDataValue("custom_notification_android_activity", settings.CustomActivity);

            manifest.AddUsesPermission("android.permission.POST_NOTIFICATIONS");

            bool enableExact = (settings.ExactAlarm & AndroidExactSchedulingOption.ExactWhenAvailable) != 0;
            manifest.Application.AddMetaDataValue("com.unity.androidnotifications.exact_scheduling", enableExact ? "1" : "0");
            if (enableExact)
            {
                bool scheduleExact = (settings.ExactAlarm & AndroidExactSchedulingOption.AddScheduleExactPermission) != 0;
                bool useExact = (settings.ExactAlarm & AndroidExactSchedulingOption.AddUseExactAlarmPermission) != 0;
                // as documented here: https://developer.android.com/reference/android/Manifest.permission#USE_EXACT_ALARM
                // only one of these two attributes should be used or max sdk set so on any device it's one or the other
                if (scheduleExact)
                {
                    var sheduleExactAlarm = manifest.AddUsesPermission("android.permission.SCHEDULE_EXACT_ALARM");
                    if (useExact)
                        sheduleExactAlarm.Attributes.MaxSdkVersion.Set(32);
                }
                if (useExact)
                    manifest.AddUsesPermission("android.permission.USE_EXACT_ALARM");

                // Battery optimizations must use "uses-permission-sdk-23", regular uses-permission does not work
                if ((settings.ExactAlarm & AndroidExactSchedulingOption.AddRequestIgnoreBatteryOptimizationsPermission) != 0)
                    manifest.AddUsesPermissionSdk23("android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
            }
        }
    }

}
#endif

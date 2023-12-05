#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEditor.Android;
using Unity.Android.Gradle.Manifest;
using System.IO;

namespace Unity.Notifications
{
    public class AndroidNotificationPostProcessor : AndroidProjectFilesModifier
    {
        private static readonly string NotificationProguardPath = "unityLibrary/proguard-unity-notifications.txt";

        [Serializable]
        private class NotificationIcon
        {
            public string Name;
            public byte[] Data;
        }

        [Serializable]
        private class NotificationResources
        {
            public NotificationIcon[] Icons;
        }

        [Serializable]
        private struct ManifestSettings
        {
            public bool UseCustomActivity;
            public string CustomActivity;
            public bool RescheduleOnRestart;
            public AndroidExactSchedulingOption ExactAlarm;
        }

        private string ToIconPath(string name)
        {
            return $"unityLibrary/src/main/res/{name}";
        }

        public override AndroidProjectFilesModifierContext Setup()
        {
            var ctx = new AndroidProjectFilesModifierContext();
            PrepareResources(ctx);

            var settings = NotificationSettingsManager.Initialize().AndroidNotificationSettingsFlat;
            ctx.SetData(nameof(ManifestSettings), new ManifestSettings()
            {
                UseCustomActivity = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.USE_CUSTOM_ACTIVITY),
                CustomActivity = GetSetting<string>(settings, NotificationSettings.AndroidSettings.CUSTOM_ACTIVITY_CLASS),
                RescheduleOnRestart = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.RESCHEDULE_ON_RESTART),
                ExactAlarm = GetSetting<AndroidExactSchedulingOption>(settings, NotificationSettings.AndroidSettings.EXACT_ALARM),
            });

            ctx.Outputs.AddFileWithContents(NotificationProguardPath);

            return ctx;
        }

        public override void OnModifyAndroidProjectFiles(AndroidProjectFiles projectFiles)
        {
            var icons = projectFiles.GetData<NotificationResources>(nameof(NotificationResources));
            foreach (var icon in icons.Icons)
                projectFiles.SetFileContents(ToIconPath(icon.Name), icon.Data);

            var manifestSettings = projectFiles.GetData<ManifestSettings>(nameof(ManifestSettings));
            InjectAndroidManifest(projectFiles.UnityLibraryManifest.Manifest, manifestSettings);
            InjectProguard(projectFiles);
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

        private static T GetSetting<T>(List<NotificationSetting> settings, string key)
        {
            return (T)settings.Find(i => i.Key == key).Value;
        }

        private static void InjectReceivers(Manifest manifest)
        {
            var receiverkNotificationManager = new Receiver();
            receiverkNotificationManager.Attributes.Name.Set("com.unity.androidnotifications.UnityNotificationManager");
            receiverkNotificationManager.Attributes.Exported.Set(false);
            manifest.Application.ReceiverList.AddElement(receiverkNotificationManager);

            var receiverNotificationRestartOnBoot = new Receiver();
            receiverNotificationRestartOnBoot.Attributes.Name.Set("com.unity.androidnotifications.UnityNotificationRestartOnBootReceiver");
            receiverNotificationRestartOnBoot.Attributes.Exported.Set(false);
            receiverNotificationRestartOnBoot.Attributes.Enabled.Set(false);

            var receiverNotificationRestartOnBootIntentFilter = new IntentFilter();
            var receiverNotificationRestartOnBootAction = new Unity.Android.Gradle.Manifest.Action();
            receiverNotificationRestartOnBootAction.Attributes.Name.Set("android.intent.action.BOOT_COMPLETED");
            receiverNotificationRestartOnBootIntentFilter.ActionList.AddElement(receiverNotificationRestartOnBootAction);

            receiverNotificationRestartOnBoot.IntentFilterList.AddElement(receiverNotificationRestartOnBootIntentFilter);

            manifest.Application.ReceiverList.AddElement(receiverNotificationRestartOnBoot);
        }

        private static void InjectAndroidManifest(Manifest manifest, ManifestSettings settings)
        {
            InjectReceivers(manifest);

            if (settings.UseCustomActivity)
                manifest.Application.AddMetaDataValue("custom_notification_android_activity", settings.CustomActivity);

            if (settings.RescheduleOnRestart)
            {
                manifest.Application.AddMetaDataValue("reschedule_notifications_on_restart", "true");
                manifest.AddUsesPermission("android.permission.RECEIVE_BOOT_COMPLETED");
            }

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
                {
                    // TODO: Missing AddUsesPermissionSdk23 function
                    var batterOptimizations = manifest.AddUsesPermission("android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
                    batterOptimizations.Attributes.Name.Set("uses-permission-sdk-23");
                }
            } 
        }

        private static void InjectProguard(AndroidProjectFiles projectFiles)
        {
            // TODO: PropertyStringArray missing Add function?
            var original = new List<string>(projectFiles.UnityLibraryBuildGradle.Android.DefaultConfig.ConsumerProguardFiles.Get());
            original.Add(Path.GetFileName(NotificationProguardPath));
            projectFiles.UnityLibraryBuildGradle.Android.DefaultConfig.ConsumerProguardFiles.Set(original.ToArray());

            projectFiles.SetFileContents(NotificationProguardPath, string.Join("\n",
                new[]
                {
                    "-keep class com.unity.androidnotifications.UnityNotificationManager { public *; }",
                    "-keep class com.unity.androidnotifications.NotificationCallback { *; }"
                }));
        }
    }
}
#endif

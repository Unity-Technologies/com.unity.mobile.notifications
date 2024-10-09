#if UNITY_ANDROID

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Android;

#if UNITY_6000_0_OR_NEWER
using Unity.Android.Gradle.Manifest;
#else
using System.Linq;
using System.Xml;
using UnityEditor;
#endif

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

#if UNITY_6000_0_OR_NEWER
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

#else
    class AndroidNotificationPostProcessor : IPostGenerateGradleAndroidProject
    {
        const string kAndroidNamespaceURI = "http://schemas.android.com/apk/res/android";

        const string kNotificationsDefaultManifest = @"
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""com.unity.androidnotifications"">
  <application>
    <receiver android:name=""com.unity.androidnotifications.UnityNotificationManager"" android:exported=""false"" />
    <meta-data android:name=""com.unity.androidnotifications.exact_scheduling"" android:value=""0"" />
  </application>
  <uses-permission android:name=""android.permission.POST_NOTIFICATIONS"" />
</manifest>
";

        public int callbackOrder { get { return 0; } }

        public void OnPostGenerateGradleAndroidProject(string projectPath)
        {
            projectPath = Path.Combine(projectPath, "mobilenotifications.androidlib");
            if (!Directory.Exists(projectPath))
                throw new Exception("mobilenotifications module not found in gradle project");

            CopyNotificationResources(projectPath);

            InjectAndroidManifest(projectPath);
        }

        private void CopyNotificationResources(string projectPath)
        {
            // Get the icons set in the UnityNotificationEditorManager and write them to the res folder, then we can use the icons as res.
            var icons = NotificationSettingsManager.Initialize().GenerateDrawableResourcesForExport();
            foreach (var icon in icons)
            {
                var fileInfo = new FileInfo(string.Format("{0}/src/main/res/{1}", projectPath, icon.Key));
                if (fileInfo.Directory != null)
                {
                    fileInfo.Directory.Create();
                    File.WriteAllBytes(fileInfo.FullName, icon.Value);
                }
            }
        }

        private void InjectAndroidManifest(string projectPath)
        {
            var manifestPath = string.Format("{0}/src/main/AndroidManifest.xml", projectPath);
            XmlDocument manifestDoc = new XmlDocument();
            manifestDoc.LoadXml(kNotificationsDefaultManifest);

            var manifestSettings = AndroidNotificationPostProcessorUtils.GetManifestSettings();

            InjectAndroidManifest(manifestPath, manifestDoc, manifestSettings);

            manifestDoc.Save(manifestPath);
        }

        internal static void InjectAndroidManifest(string manifestPath, XmlDocument manifestDoc, ManifestSettings settings)
        {
            if (settings.UseCustomActivity)
                AppendAndroidMetadataField(manifestPath, manifestDoc, "custom_notification_android_activity", settings.CustomActivity);

            if (settings.RescheduleOnRestart)
            {
                var manifestNode = manifestDoc.SelectSingleNode("manifest");
                var applicationNode = manifestNode.SelectSingleNode("application");
                var receiver = manifestDoc.CreateElement("receiver");
                receiver.SetAttribute("name", kAndroidNamespaceURI, "com.unity.androidnotifications.UnityNotificationRestartReceiver");
                receiver.SetAttribute("exported", kAndroidNamespaceURI, "false");
                applicationNode.AppendChild(receiver);
                var filterNode = manifestDoc.CreateElement("intent-filter");
                receiver.AppendChild(filterNode);
                var actionNode = manifestDoc.CreateElement("action");
                actionNode.SetAttribute("name", kAndroidNamespaceURI, "android.intent.action.BOOT_COMPLETED");
                filterNode.AppendChild(actionNode);

                AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.RECEIVE_BOOT_COMPLETED");
            }

            bool enableExact = (settings.ExactAlarm & AndroidExactSchedulingOption.ExactWhenAvailable) != 0;
            AppendAndroidMetadataField(manifestPath, manifestDoc, "com.unity.androidnotifications.exact_scheduling", enableExact ? "1" : "0");
            if (enableExact)
            {
                bool scheduleExact = (settings.ExactAlarm & AndroidExactSchedulingOption.AddScheduleExactPermission) != 0;
                bool useExact = (settings.ExactAlarm & AndroidExactSchedulingOption.AddUseExactAlarmPermission) != 0;
                // as documented here: https://developer.android.com/reference/android/Manifest.permission#USE_EXACT_ALARM
                // only one of these two attributes should be used or max sdk set so on any device it's one or the other
                if (scheduleExact)
                    AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.SCHEDULE_EXACT_ALARM", useExact ? "32" : null);
                if (useExact)
                    AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.USE_EXACT_ALARM");

                // Battery optimizations must use "uses-permission-sdk-23", regular uses-permission does not work
                if ((settings.ExactAlarm & AndroidExactSchedulingOption.AddRequestIgnoreBatteryOptimizationsPermission) != 0)
                    AppendAndroidPermissionField(manifestPath, manifestDoc, "uses-permission-sdk-23", "android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS", null);
            }
        }

        internal static void AppendAndroidPermissionField(string manifestPath, XmlDocument xmlDoc, string name, string maxSdk = null)
        {
            AppendAndroidPermissionField(manifestPath, xmlDoc, "uses-permission", name, maxSdk);
        }

        internal static void AppendAndroidPermissionField(string manifestPath, XmlDocument xmlDoc, string tagName, string name, string maxSdk)
        {
            var manifestNode = xmlDoc.SelectSingleNode("manifest");
            if (manifestNode == null)
                throw new ArgumentException(string.Format("Missing 'manifest' node in '{0}'.", manifestPath));

            XmlElement metaDataNode = null;
            foreach (XmlNode node in manifestNode.ChildNodes)
            {
                if (!(node is XmlElement) || node.Name != tagName)
                    continue;

                var element = (XmlElement)node;
                var elementName = element.GetAttribute("name", kAndroidNamespaceURI);
                if (elementName == name)
                {
                    if (maxSdk == null)
                        return;
                    var maxSdkAttr = element.GetAttribute("maxSdkVersion", kAndroidNamespaceURI);
                    if (!string.IsNullOrEmpty(maxSdkAttr))
                        return;
                    metaDataNode = element;
                }
            }

            if (metaDataNode == null)
            {
                metaDataNode = xmlDoc.CreateElement(tagName);
                metaDataNode.SetAttribute("name", kAndroidNamespaceURI, name);
            }
            if (maxSdk != null)
                metaDataNode.SetAttribute("maxSdkVersion", kAndroidNamespaceURI, maxSdk);

            manifestNode.AppendChild(metaDataNode);
        }

        internal static void AppendAndroidMetadataField(string manifestPath, XmlDocument xmlDoc, string name, string value)
        {
            var applicationNode = xmlDoc.SelectSingleNode("manifest/application");
            if (applicationNode == null)
                throw new ArgumentException(string.Format("Missing 'application' node in '{0}'.", manifestPath));

            var nodes = xmlDoc.SelectNodes("manifest/application/meta-data");
            if (nodes != null)
            {
                // Check if there is a 'meta-data' with the same name.
                foreach (XmlNode node in nodes)
                {
                    var element = node as XmlElement;
                    if (element == null)
                        continue;

                    var elementName = element.GetAttribute("name", kAndroidNamespaceURI);
                    if (elementName == name)
                    {
                        element.SetAttribute("value", kAndroidNamespaceURI, value);
                        return;
                    }
                }
            }

            XmlElement metaDataNode = xmlDoc.CreateElement("meta-data");
            metaDataNode.SetAttribute("name", kAndroidNamespaceURI, name);
            metaDataNode.SetAttribute("value", kAndroidNamespaceURI, value);

            applicationNode.AppendChild(metaDataNode);
        }
    }
#endif
}
#endif

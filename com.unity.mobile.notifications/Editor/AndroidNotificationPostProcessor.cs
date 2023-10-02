#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;

namespace Unity.Notifications
{
    public class AndroidNotificationPostProcessor : IPostGenerateGradleAndroidProject
    {
        const string kAndroidNamespaceURI = "http://schemas.android.com/apk/res/android";

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

        internal struct ManifestSettings
        {
            public bool UseCustomActivity;
            public string CustomActivity;
            public bool RescheduleOnRestart;
            public AndroidExactSchedulingOption ExactAlarm;
        }

        private void InjectAndroidManifest(string projectPath)
        {
            var manifestPath = string.Format("{0}/src/main/AndroidManifest.xml", projectPath);
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException(string.Format("'{0}' doesn't exist.", manifestPath));

            XmlDocument manifestDoc = new XmlDocument();
            manifestDoc.Load(manifestPath);

            var settings = NotificationSettingsManager.Initialize().AndroidNotificationSettingsFlat;
            var manifestSettings = new ManifestSettings()
            {
                UseCustomActivity = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.USE_CUSTOM_ACTIVITY),
                CustomActivity = GetSetting<string>(settings, NotificationSettings.AndroidSettings.CUSTOM_ACTIVITY_CLASS),
                RescheduleOnRestart = GetSetting<bool>(settings, NotificationSettings.AndroidSettings.RESCHEDULE_ON_RESTART),
                ExactAlarm = GetSetting<AndroidExactSchedulingOption>(settings, NotificationSettings.AndroidSettings.EXACT_ALARM),
            };

            InjectAndroidManifest(manifestPath, manifestDoc, manifestSettings);

            manifestDoc.Save(manifestPath);
        }

        internal static void InjectAndroidManifest(string manifestPath, XmlDocument manifestDoc, ManifestSettings settings)
        {
            if (settings.UseCustomActivity)
                AppendAndroidMetadataField(manifestPath, manifestDoc, "custom_notification_android_activity", settings.CustomActivity);

            if (settings.RescheduleOnRestart)
            {
                AppendAndroidMetadataField(manifestPath, manifestDoc, "reschedule_notifications_on_restart", "true");
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

        private static T GetSetting<T>(List<NotificationSetting> settings, string key)
        {
            return (T)settings.Find(i => i.Key == key).Value;
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
}
#endif

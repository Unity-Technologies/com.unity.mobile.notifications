#if UNITY_ANDROID
using System;
using System.IO;
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
            MinSdkCheck();

            CopyNotificationResources(projectPath);

            InjectAndroidManifest(projectPath);
        }

        private void MinSdkCheck()
        {
#if !UNITY_2021_2_OR_NEWER
        // API level 21 not supported since 2021.2, need to check for prior releases
        const AndroidSdkVersions kMinAndroidSdk = AndroidSdkVersions.AndroidApiLevel21;

        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel21)
                throw new NotSupportedException(string.Format("Minimum Android API level supported by notifications package is {0}, your Player Settings have it set to {1}",
                    (int)kMinAndroidSdk, PlayerSettings.Android.minSdkVersion));
#endif
        }

        private void CopyNotificationResources(string projectPath)
        {
            // The projectPath points to the the parent folder instead of the actual project path.
            if (!Directory.Exists(Path.Combine(projectPath, "src")))
            {
                projectPath = Path.Combine(projectPath, PlayerSettings.productName);
            }

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
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException(string.Format("'{0}' doesn't exist.", manifestPath));

            XmlDocument manifestDoc = new XmlDocument();
            manifestDoc.Load(manifestPath);

            InjectReceivers(manifestPath, manifestDoc);

            var settings = NotificationSettingsManager.Initialize().AndroidNotificationSettingsFlat;

            var useCustomActivity = (bool)settings.Find(i => i.Key == "UnityNotificationAndroidUseCustomActivity").Value;
            if (useCustomActivity)
            {
                var customActivity = (string)settings.Find(i => i.Key == "UnityNotificationAndroidCustomActivityString").Value;
                AppendAndroidMetadataField(manifestPath, manifestDoc, "custom_notification_android_activity", customActivity);
            }

            var enableRescheduleOnRestart = (bool)settings.Find(i => i.Key == "UnityNotificationAndroidRescheduleOnDeviceRestart").Value;
            if (enableRescheduleOnRestart)
            {
                AppendAndroidMetadataField(manifestPath, manifestDoc, "reschedule_notifications_on_restart", "true");
                AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.RECEIVE_BOOT_COMPLETED");
            }

            manifestDoc.Save(manifestPath);
        }

        internal static void InjectReceivers(string manifestPath, XmlDocument manifestXmlDoc)
        {
            const string kNotificationManagerName = "com.unity.androidnotifications.UnityNotificationManager";
            const string kNotificationRestartOnBootName = "com.unity.androidnotifications.UnityNotificationRestartOnBootReceiver";

            var applicationXmlNode = manifestXmlDoc.SelectSingleNode("manifest/application");
            if (applicationXmlNode == null)
                throw new ArgumentException(string.Format("Missing 'application' node in '{0}'.", manifestPath));

            XmlElement notificationManagerReceiver = null;
            XmlElement notificationRestartOnBootReceiver = null;

            var receiverNodes = manifestXmlDoc.SelectNodes("manifest/application/receiver");
            if (receiverNodes != null)
            {
                // Check existing receivers.
                foreach (XmlNode node in receiverNodes)
                {
                    var element = node as XmlElement;
                    if (element == null)
                        continue;

                    var elementName = element.GetAttribute("name", kAndroidNamespaceURI);
                    if (elementName == kNotificationManagerName)
                        notificationManagerReceiver = element;
                    else if (elementName == kNotificationRestartOnBootName)
                        notificationRestartOnBootReceiver = element;

                    if (notificationManagerReceiver != null && notificationRestartOnBootReceiver != null)
                        break;
                }
            }

            // Create notification manager receiver if necessary.
            if (notificationManagerReceiver == null)
            {
                notificationManagerReceiver = manifestXmlDoc.CreateElement("receiver");
                notificationManagerReceiver.SetAttribute("name", kAndroidNamespaceURI, kNotificationManagerName);

                applicationXmlNode.AppendChild(notificationManagerReceiver);
            }
            notificationManagerReceiver.SetAttribute("exported", kAndroidNamespaceURI, "false");

            // Create notification restart-on-boot receiver if necessary.
            if (notificationRestartOnBootReceiver == null)
            {
                notificationRestartOnBootReceiver = manifestXmlDoc.CreateElement("receiver");
                notificationRestartOnBootReceiver.SetAttribute("name", kAndroidNamespaceURI, kNotificationRestartOnBootName);

                var intentFilterNode = manifestXmlDoc.CreateElement("intent-filter");

                var actionNode = manifestXmlDoc.CreateElement("action");
                actionNode.SetAttribute("name", kAndroidNamespaceURI, "android.intent.action.BOOT_COMPLETED");

                intentFilterNode.AppendChild(actionNode);
                notificationRestartOnBootReceiver.AppendChild(intentFilterNode);
                applicationXmlNode.AppendChild(notificationRestartOnBootReceiver);
            }
            notificationRestartOnBootReceiver.SetAttribute("enabled", kAndroidNamespaceURI, "false");
            notificationRestartOnBootReceiver.SetAttribute("exported", kAndroidNamespaceURI, "false");
        }

        internal static void AppendAndroidPermissionField(string manifestPath, XmlDocument xmlDoc, string name)
        {
            var manifestNode = xmlDoc.SelectSingleNode("manifest");
            if (manifestNode == null)
                throw new ArgumentException(string.Format("Missing 'manifest' node in '{0}'.", manifestPath));

            foreach (XmlNode node in manifestNode.ChildNodes)
            {
                if (!(node is XmlElement) || node.Name != "uses-permission")
                    continue;

                var elementName = ((XmlElement)node).GetAttribute("name", kAndroidNamespaceURI);
                if (elementName == name)
                    return;
            }

            XmlElement metaDataNode = xmlDoc.CreateElement("uses-permission");
            metaDataNode.SetAttribute("name", kAndroidNamespaceURI, name);

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

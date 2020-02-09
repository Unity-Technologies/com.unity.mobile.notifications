using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;

#if UNITY_EDITOR && PLATFORM_ANDROID

#pragma warning disable 0219
namespace Unity.Notifications
{
    public class AndroidNotificationResourcesPostProcessor : IPostGenerateGradleAndroidProject
    {

        public static XmlDocument AppendAndroidPermissionField(XmlDocument xmlDoc, string key)
        {
            // <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED"/>
            string xpath = "manifest";
            var parentNode = xmlDoc.SelectSingleNode(xpath);
            XmlElement metaDataNode = xmlDoc.CreateElement("uses-permission");

            foreach (XmlNode node in parentNode.ChildNodes)
            {
                if (node.Name == "uses-permission")
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Value == key)
                            return xmlDoc;
                    }
            }
            
            metaDataNode.SetAttribute("name", "http://schemas.android.com/apk/res/android", key);
           
            parentNode.AppendChild(metaDataNode);

            return xmlDoc;
        }

        public static XmlDocument AppendAndroidMetadataField(XmlDocument xmlDoc, string key, string value)
        {
            
            string xpath = "manifest/application/meta-data";
            
            var nodes = xmlDoc.SelectNodes(xpath);
            var fieldSet = false;

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Value == key)
                        {
                            fieldSet = true;
                        }
                    }

                    if (fieldSet)
                    {
                        ((XmlElement)node).SetAttribute("value", "http://schemas.android.com/apk/res/android", value);
                        break;   
                    }
                       
                }
            }
            
            if (!fieldSet)
            {
                XmlElement metaDataNode = xmlDoc.CreateElement("meta-data");
                
                metaDataNode.SetAttribute("name", "http://schemas.android.com/apk/res/android", key);
                metaDataNode.SetAttribute("value", "http://schemas.android.com/apk/res/android", value);
                            
                var applicationNode = xmlDoc.SelectSingleNode("manifest/application");
                if (applicationNode != null)
                {
                    applicationNode.AppendChild(metaDataNode);
                }
            }
            return xmlDoc;
        }
        
        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPostGenerateGradleAndroidProject(string projectPath)
        {
            var icons = UnityNotificationEditorManager.Initialize().GenerateDrawableResourcesForExport();

            var directories = Directory.GetDirectories(projectPath);
            foreach (var icon in icons)
            {
                // When exporting a gradle project projectPath points to the the parent folder of the project
                // instead of the actual project
                if (!Directory.Exists(Path.Combine(projectPath, "src")))
                {
                    projectPath = Path.Combine(projectPath, PlayerSettings.productName);
                }
                
                var fileInfo = new FileInfo(string.Format("{0}/src/main/res/{1}", projectPath, icon.Key));
                if (fileInfo.Directory != null)
                {
                    fileInfo.Directory.Create();
                    File.WriteAllBytes(fileInfo.FullName, icon.Value);
                }
            }
            
            var settings = UnityNotificationEditorManager.Initialize().AndroidNotificationEditorSettingsFlat;
            
            var enableRescheduleOnRestart = (bool)settings
                .Find(i => i.key == "UnityNotificationAndroidRescheduleOnDeviceRestart").val;
            
            var useCustomActivity = (bool)settings
                .Find(i => i.key == "UnityNotificationAndroidUseCustomActivity").val;

            var customActivity = (string)settings
                .Find(i => i.key == "UnityNotificationAndroidCustomActivityString").val;

            if (useCustomActivity | enableRescheduleOnRestart)
            {
                string manifestPath = string.Format("{0}/src/main/AndroidManifest.xml", projectPath);
                XmlDocument manifestDoc = new XmlDocument();
                manifestDoc.Load(manifestPath);

                if (useCustomActivity)
                {
                    manifestDoc = AppendAndroidMetadataField(manifestDoc, "custom_notification_android_activity",
                        customActivity);
                }

                if (enableRescheduleOnRestart)
                {
                    manifestDoc = AppendAndroidMetadataField(manifestDoc, "reschedule_notifications_on_restart", "true");
                    manifestDoc = AppendAndroidPermissionField(manifestDoc,
                        "android.permission.RECEIVE_BOOT_COMPLETED");
                }
                manifestDoc.Save(manifestPath);
            }
        }
    }
}
#pragma warning restore 0219
#endif
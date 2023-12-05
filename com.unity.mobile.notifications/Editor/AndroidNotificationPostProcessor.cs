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

    /*/
    //*
       public class AndroidNotificationPostProcessor : IPostGenerateGradleAndroidProject
       {
           const string kAndroidNamespaceURI = "http://schemas.android.com/apk/res/android";

           public int callbackOrder { get { return 0; } }

           public void OnPostGenerateGradleAndroidProject(string projectPath)
           {
               MinSdkCheck();

               CopyNotificationResources(projectPath);

               InjectAndroidManifest(projectPath);
               InjectProguard(projectPath);
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
               InjectReceivers(manifestPath, manifestDoc);

               if (settings.UseCustomActivity)
                   AppendAndroidMetadataField(manifestPath, manifestDoc, "custom_notification_android_activity", settings.CustomActivity);

               if (settings.RescheduleOnRestart)
               {
                   AppendAndroidMetadataField(manifestPath, manifestDoc, "reschedule_notifications_on_restart", "true");
                   AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.RECEIVE_BOOT_COMPLETED");
               }

               AppendAndroidPermissionField(manifestPath, manifestDoc, "android.permission.POST_NOTIFICATIONS");

            bool enableExact = true;// (settings.ExactAlarm & AndroidExactSchedulingOption.ExactWhenAvailable) != 0;
               AppendAndroidMetadataField(manifestPath, manifestDoc, "com.unity.androidnotifications.exact_scheduling", enableExact ? "1" : "0");
               if (enableExact)
               {
                   bool scheduleExact = (settings.ExactAlarm & AndroidExactSchedulingOption.AddScheduleExactPermission) != 0;
                bool useExact = true;// (settings.ExactAlarm & AndroidExactSchedulingOption.AddUseExactAlarmPermission) != 0;
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

           private static void InjectProguard(string projectPath)
           {
               var proguardFile = $"{projectPath}/proguard-unity.txt";
               if (!File.Exists(proguardFile))
               {
                   UnityEngine.Debug.LogWarning($"Proguard file {proguardFile} not found, mobile notifications package may not function");
                   return;
               }

               var lines = File.ReadAllLines(proguardFile);
               if (InjectProguard(ref lines))
                   File.WriteAllLines(proguardFile, lines);
           }

           internal static bool InjectProguard(ref string[] lines)
           {
               bool manager = InjectProguard(ref lines,
                   "com.unity.androidnotifications.UnityNotificationManager",
                   "-keep class com.unity.androidnotifications.UnityNotificationManager { public *; }");
               bool callback = InjectProguard(ref lines,
                   "com.unity.androidnotifications.NotificationCallback",
                   "-keep class com.unity.androidnotifications.NotificationCallback { *; }");

               return manager || callback;
           }

           static bool InjectProguard(ref string[] lines, string search, string inject)
           {
               foreach (var s in lines)
                   if (s.Contains(search))
                       return false;

               lines = lines.Concat(new[] { inject }).ToArray();
               return true;
           }
       }
   // */
}
#endif

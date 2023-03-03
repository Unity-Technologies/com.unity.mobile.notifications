using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using Unity.Notifications.iOS;
using UnityEngine.Serialization;

namespace Unity.Notifications
{
    [HelpURL("Packages/com.unity.mobile.notifications/documentation.html")]
    internal class NotificationSettingsManager : ScriptableObject
    {
        internal static readonly string k_SettingsPath = "ProjectSettings/NotificationsSettings.asset";

        private static NotificationSettingsManager s_SettingsManager;

        [FormerlySerializedAs("toolbarInt")]
        public int ToolbarIndex = 0;

        public List<NotificationSetting> iOSNotificationSettings;
        public List<NotificationSetting> AndroidNotificationSettings;

        [SerializeField]
        [FormerlySerializedAs("iOSNotificationEditorSettingsValues")]
        private NotificationSettingsCollection m_iOSNotificationSettingsValues;

        [SerializeField]
        [FormerlySerializedAs("AndroidNotificationEditorSettingsValues")]
        private NotificationSettingsCollection m_AndroidNotificationSettingsValues;

        [FormerlySerializedAs("TrackedResourceAssets")]
        public List<DrawableResourceData> DrawableResources = new List<DrawableResourceData>();

        public List<NotificationSetting> iOSNotificationSettingsFlat
        {
            get
            {
                var target = new List<NotificationSetting>();
                FlattenList(iOSNotificationSettings, target);
                return target;
            }
        }

        public List<NotificationSetting> AndroidNotificationSettingsFlat
        {
            get
            {
                var target = new List<NotificationSetting>();
                FlattenList(AndroidNotificationSettings, target);
                return target;
            }
        }

        private void FlattenList(List<NotificationSetting> source, List<NotificationSetting> target)
        {
            foreach (var setting in source)
            {
                target.Add(setting);

                if (setting.Dependencies != null)
                {
                    FlattenList(setting.Dependencies, target);
                }
            }
        }

        public static NotificationSettingsManager Initialize()
        {
            if (s_SettingsManager != null)
                return s_SettingsManager;

            var settingsManager = CreateInstance<NotificationSettingsManager>();
            bool dirty = false;

            if (File.Exists(k_SettingsPath))
            {
                var settingsJson = File.ReadAllText(k_SettingsPath);
                if (!string.IsNullOrEmpty(settingsJson))
                    EditorJsonUtility.FromJsonOverwrite(settingsJson, settingsManager);
            }
            else
            {
                // Only migrate if there is no new settings asset under k_SettingsPath.
                dirty = MigrateFromLegacySettings(settingsManager);
            }

            if (settingsManager.m_iOSNotificationSettingsValues == null)
            {
                settingsManager.m_iOSNotificationSettingsValues = new NotificationSettingsCollection();
                dirty = true;
            }

            if (settingsManager.m_AndroidNotificationSettingsValues == null)
            {
                settingsManager.m_AndroidNotificationSettingsValues = new NotificationSettingsCollection();
                dirty = true;
            }

            // Create the settings for iOS.
            settingsManager.iOSNotificationSettings = new List<NotificationSetting>()
            {
                new NotificationSetting(
                    NotificationSettings.iOSSettings.REQUEST_AUTH_ON_LAUNCH,
                    "Request Authorization on App Launch",
                    "It's recommended to make the authorization request during the app's launch cycle. If this is enabled the authorization pop-up will show up immediately during launching. Otherwise you need to manually create an AuthorizationRequest before sending or receiving notifications.",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.REQUEST_AUTH_ON_LAUNCH, true, false),
                    dependencies: new List<NotificationSetting>()
                    {
                        new NotificationSetting(
                            NotificationSettings.iOSSettings.DEFAULT_AUTH_OPTS,
                            "Default Notification Authorization Options",
                            "Configure the notification interaction types which will be included in the authorization request if \"Request Authorization on App Launch\" is enabled.",
                            settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.DEFAULT_AUTH_OPTS,
                                AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, false)),
                    }),
                new NotificationSetting(
                    NotificationSettings.iOSSettings.ADD_PUSH_CAPABILITY,
                    "Enable Push Notifications",
                    "Enable this to add the push notification capability to the Xcode project, also to retrieve the device token from an AuthorizationRequest.",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.ADD_PUSH_CAPABILITY, false, false),
                    false,
                    new List<NotificationSetting>()
                    {
                        new NotificationSetting(
                            NotificationSettings.iOSSettings.REQUEST_PUSH_AUTH_ON_LAUNCH,
                            "Register for Push Notifications on App Launch",
                            "Enable this to automatically register your app with APNs after launching to receive remote notifications. You need to manually create an AuthorizationRequest to get the device token.",
                            settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.REQUEST_PUSH_AUTH_ON_LAUNCH, false, false)),
                        new NotificationSetting(
                            NotificationSettings.iOSSettings.PUSH_NOTIFICATION_PRESENTATION,
                            "Remote Notification Foreground Presentation Options",
                            "Configure the default presentation options for received remote notifications. In order to use the specified presentation options, your app must have received the authorization (the user might change it at any time).",
                            settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.PUSH_NOTIFICATION_PRESENTATION, (PresentationOption)iOSPresentationOption.All, false)),
                        new NotificationSetting(NotificationSettings.iOSSettings.USE_APS_RELEASE,
                            "Enable Release Environment for APS",
                            "Enable this when signing the app with a production certificate.",
                            settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.USE_APS_RELEASE, false, false),
                            false),
                    }),
                new NotificationSetting(NotificationSettings.iOSSettings.USE_LOCATION_TRIGGER,
                    "Include CoreLocation Framework",
                    "Include the CoreLocation framework to use the iOSNotificationLocationTrigger in your project.",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.USE_LOCATION_TRIGGER, false, false),
                    false),
                new NotificationSetting(NotificationSettings.iOSSettings.ADD_TIME_SENSITIVE_ENTITLEMENT,
                    "Enable time sensitive notifications",
                    "Enable to add entitlement for notifications with time sensitive interruption level",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.iOSSettings.ADD_TIME_SENSITIVE_ENTITLEMENT, false, false),
                    false),
            };

            // Create the settings for Android.
            settingsManager.AndroidNotificationSettings = new List<NotificationSetting>()
            {
                new NotificationSetting(
                    NotificationSettings.AndroidSettings.RESCHEDULE_ON_RESTART,
                    "Reschedule on Device Restart",
                    "Enable this to automatically reschedule all non-expired notifications after device restart. By default AndroidSettings removes all scheduled notifications after restarting.",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.AndroidSettings.RESCHEDULE_ON_RESTART, false, true)),
                new NotificationSetting(
                    NotificationSettings.AndroidSettings.EXACT_ALARM,
                    "Schedule at exact time",
                    "Whether notifications should appear at exact time or approximate",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.AndroidSettings.EXACT_ALARM, (AndroidExactSchedulingOption)0, true)),
                new NotificationSetting(
                    NotificationSettings.AndroidSettings.USE_CUSTOM_ACTIVITY,
                    "Use Custom Activity",
                    "Enable this to override the activity which will be opened when the user taps the notification.",
                    settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.AndroidSettings.USE_CUSTOM_ACTIVITY, false, true),
                    dependencies: new List<NotificationSetting>()
                    {
                        new NotificationSetting(
                            NotificationSettings.AndroidSettings.CUSTOM_ACTIVITY_CLASS,
                            "Custom Activity Name",
                            "The full class name of the activity which will be assigned to the notification.",
                            settingsManager.GetOrAddNotificationSettingValue(NotificationSettings.AndroidSettings.CUSTOM_ACTIVITY_CLASS, "com.unity3d.player.UnityPlayerActivity", true))
                    })
            };

            settingsManager.SaveSettings(dirty);

            s_SettingsManager = settingsManager;
            return s_SettingsManager;
        }

        private static bool MigrateFromLegacySettings(NotificationSettingsManager settingsManager)
        {
            const string k_LegacyAssetPath = "Assets/Editor/com.unity.mobile.notifications/NotificationSettings.asset";
            if (!File.Exists(k_LegacyAssetPath))
                return false;

            var settingsManagerLegacy = AssetDatabase.LoadAssetAtPath<NotificationSettingsManager>(k_LegacyAssetPath);
            if (settingsManagerLegacy == null)
                return false;

            settingsManager.ToolbarIndex = settingsManagerLegacy.ToolbarIndex;
            settingsManager.m_iOSNotificationSettingsValues = settingsManagerLegacy.m_iOSNotificationSettingsValues;
            settingsManager.m_AndroidNotificationSettingsValues = settingsManagerLegacy.m_AndroidNotificationSettingsValues;
            settingsManager.DrawableResources = settingsManagerLegacy.DrawableResources;

            AssetDatabase.DeleteAsset(k_LegacyAssetPath);
            DeleteEmptyDirectoryAsset("Assets/Editor/com.unity.mobile.notifications");

            return true;
        }

        private static void DeleteEmptyDirectoryAsset(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists || directoryInfo.GetDirectories().Length > 0 || directoryInfo.GetFiles().Length > 0)
                return;

            AssetDatabase.DeleteAsset(directory);
        }

        private T GetOrAddNotificationSettingValue<T>(string key, T defaultValue, bool isAndroid)
        {
            var collection = isAndroid ? m_AndroidNotificationSettingsValues : m_iOSNotificationSettingsValues;

            try
            {
                var value = collection[key];
                if (value != null)
                    return (T)value;
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning("Failed loading : " + key + " for type:" + defaultValue.GetType() + " Expected : " + collection[key].GetType());
                //Just return default value if it's a new setting that was not yet serialized.
            }

            collection[key] = defaultValue;
            return defaultValue;
        }

        public void SaveSetting(NotificationSetting setting, BuildTargetGroup target)
        {
            var collection = (target == BuildTargetGroup.Android) ? m_AndroidNotificationSettingsValues : m_iOSNotificationSettingsValues;

            if (!collection.Contains(setting.Key) || collection[setting.Key].ToString() != setting.Value.ToString())
            {
                collection[setting.Key] = setting.Value;
                SaveSettings();
            }
        }

        public void SaveSettings(bool forceSave = true)
        {
            if (!forceSave && File.Exists(k_SettingsPath))
                return;

            File.WriteAllText(k_SettingsPath, EditorJsonUtility.ToJson(this, true));
        }

        public void AddDrawableResource(string id, Texture2D image, NotificationIconType type)
        {
            /* commenting out for now, since you can have same Id's in editor
            foreach (var drawable in DrawableResources)
            {
                if (drawable.Id == id)
                {
                    Debug.LogWarning("Drawable with Id"+id+" already exists, please assign another Id");
                    return;
                }
            } */
            var drawableResource = new DrawableResourceData();
            drawableResource.Id = id;
            drawableResource.Type = type;
            drawableResource.Asset = image;

            DrawableResources.Add(drawableResource);
            SaveSettings();
        }

        public void RemoveDrawableResourceByIndex(int index)
        {
            if (index < DrawableResources.Count && index >= 0)
            {
                DrawableResources.RemoveAt(index);
                SaveSettings();
            }
            else
            {
                Debug.LogWarning("Invalid drawable index provided, drawable not removed.");
            }
        }

        public void RemoveDrawableResourceById(string id)
        {
            DrawableResourceData DrawableRes = null;
            foreach (var drawable in DrawableResources)
            {
                if (drawable.Id == id)
                {
                    DrawableRes = drawable;
                    break;
                }
            }
            if (DrawableRes == null)
            {
                Debug.LogWarning("Drawable with Id " + id + " not found. Drawable not removed.");
            }
            else
            {
                DrawableResources.Remove(DrawableRes);
                SaveSettings();
            }
        }

        public void ClearDrawableResources()
        {
            DrawableResources.Clear();
            SaveSettings();
        }

        public Dictionary<string, byte[]> GenerateDrawableResourcesForExport()
        {
            var icons = new Dictionary<string, byte[]>();
            foreach (var drawableResource in DrawableResources)
            {
                if (!drawableResource.Verify())
                {
                    Debug.LogWarning(string.Format("Failed exporting: '{0}' AndroidSettings notification icon because:\n {1} ",
                        drawableResource.Id,
                        drawableResource.GenerateErrorString()));
                    continue;
                }

                var texture = TextureAssetUtils.ProcessTextureForType(drawableResource.Asset, drawableResource.Type);

                var scale = drawableResource.Type == NotificationIconType.Small ? 0.375f : 1;

                var textXhdpi = TextureAssetUtils.ScaleTexture(texture, (int)(128 * scale), (int)(128 * scale));
                var textHdpi = TextureAssetUtils.ScaleTexture(texture, (int)(96 * scale), (int)(96 * scale));
                var textMdpi = TextureAssetUtils.ScaleTexture(texture, (int)(64 * scale), (int)(64 * scale));
                var textLdpi = TextureAssetUtils.ScaleTexture(texture, (int)(48 * scale), (int)(48 * scale));

                icons[string.Format("drawable-xhdpi-v11/{0}.png", drawableResource.Id)] = textXhdpi.EncodeToPNG();
                icons[string.Format("drawable-hdpi-v11/{0}.png", drawableResource.Id)] = textHdpi.EncodeToPNG();
                icons[string.Format("drawable-mdpi-v11/{0}.png", drawableResource.Id)] = textMdpi.EncodeToPNG();
                icons[string.Format("drawable-ldpi-v11/{0}.png", drawableResource.Id)] = textLdpi.EncodeToPNG();

                if (drawableResource.Type == NotificationIconType.Large)
                {
                    var textXxhdpi = TextureAssetUtils.ScaleTexture(texture, (int)(192 * scale), (int)(192 * scale));
                    icons[string.Format("drawable-xxhdpi-v11/{0}.png", drawableResource.Id)] = textXxhdpi.EncodeToPNG();
                }
            }

            return icons;
        }
    }
}

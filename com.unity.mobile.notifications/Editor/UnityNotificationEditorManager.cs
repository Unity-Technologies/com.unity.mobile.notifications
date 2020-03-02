using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

using Unity.Notifications.iOS;

[assembly: InternalsVisibleTo("Unity.Notifications.Tests")]
namespace Unity.Notifications
{
    [HelpURL("Packages/com.unity.mobile.notifications/documentation.html")]
    internal class UnityNotificationEditorManager : ScriptableObject
    {
        internal const string ASSET_PATH = "Editor/com.unity.mobile.notifications/NotificationSettings.asset";

        [SerializeField]
        public int toolbarInt = 0;

        public List<NotificationEditorSetting> iOSNotificationEditorSettings;
        public List<NotificationEditorSetting> AndroidNotificationEditorSettings;

        [SerializeField]
        internal NotificationEditorSettingsCollection iOSNotificationEditorSettingsValues;

        [SerializeField]
        internal NotificationEditorSettingsCollection AndroidNotificationEditorSettingsValues;

        private void SaveSetting(NotificationEditorSetting setting, NotificationEditorSettingsCollection values)
        {
            if (!values.Contains(setting.key) || values[setting.key].ToString() != setting.val.ToString())
            {
                values[setting.key] = setting.val;
                EditorUtility.SetDirty(this);
            }
        }

        internal void SerializeData()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void SaveSetting(NotificationEditorSetting setting, BuildTargetGroup target)
        {
            if (target == BuildTargetGroup.Android)
            {
                this.SaveSetting(setting, AndroidNotificationEditorSettingsValues);
            }
            else
            {
                this.SaveSetting(setting, iOSNotificationEditorSettingsValues);
            }
        }

        public T GetAndroidNotificationEditorSettingsValue<T>(string key, T defaultValue)
        {
            try
            {
                var val = AndroidNotificationEditorSettingsValues[key];
                if (val != null)
                    return (T)val;
            }
            catch (InvalidCastException)
            {
                //Just return default value if it's a new setting that was not yet serialized.
            }

            AndroidNotificationEditorSettingsValues[key] = defaultValue;
            return defaultValue;
        }

        public T GetiOSNotificationEditorSettingsValue<T>(string key, T defaultValue)
        {
            try
            {
                var val = iOSNotificationEditorSettingsValues[key];
                if (val != null)
                    return (T)val;
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning("Failed loading : " + key + " for type:" + defaultValue.GetType() + "Expe cted : " + iOSNotificationEditorSettingsValues[key].GetType());
                //Just return default value if it's a new setting that was not yet serialized.
            }

            iOSNotificationEditorSettingsValues[key] = defaultValue;
            return defaultValue;
        }

        private void FlattenList(List<NotificationEditorSetting> source, List<NotificationEditorSetting> target)
        {
            foreach (var setting in source)
            {
                target.Add(setting);

                if (setting.dependentSettings != null)
                {
                    FlattenList(setting.dependentSettings, target);
                }
            }
        }

        public List<NotificationEditorSetting> iOSNotificationEditorSettingsFlat
        {
            get
            {
                var target = new List<NotificationEditorSetting>();
                FlattenList(iOSNotificationEditorSettings, target);
                return target;
            }
        }

        public List<NotificationEditorSetting> AndroidNotificationEditorSettingsFlat
        {
            get
            {
                var target = new List<NotificationEditorSetting>();
                FlattenList(AndroidNotificationEditorSettings, target);
                return target;
            }
        }

        [SerializeField]
        public List<DrawableResourceData> TrackedResourceAssets = new List<DrawableResourceData>();

        internal Editor CustomEditor { get; set; }

        [InitializeOnLoadMethod]
        internal static void OnProjectLoaded()
        {
            Initialize();
        }

        internal static void DeleteSettings()
        {
            var assetRelPath = Path.Combine("Assets", ASSET_PATH);

            if (File.Exists(assetRelPath))
            {
                File.Delete(assetRelPath);
            }
        }

        internal static UnityNotificationEditorManager Initialize()
        {
            var assetRelPath = Path.Combine("Assets", ASSET_PATH);

            var notificationEditorManager =
                (UnityNotificationEditorManager)AssetDatabase.LoadAssetAtPath(assetRelPath,
                    typeof(UnityNotificationEditorManager));

            if (notificationEditorManager == null)
            {
                var rootDir = Path.Combine(Application.dataPath, Path.GetDirectoryName(ASSET_PATH));


                if (!Directory.Exists(rootDir))
                {
                    Directory.CreateDirectory(rootDir);
                }


                notificationEditorManager = CreateInstance<UnityNotificationEditorManager>();

                if (File.Exists(assetRelPath))
                    AssetDatabase.ImportAsset(assetRelPath);
                else
                {
                    AssetDatabase.CreateAsset(notificationEditorManager, assetRelPath);
                    AssetDatabase.SaveAssets();
                }
            }

            if (notificationEditorManager.iOSNotificationEditorSettingsValues == null)
            {
                notificationEditorManager.iOSNotificationEditorSettingsValues =
                    new NotificationEditorSettingsCollection();
            }

            if (notificationEditorManager.AndroidNotificationEditorSettingsValues == null)
            {
                notificationEditorManager.AndroidNotificationEditorSettingsValues =
                    new NotificationEditorSettingsCollection();
            }

            var iosSettings = new List<NotificationEditorSetting>()
            {
                new NotificationEditorSetting(
                    "UnityNotificationRequestAuthorizationOnAppLaunch",
                    "Request Authorization on App Launch",
                    "It's recommended to make the authorization request during the app's launch cycle. If this is enabled the user will be shown the authorization pop-up immediately when the app launches. If it’s unchecked you’ll need to manually create an AuthorizationRequest before your app can send or receive notifications.",
                    notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                        "UnityNotificationRequestAuthorizationOnAppLaunch", true),
                    dependentSettings: new List<NotificationEditorSetting>()
                    {
                        new NotificationEditorSetting(
                            "UnityNotificationDefaultAuthorizationOptions",
                            "Default Notification Authorization Options",
                            "Configure the notification interaction types your app will include in the authorisation request  if  “Request Authorisation on App Launch” is enabled. Alternatively you can specify them when creating a `AuthorizationRequest` from a script.",
                            notificationEditorManager.GetiOSNotificationEditorSettingsValue<AuthorizationOption>(
                                "UnityNotificationDefaultAuthorizationOptions",
                                (AuthorizationOption)AuthorizationOption.Alert | AuthorizationOption.Badge |
                                AuthorizationOption.Sound)
                        ),
                    }),

                new NotificationEditorSetting(
                    "UnityAddRemoteNotificationCapability",
                    "Enable Push Notifications",
                    "Enable this to add the push notification capability to you Xcode project.",
                    notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                        "UnityAddRemoteNotificationCapability", false),
                    false,
                    dependentSettings: new List<NotificationEditorSetting>()
                    {
                        new NotificationEditorSetting(
                            "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch",
                            "Register for Push Notifications on App Launch",
                            "If this is enabled the app will automatically register your app with APNs after the launch which would enable it to receive remote notifications. You’ll have to manually create a AuthorizationRequest to get the device token.",
                            notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                                "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch",
                                false)
                        ),
                        new NotificationEditorSetting(
                            "UnityRemoteNotificationForegroundPresentationOptions",
                            "Remote Notification Foreground Presentation Options",
                            "The default presentation options for received remote notifications. In order for the specified presentation options to be used your app must had received the authorization to use them (the user might change it at any time). ",
                            notificationEditorManager
                                .GetiOSNotificationEditorSettingsValue<PresentationOption>(
                                "UnityRemoteNotificationForegroundPresentationOptions",
                                (PresentationOption)PresentationOptionEditor.All)
                        ),
                        new NotificationEditorSetting("UnityUseAPSReleaseEnvironment",
                            "Enable release environment for APS",
                            "Enable this when signing the app with a production certificate.",
                            notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                                "UnityUseAPSReleaseEnvironment", false),
                            false),
                    }
                ),

                new NotificationEditorSetting("UnityUseLocationNotificationTrigger",
                    "Include CoreLocation framework",
                    "If you intend to use the iOSNotificationLocationTrigger in your notifications you must include the CoreLocation framework in your project.",
                    notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                        "UnityUseLocationNotificationTrigger", false),
                    false)
            };


            if (notificationEditorManager.iOSNotificationEditorSettings == null ||
                notificationEditorManager.iOSNotificationEditorSettings.Count != iosSettings.Count)
            {
                notificationEditorManager.iOSNotificationEditorSettings = iosSettings;
            }


            var androidSettings = new List<NotificationEditorSetting>()
            {
                new NotificationEditorSetting(
                    "UnityNotificationAndroidRescheduleOnDeviceRestart",
                    "Reschedule Notifications on Device Restart",
                    "By default AndroidSettings removes all scheduled notifications when the device is restarted. Enable this to automatically reschedule all non expired notifications when the device is turned back on.",
                    notificationEditorManager.GetAndroidNotificationEditorSettingsValue<bool>(
                        "UnityNotificationAndroidRescheduleOnDeviceRestart", false),
                    dependentSettings: null),

                new NotificationEditorSetting(
                    "UnityNotificationAndroidUseCustomActivity",
                    "Use Custom AndroidActivity",
                    "Enable this if you want to override the activity which will opened when the user click on the notification. By default activity assigned to `com.unity3d.player.UnityPlayer.currentActivity` will be used.",
                    notificationEditorManager.GetAndroidNotificationEditorSettingsValue<bool>(
                        "UnityNotificationAndroidUseCustomActivity", false),
                    dependentSettings: new List<NotificationEditorSetting>()
                    {
                        new NotificationEditorSetting(
                            "UnityNotificationAndroidCustomActivityString",
                            "Custom Android Activity Name",
                            "The full class name of the activity that you wish to be assigned to the notification.",
                            notificationEditorManager.GetAndroidNotificationEditorSettingsValue<string>(
                                "UnityNotificationAndroidCustomActivityString",
                                "com.unity3d.player.UnityPlayerActivity"),
                            dependentSettings: null
                        ),
                    }),
            };
            if (notificationEditorManager.AndroidNotificationEditorSettings == null ||
                notificationEditorManager.AndroidNotificationEditorSettings.Count != androidSettings.Count)
            {
                notificationEditorManager.AndroidNotificationEditorSettings = androidSettings;
            }

            EditorUtility.SetDirty(notificationEditorManager);
            return notificationEditorManager;
        }

        internal void RegisterDrawableResource(string id, Texture2D image, NotificationIconType type)
        {
            var drawableResource = new DrawableResourceData();
            drawableResource.Id = id;
            drawableResource.Type = type;
            drawableResource.Asset = image;

            TrackedResourceAssets.Add(drawableResource);
            SerializeData();
        }

        internal void RemoveDrawableResource(string id)
        {
            TrackedResourceAssets.RemoveAll(i => i.Id == id);
            SerializeData();
        }

        internal void RemoveDrawableResource(int i)
        {
            TrackedResourceAssets.RemoveAt(i);
            SerializeData();
        }

        internal Texture2D GetDrawableResourceAssetById(string id)
        {
            var res = TrackedResourceAssets.Find(i => i.Id == id);
            return res.Asset as Texture2D;
        }

        internal Dictionary<string, byte[]> GenerateDrawableResourcesForExport()
        {
            var icons = new Dictionary<string, byte[]>();
            foreach (var res in TrackedResourceAssets)
            {
                if (!res.Verify())
                {
                    Debug.LogWarning(string.Format("Failed exporting: '{0}' AndroidSettings notification icon because:\n {1} ", res.Id,
                        DrawableResourceData.GenerateErrorString(res.Errors))
                    );
                    continue;
                }

                var texture = TextureAssetUtils.ProcessTextureForType(res.Asset, res.Type);

                var scale = res.Type == NotificationIconType.SmallIcon ? 0.375f : 1;

                var textXhdpi = TextureAssetUtils.ScaleTexture(texture, (int)(128 * scale), (int)(128 * scale));
                var textHdpi  = TextureAssetUtils.ScaleTexture(texture, (int)(96 * scale), (int)(96 * scale));
                var textMdpi  = TextureAssetUtils.ScaleTexture(texture, (int)(64 * scale), (int)(64 * scale));
                var textLdpi  = TextureAssetUtils.ScaleTexture(texture, (int)(48 * scale), (int)(48 * scale));

                icons[string.Format("drawable-xhdpi-v11/{0}.png", res.Id)] = textXhdpi.EncodeToPNG();
                icons[string.Format("drawable-hdpi-v11/{0}.png", res.Id)] = textHdpi.EncodeToPNG();
                icons[string.Format("drawable-mdpi-v11/{0}.png", res.Id)] = textMdpi.EncodeToPNG();
                icons[string.Format("drawable-ldpi-v11/{0}.png", res.Id)] = textLdpi.EncodeToPNG();

                if (res.Type == NotificationIconType.LargeIcon)
                {
                    var textXxhdpi = TextureAssetUtils.ScaleTexture(texture, (int)(192 * scale), (int)(192 * scale));
                    icons[string.Format("drawable-xxhdpi-v11/{0}.png", res.Id)] = textXxhdpi.EncodeToPNG();
                }
            }

            return icons;
        }
    }
}

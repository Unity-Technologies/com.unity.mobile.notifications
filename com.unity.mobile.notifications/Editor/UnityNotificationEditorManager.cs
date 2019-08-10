#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

using UnityEditor.Android;
using Unity.Notifications.iOS;
using Unity.Notifications;
using UnityEditor.VersionControl;
using Object = System.Object;

#pragma warning disable 219

[assembly: InternalsVisibleTo("Unity.Notifications.Tests")]
namespace Unity.Notifications
{
    internal enum NotificationIconType
    {
        SmallIcon = 0,
        LargeIcon = 1
    }
    
    [Flags]
    internal enum PresentationOptionEditor
    {
        Badge = 1 << 0,
        Sound = 1 << 1,
        Alert = 1 << 2,
        All = ~0,
    }
    
    [Flags]
    internal enum AuthorizationOptionEditor
    {
        Badge = 1 << 0,
        Sound = 1 << 1,
        Alert = 1 << 2,
        CarPlay = (1 << 3),
        All = ~0,
    }


    [System.Serializable]
    internal class DrawableResourceData
    {
        public string Id;
        public NotificationIconType Type;
        public Texture2D Asset;

        private bool isValid = false;
        private List<string> errors = null;
        private Texture2D previewTexture;
        
        public bool IsValid
        {
            get
            {
                if (isValid == false && errors == null)
                    Verify();

                return isValid;
            }
        }
        
        public string[] Errors
        {
            get
            {
                if (isValid == false && errors == null)
                    Verify();

                return errors.ToArray();
            }
        }

        public Texture2D GetPreviewTexture(bool update)
        {
            if (Asset == null)
            {
                return null;
            }
                
            if (isValid && (previewTexture == null || update))
                previewTexture = TextureAssetUtils.ProcessTextureForType(Asset, Type);
                        
            return previewTexture;
        }
        
        internal bool Initialized()
        {
            return !string.IsNullOrEmpty(Id) && Asset != null;
        }

        public void Clean()
        {
            isValid = false;
            errors = null;
            previewTexture = null;
        }
        
        public bool Verify()
        {
            List<string> errors;
            isValid = TextureAssetUtils.VerifyTextureByType(Asset, Type, out errors);
            this.errors = errors;
            return isValid;
        }

        public static string GenerateErrorString(string[] errors)
        {
            var error = "";

            for (var i = 0;  i < errors.Length; i++)
            {
                error += string.Format("{0}{1}", errors[i], i + 1 >= errors.Length ? "." : ", ");
            }
            
            return error;
        }

    }

    internal class NotificationEditorSetting
    {
        public string key;
        public string label;
        public string tooltip;
        public object val;
        public bool writeToPlist;

        public List<NotificationEditorSetting> dependentSettings;
        public List<string> requiredSettings;
        
        public NotificationEditorSetting(string key, string label, string tooltip, object val, bool writeToPlist  = true, List<NotificationEditorSetting> dependentSettings = null, List<string> requiredSettings = null)
        {
            this.key = key;
            this.label = label;
            this.tooltip = tooltip;
            this.val = val;
            this.writeToPlist = writeToPlist;
            this.dependentSettings = dependentSettings;
            this.requiredSettings = requiredSettings;
        }
    }
    
    [System.Serializable]
    internal class NotificationEditorSettingsCollection
    {
        [SerializeField] List<string> keys;
        [SerializeField] List<string> values;

        public NotificationEditorSettingsCollection()
        {
            keys = new List<string>();
            values = new List<string>();
        }

        public bool Contains(string key)
        {
            return keys.Contains(key);
        }
        
        public object this[string key]
        {
            get
            {
                var index = keys.IndexOf(key);
                if (index == -1 || values.Count <= index)
                    return null;

                var intValue = 0;
                var boolValue = false;
                if (int.TryParse(values[index], out intValue))
                {
                    return intValue;
                }

                else if(bool.TryParse(values[index], out boolValue))
                {
                    return boolValue;
                }
                return values[index];
            }
            set
            {
                string strValue;

                if (value is Enum)
                {
                    strValue = ((int) value).ToString();
                }
                else
                {
                    strValue = value.ToString();
                }
                
                
                var index = keys.IndexOf(key);
                if (index == -1)
                {
                    keys.Add(key);
                    values.Add(strValue);
                }
                else
                    values[index] = strValue.ToString();
            }
        }
    }
    
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
                    return (T) val;
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
                    return (T) val;
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
                (UnityNotificationEditorManager) AssetDatabase.LoadAssetAtPath(assetRelPath,
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
                                (AuthorizationOption) AuthorizationOption.Alert | AuthorizationOption.Badge |
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
                                    (PresentationOption) PresentationOptionEditor.All)
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
            return  res.Asset as Texture2D;
        }

        internal Dictionary<string, byte[]> GenerateDrawableResourcesForExport()
        {
            var icons = new Dictionary<string, byte[]>();
            foreach (var res in TrackedResourceAssets)
            {
                if (!res.Verify())
                {
                    Debug.LogWarning( string.Format("Failed exporting: '{0}' AndroidSettings notification icon because:\n {1} ", res.Id,
                        DrawableResourceData.GenerateErrorString(res.Errors))
                    );
                    continue;
                }
                
                var texture = TextureAssetUtils.ProcessTextureForType(res.Asset, res.Type);

                var scale = res.Type == NotificationIconType.SmallIcon ? 0.375f : 1;
                                               
                var textXhdpi = TextureAssetUtils.ScaleTexture(texture, (int) (128 * scale), (int) (128 * scale));
                var textHdpi  = TextureAssetUtils.ScaleTexture(texture, (int) (96 * scale), (int) (96 * scale));
                var textMdpi  = TextureAssetUtils.ScaleTexture(texture, (int) (64 * scale), (int) (64 * scale));
                var textLdpi  = TextureAssetUtils.ScaleTexture(texture, (int) (48 * scale), (int) (48 * scale));

                icons[string.Format("drawable-xhdpi-v11/{0}.png", res.Id)] = textXhdpi.EncodeToPNG();
                icons[string.Format("drawable-hdpi-v11/{0}.png", res.Id)] = textHdpi.EncodeToPNG();
                icons[string.Format("drawable-mdpi-v11/{0}.png", res.Id)] = textMdpi.EncodeToPNG();
                icons[string.Format("drawable-ldpi-v11/{0}.png", res.Id)] = textLdpi.EncodeToPNG();

                if (res.Type == NotificationIconType.LargeIcon)
                {
                    var textXxhdpi = TextureAssetUtils.ScaleTexture(texture, (int) (192 * scale), (int) (192 * scale));
                    icons[string.Format("drawable-xxhdpi-v11/{0}.png", res.Id)] = textXhdpi.EncodeToPNG();
                }
            }

            return icons;
        }
    }

    internal static class TextureAssetUtils
    {
        public static bool VerifyTextureByType(Texture2D texture, NotificationIconType type, out List<string> errors)
        {            
            errors = new List<string>();

            if (texture == null)
            {
                errors.Add("no valid texture is assigned");
                return false;
            }

            var needsAlpha = true;
            var minSize = 48;

            if (type == NotificationIconType.LargeIcon)
            {
                needsAlpha = false;
                minSize = 192;
            }

            var isSquare = texture.width == texture.height;
            var isLargeEnough = texture.width >= minSize;
            var hasAlpha = true;// texture.format == TextureFormat.Alpha8;

            var isReadable = texture.isReadable;
                
            if (!isReadable)
            {
                errors.Add("Read/Write is not enabled in the texture importer");
            }
                
            if (!isLargeEnough)
                errors.Add(string.Format("it must be atleast {0}x{1} pixels (while it's {2}x{3})",
                    minSize,
                    minSize,
                    texture.width,
                    texture.height
                ));
                
            if (!isSquare)
                errors.Add(string.Format("it must have the same width and height (while it's {0}x{1})",
                    texture.width,
                    texture.height
                ));
                                
            if (!hasAlpha && needsAlpha)
                errors.Add(string.Format("contain an alpha channel"));

            return isReadable && isSquare && isLargeEnough && (!needsAlpha || hasAlpha);
        }

        public static Texture2D ProcessTextureForType(Texture2D sourceTexture, NotificationIconType type)
        {
            if (sourceTexture == null)
                return null;
                    
            string assetPath = AssetDatabase.GetAssetPath( sourceTexture );
            var importer = AssetImporter.GetAtPath( assetPath ) as TextureImporter;

            if (importer == null || !importer.isReadable)
                return null;

            var textureFormat = type == NotificationIconType.LargeIcon ? sourceTexture.format : TextureFormat.RGBA32;

            Texture2D texture;
            if (type == NotificationIconType.SmallIcon)
            {
                texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, true, false);
                for (var i  = 0; i < sourceTexture.mipmapCount; i++) {
                    var c_0 = sourceTexture.GetPixels(i);
                    var c_1 = texture.GetPixels(i);
                    for (var i1 = 0 ;i1 < c_0.Length; i1++)
                    {
                        var a = c_0[i1].r + c_0[i1].g + c_0[i1].b;
                        c_1[i1].r = c_1[i1].g = c_1[i1].b = a > 127 ? 0 : 1;
                        c_1[i1].a = c_0[i1].a;
                        
                    }
                    texture.SetPixels(c_1, i);
                }
                texture.Apply();
            }
            else
            {                
                texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, true);
                texture.SetPixels(sourceTexture.GetPixels());
                texture.Apply();
            }
            return texture;
        }
        
        public static Texture2D ScaleTexture(Texture2D sourceTexture, int width, int height)
        {

            if (sourceTexture.width == width && sourceTexture.height == sourceTexture.height)
                return sourceTexture;
            
            Rect rect = new Rect(0,0,width,height);

            sourceTexture.filterMode = FilterMode.Trilinear;
            sourceTexture.Apply(true);       
                               
            RenderTexture rtt = new RenderTexture(width, height, 32);
            Graphics.SetRenderTarget(rtt);
               
            GL.LoadPixelMatrix(0,1,1,0);
            GL.Clear(true,true,new Color(0,0,0,0));
            
            Graphics.DrawTexture(new Rect(0,0,1,1),sourceTexture);

            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
            result.Resize(width, height);
            result.ReadPixels(rect,0,0,true);
            return result;                 
        }

    }

#if UNITY_EDITOR  && PLATFORM_ANDROID
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
#endif
}
#endif

#pragma warning restore 219
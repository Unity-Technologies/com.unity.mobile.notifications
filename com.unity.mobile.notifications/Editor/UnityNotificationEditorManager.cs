#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

using UnityEditor.Android;
using Unity.Notifications.iOS;
using Unity.Notifications;

#pragma warning disable 219

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

    [System.Serializable]
    internal class DrawableResourceData
    {
        public string Id;
        public NotificationIconType Type;
        public Texture2D Asset;
        
        public Texture2D AssetXXHDPI;
        public Texture2D AssetXHDPI;
        public Texture2D AssetMDPI;
        public Texture2D AssetHDPI;
        public Texture2D AssetLDPI;


        private bool isValid = false;
        private List<string> errors = null;
        
        internal Texture2D previewTexture;
        internal bool showOtherSizes;
        
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
        
        public NotificationEditorSetting(string key, string label, string tooltip, object val, bool writeToPlist  = true, List<NotificationEditorSetting> dependentSettings = null)
        {
            this.key = key;
            this.label = label;
            this.tooltip = tooltip;
            this.val = val;
            this.writeToPlist = writeToPlist;
            this.dependentSettings = dependentSettings;
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
                var index = keys.IndexOf(key);
                if (index == -1)
                {
                    keys.Add(key);
                    values.Add(value.ToString());
                }
                else
                    values[index] = value.ToString();
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
            if (values == null )
                values = new NotificationEditorSettingsCollection();

            if (!values.Contains(setting.key) || values[setting.key].ToString() != setting.val.ToString())
            {
                values[setting.key] = setting.val;
                
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
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
            if (AndroidNotificationEditorSettingsValues == null)
                AndroidNotificationEditorSettingsValues = new NotificationEditorSettingsCollection();

            try
            {
                var val = AndroidNotificationEditorSettingsValues[key];
                if (val != null)
                    return (T) val;
            }
            catch (InvalidCastException ex)
            {
                Debug.LogWarning(ex.ToString());
                AndroidNotificationEditorSettingsValues = new NotificationEditorSettingsCollection();
            }

            AndroidNotificationEditorSettingsValues[key] = defaultValue;
            return defaultValue;

        }

        public T GetiOSNotificationEditorSettingsValue<T>(string key, T defaultValue)
        {

            if (iOSNotificationEditorSettingsValues == null)
                iOSNotificationEditorSettingsValues = new NotificationEditorSettingsCollection();

            try
            {
                var val = iOSNotificationEditorSettingsValues[key];
                if (val != null)
                    return (T) val;
            }
            catch (InvalidCastException ex)
            {
                Debug.LogWarning(ex.ToString());
                iOSNotificationEditorSettingsValues = new NotificationEditorSettingsCollection();
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
        
        internal static UnityNotificationEditorManager Initialize()
        {
            var notificationEditorManager =
                AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", ASSET_PATH), typeof(UnityNotificationEditorManager)) as
                    UnityNotificationEditorManager;
            if (notificationEditorManager == null)
            {
                var roothDir = Path.Combine(Application.dataPath, Path.GetDirectoryName(ASSET_PATH));
                var assetRelPath = Path.Combine("Assets", ASSET_PATH);
                                                
                if (!Directory.Exists(roothDir))
                {
                    Directory.CreateDirectory(roothDir);
                }
                
                notificationEditorManager = CreateInstance<UnityNotificationEditorManager>();
                AssetDatabase.CreateAsset(notificationEditorManager, assetRelPath);
                AssetDatabase.SaveAssets();
            }

            if (notificationEditorManager.iOSNotificationEditorSettings == null)
            {
                notificationEditorManager.iOSNotificationEditorSettings = new List<NotificationEditorSetting>()
                {

                    new NotificationEditorSetting(
                        "UnityNotificationRequestAuthorizationOnAppLaunch",
                        "Request Authorization on App Launch",
                        "It's recommended f to make the authorization request during the app's launch cycle. If this is enabled the user will be shown the authorizatio popup immediately when the app launches. If it’s unchecked you’ll need to manually create an AuthorizationRequest before your app can send or receive notifications.",
                        notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                            "UnityNotificationRequestAuthorizationOnAppLaunch", true),
                        dependentSettings: new List<NotificationEditorSetting>()
                        {
                            new NotificationEditorSetting(
                                "UnityNotificationDefaultAuthorizationOptions",
                                "Default Notification Authorization Options",
                                "Configure the notification interaction types your app will include in the authorisation request  if  “Request Authorisation on App Launch” is enabled. Slternatively you can specify them when creating a `AuthorizationRequest` from a script.",
                                notificationEditorManager.GetiOSNotificationEditorSettingsValue<PresentationOption>(
                                    "UnityNotificationDefaultAuthorizationOptions",
                                    (PresentationOption) PresentationOptionEditor.All)),

                        }),

                    new NotificationEditorSetting("UnityUseLocationNotificationTrigger",
                        "Include CoreLocation framework",
                        "If you intend to use the iOSNotificationLocationTrigger in your notifications you must include the CoreLocation framework in your project.",
                        notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                            "UnityUseLocationNotificationTrigger", false),
                        false),
                    
                    new NotificationEditorSetting("UnityAddRemoteNotificationCapability",
                        "Enable Push Notifications",
                        "Enable this to add the push notification capability to you Xcode project.",
                        notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                            "UnityAPSReleaseEnvironment", false),
                        false,
                        dependentSettings: new List<NotificationEditorSetting>()
                        {     
                            new NotificationEditorSetting(
                                "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch",
                                "Register for Push Notifications on App Launch",
                                "If this is enabled the app will automatically register your app with APNs after the launch which would enable it to receive remote notifications. You’ll have to manually create a AuthorizationRequest to get the device token.",
                                notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                                    "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch", false),

                                dependentSettings: new List<NotificationEditorSetting>()
                                {
                                    new NotificationEditorSetting(
                                        "UnityRemoteNotificationForegroundPresentationOptions",
                                        "Remote Notification Foreground Presentation Options",
                                        "The default presentation options for received remote notifications. In order for the specified presentation options to be used your app must had received the authorisation to use them (the user might change it at any time). ",
                                        notificationEditorManager
                                            .GetiOSNotificationEditorSettingsValue<PresentationOption>(
                                                "UnityRemoteNotificationForegroundPresentationOptions",
                                                (PresentationOption) PresentationOptionEditor.All)),
                                }),
                            new NotificationEditorSetting("UnityAPSReleaseEnvironment",
                                "Enable release environment for APS",
                                "Enable this when signing the app with a production certificate.",
                                notificationEditorManager.GetiOSNotificationEditorSettingsValue<bool>(
                                    "UnityAPSReleaseEnvironment", false),
                                false),
                        })
                    };
            }

            if (notificationEditorManager.AndroidNotificationEditorSettings == null)
            {
                notificationEditorManager.AndroidNotificationEditorSettings = new List<NotificationEditorSetting>()
                {

                    new NotificationEditorSetting(
                        "UnityNotificationAndroidRescheduleOnDeviceRestart",
                        "Reschedule Notifications on Device Restart",
                        "By default Android removes all scheduled notifications when the device is restarted. Enable this to automatically reschedule all non expired notifications when the device is turned back on.",
                        notificationEditorManager.GetAndroidNotificationEditorSettingsValue<bool>(
                            "UnityNotificationAndroidRescheduleOnDeviceRestart", false),
                        dependentSettings: null),
                };
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
        }

        internal void RemoveDrawableResource(string id)
        {
            TrackedResourceAssets.RemoveAll(i => i.Id == id);
        }
        
        internal void RemoveDrawableResource(int i)
        {
            TrackedResourceAssets.RemoveAt(i);
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
                    Debug.LogWarning( string.Format("Failed exporting: '{0}' Android notification icon because:\n {1} ", res.Id,
                        DrawableResourceData.GenerateErrorString(res.Errors))
                    );
                    continue;
                }
                                               
                var textXhdpi = TextureAssetUtils.ProcessAndResizeTextureForType(res.Asset, res.Type, ImageSize.XHDPI);
                var textHdpi  = TextureAssetUtils.ProcessAndResizeTextureForType(res.Asset, res.Type, ImageSize.HDPI);
                var textMdpi  = TextureAssetUtils.ProcessAndResizeTextureForType(res.Asset, res.Type, ImageSize.MDPI);
                var textLdpi  = TextureAssetUtils.ProcessAndResizeTextureForType(res.Asset, res.Type, ImageSize.LDPI);

                icons[string.Format("drawable-xhdpi-v11/{0}.png", res.Id)] = textXhdpi.EncodeToPNG();
                icons[string.Format("drawable-hdpi-v11/{0}.png", res.Id)] = textHdpi.EncodeToPNG();
                icons[string.Format("drawable-mdpi-v11/{0}.png", res.Id)] = textMdpi.EncodeToPNG();
                icons[string.Format("drawable-ldpi-v11/{0}.png", res.Id)] = textLdpi.EncodeToPNG();

                if (res.Type == NotificationIconType.LargeIcon)
                {
                    var textXxhdpi = TextureAssetUtils.ProcessAndResizeTextureForType(res.Asset, res.Type, ImageSize.XXHDPI);
                    icons[string.Format("drawable-xxhdpi-v11/{0}.png", res.Id)] = textXxhdpi.EncodeToPNG();
                }
            }

            return icons;
        }
    }



    public enum ImageSize
    {
        XXHDPI,
        XHDPI,
        HDPI, 
        MDPI,
        LDPI,
        
    }

    internal static class TextureAssetUtils
    {

        public static Texture2D ProcessAndResizeTextureForType(Texture2D texture, NotificationIconType type, ImageSize size)
        {
            var width = 0;
            var height = 0;
            var scale = type == NotificationIconType.SmallIcon ? 0.375f : 1;

            if (size == ImageSize.XXHDPI)
            {
                width = (int)(192 * scale);
                height =  (int)(192 * scale);
            }
            else if (size == ImageSize.XHDPI)
            {
                width = (int)(128 * scale);
                height =  (int)(128 * scale);
            }
            else if (size == ImageSize.HDPI)
            {
                width = (int)(96 * scale);
                height =  (int)(96 * scale);
            }
            else if (size == ImageSize.MDPI)
            {
                width = (int)(64 * scale);
                height =  (int)(64 * scale);
            }
            else if (size == ImageSize.LDPI)
            {
                width = (int)(48 * scale);
                height =  (int)(48 * scale);
            }
            
            var downscaled = TextureAssetUtils.ScaleTexture(texture, width, height);
            return TextureAssetUtils.ProcessTextureForType(downscaled, type);
        }
        
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

            string assetPath = AssetDatabase.GetAssetPath( texture );
            var importer = AssetImporter.GetAtPath( assetPath ) as TextureImporter;

            var isReadable = importer != null && importer.isReadable;
                
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

            if (importer != null && !importer.isReadable)
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
                    sourceTexture.filterMode = FilterMode.Point;
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
        
        public static Texture2D ScaleTextureNew(Texture2D source,int targetWidth,int targetHeight) {
            Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,true);
            Color[] rpixels=result.GetPixels(0);
            float incX=((float)1/source.width)*((float)source.width/targetWidth);
            float incY=((float)1/source.height)*((float)source.height/targetHeight);
            for(int px=0; px<rpixels.Length; px++) {
                rpixels[px] = source.GetPixelBilinear(incX*((float)px%targetWidth),
                    incY*((float)Mathf.Floor(px/targetWidth)));
            }
            result.SetPixels(rpixels,0);
            result.Apply();
            return result;
        }
        
        public static Texture2D ScaleTexture(Texture2D sourceTexture, int width, int height)
        {

            if (sourceTexture.width == width && sourceTexture.height == sourceTexture.height)
                return sourceTexture;
            
            
            Rect rect = new Rect(0,0,width,height);

            sourceTexture.filterMode = FilterMode.Point;
            sourceTexture.Apply(true);       
                               
            RenderTexture rtt = new RenderTexture(width, height, 32);
            Graphics.SetRenderTarget(rtt);
               
            GL.LoadPixelMatrix(0,1,1,0);
            GL.Clear(true,true,new Color(0,0,0,0));
            
            Graphics.DrawTexture(new Rect(0,0,1,1),sourceTexture);

            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
            result.Resize(width, height);
            result.ReadPixels(rect,0,0,true);
            result.Apply(true, false);
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


            if (enableRescheduleOnRestart)
            {
                string manifestPath = string.Format("{0}/src/main/AndroidManifest.xml", projectPath);
                XmlDocument manifestDoc = new XmlDocument();
                manifestDoc.Load(manifestPath);

                var doc = AppendAndroidMetadataField(manifestDoc, "reschedule_notifications_on_restart", "true");
                doc = AndroidNotificationResourcesPostProcessor.AppendAndroidPermissionField(doc, "android.permission.RECEIVE_BOOT_COMPLETED");
                
                doc.Save(manifestPath);
            }
            
            // meta-data android:name="reschedule_notifications_on_restart" android:value="true"
        }
    }
    #endif
}
#endif

#pragma warning restore 219
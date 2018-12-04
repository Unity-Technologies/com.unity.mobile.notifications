#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#if PLATFORM_ANDROID
using UnityEditor.Android;
#endif

using Unity.Notifications.iOS;
using UnityEngine;


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
        
        public void Verify()
        {
            List<string> errors;
            isValid = TextureAssetUtils.VerifyTextureByType(Asset, Type, out errors);
            this.errors = errors;
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
    internal class iOSNotificationEditorSettingsCollection
    {
        [SerializeField] List<string> keys;
        [SerializeField] List<string> values;

        public iOSNotificationEditorSettingsCollection()
        {
            keys = new List<string>();
            values = new List<string>();
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

        [SerializeField] 
        internal iOSNotificationEditorSettingsCollection iOSNotificationEditorSettingsValues;

        public void SaveSetting(NotificationEditorSetting setting)
        {
            if (iOSNotificationEditorSettingsValues == null)
                iOSNotificationEditorSettingsValues = new iOSNotificationEditorSettingsCollection();

            iOSNotificationEditorSettingsValues[setting.key] = setting.val;
        }

        public T GetiOSNotificationEditorSettingsValue<T>(string key, T defaultValue)
        {

            if (iOSNotificationEditorSettingsValues == null)
                iOSNotificationEditorSettingsValues = new iOSNotificationEditorSettingsCollection();

            try
            {
                var val = iOSNotificationEditorSettingsValues[key];
                if (val != null)
                    return (T) val;
            }
            catch (InvalidCastException ex)
            {
                Debug.LogWarning(ex.ToString());
                iOSNotificationEditorSettingsValues = new iOSNotificationEditorSettingsCollection();
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
                if (!res.IsValid)
                {
                    Debug.LogWarning( string.Format("Failed exporting: '{0}' Android notification icon because:\n {1} ", res.Id,
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

#if UNITY_EDITOR && PLATFORM_ANDROID
    class AndroidNotificationResourcesPostProcessor : IPostGenerateGradleAndroidProject
    {
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
        }
    }
    #endif
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
#if PLATFORM_ANDROID
using UnityEditor.Android;
#endif

using Unity.Notifications.iOS;
using UnityEngine;


namespace Unity.Notifications.Android
{
    internal enum NotificationIconType
    {
        SmallIcon = 0,
        LargeIcon = 1
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
        
        public NotificationEditorSetting(string key, string label, string tooltip, object val, bool writeToPlist  = true)
        {
            this.key = key;
            this.label = label;
            this.tooltip = tooltip;
            this.val = val;
            this.writeToPlist = writeToPlist;
        }
    }
    
    internal class UnityNotificationEditorManager : ScriptableObject
    {
        
        internal const string ASSET_PATH = "Editor/com.unity.mobile.notifications/notificationIcons.asset";


        [SerializeField] 
        public List<NotificationEditorSetting> iOSNotificationEditorSettings;
        
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
                        "TODO",
                        true),
                    
                    new NotificationEditorSetting(
                        "UnityNotificationDefaultPresentationOptions", 
                        "Default Notification Presentation Options",
                        "TODO",
                        PresentationOption.NotificationPresentationOptionBadge    | PresentationOption.NotificationPresentationOptionAlert | PresentationOption.NotificationPresentationOptionSound),

                    
                    new NotificationEditorSetting(
                        "UnityNotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch",
                        "Register for Remote Notifications on App Launch",
                        "TODO",
                        false),
                    new NotificationEditorSetting("UnityRemoteNotificationForegroundPresentationOptions",
                        "Remote Notification Foreground Presentation Options",
                        "TODO",
                        PresentationOption.NotificationPresentationOptionBadge    | PresentationOption.NotificationPresentationOptionAlert | PresentationOption.NotificationPresentationOptionSound),
                    new NotificationEditorSetting("UnityAPSReleaseEnvironment",
                        "Enable release environment for APS",
                        "TODO",
                        false,
                        false)


                };
                
                EditorUtility.SetDirty(notificationEditorManager);
//                notificationEditorManager.Update();
            }
            
            
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

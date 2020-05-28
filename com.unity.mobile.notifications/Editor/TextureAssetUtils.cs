using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Notifications
{
    internal static class TextureAssetUtils
    {
        public static bool VerifyTextureByType(Texture2D texture, NotificationIconType type, out List<string> errors)
        {
            errors = new List<string>();

            if (texture == null)
            {
                errors.Add("Texture is invalid");
                return false;
            }

            var needsAlpha = true;
            var minSize = 48;

            if (type == NotificationIconType.Large)
            {
                needsAlpha = false;
                minSize = 192;
            }

            var isSquare = texture.width == texture.height;
            var isLargeEnough = texture.width >= minSize;
            var hasAlpha = true;// TODO: texture.format == TextureFormat.Alpha8;

            var isReadable = texture.isReadable;

            if (!isReadable)
            {
                errors.Add("Read/Write is not enabled in the texture importer");
            }

            if (!isLargeEnough)
                errors.Add(string.Format("Texture must be at least {0}x{1} pixels (while it's {2}x{3})",
                    minSize,
                    minSize,
                    texture.width,
                    texture.height
                ));

            if (!isSquare)
                errors.Add(string.Format("Texture must have the same width and height (while it's {0}x{1})",
                    texture.width,
                    texture.height
                ));

            if (!hasAlpha && needsAlpha)
                errors.Add(string.Format("Texture must contain an alpha channel"));

            return isReadable && isSquare && isLargeEnough && (!needsAlpha || hasAlpha);
        }

        public static Texture2D ProcessTextureForType(Texture2D sourceTexture, NotificationIconType type)
        {
            if (sourceTexture == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null || !importer.isReadable)
                return null;

            Texture2D texture;
            if (type == NotificationIconType.Small)
            {
                texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, true, false);
                for (var i  = 0; i < sourceTexture.mipmapCount; i++)
                {
                    var c_0 = sourceTexture.GetPixels(i);
                    var c_1 = texture.GetPixels(i);
                    for (var i1 = 0; i1 < c_0.Length; i1++)
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
            if (sourceTexture.width == width && sourceTexture.height == height)
                return sourceTexture;

            if (Application.isBatchMode && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                // If unity is running in batch mode without graphics, we have to go to the slow CPU code path.
                var result = new Texture2D(width, height, TextureFormat.ARGB32, false);

                var destPixels = new Color[width * height];
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        destPixels[y * width + x] = sourceTexture.GetPixelBilinear((float)x / (float)width, (float)y / (float)height);
                    }
                }
                result.SetPixels(destPixels);
                result.Apply();

                return result;
            }
            else
            {
                // Scale the texture at GPU side.
                sourceTexture.filterMode = FilterMode.Trilinear;
                sourceTexture.Apply(true);

                var rtt = new RenderTexture(width, height, 32);
                Graphics.SetRenderTarget(rtt);

                GL.LoadPixelMatrix(0, 1, 1, 0);
                GL.Clear(true, true, new Color(0, 0, 0, 0));

                Graphics.DrawTexture(new Rect(0, 0, 1, 1), sourceTexture);

                var result = new Texture2D(width, height, TextureFormat.ARGB32, true);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0, true);

                return result;
            }
        }
    }
}

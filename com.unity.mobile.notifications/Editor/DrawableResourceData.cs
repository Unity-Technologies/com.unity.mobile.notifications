using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications
{
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

            for (var i = 0; i < errors.Length; i++)
            {
                error += string.Format("{0}{1}", errors[i], i + 1 >= errors.Length ? "." : ", ");
            }

            return error;
        }

    }

#if UNITY_EDITOR
#endif
}
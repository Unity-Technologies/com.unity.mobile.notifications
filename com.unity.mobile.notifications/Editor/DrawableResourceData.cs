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

        private bool m_IsValid = false;
        private List<string> m_Errors = null;
        private Texture2D m_PreviewTexture;

        public bool IsValid
        {
            get
            {
                if (m_IsValid == false && m_Errors == null)
                    Verify();

                return m_IsValid;
            }
        }

        public string[] Errors
        {
            get
            {
                if (m_IsValid == false && m_Errors == null)
                    Verify();

                return m_Errors.ToArray();
            }
        }

        public Texture2D GetPreviewTexture(bool update)
        {
            if (Asset == null)
                return null;

            if (m_IsValid && (m_PreviewTexture == null || update))
                m_PreviewTexture = TextureAssetUtils.ProcessTextureForType(Asset, Type);

            return m_PreviewTexture;
        }

        internal bool Initialized()
        {
            return !string.IsNullOrEmpty(Id) && Asset != null;
        }

        public void Clean()
        {
            m_IsValid = false;
            m_Errors = null;
            m_PreviewTexture = null;
        }

        public bool Verify()
        {
            m_IsValid = TextureAssetUtils.VerifyTextureByType(Asset, Type, out m_Errors);
            return m_IsValid;
        }

        public string GenerateErrorString()
        {
            var errors = Errors;

            var errorString = string.Empty;
            for (var i = 0; i < errors.Length; i++)
            {
                errorString += string.Format("{0}{1}", errors[i], i + 1 >= errors.Length ? "." : ", ");
            }

            return errorString;
        }
    }
}

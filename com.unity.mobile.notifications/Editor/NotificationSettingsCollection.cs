using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Notifications
{
    [Serializable]
    internal class NotificationSettingsCollection
    {
        [SerializeField]
        [FormerlySerializedAs("keys")]
        List<string> m_Keys;

        [SerializeField]
        [FormerlySerializedAs("values")]
        List<string> m_Values;

        public NotificationSettingsCollection()
        {
            m_Keys = new List<string>();
            m_Values = new List<string>();
        }

        public bool Contains(string key)
        {
            return m_Keys.Contains(key);
        }

        public object this[string key]
        {
            get
            {
                var index = m_Keys.IndexOf(key);
                if (index == -1 || m_Values.Count <= index)
                    return null;

                int intValue;
                if (int.TryParse(m_Values[index], out intValue))
                {
                    return intValue;
                }

                bool boolValue;
                if (bool.TryParse(m_Values[index], out boolValue))
                {
                    return boolValue;
                }

                return m_Values[index];
            }
            set
            {
                string strValue;

                if (value is Enum)
                {
                    strValue = ((int)value).ToString();
                }
                else
                {
                    strValue = value.ToString();
                }

                var index = m_Keys.IndexOf(key);
                if (index == -1)
                {
                    m_Keys.Add(key);
                    m_Values.Add(strValue);
                }
                else
                {
                    m_Values[index] = strValue;
                }
            }
        }
    }
}

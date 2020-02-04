using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Notifications
{
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

                else if (bool.TryParse(values[index], out boolValue))
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
}
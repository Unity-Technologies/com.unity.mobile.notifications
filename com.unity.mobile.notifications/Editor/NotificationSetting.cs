using System.Collections.Generic;

namespace Unity.Notifications
{
    internal class NotificationSetting
    {
        public string Key;
        public string Label;
        public string Tooltip;
        public object Value;
        public bool WriteToPlist;

        public List<NotificationSetting> Dependencies;

        public NotificationSetting(string key, string label, string tooltip, object value, bool writeToPlist = true,
                                   List<NotificationSetting> dependencies = null)
        {
            this.Key = key;
            this.Label = label;
            this.Tooltip = tooltip;
            this.Value = value;
            this.WriteToPlist = writeToPlist;
            this.Dependencies = dependencies;
        }
    }
}

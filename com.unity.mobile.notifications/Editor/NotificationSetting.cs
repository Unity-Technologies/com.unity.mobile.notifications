using System.Collections.Generic;

namespace Unity.Notifications
{
    internal class NotificationSetting
    {
        public string key;
        public string label;
        public string tooltip;
        public object value;
        public bool writeToPlist;

        public List<NotificationSetting> dependentSettings;

        public NotificationSetting(string key, string label, string tooltip, object value, bool writeToPlist = true,
                                   List<NotificationSetting> dependentSettings = null)
        {
            this.key = key;
            this.label = label;
            this.tooltip = tooltip;
            this.value = value;
            this.writeToPlist = writeToPlist;
            this.dependentSettings = dependentSettings;
        }
    }
}

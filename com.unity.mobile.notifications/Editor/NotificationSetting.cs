using System.Collections.Generic;

namespace Unity.Notifications
{
    internal class NotificationSetting
    {
        public string key;
        public string label;
        public string tooltip;
        public object val;
        public bool writeToPlist;

        public List<NotificationSetting> dependentSettings;
        public List<string> requiredSettings;

        public NotificationSetting(string key, string label, string tooltip, object val, bool writeToPlist = true,
                                   List<NotificationSetting> dependentSettings = null, List<string> requiredSettings = null)
        {
            this.key = key;
            this.label = label;
            this.tooltip = tooltip;
            this.val = val;
            this.writeToPlist = writeToPlist;
            this.dependentSettings = dependentSettings;
            this.requiredSettings = requiredSettings;
        }
    }
}

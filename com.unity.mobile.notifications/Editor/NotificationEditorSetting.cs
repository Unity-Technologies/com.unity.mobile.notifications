using System.Collections.Generic;

namespace Unity.Notifications
{
    internal class NotificationEditorSetting
    {
        public string key;
        public string label;
        public string tooltip;
        public object val;
        public bool writeToPlist;

        public List<NotificationEditorSetting> dependentSettings;
        public List<string> requiredSettings;

        public NotificationEditorSetting(string key, string label, string tooltip, object val, bool writeToPlist = true,
            List<NotificationEditorSetting> dependentSettings = null, List<string> requiredSettings = null)
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
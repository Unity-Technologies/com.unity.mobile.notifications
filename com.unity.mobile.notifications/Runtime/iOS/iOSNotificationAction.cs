using System;

namespace Unity.Notifications.iOS
{
    [Flags]
    public enum iOSNotificationActionOptions
    {
        Required = (1 << 0),
        Destructive = (1 << 1),
        Foreground = (1 << 2),
    }

    public class iOSNotificationAction
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public iOSNotificationActionOptions Options { get; set; }

        public iOSNotificationAction(string id, string title)
            : this(id, title, 0)
        {
        }

        public iOSNotificationAction(string id, string title, iOSNotificationActionOptions options)
        {
            Id = id;
            Title = title;
            Options = options;
        }

        internal virtual IntPtr CreateUNNotificationAction()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return iOSNotificationsWrapper._CreateUNNotificationAction(Id, Title, (int)Options);
#else
            return IntPtr.Zero;
#endif
        }
    }
}

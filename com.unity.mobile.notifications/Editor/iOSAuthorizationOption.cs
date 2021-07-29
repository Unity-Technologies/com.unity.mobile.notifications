using System;

namespace Unity.Notifications
{
    [Flags]
    internal enum iOSAuthorizationOption
    {
        Default = 0,
        Badge = 1 << 0,
        Sound = 1 << 1,
        Alert = 1 << 2,
        CarPlay = 1 << 3,
        CriticalAlert = 1 << 4,
        ProvidesAppNotificationSettings = 1 << 5,
        Provisional = 1 << 6,
        All = ~0,
    }
}

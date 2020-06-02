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
        CarPlay = (1 << 3),
        All = ~0,
    }
}

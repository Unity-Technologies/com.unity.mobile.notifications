using System;

namespace Unity.Notifications
{
    [Flags]
    internal enum iOSPresentationOption
    {
        Badge = 1 << 0,
        Sound = 1 << 1,
        Alert = 1 << 2,
        List = 1 << 3,
        Banner = 1 << 4,
        All = ~0,
    }
}

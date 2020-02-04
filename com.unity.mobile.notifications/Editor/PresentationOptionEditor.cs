using System;

namespace Unity.Notifications
{
    [Flags]
    internal enum PresentationOptionEditor
    {
        Badge = 1 << 0,
        Sound = 1 << 1,
        Alert = 1 << 2,
        All = ~0,
    }
}
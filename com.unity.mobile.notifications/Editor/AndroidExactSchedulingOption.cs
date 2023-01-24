using System;

namespace Unity.Notifications
{
    /// <summary>
    /// Whether to schedule notifications at exact time or approximately (saves power).
    /// Exact scheduling is available in Android 6 (API 23) and newer, lower versions always use inexact scheduling.
    /// Android 12 (API 31) or newer requires SCHEDULE_EXACT_ALARM permission and grant from user to use exact scheduling.
    /// Android 13 (API 33) or newer can use USE_EXACT_ALARM permission to use exactscheduling without requesting users grant.
    /// </summary>
    [Flags]
    public enum AndroidExactSchedulingOption
    {
        /// <summary>
        /// Use exact scheduling when possible.
        /// </summary>
        ExactWhenAvailable = 1,

        /// <summary>
        /// Add SCHEDULE_EXACT_ALARM permission to the manifest.
        /// </summary>
        AddScheduleExactPermission = 1 << 1,

        /// <summary>
        /// Add USE_EXACT_ALARM permission to the manifest.
        /// </summary>
        AddUseExactAlarmPermission = 1 << 2,

        /// <summary>
        /// Add REQUEST_IGNORE_BATTERY_OPTIMIZATIONS permission to the manifest.
        /// Required if you want to use <see cref="AndroidNotificationCenter.RequestIgnoreBatteryOptimizations()"/>.
        /// </summary>
        AddRequestIgnoreBatteryOptimizationsPermission = 1 << 3,
    }
}

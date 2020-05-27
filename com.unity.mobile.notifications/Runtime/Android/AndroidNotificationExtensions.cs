using System;
using UnityEngine;

namespace Unity.Notifications.Android
{
    internal static class AndroidNotificationExtensions
    {
        public static Importance ToImportance(this int importance)
        {
            if (Enum.IsDefined(typeof(Importance), importance))
                return (Importance)importance;

            return Importance.Default;
        }

        public static LockScreenVisibility ToLockScreenVisibility(this int lockscreenVisibility)
        {
            if (Enum.IsDefined(typeof(LockScreenVisibility), lockscreenVisibility))
                return (LockScreenVisibility)lockscreenVisibility;

            return LockScreenVisibility.Public;
        }

        public static NotificationStyle ToNotificationStyle(this int notificationStyle)
        {
            if (Enum.IsDefined(typeof(NotificationStyle), notificationStyle))
                return (NotificationStyle)notificationStyle;

            return NotificationStyle.None;
        }

        public static GroupAlertBehaviours ToGroupAlertBehaviours(this int groupAlertBehaviour)
        {
            if (Enum.IsDefined(typeof(GroupAlertBehaviours), groupAlertBehaviour))
                return (GroupAlertBehaviours)groupAlertBehaviour;

            return GroupAlertBehaviours.GroupAlertAll;
        }

        public static Color ToColor(this int color)
        {
            int a = (color >> 24) & 0xff;
            int r = (color >> 16) & 0xff;
            int g = (color >> 8) & 0xff;
            int b = (color) & 0xff;

            return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static int ToInt(this Color? color)
        {
            if (!color.HasValue)
                return 0;

            var color32 = (Color32)color.Value;
            return (color32.a & 0xff) << 24 | (color32.r & 0xff) << 16 | (color32.g & 0xff) << 8 | (color32.b & 0xff);
        }

        public static long ToLong(this DateTime dateTime)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = dateTime.ToUniversalTime() - origin;

            return (long)Math.Floor(diff.TotalMilliseconds);
        }

        public static DateTime ToDatetime(this long dateTime)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddMilliseconds(dateTime).ToLocalTime();
        }

        public static long ToLong(this TimeSpan? timeSpan)
        {
            return timeSpan.HasValue ? (long)timeSpan.Value.TotalMilliseconds : -1L;
        }

        public static TimeSpan ToTimeSpan(this long timeSpan)
        {
            return TimeSpan.FromMilliseconds(timeSpan);
        }
    }
}

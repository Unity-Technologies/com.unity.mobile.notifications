using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace Unity.Notifications
{
    /// <summary>
    /// The state of the query for last responded notification.
    /// Returned by <see cref="QueryLastRespondedNotificationOp.State"/>
    /// </summary>
    public enum QueryLastRespondedNotificationState
    {
        /// <summary>
        /// Operation is ongoing, wait for next frame and check again.
        /// </summary>
        Pending,

        /// <summary>
        /// Operation is complete, app was launched normally, no notification was tapped.
        /// </summary>
        NoRespondedNotification,

        /// <summary>
        /// Operation is complete, app was launched by tapping the notification, that can be retrieved via <see cref="QueryLastRespondedNotificationOp.Notification"/> property.
        /// </summary>
        HaveRespondedNotification,
    }

    public class QueryLastRespondedNotificationOp
        : CustomYieldInstruction
    {
        /// <inheritdoc/>
        public override bool keepWaiting =>
#if UNITY_ANDROID
            false
#else
            platformOperation.keepWaiting
#endif
        ;

        public QueryLastRespondedNotificationState State
        {
            get
            {
#if UNITY_ANDROID
#else
                return platformOperation.State switch
                {
                    Unity.Notifications.iOS.QueryLastRespondedNotificationState.Pending => QueryLastRespondedNotificationState.Pending,
                    Unity.Notifications.iOS.QueryLastRespondedNotificationState.NoRespondedNotification => QueryLastRespondedNotificationState.NoRespondedNotification,
                    Unity.Notifications.iOS.QueryLastRespondedNotificationState.HaveRespondedNotification => QueryLastRespondedNotificationState.HaveRespondedNotification,
                    _ => throw new System.Exception("Not all possible cases are handled"),
                };
#endif
            }
        }

        public Notification? Notification
        {
            get
            {
                if (notification.HasValue)
                    return notification;
#if UNITY_IOS
                if (State == QueryLastRespondedNotificationState.HaveRespondedNotification)
                {
                    var n = platformOperation.Notification;
                    if (n == null)
                        return null;
                    notification = new Notification(n);
                    return notification;
                }
#endif
                return null;
            }
        }

        Notification? notification;

#if UNITY_ANDROID
        QueryLastRespondedNotificationState state;

        internal QueryLastRespondedNotificationOp()
        {
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            if (intent == null)
                return null;
            return new Notification(intent.Notification, intent.Id);
        }
#else
        internal QueryLastRespondedNotificationOp(Unity.Notifications.iOS.QueryLastRespondedNotificationOp op)
        {
            platformOperation = op;
        }

        Unity.Notifications.iOS.QueryLastRespondedNotificationOp platformOperation;
#endif
    }
}

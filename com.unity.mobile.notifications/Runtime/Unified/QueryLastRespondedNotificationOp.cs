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

    /// <summary>
    /// An operation for retrieving notification used to open the app.
    /// When app is not running, app launches first and then notification is delivered. There may be a delay until notification is delivered.
    /// This operation may finish immediately or it may require a few frames to pass. You can return it from coroutine to wait until completion.
    /// </summary>
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

        /// <summary>
        /// The state of the operation.
        /// </summary>
        public QueryLastRespondedNotificationState State
        {
            get
            {
#if UNITY_ANDROID
                return state;
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

        /// <summary>
        /// Returns a notification the was used to open the app or null if app was launched normally.
        /// An exception will be thrown if operation has not been completed yet or no notification is available.
        /// </summary>
        public Notification Notification
        {
            get
            {
                if (notification.HasValue)
                    return notification.Value;
#if UNITY_IOS
                if (State == QueryLastRespondedNotificationState.HaveRespondedNotification)
                {
                    notification = new Notification(platformOperation.Notification);
                    return notification.Value;
                }
#endif
                throw new System.InvalidOperationException("Operation does not have a valid notification");
            }
        }

        Notification? notification;

#if UNITY_ANDROID
        QueryLastRespondedNotificationState state;

        internal QueryLastRespondedNotificationOp()
        {
            var intent = AndroidNotificationCenter.GetLastNotificationIntent();
            if (intent == null)
                state = QueryLastRespondedNotificationState.NoRespondedNotification;
            else
            {
                notification = new Notification(intent.Notification, intent.Id);
                state = QueryLastRespondedNotificationState.HaveRespondedNotification;
            }
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

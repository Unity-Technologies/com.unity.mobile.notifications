using System;
using UnityEngine;

namespace Unity.Notifications.iOS
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
        QueryLastRespondedNotificationState state;
        iOSNotification notification;
        string actionId;
        string userText;

        /// <inheritdoc/>
        public override bool keepWaiting
        {
            get
            {
                UpdateState();
                return state == QueryLastRespondedNotificationState.Pending;
            }
        }

        /// <summary>
        /// The state of the operation.
        /// </summary>
        public QueryLastRespondedNotificationState State => state;

        /// <summary>
        /// Returns a notification the was used to open the app or null if app was launched normally.
        /// </summary>
        public iOSNotification Notification
        {
            get
            {
                CheckHaveNotification();
                return notification;
            }
        }

        /// <summary>
        /// The ID of the action that the user responded to or null if notification had no actions or no notification was responded to.
        /// </summary>
        /// <seealso cref="SetNotificationCategories(IEnumerable{iOSNotificationCategory})"/>
        public string ActionId
        {
            get
            {
                CheckHaveNotification();
                return actionId;
            }
        }

        /// <summary>
        /// The text entered by user when responding to notification using a text input action.
        /// </summary>
        /// <seealso cref="SetNotificationCategories(IEnumerable{iOSNotificationCategory})"/>
        public string UserText
        {
            get
            {
                CheckHaveNotification();
                return userText;
            }
        }

        internal QueryLastRespondedNotificationOp()
        {
            state = QueryLastRespondedNotificationState.Pending;
            UpdateState();
        }

        void UpdateState()
        {
            if (state != QueryLastRespondedNotificationState.Pending || notification != null)
                return;

            var data = iOSNotificationsWrapper.GetLastNotificationData();
            if (data != null)
            {
                notification = new iOSNotification(data.Value);
                actionId = iOSNotificationsWrapper.GetLastRespondedNotificationAction();
                userText = iOSNotificationsWrapper.GetLastRespondedNotificationUserText();
                state = QueryLastRespondedNotificationState.HaveRespondedNotification;
                return;
            }

            var frameNum = Time.frameCount;
            if (iOSNotificationsWrapper.GetAppOpenedUsingNotification())
            {
                // here we know we've been launched via notification, wait for it a bit
                if (frameNum < 10)
                    return;
            }
            else
            {
                // if opened using Action, we don't get that info on launch and it takes some frames to deliver
                if (frameNum < 3)
                    return;
            }
            state = QueryLastRespondedNotificationState.NoRespondedNotification;
        }

        void CheckHaveNotification()
        {
            UpdateState();
            if (state == QueryLastRespondedNotificationState.Pending)
                throw new InvalidOperationException("Operation has not completed yet");
        }
    }
}

using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications
{
    /// <summary>
    /// The status of notification permission request.
    /// </summary>
    /// <seealso cref="NotificationsPermissionRequest.Status"/>
    public enum NotificationsPermissionStatus
    {
        /// <summary>
        /// Indicates that request is ongoing. Usually mean that user is presented with UI to respond to.
        /// </summary>
        RequestPending,

        /// <summary>
        /// Permission granted, you can post notifications.
        /// </summary>
        Granted,

        /// <summary>
        /// Permission denied, sending notifications will not work.
        /// </summary>
        Denied,
    }

    /// <summary>
    /// Track the status of notification permission request.
    /// Can be returned from coroutine to suspend it until request is either granted or denied.
    /// Permission can be granted or denied immediately, if this isn't the first request.
    /// </summary>
    public class NotificationsPermissionRequest
        : CustomYieldInstruction
    {
#if UNITY_ANDROID
        PermissionRequest request;
#else
        AuthorizationRequest request;
#endif

        internal NotificationsPermissionRequest(int options)
        {
#if UNITY_ANDROID
            // do not create request if already allowed
            if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
                request = new PermissionRequest();
#else
            request = new AuthorizationRequest((AuthorizationOption)options, false);
#endif
        }

        /// <summary>
        /// Overridden property of base class. Indicates if coroutine should be suspended.
        /// </summary>
        public override bool keepWaiting => (request == null)
            ? false
#if UNITY_ANDROID
            : request.Status == PermissionStatus.RequestPending;
#else
            : !request.IsFinished;
#endif

        /// <summary>
        /// Returns a status of this request.
        /// </summary>
        public NotificationsPermissionStatus Status
        {
            get
            {
                if (request == null)
                    return NotificationsPermissionStatus.Granted;

#if UNITY_ANDROID
                return request.Status switch
                {
                    PermissionStatus.RequestPending => NotificationsPermissionStatus.RequestPending,
                    PermissionStatus.Allowed => NotificationsPermissionStatus.Granted,
                    _ => NotificationsPermissionStatus.Denied,
                };
#else
                if (!request.IsFinished)
                    return NotificationsPermissionStatus.RequestPending;
                return request.Granted ? NotificationsPermissionStatus.Granted : NotificationsPermissionStatus.Denied;
#endif
            }
        }
    }
}

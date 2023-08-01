using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using Unity.Notifications.iOS;
#endif

namespace Unity.Notifications
{
    public enum NotificationsPermissionStatus
    {
        RequestPending,
        Granted,
        Denied,
    }

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

        public override bool keepWaiting => (request == null)
            ? false
#if UNITY_ANDROID
            : request.Status == PermissionStatus.RequestPending;
#else
            : !request.IsFinished;
#endif

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

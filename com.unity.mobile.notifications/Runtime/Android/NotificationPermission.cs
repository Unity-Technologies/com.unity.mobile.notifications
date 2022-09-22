using UnityEngine.Android;

namespace Unity.Notifications.Android
{
    /// <summary>
    /// Represents a status of the Android runtime permission.
    /// </summary>
    public enum PermissionStatus
    {
        /// <summary>
        /// No permission as user was not prompted for it.
        /// </summary>
        NotRequested = 0,

        /// <summary>
        /// User gave permission.
        /// </summary>
        Allowed = 1,

        /// <summary>
        /// User denied permission.
        /// </summary>
        Denied = 2,

        /// <summary>
        /// User denied permission and expressed intent to not be prompted again.
        /// </summary>
        DeniedDontAskAgain = 3,

        /// <summary>
        /// A request for permission was made and user hasn't responded yet.
        /// </summary>
        RequestPending = 4,
    }

    /// <summary>
    /// A class to request permission to post notifications.
    /// Before Android 13 (API 33) it is not required and Status will become Allowed immediately.
    /// May succeed or fail immediately. Users response is saved to PlayerPrefs.
    /// Respects users wish to not be asked again.
    /// </summary>
    /// <seealso cref="AndroidNotificationCenter.UserPermissionToPost"/>
    /// <seealso cref="AndroidNotificationCenter.SETTING_POST_NOTIFICATIONS_PERMISSION"/>
    public class PermissionRequest
    {
        /// <summary>
        /// The status of this request.
        /// Value other than RequestPending means request has completed.
        /// </summary>
        public PermissionStatus Status { get; set; }

        /// <summary>
        /// Create a new request.
        /// Will show user a dialog asking for permission if that is required to post notifications and user hasn't permanently denied it already.
        /// </summary>
        /// <see cref="PermissionStatus.DeniedDontAskAgain"/>
        public PermissionRequest()
        {
            Status = AndroidNotificationCenter.UserPermissionToPost;
            switch (Status)
            {
                case PermissionStatus.NotRequested:
                case PermissionStatus.Denied:
                    Status = PermissionStatus.RequestPending;
                    RequestPermission();
                    break;
            }
        }

        void RequestPermission()
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (unused) => PermissionResponse(PermissionStatus.Allowed);
            callbacks.PermissionDenied += (unused) => PermissionResponse(PermissionStatus.Denied);
            callbacks.PermissionDeniedAndDontAskAgain += (unused) => PermissionResponse(PermissionStatus.DeniedDontAskAgain);
            Permission.RequestUserPermission(AndroidNotificationCenter.PERMISSION_POST_NOTIFICATIONS, callbacks);
        }

        void PermissionResponse(PermissionStatus status)
        {
            Status = status;
            AndroidNotificationCenter.SetPostPermissionSetting(status);
        }
    }
}

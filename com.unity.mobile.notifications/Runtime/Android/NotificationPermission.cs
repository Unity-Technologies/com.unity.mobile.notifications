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
        /// No longer used. User denied permission and expressed intent to not be prompted again.
        /// </summary>
        DeniedDontAskAgain = 3,

        /// <summary>
        /// A request for permission was made and user hasn't responded yet.
        /// </summary>
        RequestPending = 4,

        /// <summary>
        /// Notifications are blocked for this app. Before API level 33 this means they were disabled in Settings.
        /// <see cref="https://developer.android.com/reference/android/app/NotificationManager#areNotificationsEnabled()"/>
        /// </summary>
        NotificationsBlockedForApp = 5,
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
        public PermissionRequest()
        {
            Status = AndroidNotificationCenter.UserPermissionToPost;
            switch (Status)
            {
                case PermissionStatus.NotRequested:
                case PermissionStatus.Denied:
                case PermissionStatus.DeniedDontAskAgain:  // this one is no longer used, but might be found in settings
                    Status = RequestPermission();
                    break;
            }
        }

        PermissionStatus RequestPermission()
        {
            if (!AndroidNotificationCenter.CanRequestPermissionToPost)
                return PermissionStatus.Denied;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (unused) => PermissionResponse(PermissionStatus.Allowed);
            callbacks.PermissionDenied += (unused) => PermissionResponse(PermissionStatus.Denied);
            Permission.RequestUserPermission(AndroidNotificationCenter.PERMISSION_POST_NOTIFICATIONS, callbacks);
            return PermissionStatus.RequestPending;
        }

        void PermissionResponse(PermissionStatus status)
        {
            Status = status;
            AndroidNotificationCenter.SetPostPermissionSetting(status);
        }
    }
}

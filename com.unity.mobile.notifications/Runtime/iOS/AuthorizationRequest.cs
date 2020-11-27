using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Enum for requesting authorization to interact with the user.
    /// </summary>
    [Flags]
    public enum AuthorizationOption
    {
        /// <summary>
        /// The ability to update the app’s badge.
        /// </summary>
        Badge = (1 << 0),

        /// <summary>
        /// The ability to play sounds.
        /// </summary>
        Sound = (1 << 1),

        /// <summary>
        /// The ability to display alerts.
        /// </summary>
        Alert = (1 << 2),

        /// <summary>
        /// The ability to display notifications in a CarPlay environment.
        /// </summary>
        CarPlay = (1 << 3),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iOSAuthorizationRequestData
    {
        internal int granted;
        internal string error;
        internal string deviceToken;
    }

    /// <summary>
    /// Use this to request authorization to interact with the user when you with to deliver local and remote notifications are delivered to the user's device.
    /// </summary>
    /// <remarks>
    /// This method must be called before you attempt to schedule any local notifications. If "Request Authorization on App Launch" is enabled in
    /// "Edit -> Project Settings -> Mobile Notification Settings" this method will be called automatically when the app launches. You might call this method again to determine the current
    /// authorizations status or retrieve the DeviceToken for Push Notifications. However the UI system prompt will not be shown if the user has already granted or denied authorization for this app.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
    /// {
    ///     while (!req.IsFinished)
    ///     {
    ///         yield return null;
    ///     };
    ///
    ///     string result = "\n RequestAuthorization: \n";
    ///     result += "\n finished: " + req.IsFinished;
    ///     result += "\n granted :  " + req.Granted;
    ///     result += "\n error:  " + req.Error;
    ///     result += "\n deviceToken:  " + req.DeviceToken;
    ///     Debug.Log(res);
    /// }
    /// </code>
    /// </example>
    public class AuthorizationRequest : IDisposable
    {
        bool m_IsFinished;
        bool m_Granted;
        string m_Error;
        string m_DeviceToken;

        /// <summary>
        /// Indicates whether the authorization request has completed.
        /// </summary>
        public bool IsFinished
        {
            get { lock (this) { return m_IsFinished; } }
            private set { m_IsFinished = value; }
        }

        /// <summary>
        /// A property indicating whether authorization was granted. The value of this parameter is set to true when authorization was granted for one or more options. The value is set to false when authorization is denied for all options.
        /// </summary>
        public bool Granted
        {
            get { lock (this) { return m_Granted; } }
            private set { m_Granted = value; }
        }

        /// <summary>
        /// Contains error information of the request failed for some reason or an empty string if no error occurred.
        /// </summary>
        public string Error
        {
            get { lock (this) { return m_Error; } }
            private set { m_Error = value; }
        }

        /// <summary>
        /// A globally unique token that identifies this device to Apple Push Notification Network. Send this token to the server that you use to generate remote notifications.
        /// Your server must pass this token unmodified back to APNs when sending those remote notifications.
        /// This property will be empty if you set the registerForRemoteNotifications parameter to false when creating the Authorization request or if the app fails registration with the APN.
        /// </summary>
        public string DeviceToken
        {
            get { lock (this) { return m_DeviceToken; } }
            private set { m_DeviceToken = value; }
        }

        static AuthorizationRequest()
        {
            iOSNotificationsWrapper.RegisterAuthorizationRequestCallback();
        }

        /// <summary>
        /// Initiate an authorization request.
        /// </summary>
        /// <param name="authorizationOption"> The authorization options your app is requesting. You may specify multiple options to request authorization for. Request only the authorization options that you plan to use.</param>
        /// <param name="registerForRemoteNotifications"> Set this to true to initiate the registration process with Apple Push Notification service after the user has granted authorization
        /// If registration succeeds the DeviceToken will be returned. You should pass this token along to the server you use to generate remote notifications for the device. </param>
        public AuthorizationRequest(AuthorizationOption authorizationOption, bool registerForRemoteNotifications)
        {
            var handle = GCHandle.Alloc(this);
            iOSNotificationsWrapper.RequestAuthorization(GCHandle.ToIntPtr(handle), (int)authorizationOption, registerForRemoteNotifications);
        }

        private void OnAuthorizationRequestCompleted(iOSAuthorizationRequestData requestData)
        {
            lock (this)
            {
                IsFinished = true;
                Granted = requestData.granted != 0;
                Error = requestData.error;
                DeviceToken = requestData.deviceToken;
            }
        }

        internal static void OnAuthorizationRequestCompleted(IntPtr request, iOSAuthorizationRequestData requestData)
        {
            try
            {
                var handle = GCHandle.FromIntPtr(request);
                var req = handle.Target as AuthorizationRequest;
                handle.Free();
                req.OnAuthorizationRequestCompleted(requestData);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Dispose to unregister the OnAuthorizationRequestCompleted callback.
        /// </summary>
        public void Dispose()
        {
        }
    }
}

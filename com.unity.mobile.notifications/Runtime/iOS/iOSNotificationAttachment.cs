namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Notification attachment.
    /// Refer to Apple documentation for details.
    /// </summary>
    /// <see cref="https://developer.apple.com/documentation/usernotifications/unnotificationattachment?language=objc"/>
    public struct iOSNotificationAttachment
    {
        /// <summary>
        /// A unique identifier for the attachments. Will be auto-generated if left empty.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// URL to local file, accessible to the application.
        /// </summary>
        public string Url { get; set; }
    }
}

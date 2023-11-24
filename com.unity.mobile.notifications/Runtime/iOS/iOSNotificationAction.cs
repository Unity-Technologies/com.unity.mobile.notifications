using System;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Options for notification actions.
    /// These represent values from UNNotificationActionOptions.
    /// For more information, refer to <see href="https://developer.apple.com/documentation/usernotifications/unnotificationactionoptions">Apple documentation</see>.
    /// </summary>
    [Flags]
    public enum iOSNotificationActionOptions
    {
        /// <summary>
        /// No specific action is performed.
        /// </summary>
        None = 0,
        /// <summary>
        /// An action that requires the user to unlock their device.
        /// </summary>
        Required = (1 << 0),
        /// <summary>
        /// An irreversible action such as deleting data.
        /// </summary>
        Destructive = (1 << 1),
        /// <summary>
        /// An action that opens the application.
        /// </summary>
        Foreground = (1 << 2),
    }

    enum iOSNotificationActionIconType
    {
        None = 0,
        SystemImageName = 1,
        TemplateImageName = 2,
    }

    /// <summary>
    /// Represents action for an actionable notification.
    /// Actions are supposed to be added to notification categories, which are then registered prior to sending notifications.
    /// User can choose to tap a notification or one of associated actions. Application gets feedback of the choice.
    /// </summary>
    /// <seealso cref="iOSNotificationCategory"/>
    /// <seealso cref="iOSNotificationCenter.GetLastRespondedNotificationAction"/>
    public class iOSNotificationAction
    {
        internal iOSNotificationActionIconType _imageType;
        internal string _image;

        /// <summary>
        /// An identifier for this action.
        /// Each action within an application unique must have unique ID.
        /// This ID will be returned by iOSNotificationCenter.GetLastRespondedNotificationAction if user chooses this action.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Title for the action.
        /// This will be the title of the button that appears below the notification.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Options for the action. Can be a combination of given flags.
        /// Refer to Apple documentation for UNNotificationActionOptions for exact meanings.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationactionoptions"/>
        /// </summary>
        public iOSNotificationActionOptions Options { get; set; }

        /// <summary>
        /// Set the icon for action using system symbol image name.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationactionicon/3747241-iconwithsystemimagename?language=objc"/>
        /// </summary>
        public string SystemImageName
        {
            get { return _imageType == iOSNotificationActionIconType.SystemImageName ? _image : null; }
            set
            {
                _imageType = iOSNotificationActionIconType.SystemImageName;
                _image = value;
            }
        }

        /// <summary>
        /// Set the icon for action using image from app's bundle.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationactionicon/3747242-iconwithtemplateimagename?language=objc"/>
        /// </summary>
        public string TemplateImageName
        {
            get { return _imageType == iOSNotificationActionIconType.TemplateImageName ? _image : null; }
            set
            {
                _imageType = iOSNotificationActionIconType.TemplateImageName;
                _image = value;
            }
        }

        /// <summary>
        /// Creates new action.
        /// </summary>
        /// <param name="id">Unique identifier for this action</param>
        /// <param name="title">Title for the action (and button label)</param>
        public iOSNotificationAction(string id, string title)
            : this(id, title, 0)
        {
        }

        /// <summary>
        /// Creates new action.
        /// </summary>
        /// <param name="id">Unique identifier for this action</param>
        /// <param name="title">Title for the action (and button label)</param>
        /// <param name="options">Options for the action</param>
        public iOSNotificationAction(string id, string title, iOSNotificationActionOptions options)
        {
            Id = id;
            Title = title;
            Options = options;
        }

        internal virtual IntPtr CreateUNNotificationAction()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return iOSNotificationsWrapper._CreateUNNotificationAction(Id, Title, (int)Options, (int)_imageType, _image);
#else
            return IntPtr.Zero;
#endif
        }
    }

    /// <summary>
    /// Represents a special notification action with text input support.
    /// Each action within an application unique must have unique ID.
    /// When user chooses this action, a prompt for text input appears.
    /// If this action is responded to by the user, iOSNotificationCenter.GetLastRespondedNotificationAction will return it's ID,
    /// while iOSNotificationCenter.GetLastRespondedNotificationUserText will return the text entered.
    /// </summary>
    public class iOSTextInputNotificationAction
        : iOSNotificationAction
    {
        /// <summary>
        /// Text label for the button for submitting the text input.
        /// </summary>
        public string TextInputButtonTitle { get; set; }

        /// <summary>
        /// The placeholder text for input.
        /// </summary>
        public string TextInputPlaceholder { get; set; }

        /// <summary>
        /// Creates new text input action.
        /// </summary>
        /// <param name="id">Unique identifier for this action</param>
        /// <param name="title">Title for the action (and button label)</param>
        /// <param name="buttonTitle">Label for a button for submitting the text input</param>
        public iOSTextInputNotificationAction(string id, string title, string buttonTitle)
            : base(id, title)
        {
            TextInputButtonTitle = buttonTitle;
        }

        /// <summary>
        /// Creates new text input action.
        /// </summary>
        /// <param name="id">Unique identifier for this action</param>
        /// <param name="title">Title for the action (and button label)</param>
        /// <param name="options">Options for the action</param>
        /// <param name="buttonTitle">Label for a button for submitting the text input</param>
        public iOSTextInputNotificationAction(string id, string title, iOSNotificationActionOptions options, string buttonTitle)
            : base(id, title, options)
        {
            TextInputButtonTitle = buttonTitle;
        }

        internal override IntPtr CreateUNNotificationAction()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return iOSNotificationsWrapper._CreateUNTextInputNotificationAction(Id, Title, (int)Options, (int)_imageType, _image, TextInputButtonTitle, TextInputPlaceholder);
#else
            return IntPtr.Zero;
#endif
        }
    }
}

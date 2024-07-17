using System;
using System.Collections.Generic;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Options for notification category. Multiple options can be combined.
    /// These represent values from <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions">UNNotificationCategoryOptions</a>.
    /// </summary>
    [Flags]
    public enum iOSNotificationCategoryOptions
    {
        /// <summary>
        /// No options specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Refer to <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions/customdismissaction?language=objc">UNNotificationCategoryOptionCustomDismissAction</a>.
        /// </summary>
        CustomDismissAction = (1 << 0),

        /// <summary>
        /// Refer to <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions/allowincarplay?language=objc">UNNotificationCategoryOptionAllowInCarPlay</a>.
        /// </summary>
        AllowInCarPlay = (1 << 1),

        /// <summary>
        /// Refer to <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions/hiddenpreviewsshowtitle?language=objc">UNNotificationCategoryOptionHiddenPreviewsShowTitle</a>.
        /// </summary>
        HiddenPreviewsShowTitle = (1 << 2),

        /// <summary>
        /// Refer to <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions/hiddenpreviewsshowsubtitle?language=objc">UNNotificationCategoryOptionHiddenPreviewsShowSubtitle</a>.
        /// </summary>
        HiddenPreviewsShowSubtitle = (1 << 3),
    }

    /// <summary>
    /// Represents notification category.
    /// Notification categories need to be registered on application start to be useful.
    /// By adding actions to category, you make all notification sent with this category identifier actionable.
    /// </summary>
    /// <seealso cref="iOSNotification.CategoryIdentifier"/>
    /// <seealso cref="https://developer.apple.com/documentation/usernotifications/unnotificationcategory"/>
    public class iOSNotificationCategory
    {
        List<iOSNotificationAction> m_Actions = new List<iOSNotificationAction>();
        List<string> m_IntentIdentifiers = new List<string>();

        /// <summary>
        /// A unique identifier for this category.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Get actions set for this category.
        /// For more information, refer to <a href="Unity.Notifications.iOS.iOSNotificationAction.html">iOSNotificationAction</a>.
        /// </summary>
        public iOSNotificationAction[] Actions { get { return m_Actions.ToArray(); } }

        /// <summary>
        /// Intent <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/1649282-intentidentifiers">identifiers</a> set for this category.
        /// </summary>
        public string[] IntentIdentifiers { get { return m_IntentIdentifiers.ToArray(); } }

        /// <summary>
        /// The <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/2873736-hiddenpreviewsbodyplaceholder">placeholder</a> text to display when the system disables notification previews for the app.
        /// </summary>
        public string HiddenPreviewsBodyPlaceholder { get; set; }

        /// <summary>
        /// A <a href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/2963112-categorysummaryformat">format</a> string used for the summary description when the system groups the notifications based on their category.
        /// </summary>
        public string SummaryFormat { get; set; }

        /// <summary>
        /// Options for how to handle notifications of this type.
        /// </summary>
        public iOSNotificationCategoryOptions Options { get; set; }

        /// <summary>
        /// Create notification category.
        /// Category must be registered using iOSNotificationCenter.SetNotificationCategories.
        /// </summary>
        /// <param name="id">A unique identifier for this category</param>
        public iOSNotificationCategory(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Create notification category.
        /// Category must be registered using iOSNotificationCenter.SetNotificationCategories.
        /// </summary>
        /// <param name="id">A unique identifier for this category</param>
        /// <param name="actions">Add provided actions to this category</param>
        public iOSNotificationCategory(string id, IEnumerable<iOSNotificationAction> actions)
            : this(id)
        {
            if (actions != null)
                m_Actions.AddRange(actions);
        }

        /// <summary>
        /// Create notification category.
        /// Category must be registered using iOSNotificationCenter.SetNotificationCategories.
        /// </summary>
        /// <param name="id">A unique identifier for this category</param>
        /// <param name="actions">Add provided actions to this category</param>
        /// <param name="intentIdentifiers">Add provided intent identifiers to this category</param>
        public iOSNotificationCategory(string id, IEnumerable<iOSNotificationAction> actions, IEnumerable<string> intentIdentifiers)
            : this(id, actions)
        {
            if (intentIdentifiers != null)
                m_IntentIdentifiers.AddRange(intentIdentifiers);
        }

        /// <summary>
        /// Add action to this category.
        /// Actions must be added prior to registering the category.
        /// </summary>
        /// <param name="action">Action to add</param>
        public void AddAction(iOSNotificationAction action)
        {
            if (action == null)
                throw new ArgumentException("Cannot add null action");
            m_Actions.Add(action);
        }

        /// <summary>
        /// Add actions to this category.
        /// Actions must be added prior to registering the category.
        /// </summary>
        /// <param name="actions">Actions to add</param>
        public void AddActions(IEnumerable<iOSNotificationAction> actions)
        {
            if (actions == null)
                throw new ArgumentException("Cannot add null actions collection");
            m_Actions.AddRange(actions);
        }

        /// <summary>
        /// Add intent identifier to this category.
        /// Intent identifiers must be added prior to registering the category.
        /// </summary>
        /// <param name="identifier">Intent identifier to add</param>
        public void AddIntentIdentifier(string identifier)
        {
            if (identifier == null)
                throw new ArgumentException("Cannot add null intent identifier");
            m_IntentIdentifiers.Add(identifier);
        }

        /// <summary>
        /// Add intent identifier to this category.
        /// Intent identifiers must be added prior to registering the category.
        /// </summary>
        /// <param name="identifiers">Intent identifiers to add</param>
        public void AddIntentIdentifiers(IEnumerable<string> identifiers)
        {
            if (identifiers == null)
                throw new ArgumentException("Cannot add null intent identifiers collection");
            m_IntentIdentifiers.AddRange(identifiers);
        }
    }
}

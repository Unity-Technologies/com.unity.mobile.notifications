using System;
using System.Collections.Generic;

namespace Unity.Notifications.iOS
{
    /// <summary>
    /// Options for notification category. Multiple options can be combined.
    /// These represent values from UNNotificationCategoryOptions.
    /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcategoryoptions"/>
    /// </summary>
    [Flags]
    public enum iOSNotificationCategoryOptions
    {
        None = 0,
        CustomDismissAction = (1 << 0),
        AllowInCarPlay = (1 << 1),
        HiddenPreviewsShowTitle = (1 << 2),
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
        /// For more info see <see cref="iOSNotificationAction"/>.
        /// </summary>
        public iOSNotificationAction[] Actions { get { return m_Actions.ToArray(); } }

        /// <summary>
        /// Intent identifiers set for this category.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/1649282-intentidentifiers"/>
        /// </summary>
        public string[] IntentIdentifiers { get { return m_IntentIdentifiers.ToArray(); } }

        /// <summary>
        /// The placeholder text to display when the system disables notification previews for the app.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/2873736-hiddenpreviewsbodyplaceholder"/>
        /// </summary>
        public string HiddenPreviewsBodyPlaceholder { get; set; }

        /// <summary>
        /// A format string for the summary description used when the system groups the category’s notifications.
        /// <see href="https://developer.apple.com/documentation/usernotifications/unnotificationcategory/2963112-categorysummaryformat"/>
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

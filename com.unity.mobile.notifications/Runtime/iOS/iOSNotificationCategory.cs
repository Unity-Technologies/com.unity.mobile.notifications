using System;
using System.Collections.Generic;

namespace Unity.Notifications.iOS
{
    [Flags]
    public enum iOSNotificationCategoryOptions
    {
        None = 0,
        CustomDismissAction = (1 << 0),
        AllowInCarPlay = (1 << 1),
        HiddenPreviewsShowTitle = (1 << 2),
        HiddenPreviewsShowSubtitle = (1 << 3),
    }

    public class iOSNotificationCategory
    {
        List<iOSNotificationAction> m_Actions = new List<iOSNotificationAction>();
        List<string> m_IntentIdentifiers = new List<string>();

        public string Id { get; set; }
        public iOSNotificationAction[] Actions { get { return m_Actions.ToArray(); } }
        public string[] IntentIdentifiers { get { return m_IntentIdentifiers.ToArray(); } }
        public string HiddenPreviewsBodyPlaceholder { get; set; }
        public string SummaryFormat { get; set; }
        public iOSNotificationCategoryOptions Options { get; set; }

        public iOSNotificationCategory(string id)
        {
            Id = id;
        }

        public iOSNotificationCategory(string id, IEnumerable<iOSNotificationAction> actions)
            : this(id)
        {
            if (actions != null)
                m_Actions.AddRange(actions);
        }

        public iOSNotificationCategory(string id, IEnumerable<iOSNotificationAction> actions, IEnumerable<string> intentIdentifiers)
            : this(id, actions)
        {
            if (intentIdentifiers != null)
                m_IntentIdentifiers.AddRange(intentIdentifiers);
        }

        public void AddAction(iOSNotificationAction action)
        {
            if (action == null)
                throw new ArgumentException("Cannot add null action");
            m_Actions.Add(action);
        }

        public void AddActions(IEnumerable<iOSNotificationAction> actions)
        {
            if (actions == null)
                throw new ArgumentException("Cannot add null actions collection");
            m_Actions.AddRange(actions);
        }

        public void AddIntentIdentifier(string identifier)
        {
            if (identifier == null)
                throw new ArgumentException("Cannot add null intent identifier");
            m_IntentIdentifiers.Add(identifier);
        }

        public void AddIntentIdentifiers(IEnumerable<string> identifiers)
        {
            if (identifiers == null)
                throw new ArgumentException("Cannot add null intent identifiers collection");
            m_IntentIdentifiers.AddRange(identifiers);
        }
    }
}

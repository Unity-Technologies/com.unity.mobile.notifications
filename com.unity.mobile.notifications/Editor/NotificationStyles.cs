using UnityEngine;

namespace Unity.Notifications
{
    internal class NotificationStyles
    {
        public static readonly GUIStyle k_EvenRow = new GUIStyle("CN EntryBackEven");
        public static readonly GUIStyle k_OddRow = new GUIStyle("CN EntryBackOdd");
        public static readonly GUIStyle k_PreviewMessageTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 8, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        public static readonly GUIStyle k_PreviewLabelTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 8, wordWrap = true, alignment = TextAnchor.UpperCenter };
        public static readonly GUIStyle k_IconHelpMessageStyle = new GUIStyle(GUI.skin.GetStyle("HelpBox")) { fontSize = 10, wordWrap = true, alignment = TextAnchor.UpperLeft };
        public static readonly GUIStyle k_ToggleStyle = new GUIStyle(GUI.skin.GetStyle("Toggle")) { alignment = TextAnchor.MiddleRight };
        public static readonly GUIStyle k_DropwDownStyle = new GUIStyle(GUI.skin.GetStyle("Button")) { alignment = TextAnchor.MiddleLeft };
        public static readonly GUIStyle k_LabelStyle = new GUIStyle(GUI.skin.GetStyle("Label")) { wordWrap = true };
    }
}

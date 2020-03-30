using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Notifications.iOS;
using UnityEngine.Assertions;

namespace Unity.Notifications
{
    [CustomEditor(typeof(NotificationSettingsManager))]
    internal class NotificationSettingsManagerEditor : Editor
    {
        private const int k_SlotSize = 64;
        private const int k_HeaderHeight = 80;

        private const int k_MaxPreviewSize = 64;
        private const int k_IconSpacing = 8;
        private const float k_Padding = 12f;
        private const float k_ToolbarHeight = 20f;

        private readonly GUIContent k_IdentifierLabelText = new GUIContent("Identifier");
        private readonly GUIContent k_TypeLabelText = new GUIContent("Type");
        private readonly string[] k_ToolbarStrings = {"Android", "iOS"};
        private const string k_InfoStringAndroid =
            "Only icons added to this list or manually added to the `res/drawable` folder can be used by notifications.\n " +
            "Small icons can only be composed simply of white pixels on a transparent backdrop and must be at least 48x48 pixels. \n" +
            "Large icons can contain any colors but must be not smaller than 192x192 pixels.";

        private SerializedProperty m_ResourceAssets;
        private SerializedObject m_Target;
        private ReorderableList m_ReorderableList;

        private Vector2 m_ScrollViewStart;

        private NotificationSettingsManager m_SettingsManager;

        [SettingsProvider]
        static SettingsProvider CreateMobileNotificationsSettingsProvider()
        {
            var settingsAsset = NotificationSettingsManager.Initialize();
            Assert.IsNotNull(settingsAsset);

            var provider = AssetSettingsProvider.CreateProviderFromObject("Project/Mobile Notifications", settingsAsset);
            provider.label = "Mobile Notifications";
            return provider;
        }

        private void OnEnable()
        {
            m_SettingsManager = NotificationSettingsManager.Initialize();

            if (target == null)
                return;

            m_Target = new SerializedObject(target);

            m_ResourceAssets = serializedObject.FindProperty("TrackedResourceAssets");

            InitReorderableList();

            Undo.undoRedoPerformed += UpdateEditorStateOnUndo;
        }

        private void InitReorderableList()
        {
            m_ReorderableList = new ReorderableList(serializedObject, m_ResourceAssets, false, true, true, true);
            m_ReorderableList.elementHeight = k_SlotSize + k_IconSpacing;
            m_ReorderableList.showDefaultBackground = false;

            m_ReorderableList.drawHeaderCallback = (rect) =>
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var paddedRect = GetContentRect(rect, 1f, -(ReorderableList.Defaults.padding + 2f));

                    var headerBackground = new GUIStyle("RL Header");
                    headerBackground.Draw(paddedRect, false, false, false, false);

                    var labelRect = GetContentRect(paddedRect, 0f, 3f);
                    GUI.Label(labelRect, "Notification Icons", EditorStyles.label);
                }
            };

            m_ReorderableList.onAddCallback = (list) => AddIconDataElement(list);
            m_ReorderableList.onRemoveCallback = (list) => RemoveIconDataElement(list);
            m_ReorderableList.onCanAddCallback = (list) => CanAddCallbackDelegate(list);
            m_ReorderableList.drawElementCallback = (rect, index, selected, focused) => DrawIconDataElement(rect, index, selected, focused);

            m_ReorderableList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                var evenRow = new GUIStyle("CN EntryBackEven");
                var oddRow = new GUIStyle("CN EntryBackOdd");

                var background = index % 2 == 0 ? evenRow : oddRow;
                background.Draw(rect, false, false, false, false);

                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, true);
            };

            m_ReorderableList.elementHeightCallback = (index) =>
            {
                var data = GetElementData(index);
                if (data == null)
                    return k_SlotSize;

                return m_ReorderableList.elementHeight + (data.Asset != null && !data.IsValid ? k_SlotSize : 0);
            };
        }

        private void UpdateEditorStateOnUndo()
        {
            serializedObject.UpdateIfRequiredOrScript();
            Repaint();
        }

        private DrawableResourceData GetElementData(int index)
        {
            var resourceAssets = NotificationSettingsManager.Initialize().TrackedResourceAssets;
            if (index < resourceAssets.Count)
                return resourceAssets[index];

            return null;
        }

        private void DrawIconDataElement(Rect rect, int index, bool selected, bool focused)
        {
            var drawableResourceDataRef = m_ResourceAssets.GetArrayElementAtIndex(index);
            if (drawableResourceDataRef == null)
                return;

            var elementRect = rect;

            float width = Mathf.Min(elementRect.width, EditorGUIUtility.labelWidth + 4 + k_SlotSize + k_IconSpacing + k_MaxPreviewSize);

            float idPropWidth = Mathf.Min(k_MaxPreviewSize, width - k_SlotSize - k_IconSpacing);
            float typePropWidth = Mathf.Min(k_MaxPreviewSize, width - k_SlotSize - k_IconSpacing);
            float assetPropWidth = k_MaxPreviewSize;
            float errorMsgWidth = elementRect.width - k_Padding * 2;

            Rect elementContentRect = GetContentRect(elementRect, 6f, 12f);

            Rect previewTextureRect = new Rect(elementContentRect.width - (assetPropWidth - k_IconSpacing * 5),
                elementContentRect.y - 6, assetPropWidth, k_SlotSize);

            Rect textureRect = new Rect(elementContentRect.width - (assetPropWidth * 2 - k_IconSpacing * 5),
                elementContentRect.y, assetPropWidth, k_SlotSize);

            Rect errorBoxRect = GetContentRect(new Rect(elementContentRect.x, elementContentRect.y + k_SlotSize, errorMsgWidth, k_SlotSize),
                4f, 4f);

            EditorGUI.LabelField(new Rect(elementContentRect.x, elementContentRect.y, idPropWidth, 20), k_IdentifierLabelText);

            EditorGUI.LabelField(new Rect(elementContentRect.x, elementContentRect.y + 25, idPropWidth, 20), k_TypeLabelText);

            var elementData = GetElementData(index);

            var newId = EditorGUI.TextField(new Rect(elementContentRect.x + k_SlotSize, elementContentRect.y, idPropWidth, 20), elementData.Id);

            var newType = (NotificationIconType)EditorGUI.EnumPopup(
                new Rect(elementContentRect.x + k_SlotSize, elementContentRect.y + 25, typePropWidth, 20),
                elementData.Type);

            var newAsset = (Texture2D)EditorGUI.ObjectField(textureRect, elementData.Asset, typeof(Texture2D), false);

            bool updatePreviewTexture = (newId != elementData.Id || newType != elementData.Type || newAsset != elementData.Asset);

            if (updatePreviewTexture)
            {
                Undo.RegisterCompleteObjectUndo(target, "Update icon data.");
                elementData.Id = newId;
                elementData.Type = newType;
                elementData.Asset = newAsset;
                elementData.Clean();
                elementData.Verify();
                m_SettingsManager.SaveSettings();
            }

            if (elementData.Asset != null && !elementData.Verify())
            {
                EditorGUI.HelpBox(errorBoxRect,
                    "Specified texture can't be used because: \n"
                    + (errorMsgWidth > 145 ? DrawableResourceData.GenerateErrorString(elementData.Errors) : "...expand to see more..."),
                    MessageType.Error);

                if (elementData.Type == NotificationIconType.SmallIcon)
                {
                    GUIStyle helpBoxMessageTextStyle = new GUIStyle(GUI.skin.label);
                    helpBoxMessageTextStyle.fontSize = 8;
                    helpBoxMessageTextStyle.wordWrap = true;
                    helpBoxMessageTextStyle.alignment = TextAnchor.MiddleCenter;
                    GUI.Box(previewTextureRect, "Preview not available. \n Make sure the texture is readable!", helpBoxMessageTextStyle);
                }
            }
            else
            {
                Texture2D previewTexture = elementData.GetPreviewTexture(updatePreviewTexture);
                if (previewTexture != null)
                {
                    GUIStyle previewLabelTextStyle = new GUIStyle(GUI.skin.label);
                    previewLabelTextStyle.fontSize = 8;
                    previewLabelTextStyle.wordWrap = true;
                    previewLabelTextStyle.alignment = TextAnchor.UpperCenter;

                    EditorGUI.LabelField(previewTextureRect, "Preview", previewLabelTextStyle);

                    Rect previewTextureRectPadded = GetContentRect(previewTextureRect, 6f, 6f);
                    previewTextureRectPadded.y += 8;

                    previewTexture.alphaIsTransparency = false;
                    GUI.DrawTexture(previewTextureRectPadded, previewTexture);
                }
            }
        }

        private void AddIconDataElement(ReorderableList list)
        {
            serializedObject.Update();

            Undo.RegisterCompleteObjectUndo(target, "Add a new icon element.");
            m_SettingsManager.RegisterDrawableResource(string.Format("icon_{0}", m_SettingsManager.TrackedResourceAssets.Count), null, NotificationIconType.SmallIcon);

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveIconDataElement(ReorderableList list)
        {
            serializedObject.Update();

            m_SettingsManager.RemoveDrawableResource(list.index);

            serializedObject.ApplyModifiedProperties();
        }

        private bool CanAddCallbackDelegate(ReorderableList list)
        {
            var trackedAssets = m_SettingsManager.TrackedResourceAssets;
            if (trackedAssets.Count <= 0)
                return true;

            return trackedAssets.All(asset => asset.Initialized());
        }

        public override void OnInspectorGUI()
        {
            if (m_Target == null)
                return;

            var width = EditorGUIUtility.currentViewWidth - 300f;
            if (width < k_SlotSize * 10)
                width = k_SlotSize * 10;

            var rect = new Rect(10f, 0f, width, Screen.height);
            OnInspectorGUI(rect);
        }

        public void OnInspectorGUI(Rect rect, bool drawInInspector = false)
        {
            if (m_Target == null)
                return;

            serializedObject.Update();

            // TODO:
            // Not sure why we have to do special things for UI in 2018.3.
#if UNITY_2018_3
            rect = new Rect(rect.x, rect.y + 10f, rect.width, rect.height);
#endif

            var headerRect = GetContentRect(new Rect(k_Padding, rect.y, rect.width - k_Padding, k_Padding * 2));

            var bodyRect = GetContentRect(
                new Rect(k_Padding, headerRect.yMax, rect.width - k_Padding, rect.height - headerRect.height),
                k_Padding,
                k_Padding
            );

            if (drawInInspector)
            {
                var viewRect = GetContentRect(
                    new Rect(rect.x, rect.y, rect.width, headerRect.height + m_ReorderableList.GetHeight() + k_SlotSize),
                    -k_Padding,
                    k_Padding
                );
                m_ScrollViewStart = GUI.BeginScrollView(rect, m_ScrollViewStart, viewRect, false, false);
            }

            var toolBaRect = new Rect(rect.x, rect.y, rect.width, k_ToolbarHeight);
            m_SettingsManager.ToolbarIndex = GUI.Toolbar(toolBaRect, m_SettingsManager.ToolbarIndex, k_ToolbarStrings);

            if (m_SettingsManager.ToolbarIndex == 0)
            {
                var settings = m_SettingsManager.AndroidNotificationSettings;
                if (settings == null)
                    return;

                var styleToggle = new GUIStyle(GUI.skin.GetStyle("Toggle"));
                styleToggle.alignment = TextAnchor.MiddleRight;

                var styleDropwDown = new GUIStyle(GUI.skin.GetStyle("Button"));
                styleDropwDown.fixedWidth = k_SlotSize * 2.5f;

                var settingsPanelRect = bodyRect;
                GUI.BeginGroup(settingsPanelRect);
                DrawSettingsElementList(settingsPanelRect, BuildTargetGroup.Android, settings, false, styleToggle, styleDropwDown);
                GUI.EndGroup();

                var headerMsgStyle = new GUIStyle(GUI.skin.GetStyle("HelpBox"));
                headerMsgStyle.alignment = TextAnchor.UpperCenter;
                headerMsgStyle.fontSize = 10;
                headerMsgStyle.wordWrap = true;

                var iconListRectHeader = new Rect(bodyRect.x, bodyRect.y + 85f, bodyRect.width, 55f);
                EditorGUI.TextArea(iconListRectHeader, k_InfoStringAndroid, headerMsgStyle);

                var iconListRectBody = new Rect(iconListRectHeader.x, iconListRectHeader.y + 95f, iconListRectHeader.width, iconListRectHeader.height - 55f);
                m_ReorderableList.DoList(iconListRectBody);

                if (!drawInInspector)
                    EditorGUILayout.GetControlRect(true, iconListRectHeader.height + m_ReorderableList.GetHeight() + k_SlotSize);
            }
            else
            {
                var settings = m_SettingsManager.iOSNotificationSettings;
                if (settings == null)
                    return;

                var styleToggle = new GUIStyle(GUI.skin.GetStyle("Toggle"));
                styleToggle.alignment = TextAnchor.MiddleRight;

                var styleDropwDown = new GUIStyle(GUI.skin.GetStyle("Button"));
                styleDropwDown.fixedWidth = k_SlotSize * 2.5f;

                var settingsPanelRect = bodyRect;
                GUI.BeginGroup(settingsPanelRect);
                DrawSettingsElementList(settingsPanelRect, BuildTargetGroup.iOS, settings, false, styleToggle, styleDropwDown);
                GUI.EndGroup();

                if (!drawInInspector)
                    EditorGUILayout.GetControlRect(true, 4 * k_SlotSize);
            }

            serializedObject.ApplyModifiedProperties();
            if (drawInInspector)
                GUI.EndScrollView();
        }

        private void DrawSettingsElementList(Rect rect, BuildTargetGroup buildTarget, List<NotificationSetting> settings, bool disabled, GUIStyle styleToggle, GUIStyle styleDropwDown, int layer = 0)
        {
            foreach (var setting in settings)
            {
                EditorGUI.BeginDisabledGroup(disabled);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(layer * 13);

                var styleLabel = new GUIStyle(GUI.skin.GetStyle("Label"));
                var width = rect.width - k_SlotSize * 4.5f - layer * 13;

                styleLabel.fixedWidth = width;
                styleLabel.wordWrap = true;

                GUILayout.Label(new GUIContent(setting.Label, setting.Tooltip), styleLabel);

                EditorGUI.BeginChangeCheck();

                bool dependenciesDisabled = false;

                if (setting.Value.GetType() == typeof(bool))
                {
                    setting.Value = EditorGUILayout.Toggle((bool)setting.Value, styleToggle);
                    dependenciesDisabled = !(bool)setting.Value;
                }
                else if (setting.Value.GetType() == typeof(string))
                {
                    setting.Value = EditorGUILayout.TextField((string)setting.Value);
                }
                else if (setting.Value.GetType() == typeof(PresentationOption))
                {
                    setting.Value = (PresentationOption)EditorGUILayout.EnumFlagsField((iOSPresentationOption)setting.Value, styleDropwDown);
                    if ((iOSPresentationOption)setting.Value == 0)
                        setting.Value = (PresentationOption)iOSPresentationOption.All;
                }
                else if (setting.Value.GetType() == typeof(AuthorizationOption))
                {
                    setting.Value = (AuthorizationOption)EditorGUILayout.EnumFlagsField((iOSAuthorizationOption)setting.Value, styleDropwDown);
                    if ((iOSAuthorizationOption)setting.Value == 0)
                        setting.Value = (AuthorizationOption)iOSAuthorizationOption.All;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    m_SettingsManager.SaveSetting(setting, buildTarget);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();

                if (setting.Dependencies != null)
                {
                    DrawSettingsElementList(rect, buildTarget, setting.Dependencies, dependenciesDisabled, styleToggle, styleDropwDown, layer + 1);
                }
            }
        }

        private static Rect GetContentRect(Rect rect, float paddingVertical = 0, float paddingHorizontal = 0)
        {
            Rect tempRect = rect;
            tempRect.yMin += paddingVertical;
            tempRect.yMax -= paddingVertical;
            tempRect.xMin += paddingHorizontal;
            tempRect.xMax -= paddingHorizontal;

            return tempRect;
        }
    }
}

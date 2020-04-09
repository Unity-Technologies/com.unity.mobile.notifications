using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Notifications.iOS;
using UnityEngine.Assertions;

namespace Unity.Notifications
{
    internal class NotificationSettingsProvider : SettingsProvider
    {
        internal const int k_SlotSize = 64;
        private const int k_MaxPreviewSize = 64;
        private const int k_IconSpacing = 8;
        private const float k_Padding = 12f;
        private const float k_ToolbarHeight = 20f;

        private readonly GUIContent k_IdentifierLabelText = new GUIContent("Identifier");
        private readonly GUIContent k_TypeLabelText = new GUIContent("Type");

        private readonly string[] k_ToolbarStrings = {"Android", "iOS"};
        private const string k_InfoStringAndroid =
            "Only icons added to this list or manually added to the `res/drawable` folder can be used by notifications.\n\n" +
            "Small icons can only be composed simply of white pixels on a transparent backdrop and must be at least 48x48 pixels.\n" +
            "Large icons can contain any colors but must be not smaller than 192x192 pixels.";

        private NotificationSettingsManager m_SettingsManager;

        private SerializedObject m_SettingsManagerObject;
        private SerializedProperty m_DrawableResources;

        private Vector2 m_ScrollViewStart;
        private ReorderableList m_ReorderableList;

        private NotificationSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
        }

        [SettingsProvider]
        static SettingsProvider CreateMobileNotificationsSettingsProvider()
        {
            var provider = new NotificationSettingsProvider("Project/Mobile Notifications", SettingsScope.Project);
            provider.Initialize();

            return provider;
        }

        private void Initialize()
        {
            label = "Mobile Notifications";
            m_SettingsManager = NotificationSettingsManager.Initialize();

            // These two are for ReorderableList.
            m_SettingsManagerObject = new SerializedObject(m_SettingsManager);
            m_DrawableResources = m_SettingsManagerObject.FindProperty("DrawableResources");

            // ReorderableList is only used to draw the drawable resources for Android settings.
            InitReorderableList();

            Undo.undoRedoPerformed += () =>
            {
                m_SettingsManagerObject.UpdateIfRequiredOrScript();
                Repaint();
            };
        }

        private void InitReorderableList()
        {
            m_ReorderableList = new ReorderableList(m_SettingsManagerObject, m_DrawableResources, false, false, true, true);
            m_ReorderableList.elementHeight = k_SlotSize + k_IconSpacing;
            m_ReorderableList.showDefaultBackground = true;
            m_ReorderableList.headerHeight = 1;

            // Register all the necessary callbacks on ReorderableList.
            m_ReorderableList.onAddCallback = (list) =>
            {
                Undo.RegisterCompleteObjectUndo(m_SettingsManager, "Add a new icon element.");
                m_SettingsManager.AddDrawableResource(string.Format("icon_{0}", m_SettingsManager.DrawableResources.Count), null, NotificationIconType.Small);
            };

            m_ReorderableList.onRemoveCallback = (list) =>
            {
                m_SettingsManager.RemoveDrawableResource(list.index);
            };

            m_ReorderableList.onCanAddCallback = (list) =>
            {
                var trackedAssets = m_SettingsManager.DrawableResources;
                if (trackedAssets.Count <= 0)
                    return true;

                return trackedAssets.All(asset => asset.Initialized());
            };

            m_ReorderableList.drawElementCallback = (rect, index, active, focused) => DrawIconDataElement(rect, index, active, focused);

            m_ReorderableList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                var background = index % 2 == 0 ? NotificationStyles.k_EvenRow : NotificationStyles.k_OddRow;
                background.Draw(rect, false, false, false, false);

                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, true);
            };

            m_ReorderableList.elementHeightCallback = (index) =>
            {
                var data = GetDrawableResource(index);
                if (data == null)
                    return k_SlotSize;

                return m_ReorderableList.elementHeight + (data.Asset != null && !data.IsValid ? k_SlotSize : 0);
            };
        }

        private void DrawIconDataElement(Rect rect, int index, bool active, bool focused)
        {
            var drawableResource = GetDrawableResource(index);
            if (drawableResource == null)
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

            var newId = EditorGUI.TextField(new Rect(elementContentRect.x + k_SlotSize, elementContentRect.y, idPropWidth, 20), drawableResource.Id);

            var newType = (NotificationIconType)EditorGUI.EnumPopup(
                new Rect(elementContentRect.x + k_SlotSize, elementContentRect.y + 25, typePropWidth, 20),
                drawableResource.Type);

            var newAsset = (Texture2D)EditorGUI.ObjectField(textureRect, drawableResource.Asset, typeof(Texture2D), false);

            bool updatePreviewTexture = (newId != drawableResource.Id || newType != drawableResource.Type || newAsset != drawableResource.Asset);

            if (updatePreviewTexture)
            {
                Undo.RegisterCompleteObjectUndo(m_SettingsManager, "Update icon data.");
                drawableResource.Id = newId;
                drawableResource.Type = newType;
                drawableResource.Asset = newAsset;
                drawableResource.Clean();
                drawableResource.Verify();
                m_SettingsManager.SaveSettings();
            }

            if (drawableResource.Asset != null && !drawableResource.Verify())
            {
                EditorGUI.HelpBox(errorBoxRect,
                    "Specified texture can't be used because: \n"
                    + (errorMsgWidth > 145 ? DrawableResourceData.GenerateErrorString(drawableResource.Errors) : "...expand to see more..."),
                    MessageType.Error);

                if (drawableResource.Type == NotificationIconType.Small)
                {
                    GUI.Box(previewTextureRect, "Preview not available. \n Make sure the texture is readable!", NotificationStyles.k_HelpBoxMessageTextStyle);
                }
            }
            else
            {
                Texture2D previewTexture = drawableResource.GetPreviewTexture(updatePreviewTexture);
                if (previewTexture != null)
                {
                    EditorGUI.LabelField(previewTextureRect, "Preview", NotificationStyles.k_PreviewLabelTextStyle);

                    Rect previewTextureRectPadded = GetContentRect(previewTextureRect, 6f, 6f);
                    previewTextureRectPadded.y += 8;

                    previewTexture.alphaIsTransparency = false;
                    GUI.DrawTexture(previewTextureRectPadded, previewTexture);
                }
            }
        }

        private DrawableResourceData GetDrawableResource(int index)
        {
            var resourceAssets = m_SettingsManager.DrawableResources;
            if (index < resourceAssets.Count)
                return resourceAssets[index];

            return null;
        }

        public override void OnGUI(string searchContext)
        {
            // This has to be called to sync all the changes between m_SettingsManager and m_SettingsManagerObject.
            m_SettingsManagerObject.Update();

            var noHeightRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(0));
            var width = noHeightRect.width;
            if (width < k_SlotSize * 10)
                width = k_SlotSize * 10;

            var totalRect = new Rect(k_Padding, 0f, width - k_Padding, Screen.height);

            // Draw the toolbar for Android/iOS.
            var toolBarRect = new Rect(totalRect.x, totalRect.y, totalRect.width, k_ToolbarHeight);
            m_SettingsManager.ToolbarIndex = GUI.Toolbar(toolBarRect, m_SettingsManager.ToolbarIndex, k_ToolbarStrings);

            var notificationsPanelRect = new Rect(totalRect.x, k_ToolbarHeight + 2, totalRect.width, totalRect.height - k_ToolbarHeight - k_Padding);

            // Draw the notification settings.
            if (!DrawNotificationSettingsPanel(notificationsPanelRect, m_SettingsManager.ToolbarIndex))
                return;

            float heightPlaceHolder = k_SlotSize;

            // Draw drawable resources list for Android.
            if (m_SettingsManager.ToolbarIndex == 0)
            {
                var iconListlabelRect = new Rect(notificationsPanelRect.x, notificationsPanelRect.y + 85f, 150f, 18f);
                GUI.Label(iconListlabelRect, "Notification Icons", EditorStyles.label);

                // Draw the help message for setting the icons.
                var iconListMessageRect = new Rect(notificationsPanelRect.x, notificationsPanelRect.y + 105f, notificationsPanelRect.width, 55f);
                EditorGUI.SelectableLabel(iconListMessageRect, k_InfoStringAndroid, NotificationStyles.k_HeaderMsgStyle);

                // Draw the reorderable list for the icon list.
                var iconListRect = new Rect(iconListMessageRect.x, iconListMessageRect.y + 58f, notificationsPanelRect.width, notificationsPanelRect.height - 55f);
                m_ReorderableList.DoList(iconListRect);

                heightPlaceHolder += iconListMessageRect.height + m_ReorderableList.GetHeight();
            }

            // We have to do this to occupy the space that ScrollView can set the scrollbars correctly.
            EditorGUILayout.GetControlRect(true, heightPlaceHolder, GUILayout.MinWidth(width));
        }

        private bool DrawNotificationSettingsPanel(Rect rect, int toolbarIndex)
        {
            Assert.IsTrue(toolbarIndex == 0 || toolbarIndex == 1);

            var settings = (toolbarIndex == 0) ? m_SettingsManager.AndroidNotificationSettings : m_SettingsManager.iOSNotificationSettings;
            if (settings == null)
                return false;

            GUI.BeginGroup(rect);
            DrawSettingElements(rect, (toolbarIndex == 0) ? BuildTargetGroup.Android : BuildTargetGroup.iOS, settings, false, 0);
            GUI.EndGroup();

            return true;
        }

        private void DrawSettingElements(Rect rect, BuildTargetGroup buildTarget, List<NotificationSetting> settings, bool disabled, int layer)
        {
            var spaceOffset = layer * 13;

            var labelStyle = new GUIStyle(NotificationStyles.s_LabelStyle);
            labelStyle.fixedWidth = k_SlotSize * 5 - spaceOffset;

            var toggleStyle = NotificationStyles.k_ToggleStyle;
            var dropdownStyle = NotificationStyles.k_DropwDownStyle;

            foreach (var setting in settings)
            {
                EditorGUI.BeginDisabledGroup(disabled);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(spaceOffset);

                GUILayout.Label(new GUIContent(setting.Label, setting.Tooltip), labelStyle);

                bool dependenciesDisabled = false;

                EditorGUI.BeginChangeCheck();
                if (setting.Value.GetType() == typeof(bool))
                {
                    setting.Value = EditorGUILayout.Toggle((bool)setting.Value, toggleStyle);
                    dependenciesDisabled = !(bool)setting.Value;
                }
                else if (setting.Value.GetType() == typeof(string))
                {
                    setting.Value = EditorGUILayout.TextField((string)setting.Value);
                }
                else if (setting.Value.GetType() == typeof(PresentationOption))
                {
                    setting.Value = (PresentationOption)EditorGUILayout.EnumFlagsField((iOSPresentationOption)setting.Value, dropdownStyle);
                    if ((iOSPresentationOption)setting.Value == 0)
                        setting.Value = (PresentationOption)iOSPresentationOption.All;
                }
                else if (setting.Value.GetType() == typeof(AuthorizationOption))
                {
                    setting.Value = (AuthorizationOption)EditorGUILayout.EnumFlagsField((iOSAuthorizationOption)setting.Value, dropdownStyle);
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
                    DrawSettingElements(rect, buildTarget, setting.Dependencies, dependenciesDisabled, layer + 1);
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

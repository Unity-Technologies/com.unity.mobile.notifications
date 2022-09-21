using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Notifications.iOS;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.Notifications
{
    internal class NotificationSettingsProvider : SettingsProvider
    {
        private const int k_SlotSize = 64;
        private const int k_IconSpacing = 8;
        private const float k_Padding = 12f;
        private const float k_ToolbarHeight = 20f;
        private const int k_VerticalSeparator = 2;
        private const int k_LabelLineHeight = 18;

        private readonly GUIContent k_IdentifierLabelText = new GUIContent("Identifier");
        private readonly GUIContent k_TypeLabelText = new GUIContent("Type");

        private readonly string[] k_ToolbarStrings = { "Android", "iOS" };
        private const string k_InfoStringAndroid =
            "Only icons added to this list or manually added to the 'res/drawable' folder can be used for notifications.\n" +
            "Note, that not all devices support colored icons.\n\n" +
            "Small icons must be at least 48x48px and only composed of white pixels on a transparent background.\n" +
            "Large icons must be no smaller than 192x192px and may contain colors.";

        private NotificationSettingsManager m_SettingsManager;

        private SerializedObject m_SettingsManagerObject;
        private SerializedProperty m_DrawableResources;

        private Vector2 m_ScrollViewStart;
        private ReorderableList m_ReorderableList;

        private NotificationSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
            Initialize();
        }

        [SettingsProvider]
        static SettingsProvider CreateMobileNotificationsSettingsProvider()
        {
            return new NotificationSettingsProvider("Project/Mobile Notifications", SettingsScope.Project);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            // in case of domain reload (enter-exit play mode, this gets lost)
            if (m_SettingsManager == null)
                Initialize();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            m_SettingsManager.SaveSettings(false);
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
                Undo.RegisterCompleteObjectUndo(m_SettingsManager, "Add a new icon element");
                m_SettingsManager.AddDrawableResource(string.Format("icon_{0}", m_SettingsManager.DrawableResources.Count), null, NotificationIconType.Small);
            };

            m_ReorderableList.onRemoveCallback = (list) =>
            {
                m_SettingsManager.RemoveDrawableResourceByIndex(list.index);
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

            var elementRect = AddPadding(rect, k_Padding, k_Padding / 2);

            // Calculate and draw id and type.
            var idLabelRect = new Rect(elementRect.x, elementRect.y, k_SlotSize, k_ToolbarHeight);
            var idTextFieldRect = new Rect(idLabelRect.x + k_SlotSize, idLabelRect.y, k_SlotSize, k_ToolbarHeight);

            var typeLabelRect = new Rect(elementRect.x, elementRect.y + 25, k_SlotSize, k_ToolbarHeight);
            var typeEnumPopupRect = new Rect(typeLabelRect.x + k_SlotSize, typeLabelRect.y, k_SlotSize, k_ToolbarHeight);

            EditorGUI.LabelField(idLabelRect, k_IdentifierLabelText);
            var newId = EditorGUI.TextField(idTextFieldRect, drawableResource.Id);

            EditorGUI.LabelField(typeLabelRect, k_TypeLabelText);
            var newType = (NotificationIconType)EditorGUI.EnumPopup(typeEnumPopupRect, drawableResource.Type);

            // Calculate and draw texture and preview.
            var textureX = Mathf.Max(elementRect.width - (k_SlotSize * 2 - k_IconSpacing * 5), k_SlotSize * 3);
            var textureRect = new Rect(textureX, elementRect.y, k_SlotSize, k_SlotSize);
            var previewTextureRect = new Rect(textureRect.x + k_SlotSize, textureRect.y - 6, k_SlotSize, k_SlotSize);

            var newAsset = (Texture2D)EditorGUI.ObjectField(textureRect, drawableResource.Asset, typeof(Texture2D), false);

            // Check if the texture has been updated.
            bool updatePreviewTexture = (newId != drawableResource.Id || newType != drawableResource.Type || newAsset != drawableResource.Asset);
            if (updatePreviewTexture)
            {
                Undo.RegisterCompleteObjectUndo(m_SettingsManager, "Update icon data");
                drawableResource.Id = newId;
                drawableResource.Type = newType;
                drawableResource.Asset = newAsset;
                drawableResource.Clean();
                drawableResource.Verify();
                m_SettingsManager.SaveSettings();
            }

            if (drawableResource.Asset != null && !drawableResource.Verify())
            {
                var errorMsgWidth = rect.width - k_Padding * 2;
                var errorRect = AddPadding(new Rect(elementRect.x, elementRect.y + k_SlotSize, errorMsgWidth, k_LabelLineHeight * 3), 4, 4);

                var errorMsg = "Specified texture can't be used because: \n" + drawableResource.GenerateErrorString();
                EditorGUI.HelpBox(errorRect, errorMsg, MessageType.Error);

                GUI.Box(previewTextureRect, "Preview not available", NotificationStyles.k_PreviewMessageTextStyle);
            }
            else
            {
                var previewTexture = drawableResource.GetPreviewTexture(updatePreviewTexture);
                if (previewTexture != null)
                {
                    previewTexture.alphaIsTransparency = false;

                    EditorGUI.LabelField(previewTextureRect, "Preview", NotificationStyles.k_PreviewLabelTextStyle);

                    var previewTextureRectPadded = AddPadding(previewTextureRect, 6f, 6f);
                    previewTextureRectPadded.y += 8;
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

        private static Rect AddPadding(Rect rect, float horizontal, float vertical)
        {
            Rect paddingRect = rect;
            paddingRect.xMin += horizontal;
            paddingRect.xMax -= horizontal;
            paddingRect.yMin += vertical;
            paddingRect.yMax -= vertical;

            return paddingRect;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SettingsManager == null)
                Initialize();

            // This has to be called to sync all the changes between m_SettingsManager and m_SettingsManagerObject.
            if (m_SettingsManagerObject.targetObject != null)
                m_SettingsManagerObject.Update();

            var noHeightRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(0));
            var width = noHeightRect.width;

            var totalRect = new Rect(k_Padding, 0f, width - k_Padding, Screen.height);

            // Draw the toolbar for Android/iOS.
            var toolBarRect = new Rect(totalRect.x, totalRect.y, totalRect.width, k_ToolbarHeight);
            var toolbarIndex = GUI.Toolbar(toolBarRect, m_SettingsManager.ToolbarIndex, k_ToolbarStrings);
            if (toolbarIndex != m_SettingsManager.ToolbarIndex)
            {
                m_SettingsManager.ToolbarIndex = toolbarIndex;
                m_SettingsManager.SaveSettings();
            }

            var notificationSettingsRect = new Rect(totalRect.x, k_ToolbarHeight + 2, totalRect.width, totalRect.height - k_ToolbarHeight - k_Padding);

            // Draw the notification settings.
            int drawnSettingsCount = DrawNotificationSettings(notificationSettingsRect, m_SettingsManager.ToolbarIndex);
            if (drawnSettingsCount <= 0)
                return;

            float heightPlaceHolder = k_SlotSize;

            // Draw drawable resources list for Android.
            if (m_SettingsManager.ToolbarIndex == 0)
            {
                var iconListlabelRect = new Rect(notificationSettingsRect.x, notificationSettingsRect.y + 85, 180, k_LabelLineHeight);
                GUI.Label(iconListlabelRect, "Notification Icons", EditorStyles.label);

                // Draw the help message for setting the icons.
                float labelHeight;
                if (notificationSettingsRect.width > 510)
                    labelHeight = k_LabelLineHeight * 4;
                else if (notificationSettingsRect.width > 500)
                    labelHeight = k_LabelLineHeight * 5;
                else
                    labelHeight = k_LabelLineHeight * 6;
                var iconListMsgRect = new Rect(iconListlabelRect.x, iconListlabelRect.y + iconListlabelRect.height + k_VerticalSeparator, notificationSettingsRect.width, labelHeight);
                EditorGUI.SelectableLabel(iconListMsgRect, k_InfoStringAndroid, NotificationStyles.k_IconHelpMessageStyle);

                // Draw the reorderable list for the icon list.
                var iconListRect = new Rect(iconListMsgRect.x, iconListMsgRect.y + iconListMsgRect.height + k_VerticalSeparator, iconListMsgRect.width, notificationSettingsRect.height - 55f);
                m_ReorderableList.DoList(iconListRect);

                heightPlaceHolder += iconListMsgRect.height + m_ReorderableList.GetHeight();
            }

            // We have to do this to occupy the space that ScrollView can set the scrollbars correctly.
            EditorGUILayout.GetControlRect(true, heightPlaceHolder, GUILayout.MinWidth(width));
        }

        private int DrawNotificationSettings(Rect rect, int toolbarIndex)
        {
            Assert.IsTrue(toolbarIndex == 0 || toolbarIndex == 1);

            List<NotificationSetting> settings;
            BuildTargetGroup buildTarget;
            int labelWidthMultiplier;
            if (toolbarIndex == 0)
            {
                settings = m_SettingsManager.AndroidNotificationSettings;
                buildTarget = BuildTargetGroup.Android;
                labelWidthMultiplier = 3;
            }
            else
            {
                settings = m_SettingsManager.iOSNotificationSettings;
                buildTarget = BuildTargetGroup.iOS;
                labelWidthMultiplier = 5;
            }

            if (settings == null)
                return 0;

            GUI.BeginGroup(rect);
            var drawnSettingsCount = DrawSettingElements(rect, buildTarget, settings, false, 0, labelWidthMultiplier);
            GUI.EndGroup();

            return drawnSettingsCount;
        }

        private int DrawSettingElements(Rect rect, BuildTargetGroup buildTarget, List<NotificationSetting> settings, bool disabled, int layer, int labelWidthMultiplier)
        {
            var spaceOffset = layer * 13;

            var labelStyle = new GUIStyle(NotificationStyles.k_LabelStyle);
            labelStyle.fixedWidth = k_SlotSize * labelWidthMultiplier - spaceOffset;

            var toggleStyle = NotificationStyles.k_ToggleStyle;
            var dropdownStyle = NotificationStyles.k_DropwDownStyle;

            int elementCount = settings.Count;
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
                }
                else if (setting.Value.GetType() == typeof(AuthorizationOption))
                {
                    setting.Value = (AuthorizationOption)EditorGUILayout.EnumFlagsField((iOSAuthorizationOption)setting.Value, dropdownStyle);
                    if ((iOSAuthorizationOption)setting.Value == 0)
                        setting.Value = (AuthorizationOption)(iOSAuthorizationOption.Badge | iOSAuthorizationOption.Sound | iOSAuthorizationOption.Alert);
                }
                else if (setting.Value.GetType() == typeof(AndroidExactSchedulingOption))
                {
                    setting.Value = (AndroidExactSchedulingOption)EditorGUILayout.EnumFlagsField((AndroidExactSchedulingOption)setting.Value, dropdownStyle);
                }
                else
                    Debug.LogError("Unsupported setting type: " + setting.Value.GetType());
                if (EditorGUI.EndChangeCheck())
                {
                    m_SettingsManager.SaveSetting(setting, buildTarget);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();

                if (setting.Dependencies != null)
                {
                    elementCount += DrawSettingElements(rect, buildTarget, setting.Dependencies, dependenciesDisabled, layer + 1, labelWidthMultiplier);
                }
            }

            return elementCount;
        }
    }
}

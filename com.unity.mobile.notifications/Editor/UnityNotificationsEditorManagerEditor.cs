using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;

using Unity.Notifications.iOS;
using Object = System.Object;


#pragma warning disable 219, 414


namespace Unity.Notifications
{
	[CustomEditor(typeof(UnityNotificationEditorManager))]
	class UnityNotificationsEditorManagerEditor : Editor
	{
		internal delegate void ChangedCallbackDelegate(ReorderableList list);
		internal ChangedCallbackDelegate onChangedCallback = null;
		
		SerializedProperty m_ResourceAssets;
		SerializedProperty m_iOSNotificationEditorSettings;
		private SerializedObject m_Target;
		private ReorderableList m_ReorderableList;
				
		protected const int kSlotSize = 64;
		protected const int kHeaderHeight = 80;

		protected const int kMaxPreviewSize = 64;
		protected const int kIconSpacing = 8;
		protected const float kPadding = 12f;
		protected const float kToolbarHeight = 20f;

		
		private GUIContent identifierLabelText = new GUIContent("Identifier");
		private GUIContent typeLabelText = new GUIContent("Type");

		private Vector2 m_ScrollViewStart;

		private UnityNotificationEditorManager manager;
		
		public string[] toolbarStrings = new string[] {"Android", "iOS"};
		
		private string infoStringAndroid =
			"Only icons added to this list or manually added to the `res/drawable` folder can be used by notifications.\n " +
			"Small icons can only be  composed simply of white pixels on a transparent backdrop and must be at least 48x48 pixels. \n" +
			"Large icons can contain any colors but must be not smaller than 192x192 pixels.";
		
		
#if UNITY_2018_3_OR_NEWER
		[SettingsProvider]
		static SettingsProvider CreateMobileNotificationsSettingsProvider()
		{
			var provider = AssetSettingsProvider.CreateProviderFromObject("Project/Mobile Notification Settings",
				UnityNotificationEditorManager.Initialize());
			provider.label = "Mobile Notification Settings";
			return provider;
		}
#endif
		
		void OnEnable()
		{
			manager = UnityNotificationEditorManager.Initialize();
			manager.CustomEditor = this;
			
			if (target == null)
				return;


			m_Target = new SerializedObject(target);
			
			m_ResourceAssets = serializedObject.FindProperty("TrackedResourceAssets");

			
			serializedObject.FindProperty("iOSRequestAuthorizationOnAppLaunch");

			serializedObject.FindProperty("iOSRequestAuthorizationForRemoteNotificationsOnAppLaunch");
			serializedObject.FindProperty("iOSRemoteNotificationForegroundPresentationOptions");

			
			m_ReorderableList = new ReorderableList(serializedObject, m_ResourceAssets, false, true, true, true);
			m_ReorderableList.elementHeight = kSlotSize + kIconSpacing;

			m_ReorderableList.showDefaultBackground = false;

			m_ReorderableList.drawHeaderCallback = (rect) =>
			{
				if (Event.current.type == EventType.Repaint)
				{

					var boxBackground = new GUIStyle("RL Background");
					var headerBackground = new GUIStyle("RL Header");

					var paddedRect = GetContentRect(rect,
						1f,
						(ReorderableList.Defaults.padding + 2f) * -1);
					
					headerBackground.Draw(
						paddedRect,
						false, false, false, false);

					var labelRect = GetContentRect(paddedRect, 0f, 3f);
					
					GUI.Label(labelRect, "Notification icons", EditorStyles.label);
				}
			};
			m_ReorderableList.onAddCallback = (list) =>
				AddIconDataElement(list);

			m_ReorderableList.onRemoveCallback = (list) =>
				RemoveIcondDataElement(list);

			m_ReorderableList.onCanAddCallback = (list) =>
				CanAddCallbackDelegate(list);

			m_ReorderableList.drawElementBackgroundCallback = (rect, index, active, focused) =>
			{	
				if (! (Event.current.type == EventType.Repaint) )
					return;

				var evenRow = new GUIStyle("CN EntryBackEven");
				var oddRow = new GUIStyle("CN EntryBackOdd");

				var bg = index % 2 == 0 ? evenRow : oddRow;
				bg.Draw(rect, false, false, false, false);
				ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, true);
			};


			m_ReorderableList.drawElementCallback = (rect, index, selected, focused) =>
				DrawIconDataElement(rect, index, selected, focused);

			m_ReorderableList.elementHeightCallback = index =>
			{
				var data = GetElementData(index);
				if (data == null)
					return kSlotSize;
				if (data.showOtherSizes)
					return m_ReorderableList.elementHeight * 6;				
				return m_ReorderableList.elementHeight +
					(data.Asset != null && !data.IsValid ? kSlotSize : 0);
			};

			Undo.undoRedoPerformed += UpdateEditorStateOnUndo;
		}

		void UpdateEditorStateOnUndo()
		{
			serializedObject.UpdateIfRequiredOrScript();
			Repaint();
		}

		internal float GetMinimumEditorWidth(float requiredWidth)
		{
			
			var minWidth = kSlotSize * 6;
			if (requiredWidth < minWidth)
				return minWidth;
			
			return requiredWidth;
		}

		DrawableResourceData GetElementData(int index)
		{
			var res = UnityNotificationEditorManager.Initialize().TrackedResourceAssets;
			if (index < res.Count )
				return res[index];
			return null;
		}

		static void DrawIconTextureSlot(Rect elementRect, int row, DrawableResourceData data, ImageSize size, NotificationIconType newType, string newId, UnityEngine.Object target)
		{
			Rect elementContentRect = GetContentRect(elementRect, 6f, 12f);
			float errorMsgWidth = elementRect.width - kSlotSize * 3;
			                      
			var textureRect  = new Rect(elementContentRect.width - (kMaxPreviewSize * 2 - kIconSpacing * 5),
						elementContentRect.y + kIconSpacing * row + kSlotSize * row, kMaxPreviewSize, kSlotSize);

			var errorBoxRect = GetContentRect(
				new Rect(elementContentRect.x,
					elementContentRect.y + kIconSpacing * row + kSlotSize * row, errorMsgWidth, kSlotSize),
				4f, 4f);
			
			Rect previewTextureRect = new Rect(elementContentRect.width - (kMaxPreviewSize - kIconSpacing * 5),
				elementContentRect.y + kIconSpacing * row + kSlotSize * row , kMaxPreviewSize, kSlotSize);

			Texture2D assetTexture = null;
			
			if (size == ImageSize.XXHDPI)
				assetTexture = data.AssetXXHDPI;
			
			if (size == ImageSize.XHDPI)
				assetTexture = data.AssetXHDPI;
			
			if (size == ImageSize.HDPI)
				assetTexture = data.AssetHDPI;
			
			if (size == ImageSize.MDPI)
				assetTexture = data.AssetMDPI;
			
			if (size == ImageSize.LDPI)
				assetTexture = data.AssetLDPI;

			
			var newAsset = (Texture2D) EditorGUI.ObjectField(
				textureRect,
				assetTexture,
				typeof(Texture2D),
				false);
			
			// ---

			bool updatePreviewTexture = (newId != data.Id || newType != data.Type || newAsset != data.Asset);
			if (updatePreviewTexture)
			{
				Undo.RegisterCompleteObjectUndo(target, "Update icon data.");
				data.Id = newId;
				data.Type = newType;
				data.Asset = newAsset;

				if (size == ImageSize.XXHDPI)
					data.AssetXXHDPI = newAsset;
			
				if (size == ImageSize.XHDPI)
					data.AssetXHDPI = newAsset;
			
				if (size == ImageSize.HDPI)
					data.AssetHDPI = newAsset;
			
				if (size == ImageSize.MDPI)
					data.AssetMDPI = newAsset;
			
				if (size == ImageSize.LDPI)
					data.AssetLDPI = newAsset;
				
				data.previewTexture = data.GetPreviewTexture(updatePreviewTexture);

				data.Clean();
				data.Verify();
			}
			
			// TODO Allocates a lot of memory for some reason, cache.

			
			if (data.Asset != null && !data.Verify())
			{
				EditorGUI.HelpBox(
					errorBoxRect,
					"Specified texture can't be used because: \n" + (errorMsgWidth > 145
						? DrawableResourceData.GenerateErrorString(data.Errors)
						: "...expand to see more..."),
					MessageType.Error
				);

				if (data.Type == NotificationIconType.SmallIcon)
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
				if (data.previewTexture != null)
				{

					GUIStyle previewLabelTextStyle = new GUIStyle(GUI.skin.label);
					previewLabelTextStyle.fontSize = 8;
					previewLabelTextStyle.wordWrap = true;
					previewLabelTextStyle.alignment = TextAnchor.UpperCenter;

					EditorGUI.LabelField(previewTextureRect, "Preview", previewLabelTextStyle);

					Rect previewTextureRectPadded = GetContentRect(previewTextureRect, 6f, 6f);
					previewTextureRectPadded.y += 8;

					data.previewTexture.alphaIsTransparency = false;
					GUI.DrawTexture(previewTextureRectPadded, data.previewTexture);
				}
			}
		}
		
		void DrawIconDataElement(Rect rect, int index, bool selected, bool focused)
		{
			var drawableResourceDataRef = m_ResourceAssets.GetArrayElementAtIndex(index);
			var elementRect = rect;
			
            int slotHeight = kSlotSize;

			if (drawableResourceDataRef != null)
			{
				var idProperty = drawableResourceDataRef.FindPropertyRelative("Id");
				var typeProperty = drawableResourceDataRef.FindPropertyRelative("Type");
				var assetProperty = drawableResourceDataRef.FindPropertyRelative("Asset");
				
				float width = Mathf.Min(elementRect.width,
					EditorGUIUtility.labelWidth + 4 + kSlotSize + kIconSpacing + kMaxPreviewSize);

				float idPropWidth = Mathf.Min(kMaxPreviewSize, width - kSlotSize - kIconSpacing);
				float typePropWidth = Mathf.Min(kMaxPreviewSize, width - kSlotSize - kIconSpacing);
				float assetPropWidth = kMaxPreviewSize;
				float errorMsgWidth = elementRect.width - kPadding * 2;//(elementRect.width - (assetPropWidth * 2 + kIconSpacing *2 )) - (elementRect.x + kSlotSize + typePropWidth);

				Rect elementContentRect = GetContentRect(elementRect, 6f, 12f);

				Rect previewTextureRect = new Rect(elementContentRect.width - (assetPropWidth - kIconSpacing * 5),
						elementContentRect.y - 6, assetPropWidth, slotHeight);
				
							
				var data = GetElementData(index);

				var showOtherSizes = EditorGUI.Foldout(
					new Rect(elementContentRect.x - 20, elementContentRect.y, idPropWidth, 20),
					data.showOtherSizes, " ");

				
//				Rect textureRect = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y, assetPropWidth, slotHeight);
//				
//				Rect textureRectXXHDPI = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y + kIconSpacing * 1 + slotHeight * 1, assetPropWidth, slotHeight);
//				
//				Rect textureRectXHDPI = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y + kIconSpacing * 2 + slotHeight * 2, assetPropWidth, slotHeight);
//				
//				Rect textureRectHDPI = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y + kIconSpacing * 3 + slotHeight * 3, assetPropWidth, slotHeight);
//				
//				Rect textureRectMDPI = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y + kIconSpacing * 4 + slotHeight * 4, assetPropWidth, slotHeight);
//				
//				Rect textureRectLDPI = 
//					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
//						elementContentRect.y + kIconSpacing * 5 + slotHeight * 5, assetPropWidth, slotHeight);

				Rect errorBoxRect;
				
				if (!showOtherSizes)
				{
					errorBoxRect = GetContentRect(
						new Rect(elementContentRect.x,
							elementContentRect.y + kSlotSize, errorMsgWidth, slotHeight),
						4f, 4f);
				}
				else
				{
					errorBoxRect = GetContentRect(
						new Rect(elementContentRect.x,
							elementContentRect.y + kSlotSize, errorMsgWidth - (assetPropWidth * 2 + kIconSpacing), slotHeight + assetPropWidth),
						4f, 4f);
				}

				EditorGUI.LabelField(
					new Rect(elementContentRect.x, elementContentRect.y, idPropWidth, 20),
					"Identifier"
				);
				
				EditorGUI.LabelField(
					new Rect(elementContentRect.x, elementContentRect.y + 25, idPropWidth, 20),
					"Type"
				);
								
				EditorGUI.BeginChangeCheck();
							
				var newId =  EditorGUI.TextField(
					new Rect(elementContentRect.x + kSlotSize, elementContentRect.y, idPropWidth, 20),
					data.Id);
				
				var newType = (NotificationIconType) EditorGUI.EnumPopup(
					new Rect(elementContentRect.x + kSlotSize, elementContentRect.y + 25, typePropWidth, 20),
					(NotificationIconType) (int)data.Type
				);



				var newAssetXXHDPI = data.AssetXXHDPI;
				var newAssetXHDPI = data.AssetXHDPI;
				var newAssetHDPI = data.AssetHDPI;
				var newAssetMDPI = data.AssetMDPI;
				var newAssetLDPI = data.AssetLDPI;
					
				DrawIconTextureSlot(elementRect, 0, data, ImageSize.XXHDPI, newType, newId, target);
				if (showOtherSizes)
				{
					DrawIconTextureSlot(elementRect, 1, data, ImageSize.LDPI, newType, newId, target);
					DrawIconTextureSlot(elementRect, 2, data, ImageSize.MDPI, newType, newId, target);
					DrawIconTextureSlot(elementRect, 3, data, ImageSize.HDPI, newType, newId, target);
					DrawIconTextureSlot(elementRect, 4, data, ImageSize.XHDPI, newType, newId, target);
					DrawIconTextureSlot(elementRect, 5, data, ImageSize.XXHDPI, newType, newId, target);
				}




//				if (showOtherSizes)
//				{
//					newAssetXXHDPI = (Texture2D) EditorGUI.ObjectField(
//						textureRectXXHDPI,
//						data.AssetXXHDPI,
//						typeof(Texture2D),
//						false);
//
//					newAssetXHDPI = (Texture2D) EditorGUI.ObjectField(
//						textureRectXHDPI,
//						data.AssetXHDPI,
//						typeof(Texture2D),
//						false);
//
//					newAssetHDPI = (Texture2D) EditorGUI.ObjectField(
//						textureRectHDPI,
//						data.AssetHDPI,
//						typeof(Texture2D),
//						false);
//
//					newAssetMDPI = (Texture2D) EditorGUI.ObjectField(
//						textureRectMDPI,
//						data.AssetMDPI,
//						typeof(Texture2D),
//						false);
//
//					newAssetLDPI = (Texture2D) EditorGUI.ObjectField(
//						textureRectLDPI,
//						data.AssetLDPI,
//						typeof(Texture2D),
//						false);
//				}

				data.showOtherSizes = showOtherSizes;

								
			}
		}
		
		void OnChange(ReorderableList list)
		{
			if (onChangedCallback != null)
				onChangedCallback(list);
			
			serializedObject.Update();
		}


		void AddIconDataElement( ReorderableList list)
		{
			Undo.RegisterCompleteObjectUndo(target, "Add a new icon element.");
			var manager = UnityNotificationEditorManager.Initialize();
			manager.RegisterDrawableResource(string.Format("icon_{0}", manager.TrackedResourceAssets.Count), null, NotificationIconType.SmallIcon);
			
			OnChange(list);
		}

		void RemoveIcondDataElement(ReorderableList list)
		{
			var i = UnityNotificationEditorManager.Initialize();
			i.RemoveDrawableResource(list.index);
			
			OnChange(list);
		}

		bool CanAddCallbackDelegate(ReorderableList list)
		{
			var trackedAssets = UnityNotificationEditorManager.Initialize().TrackedResourceAssets;
			if (trackedAssets.Count <= 0)
				return true;
			
			return !trackedAssets.Any(i =>  i.Initialized() == false);
		}

		public override void OnInspectorGUI()
		{
			if (m_Target == null)
				return;

			var width = GetMinimumEditorWidth(EditorGUIUtility.currentViewWidth - 300f);
			var rect = new Rect(10f, 0f, width, 400f);
			OnInspectorGUI(rect);
		}
		
		public void OnInspectorGUI(Rect rect, bool drawInInspector = false)
		{
			if (m_Target == null)
				return;
			
#if UNITY_2018_3
			rect = new Rect(rect.x, rect.y + 10f, rect.width, rect.height);
#endif
			
			serializedObject.UpdateIfRequiredOrScript();

			bool userHeader = false;//manager.toolbarInt == 0;
			var headerRect = GetContentRect(
				new Rect(kPadding, rect.y, rect.width - kPadding, kPadding * 2),
				0f,
				0f
			);

			if (userHeader)
			{
				headerRect = GetContentRect(
					new Rect(kPadding, rect.y + kToolbarHeight + kPadding, rect.width - kPadding, kHeaderHeight),
					kPadding,
					kPadding
				);
			}

			var bodyRect = GetContentRect(
				new Rect(kPadding, headerRect.yMax, rect.width - kPadding, rect.height - headerRect.height),
				kPadding,
				kPadding
			);
			
			var viewRect = GetContentRect(
				new Rect(rect.x, rect.y, rect.width,
					headerRect.height + m_ReorderableList.GetHeight() + kSlotSize),
				kPadding * -1,
				kPadding
			);

			if (drawInInspector)
				m_ScrollViewStart = GUI.BeginScrollView(rect, m_ScrollViewStart, viewRect, false, false);

			var toolBaRect = new Rect(rect.x, rect.y, rect.width, kToolbarHeight);
			manager.toolbarInt = GUI.Toolbar(toolBaRect,manager.toolbarInt, toolbarStrings);

			var headerMsgStyle = GUI.skin.GetStyle("HelpBox");
			headerMsgStyle.alignment = TextAnchor.UpperCenter;
			headerMsgStyle.fontSize = 10;
			headerMsgStyle.wordWrap = true;

			
			if (manager.toolbarInt == 0)
			{
				
				var settingsPanelRect = bodyRect;
				var settings = manager.AndroidNotificationEditorSettings;
				if (settings == null)
					return;
				
				var settingsFlat = settings.Where(s => s is NotificationEditorSetting).Cast<NotificationEditorSetting>().ToList();
				
				var styleToggle = new GUIStyle(GUI.skin.GetStyle("Toggle"));
				styleToggle.alignment = TextAnchor.MiddleRight;
			
				var styleDropwDown = new GUIStyle(GUI.skin.GetStyle("Button"));
				styleDropwDown.fixedWidth = kSlotSize * 2.5f;
				
				GUI.BeginGroup(settingsPanelRect);
				DrawSettingsElementList(BuildTargetGroup.Android, settings, false, styleToggle, styleDropwDown, settingsPanelRect);
				GUI.EndGroup();
				
				
				var iconListRectHeader = new Rect(bodyRect.x, bodyRect.y + 55f, bodyRect.width, 55f);
				DrawHeader(iconListRectHeader, infoStringAndroid, headerMsgStyle);
				
				var iconListRectBody = new Rect(iconListRectHeader.x, iconListRectHeader.y + 65f, iconListRectHeader.width, iconListRectHeader.height-55f);

				m_ReorderableList.DoList(iconListRectBody);
				if (!drawInInspector)
					EditorGUILayout.GetControlRect(true, iconListRectHeader.height + m_ReorderableList.GetHeight() + kSlotSize);
			}
			else
			{
				var settingsPanelRect = bodyRect;//GetContentRect(rect, kPadding, kPadding);

				var settings = manager.iOSNotificationEditorSettings;
				if (settings == null)
					return;

				var settingsFlat = settings.Where(s => s is NotificationEditorSetting).Cast<NotificationEditorSetting>().ToList();

				var styleToggle = new GUIStyle(GUI.skin.GetStyle("Toggle"));
				styleToggle.alignment = TextAnchor.MiddleRight;
			
				var styleDropwDown = new GUIStyle(GUI.skin.GetStyle("Button"));
				styleDropwDown.fixedWidth = kSlotSize * 2.5f;
				
				GUI.BeginGroup(settingsPanelRect);
				DrawSettingsElementList(BuildTargetGroup.iOS, settings, false, styleToggle, styleDropwDown, settingsPanelRect);
				GUI.EndGroup();
			}
			if (drawInInspector)
				GUI.EndScrollView();

		}

		private void DrawSettingsElementList(BuildTargetGroup target, List<NotificationEditorSetting> settings, bool disabled, GUIStyle  styleToggle, GUIStyle  styleDropwDown, Rect rect, int layer = 0)
		{
			int totalHeight = 0;
			foreach (var setting in settings)
			{
				EditorGUI.BeginDisabledGroup(disabled);
				Rect r = EditorGUILayout.BeginHorizontal();
				GUILayout.Space(layer * 13);
				
				var styleLabel = new GUIStyle(GUI.skin.GetStyle("Label"));
				
				var width = rect.width - kSlotSize * 3 - layer * 13;

				styleLabel.fixedWidth = width;
				styleLabel.wordWrap = true;

				
				GUILayout.Label(new GUIContent(setting.label, setting.tooltip), styleLabel);

				if (setting.val.GetType() == typeof(bool))
				{
					setting.val = (object)EditorGUILayout.Toggle((bool)setting.val, styleToggle);
				}
				else if (setting.val.GetType() == typeof(PresentationOption))
				{
					setting.val =
						(PresentationOption) EditorGUILayout.EnumFlagsField((PresentationOptionEditor) setting.val, styleDropwDown);
					if ((int)(PresentationOptionEditor)setting.val == 0)
						setting.val = (PresentationOption)PresentationOptionEditor.All;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				bool dependentDisabled = false;
				if (setting.val is bool)
					dependentDisabled = !(bool) setting.val;
				
				if (setting.dependentSettings != null)
				{
					layer++;
					DrawSettingsElementList(target, setting.dependentSettings, dependentDisabled, styleToggle, styleDropwDown, rect, layer);
				}
				manager.SaveSetting(setting, target);
			}
		}

		
		private void DrawHeader(Rect headerRect, string infoStr, GUIStyle style)
		{						
			EditorGUI.TextArea(headerRect, infoStr, style);
        }
	
		public static Rect GetContentRect(Rect rect, float paddingVertical = 0, float paddingHorizontal = 0)
		{
			Rect r = rect;

			r.yMin += paddingVertical;
			r.yMax -= paddingVertical;
			r.xMin += paddingHorizontal;
			r.xMax -= paddingHorizontal;
			return r;
		}
	}
}

#pragma warning restore 219, 414

using System;
using System.Linq;
using System.Reflection;
using Boo.Lang;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;

namespace Unity.Notifications.Android
{
	[CustomEditor(typeof(UnityNotificationEditorManager))]
	public class UnityNotificationsEditorManagerEditor : Editor
	{
		public delegate void ChangedCallbackDelegate(ReorderableList list);
		public ChangedCallbackDelegate onChangedCallback = null;
		
		SerializedProperty m_ResourceAssets;
		SerializedProperty m_iOSNotificationEditorSettings;
		private SerializedObject m_Target;
		private ReorderableList m_ReorderableList;
				
		protected const int kSlotSize = 64;
		protected const float kHeaderHeight = 80;
		protected const int kMaxElementHeight = 116;
		protected const int kMaxPreviewSize = 64;
		protected const int kIconSpacing = 8;
		protected const float kPadding = 12f;
		
		private GUIContent identifierLabelText = new GUIContent("Identifier");
		private GUIContent typeLabelText = new GUIContent("Type");

		private Vector2 m_ScrollViewStart;
		
		
		public int toolbarInt = 0;
		public string[] toolbarStrings = new string[] {"Android", "iOS"};
		
		void OnEnable()
		{
			UnityNotificationEditorManager.Initialize().CustomEditor = this;
			
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

		internal float GetMinimumEditorWidth()
		{
			return kMaxPreviewSize * 4 + kIconSpacing * 6 + kPadding * 3;
		}

		DrawableResourceData GetElementData(int index)
		{
			var res = UnityNotificationEditorManager.Initialize().TrackedResourceAssets;
			if (index < res.Count )
				return res[index];
			return null;
		}
		
		void DrawIconDataElement(Rect rect, int index, bool selected, bool focused)
		{
			var drawableResourceDataRef = m_ResourceAssets.GetArrayElementAtIndex(index);
			var elementRect = rect;
			
            int slotHeight = kSlotSize;
            int previewWidth = kSlotSize;
            int previewHeight = kSlotSize;


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
				
				Rect textureRect = 
					new Rect(elementContentRect.width - (assetPropWidth * 2 - kIconSpacing * 5),
						elementContentRect.y, assetPropWidth, slotHeight);

				
				Rect errorBoxRect =  GetContentRect(
					new Rect(elementContentRect.x,
						elementContentRect.y + kSlotSize, errorMsgWidth, slotHeight), 
					4f, 4f);
				
				EditorGUI.LabelField(
					new Rect(elementContentRect.x, elementContentRect.y, idPropWidth, 20),
					"Identifier"
				);
				
				EditorGUI.LabelField(
					new Rect(elementContentRect.x, elementContentRect.y + 25, idPropWidth, 20),
					"Type"
				);
								
				EditorGUI.BeginChangeCheck();
							
				var data = GetElementData(index);

				var newId =  EditorGUI.TextField(
					new Rect(elementContentRect.x + kSlotSize, elementContentRect.y, idPropWidth, 20),
					data.Id);
				
				var newType = (NotificationIconType) EditorGUI.EnumPopup(
					new Rect(elementContentRect.x + kSlotSize, elementContentRect.y + 25, typePropWidth, 20),
					(NotificationIconType) (int)data.Type
				);

				var newAsset = (Texture2D) EditorGUI.ObjectField(
					textureRect,
					data.Asset,
					typeof(Texture2D),
					false);

				bool updatePreviewTexture = (newId != data.Id || newType != data.Type || newAsset != data.Asset);
				
				if (updatePreviewTexture)
				{
					Undo.RegisterCompleteObjectUndo(target, "Update icon data.");
					data.Id = newId;
					data.Type = newType;
					data.Asset = newAsset;
					data.Clean();
					data.Verify();
				}

				Texture2D previewTexture = data.GetPreviewTexture(updatePreviewTexture);

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
				
				if (data.Asset != null && !data.IsValid)
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
			Debug.Log("Remove!");
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
			
			m_ReorderableList.DoLayoutList();
		}
		
		public void OnInspectorGUI(Rect rect)
		{
			if (m_Target == null)
				return;
			
			serializedObject.UpdateIfRequiredOrScript();
			toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

			if (toolbarInt == 0)
			{

				var headerRect = GetContentRect(
					new Rect(kPadding, 10, rect.width - kPadding, kHeaderHeight),
					kPadding,
					kPadding
				);

				var bodyRect = GetContentRect(
					new Rect(kPadding, headerRect.bottom, rect.width - kPadding, rect.height - headerRect.height),
					kPadding,
					kPadding
				);

				var viewRect = GetContentRect(
					new Rect(rect.x, rect.y, rect.width,
						headerRect.height + m_ReorderableList.GetHeight() + kPadding * 2),
					kPadding * -1,
					kPadding
				);

				m_ScrollViewStart = GUI.BeginScrollView(rect, m_ScrollViewStart, viewRect, false, false);

				DrawHeader(headerRect);

				m_ReorderableList.DoList(bodyRect);

				GUI.EndScrollView();
			}
			else
			{
				
				var settings = UnityNotificationEditorManager.Initialize().iOSNotificationEditorSettings;
				if (settings == null)
					return;

				var longestLabel = settings.Find( s =>  s.label.Length == settings.Max(v => v.label.Length));
//				
//
//
				var styleLabel = new GUIStyle(GUI.skin.GetStyle("Label"));
				var minSize = styleLabel.CalcSize(new GUIContent(longestLabel.label));

				styleLabel.fixedWidth = minSize.x;
//				
//
				var styleToggle = new GUIStyle(GUI.skin.GetStyle("Toggle"));
//				styleToggle.stretchWidth = true;
////				styleToggle.wordWrap = true;
//				styleToggle.alignment = TextAnchor.MiddleCenter;

				
				var styleDropwDown = new GUIStyle(GUI.skin.GetStyle("Button"));
				styleDropwDown.fixedWidth = 90f;
//				styleDropwDown.stretchWidth = true;
//				styleDropwDown.alignment = TextAnchor.MiddleCenter;

				
				for (var i = 0; i < settings.Count; i++)
				{
					var setting = settings[i];
					
					
					Rect r = EditorGUILayout.BeginHorizontal();
					GUILayout.Label(new GUIContent( setting.label, setting.tooltip), styleLabel);

					if (setting.val.GetType() == typeof(bool))
					{
						setting.val = (object)EditorGUILayout.Toggle((bool)setting.val, styleToggle);
					}
					else if (setting.val.GetType() == typeof(PresentationOption))
					{
						setting.val =
							(object) EditorGUILayout.EnumFlagsField((PresentationOption) setting.val, styleDropwDown);
					}
					EditorGUILayout.EndHorizontal();
				}		
			}
		}


		
		private void DrawHeader(Rect headerRect)
		{
			var infoStr =
				"Only icons added to this list or manually added to the `res/drawable` folder can be used by notifications.\n " +
				"Small icons can only be  composed simply of white pixels on a transparent backdrop and must be at least 48x48 pixels. \n" +
				"Large icons can contain any colors but must be not smaller than 192x192 pixels.";
			
			var headerMsgStyle = GUI.skin.GetStyle("HelpBox");
			headerMsgStyle.alignment = TextAnchor.UpperCenter;
			headerMsgStyle.fontSize = 10;
			
			EditorGUI.TextArea(headerRect, infoStr, headerMsgStyle);
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
#if !UNITY_2018_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Notifications;
using UnityEditor;
using UnityEngine;
using Unity.Notifications.AndroidSettings;


public class UnityNotificationsEditorWindow : EditorWindow
{

	private static UnityNotificationsEditorManagerEditor _editor; 
	
	[MenuItem ("Edit/Project Settings/Mobile Notification Settings")]
	public static void  ShowWindow () {
		var type = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
		GetWindow<UnityNotificationsEditorWindow>("Mobile Notifications", true, type);
	}

	void OnGUI () {		
		

		if (_editor == null)
		{
				_editor =
					Editor.CreateEditor(UnityNotificationEditorManager.Initialize()) as
						UnityNotificationsEditorManagerEditor;
				if (_editor == null)
					return;
		}

		this.minSize = new Vector2(_editor.GetMinimumEditorWidth(150f), 0f);

		var rect = new Rect(0f, 0f, this.position.width, this.position.height);
		_editor.OnInspectorGUI(rect, true);
		
	}
}
#endif
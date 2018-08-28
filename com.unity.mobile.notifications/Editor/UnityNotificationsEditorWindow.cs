using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Notifications.Android;

public class UnityNotificationsEditorWindow : EditorWindow
{

	private static UnityNotificationsEditorManagerEditor _editor; 
	
	[MenuItem ("Edit/Project Settings/Mobile Notification Settings")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(UnityNotificationsEditorWindow));
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

		this.minSize = new Vector2(_editor.GetMinimumEditorWidth(), 0f);

		var rect = new Rect(0f, 0f, this.position.width, this.position.height);
		_editor.OnInspectorGUI(rect);
		
	}
}


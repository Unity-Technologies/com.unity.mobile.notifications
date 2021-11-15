using System.Collections;
using System.Collections.Generic;
using Unity.Notifications;
using UnityEditor;
using UnityEngine;
using Unity.Notifications.Android;

public class CheckDrawables : Editor
{
    public static Texture2D t2d;
    // Start is called before the first frame update
    void Start()
    {
        t2d = Resources.Load("Drawable1") as Texture2D;
    }


    [MenuItem("Drawables/Add")]
    private static void DrawablesAdd()
    {
        t2d = Resources.Load("Drawable1") as Texture2D;
        Debug.Log("Adding drawable " + t2d);
        NotificationSettings.AndroidSettings.AddDrawableResource("testIndex", t2d, NotificationIconType.Small);
        //nSettings.
    }

    [MenuItem("Drawables/Remove all")]
    private static void DrawablesRemove()
    {
        Debug.Log("Removing all drawables");
        NotificationSettings.AndroidSettings.ClearDrawableResources();
        //nSettings.
    }

    [MenuItem("Drawables/Remove at last index: 2")]
    private static void DrawablesRemoveAtIndex()
    {
        Debug.Log("Removing drawable at index 2");
        NotificationSettings.AndroidSettings.RemoveDrawableResource(2);
        //nSettings.
    }

    [MenuItem("Drawables/Remove with ID: testIndex")]
    private static void DrawablesRemoveAtID()
    {
        Debug.Log("Removing drwawable with ID: " + "testIndex");
        NotificationSettings.AndroidSettings.RemoveDrawableResource("testIndex");
        //nSettings.
    }
}

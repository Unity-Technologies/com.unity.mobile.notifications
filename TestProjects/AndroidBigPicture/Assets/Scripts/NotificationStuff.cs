using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Notifications.Android;

public class NotificationStuff : MonoBehaviour
{
    const string channel = "default";
    string picturePath;
    string sharedImages;
    float notificationDelay = 0;


    private void Start()
    {
        AndroidNotificationCenter.Initialize();
        var chan = new AndroidNotificationChannel(channel, "Default", "Default", Importance.Default);
        AndroidNotificationCenter.RegisterNotificationChannel(chan);
        PrepareImages();
        if (AndroidNotificationCenter.UserPermissionToPost != PermissionStatus.Allowed)
            new PermissionRequest();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(50, 50, 300, 200), "Content title + summary text"))
            ContentTitleAndSummaryText();
        if (GUI.Button(new Rect(50, 300, 300, 200), "Content title + content description"))
            ContentTitleAndContentDescription();
        if (GUI.Button(new Rect(50, 550, 300, 200), "Large icon + show when collapsed"))
            LargeIconAndShowCollapsed();
        if (GUI.Button(new Rect(50, 800, 300, 200), "From pictures"))
            FromPictures();
        if (GUI.Button(new Rect(50, 1050, 300, 200), "Picture from resources and regular text"))
            FromResourcesAndRegularText();
        if (GUI.Button(new Rect(400, 50, 300, 200), "Small icon URI"))
            SmallIconUri();
        if (GUI.Button(new Rect(400, 300, 300, 200), "Large icon URI"))
            LargeIconUri();

        GUI.Label(new Rect(50, Screen.height - 50, 100, 50), "Delay (0-50s):");
        notificationDelay = GUI.HorizontalSlider(new Rect(150, Screen.height - 50, Screen.width - 200, 50), notificationDelay, 0, 50);
    }

    DateTime GetNotificationDelay()
    {
        int delay = (int)notificationDelay;
        var dt = DateTime.Now;
        if (delay > 0)
            dt = dt.AddSeconds(delay);
        return dt;
    }

    BigPictureStyle MakeBigPicture()
    {
        return new BigPictureStyle()
        {
            Picture = picturePath,
            ContentTitle = "Big Picture",
        };
    }

    void ContentTitleAndSummaryText()
    {
        var bigPicture = MakeBigPicture();
        bigPicture.SummaryText = "Summary txt";
        SendNotification(bigPicture);
    }

    void ContentTitleAndContentDescription()
    {
        var bigPicture = MakeBigPicture();
        bigPicture.ContentTitle = "Content title";
        SendNotification(bigPicture);
    }

    void LargeIconAndShowCollapsed()
    {
        var bigPicture = MakeBigPicture();
        bigPicture.LargeIcon = "override";
        bigPicture.ShowWhenCollapsed = true;
        SendNotification(bigPicture);
    }

    void FromPictures()
    {
        StartCoroutine(NotificationFromPictures());
    }

    void FromResourcesAndRegularText()
    {
        var notification = new AndroidNotification("title", "text", GetNotificationDelay());
        notification.BigPicture = new BigPictureStyle()
        {
            Picture = "override",
        };
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    void SmallIconUri()
    {
        var uri = GetSmallIconUri();
        var notification = new AndroidNotification("Small icon", uri, GetNotificationDelay());
        notification.SmallIcon = uri;
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    void LargeIconUri()
    {
        var uri = GetSmallIconUri();
        var notification = new AndroidNotification("Large icon", uri, GetNotificationDelay());
        notification.LargeIcon = uri;
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    string GetSmallIconUri()
    {
        AndroidJavaObject activity;
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = player.GetStatic<AndroidJavaObject>("currentActivity");
        }

        string uri;
        using (var klass = new AndroidJavaClass("com.unity.fileaccess.ObtainPicture"))
        {
            uri = klass.CallStatic<string>("getUri", activity, Path.Combine(sharedImages, "small_icon.png"));
        }
        activity.Dispose();
        return uri;
    }

    IEnumerator NotificationFromPictures()
    {
        string sharedImages = Path.Combine(Application.persistentDataPath, "sharedImages");
        if (!Directory.Exists(sharedImages))
            Directory.CreateDirectory(sharedImages);
        string destFile = Path.Combine(sharedImages, "show");
        AndroidJavaObject activity;
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = player.GetStatic<AndroidJavaObject>("currentActivity");
        }
        using (var obtainPicture = new AndroidJavaObject("com.unity.fileaccess.ObtainPicture", activity, destFile))
        {
            while (!obtainPicture.Call<bool>("isFinished"))
                yield return null;
            var uri = obtainPicture.Call<string>("getUri");
            var bigPicture = new BigPictureStyle()
            {
                Picture = uri,
                ContentTitle = "Selected Picture",
            };
            SendNotification(bigPicture);
        }
        activity.Dispose();
    }

    void SendNotification(BigPictureStyle bigPicture)
    {
        var notification = new AndroidNotification("hello", "world", GetNotificationDelay());
        notification.LargeIcon = "default_icon";
        notification.BigPicture = bigPicture;
        AndroidNotificationCenter.SendNotification(notification, channel);
    }

    void PrepareImages()
    {
        string filename = "image.png";
        picturePath = Path.Combine(Application.persistentDataPath, filename);
        sharedImages = Path.Combine(Application.persistentDataPath, "sharedImages");
        if (!Directory.Exists(sharedImages))
            Directory.CreateDirectory(sharedImages);

        StartCoroutine(PrepareImage(Application.persistentDataPath, filename));
        StartCoroutine(PrepareImage(sharedImages, "small_icon.png"));
    }

    IEnumerator PrepareImage(string destDir, string filename)
    {
        string path = Path.Combine(destDir, filename);
        if (File.Exists(path))
            yield break;
        using var uwr = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, filename));
        yield return uwr.SendWebRequest();
        File.WriteAllBytes(path, uwr.downloadHandler.data);
    }
}

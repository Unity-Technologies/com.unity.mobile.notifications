using System;
using NUnit.Framework;
using UnityEngine;
using Unity.Notifications.Android;

class AndroidNotificationSimpleTests
{
    const string kChannelId = "SerializeDeserializeNotificationIntentChannel";

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        var c = new AndroidNotificationChannel();
        c.Id = kChannelId;
        c.Name = "SerializeDeserializeNotificationIntent channel";
        c.Description = "SerializeDeserializeNotificationIntent channel";
        c.Importance = Importance.High;
        AndroidNotificationCenter.RegisterNotificationChannel(c);
    }

    [Test]
    public void CreateNotificationChannel_NotificationChannelIsCreated()
    {
#if !UNITY_EDITOR
        var testChannelId = "default_test_channel_10";
        AndroidNotificationCenter.DeleteNotificationChannel(testChannelId);
        Assert.AreNotEqual("default_test_channel_10", AndroidNotificationCenter.GetNotificationChannel(testChannelId).Id);

        var newChannel = new AndroidNotificationChannel();
        newChannel.Id = testChannelId;
        newChannel.Name = "Default Channel";
        newChannel.Description = "Generic spam";

        var currentChannelCount = AndroidNotificationCenter.GetNotificationChannels().Length;
        AndroidNotificationCenter.RegisterNotificationChannel(newChannel);
        currentChannelCount++;

        Assert.AreEqual(currentChannelCount, AndroidNotificationCenter.GetNotificationChannels().Length);
#endif
    }

    [Test]
    public void DeleteNotificationChannel_NotificationChannelIsDeleted()
    {
#if !UNITY_EDITOR
        var ch = new AndroidNotificationChannel();
        ch.Id = "default_test_channel_0";
        ch.Name = "Default Channel";
        ch.Description = "Generic spam";
        ch.Importance = Importance.Default;
        AndroidNotificationCenter.RegisterNotificationChannel(ch);

        int numChannels = AndroidNotificationCenter.GetNotificationChannels().Length;
        AndroidNotificationCenter.DeleteNotificationChannel(ch.Id);

        Assert.AreEqual(numChannels - 1, AndroidNotificationCenter.GetNotificationChannels().Length);
#endif
    }

    [Test]
    public void SetNotificationFireTime_TimeIsConvertedToUnixTimeAndBack()
    {
        var n = new AndroidNotification();
        var fireTime = new DateTime(2018, 5, 24, 12, 41, 30, 122);
        n.FireTime = new DateTime(2018, 5, 24, 12, 41, 30, 122);

        Assert.AreEqual(fireTime, n.FireTime);
    }

    [Test]
    public void SetNotificationRepeatInterval_TimeIsConvertedToUnixTimeAndBack()
    {
        var n = new AndroidNotification();
        var repeatInterval = TimeSpan.FromSeconds(666);
        n.RepeatInterval = repeatInterval;

        Assert.AreEqual(repeatInterval, n.RepeatInterval);
    }

    [Test]
    public void SerializeDeserializeNotificationIntent_AllParameters()
    {
        const int notificationId = 123;

        var original = new AndroidNotification();
        original.Title = "title";
        original.Text = "text";
        original.SmallIcon = "small_icon";
        original.FireTime = DateTime.Now;
        original.RepeatInterval = new TimeSpan(0, 0, 5);
        original.LargeIcon = "large_icon";
        original.Style = NotificationStyle.BigTextStyle;
        original.Color = new Color(0.2f, 0.4f, 0.6f, 1.0f);
        original.Number = 15;
        original.ShouldAutoCancel = true;
        original.UsesStopwatch = true;
        original.Group = "group";
        original.GroupSummary = true;
        original.GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertChildren;
        original.SortKey = "sorting";
        original.IntentData = "string for intent";
        original.ShowTimestamp = true;
        original.CustomTimestamp = new DateTime(2018, 5, 24, 12, 41, 30, 122);

        using var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, channelId);
        using var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
        using var intent = new AndroidJavaObject("android.content.Intent", context, managerClass);
        using var javaNotif = builder.Call<AndroidJavaObject>("build");
        intent.Call<AndroidJavaObject>("putExtra", "unityNotification", javaNotif).Dispose();
        using var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");

        var serializedString = utilsClass.CallStatic<string>("serializeNotificationIntent", intent);
        Assert.IsNotEmpty(serializedString);

        using var deserializedIntent = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotificationIntent", context, serializedString);
        using var deserializedNotification = deserializedIntent.Call<AndroidJavaObject>("getParcelableExtra", "unityNotification");
        var deserializedData = AndroidNotificationCenter.GetNotificationData(deserializedNotification);
        var deserialized = deserializedData.Notification;

        Assert.AreEqual(notificationId, deserializedData.Id);
        Assert.AreEqual(channelId, deserializedData.Channel);
        Assert.AreEqual(original.Title, deserialized.Title);
        Assert.AreEqual(original.Text, deserialized.Text);
        Assert.AreEqual(original.SmallIcon, deserialized.SmallIcon);
        Assert.AreEqual(original.FireTime.ToString(), deserialized.FireTime.ToString());
        Assert.AreEqual(original.RepeatInterval, deserialized.RepeatInterval);
        Assert.AreEqual(original.LargeIcon, deserialized.LargeIcon);
        Assert.AreEqual(original.Style, deserialized.Style);
        Assert.AreEqual(original.Color, deserialized.Color);
        Assert.AreEqual(original.Number, deserialized.Number);
        Assert.AreEqual(original.ShouldAutoCancel, deserialized.ShouldAutoCancel);
        Assert.AreEqual(original.UsesStopwatch, deserialized.UsesStopwatch);
        Assert.AreEqual(original.Group, deserialized.Group);
        Assert.AreEqual(original.GroupSummary, deserialized.GroupSummary);
        Assert.AreEqual(original.GroupAlertBehaviour, deserialized.GroupAlertBehaviour);
        Assert.AreEqual(original.SortKey, deserialized.SortKey);
        Assert.AreEqual(original.IntentData, deserialized.IntentData);
        Assert.AreEqual(original.ShowTimestamp, deserialized.ShowTimestamp);
        Assert.AreEqual(original.CustomTimestamp, deserialized.CustomTimestamp);
    }
}

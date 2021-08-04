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
    public void BasicSerializeDeserializeNotification_AllParameters()
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

        var deserializedData = SerializeDeserializeNotification(original, notificationId);

        Assert.AreEqual(notificationId, deserializedData.Id);
        Assert.AreEqual(kChannelId, deserializedData.Channel);
        CheckNotificationsMatch(original, deserializedData.Notification);
    }

    AndroidNotificationIntentData SerializeDeserializeNotification(AndroidNotification original, int notificationId)
    {
        using var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        return SerializeDeserializeNotification(builder);
    }

    AndroidNotificationIntentData SerializeDeserializeNotification(AndroidJavaObject builder)
    {
        using var javaNotif = builder.Call<AndroidJavaObject>("build");
        using var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        AndroidJavaObject serializedBytes;  // use java object, since we don't need the bytes, so don't waste time on marshalling
        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayOutputStream"))
        {
            using var dataStream = new AndroidJavaObject("java.io.DataOutputStream", byteStream);
            var didSerialize = utilsClass.CallStatic<bool>("serializeNotificationCustom", javaNotif, dataStream);
            Assert.IsTrue(didSerialize);
            dataStream.Call("close");
            serializedBytes = byteStream.Call<AndroidJavaObject>("toByteArray");
        }
        Assert.IsNotNull(serializedBytes);

        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayInputStream", serializedBytes))
        {
            using var dataStream = new AndroidJavaObject("java.io.DataInputStream", byteStream);
            // don't dispose notification, it is kept in AndroidNotificationIntentData
            var deserializedNotification = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotificationCustom", dataStream);
            Assert.IsNotNull(deserializedNotification);
            return AndroidNotificationCenter.GetNotificationData(deserializedNotification);
        }
    }

    void CheckNotificationsMatch(AndroidNotification original, AndroidNotification other)
    {
        Assert.AreEqual(original.Title, other.Title);
        Assert.AreEqual(original.Text, other.Text);
        Assert.AreEqual(original.SmallIcon, other.SmallIcon);
        Assert.AreEqual(original.FireTime.ToString(), other.FireTime.ToString());
        Assert.AreEqual(original.RepeatInterval, other.RepeatInterval);
        Assert.AreEqual(original.LargeIcon, other.LargeIcon);
        Assert.AreEqual(original.Style, other.Style);
        Assert.AreEqual(original.Color, other.Color);
        Assert.AreEqual(original.Number, other.Number);
        Assert.AreEqual(original.ShouldAutoCancel, other.ShouldAutoCancel);
        Assert.AreEqual(original.UsesStopwatch, other.UsesStopwatch);
        Assert.AreEqual(original.Group, other.Group);
        Assert.AreEqual(original.GroupSummary, other.GroupSummary);
        Assert.AreEqual(original.GroupAlertBehaviour, other.GroupAlertBehaviour);
        Assert.AreEqual(original.SortKey, other.SortKey);
        Assert.AreEqual(original.IntentData, other.IntentData);
        Assert.AreEqual(original.ShowTimestamp, other.ShowTimestamp);
        Assert.AreEqual(original.CustomTimestamp, other.CustomTimestamp);
    }

    [Test]
    public void BasicSerializeDeserializeNotification_MinimumParameters()
    {
        const int notificationId = 124;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now;

        var deserializedData = SerializeDeserializeNotification(original, notificationId);

        Assert.AreEqual(notificationId, deserializedData.Id);
        Assert.AreEqual(kChannelId, deserializedData.Channel);
        CheckNotificationsMatch(original, deserializedData.Notification);
    }

    [Test]
    public void BasicSerializeDeserializeNotification_CanPutSimpleExtras()
    {
        const int notificationId = 125;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now;

        using var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        using var extras = builder.Call<AndroidJavaObject>("getExtras");
        extras.Call("putInt", "testInt", 5);
        extras.Call("putBoolean", "testBool", true);
        extras.Call("putString", "testString", "the_test");

        var deserializedData = SerializeDeserializeNotification(builder);

        using var deserializedExtras = deserializedData.NativeNotification.Get<AndroidJavaObject>("extras");
        Assert.IsNotNull(deserializedExtras);
        Assert.AreEqual(5, deserializedExtras.Call<int>("getInt", "testInt"));
        Assert.AreEqual(true, deserializedExtras.Call<bool>("getBoolean", "testBool"));
        Assert.AreEqual("the_test", deserializedExtras.Call<string>("getString", "testString"));
    }
}
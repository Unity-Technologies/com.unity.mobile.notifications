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
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            return SerializeDeserializeNotification(builder);
        }
    }

    AndroidNotificationIntentData SerializeDeserializeNotification(AndroidJavaObject builder)
    {
        var javaNotif = builder.Call<AndroidJavaObject>("build");
        var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        AndroidJavaObject serializedBytes;  // use java object, since we don't need the bytes, so don't waste time on marshalling
        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayOutputStream"))
        {
            var dataStream = new AndroidJavaObject("java.io.DataOutputStream", byteStream);
            var didSerialize = utilsClass.CallStatic<bool>("serializeNotificationCustom", javaNotif, dataStream);
            Assert.IsTrue(didSerialize);
            dataStream.Call("close");
            serializedBytes = byteStream.Call<AndroidJavaObject>("toByteArray");
        }
        Assert.IsNotNull(serializedBytes);

        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayInputStream", serializedBytes))
        {
            var dataStream = new AndroidJavaObject("java.io.DataInputStream", byteStream);
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

        var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        var extras = builder.Call<AndroidJavaObject>("getExtras");
        extras.Call("putInt", "testInt", 5);
        extras.Call("putBoolean", "testBool", true);
        extras.Call("putString", "testString", "the_test");

        var deserializedData = SerializeDeserializeNotification(builder);

        var deserializedExtras = deserializedData.NativeNotification.Get<AndroidJavaObject>("extras");
        Assert.IsNotNull(deserializedExtras);
        Assert.AreEqual(5, deserializedExtras.Call<int>("getInt", "testInt"));
        Assert.AreEqual(true, deserializedExtras.Call<bool>("getBoolean", "testBool"));
        Assert.AreEqual("the_test", deserializedExtras.Call<string>("getString", "testString"));
    }

    AndroidJavaObject CreateBitmap()
    {
        var configClass = new AndroidJavaClass("android.graphics.Bitmap$Config");
        var ARGB_8888 = configClass.GetStatic<AndroidJavaObject>("ARGB_8888");
        var bitmapClass = new AndroidJavaClass("android.graphics.Bitmap");
        return bitmapClass.CallStatic<AndroidJavaObject>("createBitmap", 10000, 10000, ARGB_8888);
    }

    [Test]
    public void BasicSerializeDeserializeNotification_WorksWithBinderExtras()
    {
        const int notificationId = 126;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now.AddSeconds(2);

        var bitmap = CreateBitmap();
        Assert.IsNotNull(bitmap);

        var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        var extras = builder.Call<AndroidJavaObject>("getExtras");

        extras.Call("putParcelable", "binder_item", bitmap);

        var deserializedData = SerializeDeserializeNotification(builder);

        var deserializedExtras = deserializedData.NativeNotification.Get<AndroidJavaObject>("extras");
        var bitmapAfterSerialization = deserializedExtras.Call<AndroidJavaObject>("getParcelable", "binder_item");

        // both these are in extras, so we should have lost bitmap, but preserved fire time
        // bitmap is binder object and can't be parcelled, while our fallback custom serialization only preserves our stuff
        Assert.IsNull(bitmapAfterSerialization);
        Assert.AreEqual(original.FireTime.ToString(), deserializedData.Notification.FireTime.ToString());
    }

    AndroidNotificationIntentData SerializeDeserializeNotificationIntent(AndroidNotification original, int notificationId)
    {
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            return SerializeDeserializeNotificationIntent(builder);
        }
    }

    AndroidNotificationIntentData SerializeDeserializeNotificationIntent(AndroidJavaObject builder)
    {
        var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var context = activity.Call<AndroidJavaObject>("getApplicationContext");
        var intent = new AndroidJavaObject("android.content.Intent", context, managerClass);
        var javaNotif = builder.Call<AndroidJavaObject>("build");
        intent.Call<AndroidJavaObject>("putExtra", "unityNotification", javaNotif).Dispose();
        var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");

        var serializedString = utilsClass.CallStatic<AndroidJavaObject>("serializeNotificationIntent", intent);
        Assert.IsNotNull(serializedString);

        var deserializedIntent = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotificationIntent", context, serializedString);
        Assert.IsNotNull(deserializedIntent);
        // don't dispose notification, it is kept in AndroidNotificationIntentData
        var deserializedNotification = deserializedIntent.Call<AndroidJavaObject>("getParcelableExtra", "unityNotification");
        Assert.IsNotNull(deserializedNotification);
        return AndroidNotificationCenter.GetNotificationData(deserializedNotification);
    }

    [Test]
    public void NotificationIntentSerialization_SimpleNotification()
    {
        const int notificationId = 1234;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now.AddSeconds(2);

        var deserializedData = SerializeDeserializeNotificationIntent(original, notificationId);
        Assert.AreEqual(original.FireTime.ToString(), deserializedData.Notification.FireTime.ToString());
    }

    [Test]
    public void NotificationIntentSerialization_NotificationWithBinderObject()
    {
        const int notificationId = 1234;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now.AddSeconds(2);

        var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        var extras = builder.Call<AndroidJavaObject>("getExtras");
        var bitmap = CreateBitmap();
        Assert.IsNotNull(bitmap);
        extras.Call("putParcelable", "binder_item", bitmap);

        var deserializedData = SerializeDeserializeNotificationIntent(builder);

        Assert.AreEqual(original.FireTime.ToString(), deserializedData.Notification.FireTime.ToString());
        var deserializedExtras = deserializedData.NativeNotification.Get<AndroidJavaObject>("extras");
        var bitmapAfterSerialization = deserializedExtras.Call<AndroidJavaObject>("getParcelable", "binder_item");
        // bitmap is binder object and can't be parcelled, while our fallback custom serialization only preserves our stuff
        Assert.IsNull(bitmapAfterSerialization);
    }

    [Test]
    public void OldTypeSerializedNotificationCanBedeserialized()
    {
        const int notificationId = 12345;

        var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var context = activity.Call<AndroidJavaObject>("getApplicationContext");
        var notificationIntent = new AndroidJavaObject("android.content.Intent", context, managerClass);

        var fireTime = DateTime.Now.AddSeconds(3);
        var repeatInterval = new TimeSpan(0, 0, 5);
        Color? color = new Color(0.2f, 0.4f, 0.6f, 1.0f);

        notificationIntent.Call<AndroidJavaObject>("putExtra", "id", notificationId);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "channelID", kChannelId);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "textTitle", "notification.Title");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "textContent", "notification.Text");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "smallIconStr", "notification.SmallIcon");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "autoCancel", true);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "usesChronometer", true);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "fireTime", fireTime.ToLong());
        notificationIntent.Call<AndroidJavaObject>("putExtra", "repeatInterval", (long)repeatInterval.TotalMilliseconds);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "largeIconStr", "notification.LargeIcon");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "style", (int)NotificationStyle.BigTextStyle);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "color", color.ToInt());
        notificationIntent.Call<AndroidJavaObject>("putExtra", "number", 25);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "data", "notification.IntentData");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "group", "notification.Group");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "groupSummary", true);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "sortKey", "notification.SortKey");
        notificationIntent.Call<AndroidJavaObject>("putExtra", "groupAlertBehaviour", (int)GroupAlertBehaviours.GroupAlertChildren);
        notificationIntent.Call<AndroidJavaObject>("putExtra", "showTimestamp", true);

        var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        var bundle = notificationIntent.Call<AndroidJavaObject>("getExtras");
        var parcelClass = new AndroidJavaClass("android.os.Parcel");
        var parcel = parcelClass.CallStatic<AndroidJavaObject>("obtain");
        bundle.Call("writeToParcel", parcel, 0);
        var serialized = parcel.Call<AndroidJavaObject>("marshall");
        Assert.IsNotNull(serialized);

        var deserialized = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotificationIntent", context, serialized);
        Assert.IsNotNull(deserialized);
        var deserializedNotification = deserialized.Call<AndroidJavaObject>("getParcelableExtra", "unityNotification");
        Assert.IsNotNull(deserializedNotification);
        var notificationData = AndroidNotificationCenter.GetNotificationData(deserializedNotification);
        Assert.IsNotNull(notificationData);
        Assert.AreEqual(notificationId, notificationData.Id);
        Assert.AreEqual(kChannelId, notificationData.Channel);
        Assert.AreEqual("notification.Title", notificationData.Notification.Title);
        Assert.AreEqual("notification.Text", notificationData.Notification.Text);
        Assert.AreEqual("notification.SmallIcon", notificationData.Notification.SmallIcon);
        Assert.AreEqual(true, notificationData.Notification.ShouldAutoCancel);
        Assert.AreEqual(true, notificationData.Notification.UsesStopwatch);
        Assert.AreEqual(fireTime.ToString(), notificationData.Notification.FireTime.ToString());
        Assert.AreEqual(repeatInterval.TotalMilliseconds, notificationData.Notification.RepeatInterval.Value.TotalMilliseconds);
        Assert.AreEqual("notification.LargeIcon", notificationData.Notification.LargeIcon);
        Assert.AreEqual(NotificationStyle.BigTextStyle, notificationData.Notification.Style);
        Assert.AreEqual(color.ToInt(), notificationData.Notification.Color.ToInt());
        Assert.AreEqual(25, notificationData.Notification.Number);
        Assert.AreEqual("notification.IntentData", notificationData.Notification.IntentData);
        Assert.AreEqual("notification.Group", notificationData.Notification.Group);
        Assert.AreEqual(true, notificationData.Notification.GroupSummary);
        Assert.AreEqual("notification.SortKey", notificationData.Notification.SortKey);
        Assert.AreEqual(GroupAlertBehaviours.GroupAlertChildren, notificationData.Notification.GroupAlertBehaviour);
        Assert.AreEqual(true, notificationData.Notification.ShowTimestamp);
    }
}

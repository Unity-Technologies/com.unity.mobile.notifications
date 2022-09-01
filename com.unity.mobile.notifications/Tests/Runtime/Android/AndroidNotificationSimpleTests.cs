using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Notifications.Android;
#if UNITY_EDITOR
using UnityEditor;
#endif

class AndroidNotificationSimpleTests
    : IPrebuildSetup, IPostBuildCleanup
{
    const string kChannelId = "SerializeDeserializeNotificationChannel";

#if UNITY_EDITOR
    PluginImporter GetTestUtils()
    {
        var assets = AssetDatabase.FindAssets("UnityNotificationTestUtils");
        if (assets.Length != 1)
            throw new Exception("UnityNotificationTestUtils.java not found");
        return (PluginImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]));
    }
#endif

    public void Setup()
    {
#if UNITY_EDITOR
        var testUtils = GetTestUtils();
        testUtils.SetCompatibleWithPlatform(BuildTarget.Android, true);
        testUtils.SaveAndReimport();
#endif
    }

    public void Cleanup()
    {
#if UNITY_EDITOR
        var testUtils = GetTestUtils();
        testUtils.SetCompatibleWithPlatform(BuildTarget.Android, false);
        testUtils.SaveAndReimport();
#endif
    }

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        var c = new AndroidNotificationChannel();
        c.Id = kChannelId;
        c.Name = "SerializeDeserializeNotification channel";
        c.Description = "SerializeDeserializeNotification channel";
        c.Importance = Importance.High;
        AndroidNotificationCenter.RegisterNotificationChannel(c);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void CreateNotificationChannel_NotificationChannelIsCreated()
    {
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
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void GetNotificationChannel_ReturnsTheChannel()
    {
        var channel = AndroidNotificationCenter.GetNotificationChannel(kChannelId);
        Assert.IsNotNull(channel);
        Assert.AreEqual("SerializeDeserializeNotification channel", channel.Name);
        Assert.AreEqual("SerializeDeserializeNotification channel", channel.Description);
        Assert.AreEqual(Importance.High, channel.Importance);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void GetNotificationChannel_NonExistentChannel_ReturnsNull()
    {
        var channel = AndroidNotificationCenter.GetNotificationChannel("DoesNotExist");
        Assert.IsNotNull(channel);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void GetNotificationChannels_NoChannels_ReturnsEmptyArray()
    {
        var channels = AndroidNotificationCenter.GetNotificationChannels();
        foreach (var channel in channels)
            AndroidNotificationCenter.DeleteNotificationChannel(channel.Id);

        try
        {
            var chans = AndroidNotificationCenter.GetNotificationChannels();
            Assert.IsNotNull(chans);
            Assert.AreEqual(0, chans.Length);
        }
        finally
        {
            // recreate test channels to not break other tests
            foreach (var channel in channels)
                AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void DeleteNotificationChannel_NotificationChannelIsDeleted()
    {
        var ch = new AndroidNotificationChannel();
        ch.Id = "default_test_channel_0";
        ch.Name = "Default Channel";
        ch.Description = "Generic spam";
        ch.Importance = Importance.Default;
        AndroidNotificationCenter.RegisterNotificationChannel(ch);

        int numChannels = AndroidNotificationCenter.GetNotificationChannels().Length;
        AndroidNotificationCenter.DeleteNotificationChannel(ch.Id);

        Assert.AreEqual(numChannels - 1, AndroidNotificationCenter.GetNotificationChannels().Length);
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

    AndroidNotification CreateNotificationWithAllParameters()
    {
        var notification = new AndroidNotification();
        notification.Title = "title";
        notification.Text = "text";
        notification.SmallIcon = "small_icon";
        notification.FireTime = DateTime.Now;
        notification.RepeatInterval = new TimeSpan(0, 0, 5);
        notification.LargeIcon = "large_icon";
        notification.Style = NotificationStyle.BigTextStyle;
        notification.Color = new Color(0.2f, 0.4f, 0.6f, 1.0f);
        notification.Number = 15;
        notification.ShouldAutoCancel = true;
        notification.UsesStopwatch = true;
        notification.Group = "group";
        notification.GroupSummary = true;
        notification.GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertChildren;
        notification.SortKey = "sorting";
        notification.IntentData = "string for intent";
        notification.ShowTimestamp = true;
        notification.ShowInForeground = false;  // this one defaults to true
        notification.CustomTimestamp = new DateTime(2018, 5, 24, 12, 41, 30, 122);

        return notification;
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void BasicSerializeDeserializeNotification_AllParameters()
    {
        const int notificationId = 123;

        var original = CreateNotificationWithAllParameters();

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
        return SerializeDeserializeNotificationWithFunc(builder, (u, s, j) => SerializeNotificationCustom(u, s, j));
    }

    AndroidNotificationIntentData SerializeDeserializeNotificationWithFunc(AndroidJavaObject builder, Func<AndroidJavaClass, AndroidJavaObject, AndroidJavaObject, bool> serialize)
    {
        var javaNotif = builder.Call<AndroidJavaObject>("build");
        var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        AndroidJavaObject serializedBytes;  // use java object, since we don't need the bytes, so don't waste time on marshalling
        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayOutputStream"))
        {
            var dataStream = new AndroidJavaObject("java.io.DataOutputStream", byteStream);
            var didSerialize = serialize(utilsClass, dataStream, javaNotif);
            Assert.IsTrue(didSerialize);
            dataStream.Call("close");
            serializedBytes = byteStream.Call<AndroidJavaObject>("toByteArray");
        }
        Assert.IsNotNull(serializedBytes);

        using (var byteStream = new AndroidJavaObject("java.io.ByteArrayInputStream", serializedBytes))
        {
            var dataStream = new AndroidJavaObject("java.io.DataInputStream", byteStream);
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            // don't dispose notification, it is kept in AndroidNotificationIntentData
            using (var deserializedNotificationBuilder = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotificationCustom", activity, dataStream))
            {
                Assert.IsNotNull(deserializedNotificationBuilder);
                var deserializedNotification = deserializedNotificationBuilder.Call<AndroidJavaObject>("build");
                Assert.IsNotNull(deserializedNotification);
                return AndroidNotificationCenter.GetNotificationData(deserializedNotification);
            }
        }
    }

    static bool SerializeNotificationCustom(AndroidJavaClass utilsClass, AndroidJavaObject byteStream, AndroidJavaObject javaNotif)
    {
        using (var dataStream = new AndroidJavaObject("java.io.DataOutputStream", byteStream))
        {
            var didSerialize = utilsClass.CallStatic<bool>("serializeNotificationCustom", javaNotif, dataStream);
            dataStream.Call("close");
            return didSerialize;
        }
    }

    static bool SerializeNotificationCustom_v0(AndroidJavaClass utilsClass, AndroidJavaObject byteStream, AndroidJavaObject javaNotif)
    {
        return SerializeNotificationCustom_old("serializeNotificationCustom_v0", byteStream, javaNotif);
    }

    static bool SerializeNotificationCustom_v1(AndroidJavaClass utilsClass, AndroidJavaObject byteStream, AndroidJavaObject javaNotif)
    {
        return SerializeNotificationCustom_old("serializeNotificationCustom_v1", byteStream, javaNotif);
    }

    static bool SerializeNotificationCustom_old(string method, AndroidJavaObject byteStream, AndroidJavaObject javaNotif)
    {
        using (var dataStream = new AndroidJavaObject("java.io.DataOutputStream", byteStream))
        {
            bool didSerialize;
            using (var testUtils = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationTestUtils"))
            {
                didSerialize = testUtils.CallStatic<bool>(method, javaNotif, dataStream);
            }
            dataStream.Call("close");
            return didSerialize;
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
        Assert.AreEqual(original.ShowInForeground, other.ShowInForeground);
        Assert.AreEqual(original.CustomTimestamp, other.CustomTimestamp);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
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

    AndroidJavaObject CreateBitmap()
    {
        var configClass = new AndroidJavaClass("android.graphics.Bitmap$Config");
        var ARGB_8888 = configClass.GetStatic<AndroidJavaObject>("ARGB_8888");
        var bitmapClass = new AndroidJavaClass("android.graphics.Bitmap");
        return bitmapClass.CallStatic<AndroidJavaObject>("createBitmap", 1000, 1000, ARGB_8888);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
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

    AndroidNotificationIntentData SerializeDeserializeNotification(AndroidNotification original, int notificationId, Action<AndroidJavaObject> inBetween = null)
    {
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            return SerializeDeserializeNotification(builder, inBetween);
        }
    }

    AndroidNotificationIntentData SerializeDeserializeNotification(AndroidJavaObject builder, Action<AndroidJavaObject> inBetween = null)
    {
        var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var context = activity.Call<AndroidJavaObject>("getApplicationContext");
        var javaNotif = builder.Call<AndroidJavaObject>("build");
        var utilsClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");

        var prefs = context.Call<AndroidJavaObject>("getSharedPreferences", "android.notification.test.key", 0 /* MODE_PRIVATE */);
        utilsClass.CallStatic("serializeNotification", prefs, javaNotif);

        if (inBetween != null)
            inBetween(prefs);

        var deserializedNotificationBuilder = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotification", context, prefs);
        // don't dispose notification, it is kept in AndroidNotificationIntentData
        Assert.IsNotNull(deserializedNotificationBuilder);
        var deserializedNotification = deserializedNotificationBuilder.Call<AndroidJavaObject>("build");
        Assert.IsNotNull(deserializedNotification);
        return AndroidNotificationCenter.GetNotificationData(deserializedNotification);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void NotificationSerialization_SimpleNotification()
    {
        const int notificationId = 1234;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now.AddSeconds(2);

        var deserializedData = SerializeDeserializeNotification(original, notificationId);
        Assert.AreEqual(original.FireTime.ToString(), deserializedData.Notification.FireTime.ToString());
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void NotificationSerialization_NotificationWithBinderObject()
    {
        const int notificationId = 1234;

        var original = new AndroidNotification();
        original.FireTime = DateTime.Now.AddSeconds(2);
        original.ShowInForeground = false;  // non default value

        var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        var extras = builder.Call<AndroidJavaObject>("getExtras");
        var bitmap = CreateBitmap();
        Assert.IsNotNull(bitmap);
        extras.Call("putParcelable", "binder_item", bitmap);

        var deserializedData = SerializeDeserializeNotification(builder);

        Assert.AreEqual(original.FireTime.ToString(), deserializedData.Notification.FireTime.ToString());
        Assert.IsFalse(deserializedData.Notification.ShowInForeground);
        var deserializedExtras = deserializedData.NativeNotification.Get<AndroidJavaObject>("extras");
        var bitmapAfterSerialization = deserializedExtras.Call<AndroidJavaObject>("getParcelable", "binder_item");
        // bitmap is binder object and can't be parcelled, while our fallback custom serialization only preserves our stuff
        Assert.IsNull(bitmapAfterSerialization);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
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

        var deserializedNotificationBuilder = utilsClass.CallStatic<AndroidJavaObject>("deserializeNotification", context, serialized);
        Assert.IsNotNull(deserializedNotificationBuilder);
        var deserializedNotification = deserializedNotificationBuilder.Call<AndroidJavaObject>("build");
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

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void LegacyRecoverBuilderProducesTheSameNotification()
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

        var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId);
        var notification = builder.Call<AndroidJavaObject>("build");
        var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        var manager = managerClass.GetStatic<AndroidJavaObject>("mUnityNotificationManager");
        var context = manager.Get<AndroidJavaObject>("mContext");
        var utils = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        var recoveredBuilder = utils.CallStatic<AndroidJavaObject>("recoverBuilderCustom", context, notification);
        Assert.IsNotNull(recoveredBuilder);

        var notificationAfterRecover = recoveredBuilder.Call<AndroidJavaObject>("build");
        Assert.IsNotNull(notificationAfterRecover);

        var notificationData = AndroidNotificationCenter.GetNotificationData(notificationAfterRecover);
        Assert.IsNotNull(notificationData);
        var notification2 = notificationData.Notification;
        CheckNotificationsMatch(original, notification2);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void CorruptedPrimarySerialization_FallsBack()
    {
        const int notificationId = 234;

        var original = new AndroidNotification();
        original.Title = "title";
        original.Text = "text";
        original.SmallIcon = "small_icon";
        original.FireTime = DateTime.Now;
        original.LargeIcon = "large_icon";

        AndroidJavaObject context, prefs;
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            context = activity.Call<AndroidJavaObject>("getApplicationContext");
            var javaNotif = builder.Call<AndroidJavaObject>("build");
            var testUtils = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationTestUtils");

            prefs = context.Call<AndroidJavaObject>("getSharedPreferences", "android.notification.test.key", 0 /* MODE_PRIVATE */);
            testUtils.CallStatic("serializeNotification", prefs, javaNotif);
        }

        var data = prefs.Call<string>("getString", "data", "");
        // corrupt data
        using (var editor = prefs.Call<AndroidJavaObject>("edit"))
        {
            editor.Call<AndroidJavaObject>("putString", "data", "jfkasjflksdjflkasdjflkjdsafkjsadfl").Dispose();
            editor.Call("apply");
        }

        var data2 = prefs.Call<string>("getString", "data", "");
        Assert.AreNotEqual(data, data2);

        var utils = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationUtilities");
        var deserializedNotificationBuilder = utils.CallStatic<AndroidJavaObject>("deserializeNotification", context, prefs);
        // don't dispose notification, it is kept in AndroidNotificationIntentData
        Assert.IsNotNull(deserializedNotificationBuilder);
        var deserializedNotification = deserializedNotificationBuilder.Call<AndroidJavaObject>("build");
        Assert.IsNotNull(deserializedNotification);
        var deserializedData = AndroidNotificationCenter.GetNotificationData(deserializedNotification);

        Assert.AreEqual(original.Title, deserializedData.Notification.Title);
        Assert.AreEqual(original.Text, deserializedData.Notification.Text);
        Assert.AreEqual(original.SmallIcon, deserializedData.Notification.SmallIcon);
        Assert.AreEqual(original.LargeIcon, deserializedData.Notification.LargeIcon);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void CanDeserializeCustomSerializedNotification_v0()
    {
        const int notificationId = 245;

        var original = CreateNotificationWithAllParameters();

        AndroidNotificationIntentData deserialized;
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            // put something to extrax to force completely custom serialization of them
            var bitmap = CreateBitmap();
            Assert.IsNotNull(bitmap);
            var extras = builder.Call<AndroidJavaObject>("getExtras");
            extras.Call("putParcelable", "binder_item", bitmap);

            // Serialize like we did in version 0
            deserialized = SerializeDeserializeNotificationWithFunc(builder, (u, s, j) => SerializeNotificationCustom_v0(u, s, j));
        }

        Assert.IsNotNull(deserialized);
        original.ShowInForeground = true;  // v0 did not have this, so should default to true
        CheckNotificationsMatch(original, deserialized.Notification);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
    public void CanDeserializeCustomSerializedNotification_v1()
    {
        const int notificationId = 255;

        var original = CreateNotificationWithAllParameters();

        AndroidNotificationIntentData deserialized;
        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(notificationId, original, kChannelId))
        {
            // put something to extrax to force completely custom serialization of them
            var bitmap = CreateBitmap();
            Assert.IsNotNull(bitmap);
            var extras = builder.Call<AndroidJavaObject>("getExtras");
            extras.Call("putParcelable", "binder_item", bitmap);

            // Serialize like we did in version 0
            deserialized = SerializeDeserializeNotificationWithFunc(builder, (u, s, j) => SerializeNotificationCustom_v1(u, s, j));
        }

        Assert.IsNotNull(deserialized);
        CheckNotificationsMatch(original, deserialized.Notification);
    }
}

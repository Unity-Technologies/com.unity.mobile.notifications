using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Threading;
using Unity.Notifications.Android;

class AndroidNotificationTests
{
    const string kDefaultTestChannel = "default_test_channel";

    class NotificationReceivedHandler
    {
        public int receivedNotificationCount = 0;
        public AndroidNotificationIntentData lastNotification;

        public void OnReceiveNotification(AndroidNotificationIntentData data)
        {
            ++receivedNotificationCount;
            lastNotification = data;
        }
    }

    NotificationReceivedHandler currentHandler;

    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        var c = new AndroidNotificationChannel();
        c.Id = kDefaultTestChannel;
        c.Name = "Default Channel 5";
        c.Description = "test_channel 5";
        c.Importance = Importance.High;

        AndroidNotificationCenter.RegisterNotificationChannel(c);
    }

    [SetUp]
    public void BeforeEachTest()
    {
        AndroidNotificationCenter.CancelAllNotifications();
        currentHandler = new NotificationReceivedHandler();
        AndroidNotificationCenter.OnNotificationReceived += currentHandler.OnReceiveNotification;
    }

    [TearDown]
    public void AfterEachTest()
    {
        AndroidNotificationCenter.OnNotificationReceived -= currentHandler.OnReceiveNotification;
        currentHandler = null;
    }

    IEnumerator WaitForNotification(float timeout)
    {
        float passed = 0.0f;
        int notificationCound = currentHandler.receivedNotificationCount;
        while (notificationCound == currentHandler.receivedNotificationCount && passed < timeout)
        {
            yield return null;
            passed += Time.deltaTime;
        }
    }

    [Test]
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

    [UnityTest]
    public IEnumerator SendNotificationExplicitID_NotificationIsReceived()
    {
        int originalId = 456;

        var n = new AndroidNotification();
        n.Title = "SendNotificationExplicitID_NotificationIsReceived : " + originalId.ToString();
        n.Text = "SendNotificationExplicitID_NotificationIsReceived Text";
        n.FireTime = System.DateTime.Now;
        n.Group = "test.dummy.group";


        Debug.LogWarning("SendNotificationExplicitID_NotificationIsReceived sends notification with ID " + originalId);
        AndroidNotificationCenter.SendNotificationWithExplicitID(n, kDefaultTestChannel, originalId);

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotificationExplicitID_NotificationIsReceived completed. Received notifications: "  + currentHandler.receivedNotificationCount);

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        Assert.AreEqual(originalId, currentHandler.lastNotification.Id);
        Assert.AreEqual(n.Group, currentHandler.lastNotification.Notification.Group);
    }

    [UnityTest]
    public IEnumerator SendNotification_NotificationIsReceived()
    {
        var n = new AndroidNotification();
        n.Title = "SendNotification_NotificationIsReceived";
        n.Text = "SendNotification_NotificationIsReceived Text";
        n.FireTime = System.DateTime.Now;

        Debug.LogWarning("SendNotification_NotificationIsReceived sends notification");
        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotification_NotificationIsReceived completed. Received notifications: "  + currentHandler.receivedNotificationCount);

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        Assert.AreEqual(originalId, currentHandler.lastNotification.Id);
    }

    [UnityTest]
    public IEnumerator SendNotificationAndCancelNotification_NotificationIsNotReceived()
    {
        var n = new AndroidNotification();
        n.Title = "SendNotificationAndCancelNotification_NotificationIsNotReceived";
        n.Text = "SendNotificationAndCancelNotification_NotificationIsNotReceived Text";
        n.FireTime = System.DateTime.Now.AddSeconds(2.0f);

        Debug.LogWarning("SendNotificationAndCancelNotification_NotificationIsNotReceived sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return null;
        AndroidNotificationCenter.CancelScheduledNotification(originalId);

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotificationAndCancelNotification_NotificationIsNotReceived completed.");

        Assert.AreEqual(0, currentHandler.receivedNotificationCount);
    }

    [UnityTest]
    public IEnumerator ScheduleRepeatableNotification_NotificationsAreReceived()
    {
        var n = new AndroidNotification();
        n.Title = "Repeating Notification Title";
        n.Text = "Repeating Notification Text";
        n.FireTime = System.DateTime.Now.AddSeconds(2.0f);
        n.RepeatInterval = new System.TimeSpan(0, 0, 1);

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        //The notification should be repeated every second
        yield return WaitForNotification(8.0f);
        yield return WaitForNotification(8.0f);
        yield return WaitForNotification(8.0f);
        AndroidNotificationCenter.CancelScheduledNotification(originalId);

        Assert.GreaterOrEqual(3, currentHandler.receivedNotificationCount);
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

    [UnityTest]
    public IEnumerator NotificationIsScheduled_NotificationStatusIsCorrectlyReported()
    {
        var n = new AndroidNotification();
        n.Title = "NotificationStatusIsCorrectlyReported";
        n.Text = "NotificationStatusIsCorrectlyReported";
        n.FireTime = System.DateTime.Now.AddSeconds(2f);

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return null;

        var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Scheduled, status);

        yield return WaitForNotification(8.0f);

        status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Delivered, status);

        AndroidNotificationCenter.CancelNotification(originalId);
        yield return new WaitForSeconds(1.5f);

        status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Unknown, status);
    }

    [Test]
    public void CreateNotificationChannelWithInitializedSettings_ChannelSettingsAreSaved()
    {
        var chOrig = new AndroidNotificationChannel();
        chOrig.Id = "test_channel_settings_are_saved_0";
        chOrig.Name = "spam Channel";
        chOrig.Description = "Generic spam";
        chOrig.Importance = Importance.High;
        chOrig.CanBypassDnd = true;
        chOrig.CanShowBadge = true;
        chOrig.EnableLights = true;
        chOrig.EnableVibration = false;
        // this opne should be read-only, it reports the system setting, but can't be set
        // chOrig.LockScreenVisibility = LockScreenVisibility.Private;

        AndroidNotificationCenter.RegisterNotificationChannel(chOrig);

        var ch = AndroidNotificationCenter.GetNotificationChannel(chOrig.Id);

        Assert.AreEqual(chOrig.Id, ch.Id);
        Assert.AreEqual(chOrig.Name, ch.Name);
        Assert.AreEqual(chOrig.Description, ch.Description);
        Assert.AreEqual(chOrig.Importance, ch.Importance);
        Assert.AreEqual(chOrig.EnableLights, ch.EnableLights);
        Assert.AreEqual(chOrig.EnableVibration, ch.EnableVibration);
        //Assert.AreEqual(chOrig.LockScreenVisibility, ch.LockScreenVisibility);
    }

    [UnityTest]
    public IEnumerator SendNotification_NotificationIsReceived_CallMainThread()
    {
        var gameObjects = new GameObject[1];

        AndroidNotificationCenter.NotificationReceivedCallback receivedNotificationHandler =
            delegate(AndroidNotificationIntentData data)
        {
            gameObjects[0] = new GameObject();
            gameObjects[0].name = "Hello_World";
            Assert.AreEqual("Hello_World", gameObjects[0].name);
        };

        var n = new AndroidNotification();
        n.Title = "SendNotification_NotificationIsReceived";
        n.Text = "SendNotification_NotificationIsReceived Text";
        n.FireTime = System.DateTime.Now;

        Debug.LogWarning("SendNotification_NotificationIsReceived_CallMainThread sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        AndroidNotificationCenter.OnNotificationReceived += receivedNotificationHandler;

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotification_NotificationIsReceived_CallMainThread completed");

        AndroidNotificationCenter.OnNotificationReceived -= receivedNotificationHandler;
        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        Assert.AreEqual(originalId, currentHandler.lastNotification.Id);
        Assert.IsNotNull(gameObjects[0]);
    }

    [UnityTest]
    public IEnumerator SendNotification_CanAccessNativeBuilder()
    {
        var n = new AndroidNotification();
        n.Title = "SendNotification_CanAccessNativeBuilder";
        n.Text = "SendNotification_CanAccessNativeBuilder Text";
        n.FireTime = System.DateTime.Now;

        Debug.LogWarning("SendNotification_CanAccessNativeBuilder sends notification");

        using (var builder = AndroidNotificationCenter.CreateNotificationBuilder(n, kDefaultTestChannel))
        {
            using (var extras = builder.Call<AndroidJavaObject>("getExtras"))
            {
                extras.Call("putString", "notification.test.string", "TheTest");
            }

            AndroidNotificationCenter.SendNotification(builder);
        }

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotification_CanAccessNativeBuilder completed");

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        using (var extras = currentHandler.lastNotification.NativeNotification.Get<AndroidJavaObject>("extras"))
        {
            Assert.AreEqual("TheTest", extras.Call<string>("getString", "notification.test.string"));
        }
    }
}

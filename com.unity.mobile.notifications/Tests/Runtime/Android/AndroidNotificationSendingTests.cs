using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Notifications.Android;

class AndroidNotificationSendingTests
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
#if !UNITY_EDITOR
        var c = new AndroidNotificationChannel();
        c.Id = kDefaultTestChannel;
        c.Name = "Default Channel 5";
        c.Description = "test_channel 5";
        c.Importance = Importance.High;

        AndroidNotificationCenter.RegisterNotificationChannel(c);
#endif
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        AndroidNotificationCenter.CancelAllNotifications();
    }

    [SetUp]
    public void BeforeEachTest()
    {
#if !UNITY_EDITOR
        AndroidNotificationCenter.CancelAllNotifications();
        currentHandler = new NotificationReceivedHandler();
        AndroidNotificationCenter.OnNotificationReceived += currentHandler.OnReceiveNotification;
#endif
    }

    [TearDown]
    public void AfterEachTest()
    {
#if !UNITY_EDITOR
        AndroidNotificationCenter.OnNotificationReceived -= currentHandler.OnReceiveNotification;
        currentHandler = null;
#endif
    }

    IEnumerator WaitForNotification(float timeout)
    {
        float passed = 0.0f;
        int notificationCount = currentHandler.receivedNotificationCount;
        while (notificationCount == currentHandler.receivedNotificationCount && passed < timeout)
        {
            yield return null;
            passed += Time.deltaTime;
        }
        if (passed > timeout)
            Debug.LogWarning("Timeout waiting for notification");
    }

    [UnityTest]
    public IEnumerator SendNotificationExplicitID_NotificationIsReceived()
    {
#if !UNITY_EDITOR
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
#else
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator SendNotification_NotificationIsReceived()
    {
#if !UNITY_EDITOR
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
#else
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator SendNotificationAndCancelNotification_NotificationIsNotReceived()
    {
#if !UNITY_EDITOR
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
#else
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator ScheduleRepeatableNotification_NotificationsAreReceived()
    {
#if !UNITY_EDITOR
        var n = new AndroidNotification();
        n.Title = "Repeating Notification Title";
        n.Text = "Repeating Notification Text";
        n.FireTime = System.DateTime.Now;
        n.RepeatInterval = new System.TimeSpan(0, 0, 5); // interval needs to be quite big for test to be reliable

        Debug.LogWarning("ScheduleRepeatableNotification_NotificationsAreReceived sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        // we use inexact scheduling, so repeated notification may take a while to appear
        // inexact also can group, so for test purposes we only check that it repeats at least once
        yield return WaitForNotification(8.0f);
        yield return WaitForNotification(30.0f);
        AndroidNotificationCenter.CancelScheduledNotification(originalId);

        Debug.LogWarning("ScheduleRepeatableNotification_NotificationsAreReceived completed");

        Assert.GreaterOrEqual(currentHandler.receivedNotificationCount, 2);
#else
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator NotificationIsScheduled_NotificationStatusIsCorrectlyReported()
    {
#if !UNITY_EDITOR
        var n = new AndroidNotification();
        n.Title = "NotificationStatusIsCorrectlyReported";
        n.Text = "NotificationStatusIsCorrectlyReported";
        n.FireTime = System.DateTime.Now.AddSeconds(2f);

        Debug.LogWarning("NotificationIsScheduled_NotificationStatusIsCorrectlyReported sends notification");

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

        Debug.LogWarning("NotificationIsScheduled_NotificationStatusIsCorrectlyReported completed");
#else
        yield break;
#endif
    }

    [Test]
    public void CreateNotificationChannelWithInitializedSettings_ChannelSettingsAreSaved()
    {
#if !UNITY_EDITOR
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
#endif
    }

    [UnityTest]
    public IEnumerator SendNotification_NotificationIsReceived_CallMainThread()
    {
#if !UNITY_EDITOR
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
#else
        yield break;
#endif
    }

    [UnityTest]
    public IEnumerator SendNotification_CanAccessNativeBuilder()
    {
#if !UNITY_EDITOR
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
#else
        yield break;
#endif
    }
}

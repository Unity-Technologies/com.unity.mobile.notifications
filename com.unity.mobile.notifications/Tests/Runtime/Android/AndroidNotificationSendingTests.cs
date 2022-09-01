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
    [UnityPlatform(RuntimePlatform.Android)]
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

        Debug.LogWarning("SendNotificationExplicitID_NotificationIsReceived completed. Received notifications: " + currentHandler.receivedNotificationCount);

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        Assert.AreEqual(originalId, currentHandler.lastNotification.Id);
        Assert.AreEqual(n.Group, currentHandler.lastNotification.Notification.Group);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator SendNotification_NotificationIsReceived()
    {
        var n = new AndroidNotification();
        n.Title = "SendNotification_NotificationIsReceived";
        n.Text = "SendNotification_NotificationIsReceived Text";
        n.FireTime = System.DateTime.Now;

        Debug.LogWarning("SendNotification_NotificationIsReceived sends notification");
        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        yield return WaitForNotification(8.0f);

        Debug.LogWarning("SendNotification_NotificationIsReceived completed. Received notifications: " + currentHandler.receivedNotificationCount);

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        Assert.AreEqual(originalId, currentHandler.lastNotification.Id);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
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
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator ScheduleRepeatableNotification_NotificationsAreReceived()
    {
        var n = new AndroidNotification();
        n.Title = "Repeating Notification Title";
        n.Text = "Repeating Notification Text";
        n.FireTime = System.DateTime.Now.AddSeconds(2);
        n.RepeatInterval = new System.TimeSpan(0, 0, 5); // interval needs to be quite big for test to be reliable

        Debug.LogWarning("ScheduleRepeatableNotification_NotificationsAreReceived sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);

        // we use inexact scheduling, so repeated notification may take a while to appear
        // inexact also can group, so for test purposes we only check that it repeats at least once
        yield return WaitForNotification(120.0f);
        yield return WaitForNotification(120.0f);
        AndroidNotificationCenter.CancelScheduledNotification(originalId);

        Debug.LogWarning("ScheduleRepeatableNotification_NotificationsAreReceived completed");

        Assert.GreaterOrEqual(currentHandler.receivedNotificationCount, 2);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator NotificationIsScheduled_NotificationStatusIsCorrectlyReported()
    {
        var n = new AndroidNotification();
        n.Title = "NotificationStatusIsCorrectlyReported";
        n.Text = "NotificationStatusIsCorrectlyReported";
        n.FireTime = System.DateTime.Now.AddSeconds(2f);

        Debug.LogWarning("NotificationIsScheduled_NotificationStatusIsCorrectlyReported sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return null;

        var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Scheduled, status);

        yield return WaitForNotification(120.0f);
        yield return new WaitForSeconds(1.0f);  // give some time for Status Bar to update
        status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Delivered, status);

        AndroidNotificationCenter.CancelNotification(originalId);
        yield return new WaitForSeconds(1.5f);

        status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Unknown, status);

        Debug.LogWarning("NotificationIsScheduled_NotificationStatusIsCorrectlyReported completed");
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator ArrivedAndUserDismissedNotification_DoesNotReportStatusAsScheduled()
    {
        var n = new AndroidNotification("ArrivedNotificationAndDissmissed", "ArrivedNotificationAndDissmissed", System.DateTime.Now);
        yield return DismissedNotification_DoesNotReportStatusAsScheduled(n);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator ArrivedAndUserDismissedScheduledNotification_DoesNotReportStatusAsScheduled()
    {
        var n = new AndroidNotification("ArrivedNotificationAndDissmissedScheduled", "ArrivedNotificationAndDissmissedScheduled", System.DateTime.Now.AddSeconds(2));
        yield return DismissedNotification_DoesNotReportStatusAsScheduled(n);
    }

    public IEnumerator DismissedNotification_DoesNotReportStatusAsScheduled(AndroidNotification n)
    {
        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return WaitForNotification(8.0f);

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);

        AndroidNotificationCenter.CancelDisplayedNotification(originalId);
        yield return new WaitForSeconds(2.0f); // cancel is async
        var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Unknown, status);
    }

    [Test]
    [UnityPlatform(RuntimePlatform.Android)]
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
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator SendNotification_NotificationIsReceived_CallMainThread()
    {
        var gameObjects = new GameObject[1];

        AndroidNotificationCenter.NotificationReceivedCallback receivedNotificationHandler =
            delegate (AndroidNotificationIntentData data)
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
    [UnityPlatform(RuntimePlatform.Android)]
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

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator SendNotification_CanReschedule()
    {
        var managerClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationManager");
        var rebootClass = new AndroidJavaClass("com.unity.androidnotifications.UnityNotificationRestartOnBootReceiver");
        AndroidJavaObject context;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                context = activity.Call<AndroidJavaObject>("getApplicationContext");
        }

        var n = new AndroidNotification();
        n.Title = "SendNotification_CanReschedule";
        n.Text = "SendNotification_CanReschedule Text";
        n.FireTime = System.DateTime.Now.AddSeconds(5);

        Debug.LogWarning("SendNotification_CanReschedule sends notification");

        int id = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return new WaitForSeconds(0.2f);

        var manager = managerClass.GetStatic<AndroidJavaObject>("mUnityNotificationManager");
        // clear cached notifications to not mess up future tests
        manager.Get<AndroidJavaObject>("mScheduledNotifications").Call("clear");
        // simulate reboot by directly cancelling scheduled alarms preserving saves
        manager.Call("cancelPendingNotificationIntent", id);
        // temporary null the manager, cause that's what we have in reality
        managerClass.SetStatic<AndroidJavaObject>("mUnityNotificationManager", null);

        yield return new WaitForSeconds(0.2f);
        // simulate reboot by calling reschedule method, that is called after reboot
        rebootClass.CallStatic("rescheduleSavedNotifications", context);

        var newManager = managerClass.GetStatic<AndroidJavaObject>("mUnityNotificationManager");
        // new manager was supposed to be created, assign callback from original one to get notifications
        newManager.Set("mNotificationCallback", manager.Get<AndroidJavaObject>("mNotificationCallback"));

        yield return WaitForNotification(120.0f);

        Debug.LogWarning("SendNotification_CanReschedule completed");

        // restore manager (to not ruin other tests)
        managerClass.SetStatic("mUnityNotificationManager", manager);
        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.Android)]
    public IEnumerator SendNotificationNotShownInForeground_IsDeliveredButNotShown()
    {
        var n = new AndroidNotification();
        n.Title = "SendNotificationNotShownInForeground_ISDeliveredButNotShown";
        n.Text = "SendNotificationNotShownInForeground_ISDeliveredButNotShown Text";
        n.FireTime = System.DateTime.Now;
        n.ShowInForeground = false;

        Debug.LogWarning("SendNotificationNotShownInForeground_ISDeliveredButNotShown sends notification");

        int originalId = AndroidNotificationCenter.SendNotification(n, kDefaultTestChannel);
        yield return WaitForNotification(5.0f);

        Debug.LogWarning("SendNotificationNotShownInForeground_ISDeliveredButNotShown sends completed");

        Assert.AreEqual(1, currentHandler.receivedNotificationCount);
        yield return new WaitForSeconds(2.0f);  // give some time, since on some devices we don't immediately get infor on delivered notifications
        var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(originalId);
        Assert.AreEqual(NotificationStatus.Unknown, status);  // status should be unknown, rather than Delivered
    }
}

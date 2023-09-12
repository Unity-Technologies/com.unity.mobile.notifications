using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Notifications;

class UnifiedNotificationsSendingTests
{
    [OneTimeSetUp]
    public void BeforeTests()
    {
        var args = NotificationCenterArgs.Default;
        args.AndroidChannelId = "Unified";
        args.AndroidChannelName = "Unified";
        args.AndroidChannelDescription = "Unified notifications";
        NotificationCenter.Initialize(args);
        NotificationCenter.OnNotificationReceived += OnNotificationReceived;
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        NotificationCenter.CancelAllScheduledNotifications();
    }

    [SetUp]
    public void BeforeEachTest()
    {
        NotificationCenter.CancelAllScheduledNotifications();
        receivedNotificationCount = 0;
        lastNotification = null;
    }

    uint receivedNotificationCount = 0;
    Notification? lastNotification;
    void OnNotificationReceived(Notification notification)
    {
        ++receivedNotificationCount;
        lastNotification = notification;
    }

    IEnumerator WaitForNotification(float timeout)
    {
        var startCount = receivedNotificationCount;
        float timePassed = 0;
        while (receivedNotificationCount == startCount && timePassed < timeout)
        {
            yield return null;
            timePassed += Time.deltaTime;
        }
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer })]
    public IEnumerator SendNotificationAllOptionsRoundtrip()
    {
        var notification = new Notification()
        {
            Identifier = 15,
            Title = "Test",
            Text = "Testing",
            Data = "TestData",
            Badge = 2,
        };
        NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule(TimeSpan.FromSeconds(2)));

        yield return new WaitForSeconds(0.5f);  // test notification is trully delayed
        yield return WaitForNotification(60.0f);

        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsTrue(lastNotification.HasValue);
        var n = lastNotification.Value;
        Assert.AreEqual(15, n.Identifier);
        Assert.AreEqual("Test", n.Title);
        Assert.AreEqual("Testing", n.Text);
        Assert.AreEqual("TestData", n.Data);
        Assert.AreEqual(2, n.Badge);
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleNotificationAtSpecificTime()
    {
        var notification = new Notification()
        {
            Title = "AtTime",
            Text = "AtSpecificTime",
        };
        NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(2)));

        yield return new WaitForSeconds(0.5f);  // test notification is trully delayed
        yield return WaitForNotification(60.0f);

        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsTrue(lastNotification.HasValue);
        var n = lastNotification.Value;
        Assert.AreEqual("AtTime", n.Title);
        Assert.AreEqual("AtSpecificTime", n.Text);
        Assert.AreNotEqual(0, n.Identifier);  // ID should be auto-generated
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleAndCancelNotification_DoesNotArrive()
    {
        var notification = new Notification()
        {
            Title = "Cancel",
            Text = "To be cancelled",
        };
        int id = NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(3)));

        yield return new WaitForSeconds(0.5f);
        NotificationCenter.CancelScheduledNotification(id);
        yield return WaitForNotification(6.0f);

        Assert.AreEqual(0, receivedNotificationCount);
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android })]
    public IEnumerator ScheduleNotification_WithExplicitCategory_UsesAndroidChannel()
    {
#if UNITY_ANDROID
        const string category = "the_category";
        Unity.Notifications.Android.AndroidNotificationCenter.RegisterNotificationChannel(
            new Unity.Notifications.Android.AndroidNotificationChannel()
            {
                Id = category,
                Name = "Category",
                Description = "For category testing",
                Importance = Unity.Notifications.Android.Importance.Default,
            }
        );
        Unity.Notifications.Android.AndroidNotificationCenter.OnNotificationReceived += OnAndroidNotification;

        yield return ScheduleNotification_WithExplicitCategory(category);

        Unity.Notifications.Android.AndroidNotificationCenter.OnNotificationReceived -= OnAndroidNotification;

        Assert.IsNotNull(lastAndroidNotification);
        Assert.AreEqual(category, lastAndroidNotification.Channel);
#else
        yield break;
#endif
    }

#if UNITY_ANDROID
    Unity.Notifications.Android.AndroidNotificationIntentData lastAndroidNotification;
    void OnAndroidNotification(Unity.Notifications.Android.AndroidNotificationIntentData intentData)
    {
        lastAndroidNotification = intentData;
    }
#endif

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleNotification_WithExplicitCategory_UsesiOSCategory()
    {
#if UNITY_IOS
        const string category = "the_category";
        yield return ScheduleNotification_WithExplicitCategory(category);

        var notification = (Unity.Notifications.iOS.iOSNotification)lastNotification.Value;
        Assert.AreEqual(category, notification.CategoryIdentifier);
#else
        yield break;
#endif
    }

    public IEnumerator ScheduleNotification_WithExplicitCategory(string category)
    {
        var notification = new Notification()
        {
            Title = "With category",
            Text = "Sent to category",
        };
        NotificationCenter.ScheduleNotification(notification, category, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(3)));
        yield return WaitForNotification(60.0f);

        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsTrue(lastNotification.HasValue);
        var n = lastNotification.Value;
        Assert.AreEqual("With category", n.Title);
        Assert.AreEqual("Sent to category", n.Text);
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleNotification_WithoutCategory_UsesNoiOSCategory()
    {
        var notification = new Notification()
        {
            Title = "Without category",
            Text = "Sent to no category",
        };
        NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(3)));
        yield return WaitForNotification(60.0f);

        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsTrue(lastNotification.HasValue);
        var n = lastNotification.Value;
        Assert.AreEqual("Without category", n.Title);
        Assert.AreEqual("Sent to no category", n.Text);

#if UNITY_IOS
        var iosNotification = (Unity.Notifications.iOS.iOSNotification)n;
        Assert.IsNull(iosNotification.CategoryIdentifier);
#endif
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleWithSameID_ReplacesNotification()
    {
        var notification = new Notification()
        {
            Identifier = 123,
            Title = "Replace",
            Text = "To be replaced",
        };
        NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(2)));
        yield return null;

        notification.Text = "Replacement text";
        NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(3)));

        yield return WaitForNotification(60.0f);
        yield return new WaitForSeconds(5);  // wait a bit more in case the second notification does arrive

        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsTrue(lastNotification.HasValue);
        var n = lastNotification.Value;
        Assert.AreEqual(123, n.Identifier.Value);
        Assert.AreEqual("Replace", n.Title);
        Assert.AreEqual("Replacement text", n.Text);
    }

    [UnityTest]
    [UnityPlatform(new[] { RuntimePlatform.Android, RuntimePlatform.IPhonePlayer })]
    public IEnumerator ScheduleWithoutIDTwice_DeliversTwo()
    {
        var notification = new Notification()
        {
            Title = "No replace",
            Text = "Not replaced",
        };
        var id1 = NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(2)));
        yield return null;

        var id2 = NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(DateTime.Now.AddSeconds(3)));

        Assert.AreNotEqual(id1, id2);

        yield return WaitForNotification(60.0f);
        // Both could be batched
        if (receivedNotificationCount < 2)
            yield return WaitForNotification(60.0f);

        Assert.AreEqual(2, receivedNotificationCount);
    }
}

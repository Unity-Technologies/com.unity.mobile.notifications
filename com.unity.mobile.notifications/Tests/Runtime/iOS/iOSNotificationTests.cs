using System;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.Notifications.iOS;
#if UNITY_EDITOR
using Unity.Notifications;
using UnityEditor;
#endif

class iOSNotificationTests
    : IPrebuildSetup, IPostBuildCleanup
{
    private static int receivedNotificationCount = 0;
    private static iOSNotification lastReceivedNotification = null;
#if UNITY_EDITOR
    private static iOSSdkVersion originaliOSSDK;
    private static bool originalRequestAuthorizationOnAppLaunch;
    private static AuthorizationOption originalAuthorizationOptions;
    private static bool originalAddRemoteNotificationCapability;
    private static bool originalRequestRemoteOnLaunch;
#endif

    public void Setup()
    {
#if UNITY_EDITOR
        originaliOSSDK = PlayerSettings.iOS.sdkVersion;
        originalRequestAuthorizationOnAppLaunch = NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch;
        originalAuthorizationOptions = NotificationSettings.iOSSettings.DefaultAuthorizationOptions;
        originalAddRemoteNotificationCapability = NotificationSettings.iOSSettings.AddRemoteNotificationCapability;
        originalRequestRemoteOnLaunch = NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch;

        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = true;
        NotificationSettings.iOSSettings.DefaultAuthorizationOptions = originalAuthorizationOptions | AuthorizationOption.Provisional;
        NotificationSettings.iOSSettings.AddRemoteNotificationCapability = false;
        NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = false;
#endif
    }

    public void Cleanup()
    {
#if UNITY_EDITOR
        PlayerSettings.iOS.sdkVersion = originaliOSSDK;
        NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = originalRequestAuthorizationOnAppLaunch;
        NotificationSettings.iOSSettings.DefaultAuthorizationOptions = originalAuthorizationOptions;
        NotificationSettings.iOSSettings.AddRemoteNotificationCapability = originalAddRemoteNotificationCapability;
        NotificationSettings.iOSSettings.NotificationRequestAuthorizationForRemoteNotificationsOnAppLaunch = originalRequestRemoteOnLaunch;
#endif
    }

#if !UNITY_EDITOR
    [OneTimeSetUp]
    public void BeforeTests()
    {
        iOSNotificationCenter.OnNotificationReceived += receivedNotification =>
        {
            receivedNotificationCount += 1;
            lastReceivedNotification = receivedNotification;
            var msg = "Notification received : " + receivedNotification.Identifier + "\n";
            msg += "\n Notification received: ";
            msg += "\n .Title: " + receivedNotification.Title;
            msg += "\n .Badge: " + receivedNotification.Badge;
            msg += "\n .Body: " + receivedNotification.Body;
            msg += "\n .CategoryIdentifier: " + receivedNotification.CategoryIdentifier;
            msg += "\n .Subtitle: " + receivedNotification.Subtitle;
            Debug.Log(msg);
        };
    }

    [TearDown]
    public void AfterEachTest()
    {
        receivedNotificationCount = 0;
        lastReceivedNotification = null;
    }
#endif

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
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendSimpleNotification_NotificationIsReceived()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        // You can optionally specify a custom Identifier which can later be
        // used to cancel the notification, if you don't set one, an unique
        // string will be generated automatically.
        var notification = new iOSNotification()
        {
            Identifier = "_notification_01",
            Title = "SendSimpleNotification_NotificationIsReceived",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert |
                PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);

        yield return WaitForNotification(6.0f);
        Assert.AreEqual(1, receivedNotificationCount);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendNotificationWithUserInfo_NotificationIsReceivedWithSameUserInfo()
    {
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "_notification_02",
            Title = "SendNotificationWithUserInfo_NotificationIsReceivedWithSameUserInfo",
            Body = "Scheduled at: " + DateTime.Now.ToShortDateString() + " triggered in 5 seconds",
            Subtitle = "This is a subtitle, something, something important...",
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        notification.UserInfo.Add("key1", "value1");

        iOSNotificationCenter.ScheduleNotification(notification);

        yield return WaitForNotification(6.0f);
        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsNotNull(lastReceivedNotification);
        Assert.IsTrue(lastReceivedNotification.UserInfo.ContainsKey("key1"));
        Assert.AreEqual("value1", lastReceivedNotification.UserInfo["key1"]);
    }

    IEnumerator SendNotificationUsingCalendarTrigger_NotificationIsReceived(string text, bool useUtc)
    {
        var dateTime = useUtc ? DateTime.UtcNow : DateTime.Now;
        var dt = dateTime.AddSeconds(3);
        var trigger = new iOSNotificationCalendarTrigger()
        {
            Year = dt.Year,
            Month = dt.Month,
            Day = dt.Day,
            Hour = dt.Hour,
            Minute = dt.Minute,
            Second = dt.Second,
            UtcTime = useUtc,
        };

        var notification = new iOSNotification()
        {
            Title = text,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert,
            Trigger = trigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        yield return WaitForNotification(5.0f);
        Assert.AreEqual(1, receivedNotificationCount);
        Assert.IsNotNull(lastReceivedNotification);
        Assert.AreEqual(text, lastReceivedNotification.Title);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendNotificationUsingCalendarTriggerLocalTime_NotificationIsReceived()
    {
        yield return SendNotificationUsingCalendarTrigger_NotificationIsReceived("SendNotificationUsingCalendarTriggerLocalTime_NotificationIsReceived", false);
    }

    [UnityTest]
    [UnityPlatform(RuntimePlatform.IPhonePlayer)]
    public IEnumerator SendNotificationUsingCalendarTriggerUtcTime_NotificationIsReceived()
    {
        yield return SendNotificationUsingCalendarTrigger_NotificationIsReceived("SendNotificationUsingCalendarTriggerUtcTime_NotificationIsReceived", true);
    }

    [Test]
    public void iOSNotificationCalendarTrigger_ToUtc_DoesNotConvertUtcTrigger()
    {
        var trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = 5,
            Minute = 5,
            UtcTime = true,
        };

        var utcTrigger = trigger.ToUtc();

        Assert.AreEqual(5, utcTrigger.Hour);
        Assert.AreEqual(5, utcTrigger.Minute);
    }

    [Test]
    public void iOSNotificationCalendarTrigger_ToUtc_ConvertsLocalTrigger()
    {
        var localTime = DateTime.Now;
        var utcTime = localTime.ToUniversalTime();
        if (DateTime.Compare(localTime, utcTime) == 0)
            return; // running test in GMT time zode

        var trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = localTime.Hour,
            Minute = localTime.Minute,
            UtcTime = false,
        };

        var utcTrigger = trigger.ToUtc();

        Assert.AreEqual(utcTime.Hour, utcTrigger.Hour);
        Assert.AreEqual(utcTime.Minute, utcTrigger.Minute);
    }

    [Test]
    public void iOSNotificationCalendarTrigger_ToLocal_DoesNotConvertLocalTrigger()
    {
        var trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = 5,
            Minute = 5,
            UtcTime = false,
        };

        var localTrigger = trigger.ToLocal();

        Assert.AreEqual(5, localTrigger.Hour);
        Assert.AreEqual(5, localTrigger.Minute);
    }

    [Test]
    public void iOSNotificationCalendarTrigger_ToLocal_ConvertsUtcTrigger()
    {
        var localTime = DateTime.Now;
        var utcTime = localTime.ToUniversalTime();
        if (DateTime.Compare(localTime, utcTime) == 0)
            return; // running test in GMT time zode

        var trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = utcTime.Hour,
            Minute = utcTime.Minute,
            UtcTime = true,
        };

        var localTrigger = trigger.ToLocal();

        Assert.AreEqual(localTime.Hour, localTrigger.Hour);
        Assert.AreEqual(localTime.Minute, localTrigger.Minute);
    }

    [Test]
    public void iOSNotificationCalendarTrigger_AssignDateTimeComponents_OnlyChangesNonNullFields()
    {
        var dt = new DateTime(2025, 5, 5, 6, 6, 6);

        var trigger = new iOSNotificationCalendarTrigger()
        {
            Year = 2020,
            Month = 10,
            Day = 8,
        };

        var check = trigger.AssignDateTimeComponents(dt);
        Assert.AreEqual(2020, check.Year);
        Assert.AreEqual(10, check.Month);
        Assert.AreEqual(8, check.Day);
        Assert.AreEqual(6, check.Hour);
        Assert.AreEqual(6, check.Minute);
        Assert.AreEqual(6, check.Second);

        trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = 3,
            Minute = 4,
            Second = 20,
        };

        check = trigger.AssignDateTimeComponents(dt);
        Assert.AreEqual(2025, check.Year);
        Assert.AreEqual(5, check.Month);
        Assert.AreEqual(5, check.Day);
        Assert.AreEqual(3, check.Hour);
        Assert.AreEqual(4, check.Minute);
        Assert.AreEqual(20, check.Second);
    }

    [Test]
    public void OSNotificationCalendarTrigger_AssignNonEmptyComponents_Works()
    {
        var dt = new DateTime(2025, 1, 2, 3, 4, 5);

        var trigger = new iOSNotificationCalendarTrigger()
        {
            Year = 2020,
            Month = 10,
            Day = 10,
        };

        trigger.AssignNonEmptyComponents(dt);
        Assert.AreEqual(2025, trigger.Year);
        Assert.AreEqual(1, trigger.Month);
        Assert.AreEqual(2, trigger.Day);
        Assert.IsTrue(null == trigger.Hour);
        Assert.IsTrue(null == trigger.Minute);
        Assert.IsTrue(null == trigger.Second);

        trigger = new iOSNotificationCalendarTrigger()
        {
            Hour = 10,
            Minute = 10,
            Second = 10,
        };

        trigger.AssignNonEmptyComponents(dt);
        Assert.IsTrue(null == trigger.Year);
        Assert.IsTrue(null == trigger.Month);
        Assert.IsTrue(null == trigger.Day);
        Assert.AreEqual(3, trigger.Hour);
        Assert.AreEqual(4, trigger.Minute);
        Assert.AreEqual(5, trigger.Second);
    }
}
